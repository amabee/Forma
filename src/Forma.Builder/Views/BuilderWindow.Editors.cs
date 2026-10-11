using System.Text.Json;
using FControl = Forma.Core.Controls.Control;

namespace Forma.Builder;

// Docked component code editors and their tabs.
public sealed partial class BuilderWindow
{
    private void RegisterEditor(string targetKey, ComponentEditorView editor)
    {
        editor.MetadataChanged += async (_, _) => await PublishEditorTabsAsync();
        editor.Closed += async (_, _) => {
            _editors.Remove(targetKey); _orphanedEditors.Remove(targetKey);
            var wasActive = ReferenceEquals(_componentEditor, editor);
            if (wasActive) {
                _componentEditor = null; _editorTargetId = null;
                if (!_closingEditors && _editors.Count > 0) {
                    var nextKey = _editors.Keys.Last();
                    var visible = _codeVisible;
                    var owner = _viewModel.Forms.FirstOrDefault(form => Walk(form).Any(control => control.Id == nextKey));
                    if (visible && owner is not null && _viewModel.Form != owner) await SwitchFormAsync(owner.Id);
                    ActivateEditor(nextKey, visible);
                }
            }
            if (!_closingEditors) await PublishEditorTabsAsync(wasActive && _codeVisible && _editors.Count > 0);
        };
    }

    private void ActivateEditor(string key, bool visible = true)
    {
        foreach (var editor in _editors.Values) editor.SetEditorVisible(false);
        _editorTargetId = key; _componentEditor = _editors[key]; _codeVisible = visible;
        _componentEditor.SetEditorVisible(visible);
        if (visible) _componentEditor.BringToFront();
    }

    private Task PublishEditorTabsAsync(bool show = false)
    {
        if (!_ready || _bridge is null || IsDisposed) return Task.CompletedTask;
        return _bridge.SendAsync(new {
            type = "designer", action = "editor-tabs", activeKey = _editorTargetId, show,
            editors = _editors.Select(pair => new { key = pair.Key, name = pair.Value.DocumentName, dirty = pair.Value.HasUnsavedChanges }).ToArray()
        });
    }

    private void CloseDockedEditor()
    {
        _closingEditors = true;
        try { foreach (var editor in _editors.Values.ToArray()) editor.Close(); }
        finally { _closingEditors = false; }
        _editors.Clear(); _componentEditor = null; _editorTargetId = null; _codeVisible = false;
        if (_ready && _bridge is not null) _ = PublishEditorTabsAsync();
    }

    /// <summary>Opens (or focuses) the code editor for a component, the global script or one event handler.</summary>
    private async Task OpenComponentEditorAsync(string sourceCommand, string? targetId, JsonElement payload)
    {
        var globalEditor = sourceCommand == "edit-global-script";
        var eventName = sourceCommand == "edit-event" ? String(payload, "eventName") : null;
        if (sourceCommand == "edit-event" && (string.IsNullOrWhiteSpace(eventName) || eventName.Length > 64 || eventName.Any(c => !char.IsAsciiLetter(c) && c != '-'))) return;
        var document = globalEditor || sourceCommand is "view-script" or "edit-event" ? "behavior" : sourceCommand == "view-events" ? "events" : sourceCommand == "view-custom-properties" ? "characteristics" : "css";
        var editorControl = globalEditor ? _viewModel.ProjectRoot : sourceCommand == "edit-event" || sourceCommand?.StartsWith("view-", StringComparison.Ordinal) == true
            ? _viewModel.ProjectControls.FirstOrDefault(c => c.Id == targetId) : _viewModel.SelectedControl;
        if (editorControl is FControl selected && !_viewModel.PreviewMode && (globalEditor || !_viewModel.Appearance[selected.Id].Locked))
        {
            if (!globalEditor) {
                var owner = _viewModel.Forms.First(form => Walk(form).Any(control => control.Id == selected.Id));
                await SwitchFormAsync(owner.Id);
                _viewModel.ExecuteEdit("select", selected.Id, default);
                await StateAsync("Component source opened");
            }
            var targetKey = globalEditor ? "global:" + selected.Id : selected.Id;
            if (_editors.TryGetValue(targetKey, out var existing)) {
                ActivateEditor(targetKey);
                existing.ActivateDocument(document);
                if (eventName is not null) existing.OpenEventHandler(eventName);
                await PublishEditorTabsAsync(true);
                return;
            }
            var editor = new ComponentEditorView(
                globalEditor ? "main.js" : selected.Name ?? selected.ControlType,
                globalEditor ? Path.Combine(ComponentEditorService.Folder(_viewModel, selected), "global") : ComponentEditorService.Folder(_viewModel, selected),
                globalEditor ? new ComponentCustomization { Behavior = _viewModel.GlobalScript } : ComponentEditorService.Template(selected, _viewModel.Appearance[selected.Id]),
                async source =>
                {
                    if (!globalEditor) {
                        var owner = _viewModel.Forms.FirstOrDefault(form => Walk(form).Any(control => control.Id == selected.Id));
                        if (owner is null) throw new InvalidOperationException("This component no longer belongs to the project.");
                        if (_viewModel.Form != owner) await SwitchFormAsync(owner.Id);
                    }
                    DesignerEditResult result;
                    try {
                        result = globalEditor ? _viewModel.ExecuteEdit("global-script", selected.Id, JsonSerializer.SerializeToElement(new { script = source.Behavior }))
                            : _viewModel.ExecuteEdit("customize", selected.Id, JsonSerializer.SerializeToElement(source));
                    } catch (Exception error) {
                        await RollBackFailedEditAsync($"Could not apply component code: {error.Message}");
                        throw;
                    }
                    await StateAsync(result.Status);
                }, () => new { controls = Walk(globalEditor ? _viewModel.Form! : _viewModel.Forms.FirstOrDefault(form => Walk(form).Any(control => control.Id == selected.Id)) ?? _viewModel.Form!).Select(c => new { id = c.Id, name = c.Name, kind = c.ControlType, properties = InspectorCatalog.ForKind(c.ControlType).Select(p => new { key = p.Id, type = p.Editor, options = p.Options }).ToArray() }).ToArray(), modules = ComponentEditorService.ModuleNames(_viewModel.GlobalScript), globalSource = _viewModel.GlobalScript, projectFiles = ProjectEntryService.ModuleSources(_viewModel.Files).Select(file => new { path = file.Path, content = file.Content }).ToArray() }, globalScript: globalEditor, componentKind: selected.ControlType);
            _editors.Add(targetKey, editor);
            ActivateEditor(targetKey);
            editor.ActivateDocument(document);
            if (eventName is not null) editor.OpenEventHandler(eventName);
            editor.ApplyTheme(_workspaceTheme);
            editor.MinimumSize = Size.Empty;
            editor.Bounds = new Rectangle(300, Math.Max(120, ClientSize.Height / 2), Math.Max(100, ClientSize.Width - 590), Math.Max(100, ClientSize.Height / 2 - 32));
            Controls.Add(editor);
            RegisterEditor(targetKey, editor);
            editor.Show(); editor.BringToFront();
            await PublishEditorTabsAsync(true);
        }
    }
}
