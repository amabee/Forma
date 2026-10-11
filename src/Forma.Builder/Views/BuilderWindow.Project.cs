using System.Text.Json;
using FControl = Forma.Core.Controls.Control;

namespace Forma.Builder;

// Project lifecycle: new, open, save, form switching and history restores.
public sealed partial class BuilderWindow
{
    private async Task NewProjectAsync()
    {
        CloseDockedEditor();
        if (_renderer is null || _bridge is null)
            return;
        _ready = false;
        if (_viewModel.Form is not null)
            await _renderer.RemoveAsync(_viewModel.Form);
        DisposeComponents();
        _viewModel.CreateNew();
        await _renderer.RenderAsync(_viewModel.Form);
        await _bridge.SendAsync(
            new
            {
                type = "designer",
                action = "initialize",
                id = _viewModel.Form.Id,
                title = _viewModel.Form.Title,
            }
        );
        await StateAsync("Ready — drag a control onto the form");
        _ready = true;
    }

    private async Task AddFormAsync()
    {
        if (_renderer is null || _bridge is null || _viewModel.Form is null) return;
        _ready = false;
        try {
            var previous = _viewModel.Form;
            _viewModel.AddForm();
            await _renderer.RemoveAsync(previous);
            await RenderActiveFormAsync();
        } finally { _ready = true; }
        await StateAsync("Form added to the current project");
    }

    private async Task SwitchFormAsync(string id)
    {
        if (_renderer is null || _bridge is null || _viewModel.Form?.Id == id || !_viewModel.Forms.Any(form => form.Id == id)) return;
        _ready = false;
        try {
            var previous = _viewModel.Form;
            if (!_viewModel.SelectForm(id)) return;
            if (previous is not null) await _renderer.RemoveAsync(previous);
            await RenderActiveFormAsync();
        } finally { _ready = true; }
        await StateAsync("Form selected");
    }

    private async Task RenderActiveFormAsync()
    {
        foreach (var editor in _editors.Values) editor.SetEditorVisible(false);
        _codeVisible = false;
        await _renderer!.RenderAsync(_viewModel.Form!);
        foreach (var control in Walk(_viewModel.Form!)) SubscribeControl(control);
        await _bridge!.SendAsync(new { type = "designer", action = "initialize", id = _viewModel.Form!.Id, title = _viewModel.Form.Title });
        await _bridge.SendAsync(new { type = "designer", action = "form-selected" });
    }

    private async Task RestoreHistoryAsync(DesignSnapshot snapshot, string status)
    {
        var restored = ProjectFile.RestoreProject(ProjectFile.Parse(snapshot.Json));
        var appearances = restored.Appearance.ToDictionary(p => p.Key, p => p.Value.Deserialize<Appearance>()!);
        _ready = false;
        try {
            if (_viewModel.Form is not null) await _renderer!.RemoveAsync(_viewModel.Form);
            DisposeComponents();
            _viewModel.ApplyProject(restored.Forms, appearances, snapshot.ActiveFormId, snapshot.SelectedId, restored.Files);
            await RenderActiveFormAsync();
        } finally { _ready = true; }
        await StateAsync(status);
    }

    private ProjectDocument CaptureProject(bool embedImages = false) => _viewModel.CaptureProject(embedImages);

    private void UpdateProjectTitle() => _viewModel.RefreshDocumentState();

    private bool ConfirmDiscard()
    {
        if (_editors.Values.Any(editor => editor.HasUnsavedChanges) && MessageBox.Show(this, "Discard unsaved component code before continuing? Use Save in the code panel to apply it first.", "Unsaved code", MessageBoxButtons.YesNo) != DialogResult.Yes) return false;
        if (!HasUnsavedChanges) return true;
        return MessageBox.Show(this, "Save changes to this design before continuing?", "Forma Builder", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) switch {
            DialogResult.Yes => SaveProject(), DialogResult.No => true, _ => false
        };
    }

