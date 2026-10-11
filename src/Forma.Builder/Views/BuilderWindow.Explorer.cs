using System.Text.Json;
using FControl = Forma.Core.Controls.Control;

namespace Forma.Builder;

// Solution Explorer actions: project files, folders, renames and deletes.
public sealed partial class BuilderWindow
{
    private static string? PromptName(IWin32Window owner, string title, string initial)
    {
        using var dialog = new System.Windows.Forms.Form { Text = title, ClientSize = new Size(420, 116), FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false };
        var input = new TextBox { Text = initial, Left = 16, Top = 18, Width = 388 };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 232, Top = 66, Width = 80 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 324, Top = 66, Width = 80 };
        dialog.Controls.AddRange([input, ok, cancel]); dialog.AcceptButton = ok; dialog.CancelButton = cancel;
        dialog.Shown += (_, _) => { input.Focus(); input.SelectAll(); };
        return dialog.ShowDialog(owner) == DialogResult.OK ? input.Text : null;
    }

    private async Task OpenProjectEntryAsync(string id)
    {
        var entry = _viewModel.Files.FirstOrDefault(file => file.Id == id && !file.IsFolder) ?? throw new ArgumentException("Project file not found.");
        var key = "file:" + id;
        if (_editors.ContainsKey(key)) { ActivateEditor(key); await PublishEditorTabsAsync(true); return; }
        var folder = Path.Combine(ComponentEditorService.Folder(_viewModel, _viewModel.ProjectRoot), "project-files", id);
        var editor = new ComponentEditorView(entry.Name, folder, new ComponentCustomization { Behavior = entry.Content },
            async source => {
                _viewModel.EditProjectFiles(files => { ProjectEntryService.SetContent(files, id, source.Behavior); return true; });
                await StateAsync("Project file saved");
            }, () => new { controls = Walk(_viewModel.Form!).Select(control => new { id = control.Id, name = control.Name, kind = control.ControlType }).ToArray(), modules = ComponentEditorService.ModuleNames(_viewModel.GlobalScript), globalSource = _viewModel.GlobalScript, projectFiles = ProjectEntryService.ModuleSources(_viewModel.Files).Select(file => new { path = file.Path, content = file.Content }).ToArray(), filePath = ProjectEntryService.FilePath(_viewModel.Files, _viewModel.Files.FirstOrDefault(file => file.Id == entry.Id) ?? entry) }, projectFileName: entry.Name);
        _editors.Add(key, editor); ActivateEditor(key); editor.ApplyTheme(_workspaceTheme);
        editor.MinimumSize = Size.Empty; editor.Bounds = new Rectangle(280, 160, Math.Max(100, ClientSize.Width - 580), Math.Max(100, ClientSize.Height - 200));
        Controls.Add(editor); RegisterEditor(key, editor); editor.Show(); editor.BringToFront();
        await PublishEditorTabsAsync(true);
    }

    private async Task HandleExplorerActionAsync(JsonElement payload)
    {
        var action = String(payload, "action"); var key = String(payload, "key") ?? "";
        if (action == "toggle-lock") {
            // Lock or unlock a component from the explorer, switching to its form first.
            var controlId = String(payload, "controlId");
            var lockOwner = _viewModel.Forms.FirstOrDefault(form => Walk(form).Any(control => control.Id == controlId));
            if (lockOwner is null || controlId is null || !_viewModel.Appearance.TryGetValue(controlId, out var appearance)) return;
            if (_viewModel.Form != lockOwner) await SwitchFormAsync(lockOwner.Id);
            _viewModel.ExecuteEdit("select", controlId, default);
            _viewModel.ExecuteEdit("property", controlId, JsonSerializer.SerializeToElement(new { property = "locked", value = !appearance.Locked }));
            await StateAsync(appearance.Locked ? "Component locked" : "Component unlocked");
            return;
        }
        var item = key.StartsWith("file:", StringComparison.Ordinal) ? _viewModel.Files.FirstOrDefault(file => file.Id == key[5..]) : null;
        if (action is "open-project-file" or "rename-item" or "delete-item") {
            if (item is null) throw new ArgumentException("Project item not found.");
            if (action == "open-project-file") { await OpenProjectEntryAsync(item.Id); return; }
            if (action == "rename-item") {
                var name = PromptName(this, item.IsFolder ? "Rename folder" : "Rename file", item.Name); if (name is null) return;
                _viewModel.EditProjectFiles(files => { ProjectEntryService.Rename(files, item.Id, name); return true; });
                await StateAsync("Project item renamed"); return;
            }
            if (MessageBox.Show(this, $"Remove {item.Name} from the project? Folder contents and unsaved editor drafts will also be removed. This project edit can be undone.", "Remove project item", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            var removed = ProjectEntryService.Descendants(_viewModel.Files, item.Id).ToHashSet();
            _viewModel.EditProjectFiles(files => files.RemoveAll(file => removed.Contains(file.Id)));
            await StateAsync("Project item removed"); return;
        }
        if (action is "new-js" or "new-css" or "new-json" or "new-folder" or "import-file") {
        var parent = key == "files" || key == "project:" + _viewModel.ProjectRoot.Id ? null : item?.IsFolder == true ? item.Id : throw new ArgumentException("Choose a project folder.");
            var folder = action == "new-folder"; string name; string? content = null;
            if (action == "import-file") {
                using var picker = new OpenFileDialog { Title = "Add existing project file", Filter = "Project source files|*.js;*.css;*.json", CheckFileExists = true };
                if (picker.ShowDialog(this) != DialogResult.OK) return;
                if (new FileInfo(picker.FileName).Length > 800_000) throw new ArgumentException("File is too large.");
                name = Path.GetFileName(picker.FileName); content = File.ReadAllText(picker.FileName);
                if (content.Length > 200_000) throw new ArgumentException("File is limited to 200,000 characters.");
            } else {
                name = PromptName(this, folder ? "New folder" : "New project file", folder ? "NewFolder" : action == "new-css" ? "styles.css" : action == "new-json" ? "settings.json" : "helpers.js") ?? "";
                if (name == "") return;
            }
            var entry = _viewModel.EditProjectFiles(files => {
                var created = ProjectEntryService.Add(files, parent, name, folder);
                if (content is not null) ProjectEntryService.SetContent(files, created.Id, content);
                return created;
            });
            await StateAsync("Project item added");
            await _bridge!.SendAsync(new { type = "designer", action = "explorer-reveal", key = "file:" + entry.Id });
            if (!folder) await OpenProjectEntryAsync(entry.Id);
            return;
        }
        if (action == "reveal-project") {
            if (_viewModel.ProjectPath is null) throw new InvalidOperationException("Save the project first.");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path.GetDirectoryName(Path.GetFullPath(_viewModel.ProjectPath))!) { UseShellExecute = true }); return;
        }
        if (action == "add-image") {
            if (_viewModel.Appearance[_viewModel.Form!.Id].Locked) throw new InvalidOperationException("Unlock the form first.");
            using var picker = new OpenFileDialog { Title = "Add image to current form", Filter = "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp;*.svg;*.ico", CheckFileExists = true };
            if (picker.ShowDialog(this) != DialogResult.OK) return;
            var added = _viewModel.ExecuteEdit("drop", _viewModel.Form.Id, JsonSerializer.SerializeToElement(new { control = "image", x = 32, y = 32 })).AddedControl;
            if (added is null) throw new InvalidOperationException("Could not add an image to this form.");
            SubscribeControl(added);
            _viewModel.ExecuteEdit("image-source", added.Id, JsonSerializer.SerializeToElement(new { value = new Uri(picker.FileName).AbsoluteUri }));
            await StateAsync("Image added to form"); return;
        }
        var id = String(payload, "controlId");
        var owner = _viewModel.Forms.FirstOrDefault(form => Walk(form).Any(control => control.Id == id));
        if (owner is null) throw new ArgumentException("Component not found.");
        await SwitchFormAsync(owner.Id);
        _viewModel.ExecuteEdit("select", id, default);
        if (action == "choose-image") {
            if (_viewModel.SelectedControl is not Forma.Core.Controls.Image || _viewModel.Appearance[id!].Locked) throw new InvalidOperationException("Select an unlocked image component.");
            using var picker = new OpenFileDialog { Title = "Choose image", Filter = "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp;*.svg;*.ico", CheckFileExists = true };
            if (picker.ShowDialog(this) != DialogResult.OK) return;
            _viewModel.ExecuteEdit("image-source", id, JsonSerializer.SerializeToElement(new { value = new Uri(picker.FileName).AbsoluteUri }));
        } else if (action == "rename-control") {
            var name = PromptName(this, "Rename component", _viewModel.SelectedControl!.Name ?? ""); if (name is null) return;
            _viewModel.ExecuteEdit("property", id, JsonSerializer.SerializeToElement(new { property = "name", value = name }));
        } else if (action is "delete-control" or "delete-form") {
            if (MessageBox.Show(this, "Remove this item and its contents from the project? Unsaved source drafts for removed items will be discarded. This edit can be undone.", "Remove project item", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            if (action == "delete-form") {
                var previous = _viewModel.Form!; _viewModel.RemoveForm(previous.Id);
                await _renderer!.RemoveAsync(previous); await RenderActiveFormAsync();
            } else _viewModel.ExecuteEdit("command", id, JsonSerializer.SerializeToElement(new { command = "delete" }));
        } else throw new ArgumentException("Unknown Explorer action.");
        await StateAsync("Project updated");
    }
}