    private bool SaveProject(bool saveAs = false)
    {
        if (_viewModel.Form is null) return false;
        var path = _viewModel.ProjectPath;
        if (path is null || saveAs) {
            using var dialog = new SaveFileDialog { Title = "Save Forma design", Filter = "Forma design (*.forma)|*.forma|Forma design (*.frma)|*.frma", DefaultExt = "forma", AddExtension = true, OverwritePrompt = true, FileName = path is null ? "Untitled.forma" : Path.GetFileName(path), InitialDirectory = path is null ? "" : Path.GetDirectoryName(path) };
            if (dialog.ShowDialog(this) != DialogResult.OK) return false;
            path = dialog.FileName;
        }
        try {
            var skippedImages = new List<string>();
            var document = _viewModel.CaptureProject(embedImages: true, skippedImages);
            ProjectFile.Write(path, document);
            // Keep the opened image available even if its original file is moved later.
            void UseEmbeddedImages(ProjectNode node) {
                if (_viewModel.ProjectControls.FirstOrDefault(c => c.Id == node.Id) is Forma.Core.Controls.Image image && node.Properties.TryGetValue("Source", out var source)) image.Source = source.GetString() ?? "";
                foreach (var child in node.Children) UseEmbeddedImages(child);
            }
            foreach (var root in document.Version == 1 ? new[] { document.Root } : document.Forms.ToArray()) UseEmbeddedImages(root);
            _viewModel.ProjectPath = path; _viewModel.MarkSaved(); UpdateProjectTitle();
            _recovery?.Clear();
            _ = StateAsync(skippedImages.Count == 0 ? $"Saved {Path.GetFileName(path)}"
                : $"Saved {Path.GetFileName(path)}; {skippedImages.Count} image(s) are missing or over 10 MB and stay linked, not embedded");
            return true;
        } catch (Exception error) {
            MessageBox.Show(this, error.Message, "Could not save design", MessageBoxButtons.OK, MessageBoxIcon.Error); return false;
        }
    }

    private async Task OpenProjectAsync()
    {
        using var dialog = new OpenFileDialog { Title = "Open Forma design", Filter = "Forma designs (*.forma;*.frma)|*.forma;*.frma|All files|*.*", CheckFileExists = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try {
            if (await LoadProjectAsync(dialog.FileName, dialog.FileName, saved: true))
                await StateAsync($"Opened {Path.GetFileName(_viewModel.ProjectPath)}");
        } catch (Exception error) {
            _ready = true;
            MessageBox.Show(this, error.Message, "Could not open design", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Replaces the open design with the one in <paramref name="file"/>. A recovered autosave
    /// keeps its original <paramref name="projectPath"/> and stays unsaved.
    /// </summary>
    private async Task<bool> LoadProjectAsync(string file, string? projectPath, bool saved)
    {
        // Fully decode before replacing the active design; bad files leave it intact.
        var restored = ProjectFile.RestoreProject(ProjectFile.Read(file));
        var appearances = restored.Appearance.ToDictionary(p => p.Key, p => p.Value.Deserialize<Appearance>() ?? throw new InvalidDataException("Invalid appearance."));
        if (!ConfirmDiscard()) return false;
        CloseDockedEditor();
        _ready = false;
        if (_viewModel.Form is not null) await _renderer!.RemoveAsync(_viewModel.Form);
        DisposeComponents();
        _viewModel.ApplyProject(restored.Forms, appearances, files: restored.Files);
        _viewModel.ProjectPath = projectPath;
        _viewModel.ClearHistory();
        await RenderActiveFormAsync();
        if (saved) _viewModel.MarkSaved();
        _ready = true;
        return true;
    }

    private void DisposeComponents()
    {
        foreach (var disposable in _viewModel.ProjectControls.OfType<IDisposable>()) disposable.Dispose();
        _subscribedControls.Clear();
    }

    private async Task ChoosePathAsync(Forma.Core.Controls.PathPicker picker, bool design)
    {
        if (!_ready || IsDisposed || _viewModel.Form is null || !Walk(_viewModel.Form).Contains(picker)
            || !_viewModel.Appearance.TryGetValue(picker.Id, out var appearance)
            || (design ? _viewModel.PreviewMode || appearance.Locked : !_viewModel.PreviewMode || !appearance.Enabled)) return;
        try
        {
            string? selected = null;
            if (picker is Forma.Core.Controls.FilePicker file)
            {
                using var dialog = new OpenFileDialog { Title = picker.DialogTitle, Filter = file.Filter, CheckFileExists = true };
                if (File.Exists(picker.SelectedPath)) dialog.FileName = picker.SelectedPath;
                if (dialog.ShowDialog(this) == DialogResult.OK) selected = dialog.FileName;
            }
            else
            {
                using var dialog = new FolderBrowserDialog { Description = picker.DialogTitle, UseDescriptionForTitle = true };
                if (Directory.Exists(picker.SelectedPath)) dialog.SelectedPath = picker.SelectedPath;
                if (dialog.ShowDialog(this) == DialogResult.OK) selected = dialog.SelectedPath;
            }
            if (selected is null) return;
            if (design) _viewModel.ExecuteEdit("path-source", picker.Id,
                JsonSerializer.SerializeToElement(new { property = "selectedPath", value = selected }));
            else picker.SelectedPath = selected;
            await StateAsync("Path selected");
        }
        catch (Exception error)
        {
            if (!IsDisposed) await StateAsync($"Could not choose path: {error.Message}");
        }
    }
}
