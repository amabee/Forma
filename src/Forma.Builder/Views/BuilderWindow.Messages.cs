using System.Text.Json;
using Forma.Core.Rendering;
using FControl = Forma.Core.Controls.Control;

namespace Forma.Builder;

// Routing for messages the designer page sends to the native window.
public sealed partial class BuilderWindow
{
    private void OnDesignerMessage(object? sender, BridgeMessage message)
    {
        if (IsDisposed || Disposing) return;
        // WebView2 forbids nested modal message loops in its callbacks. Post the
        // command to WinForms so dialogs open after the WebView2 callback returns.
        BeginInvoke(new Action(async () => {
            // async void: an escaping exception would crash the WinForms message loop.
            try { if (!IsDisposed && !Disposing) await HandleDesignerMessageAsync(message); }
            catch (Exception error) { if (!IsDisposed) await StateAsync($"Designer error: {error.Message}"); }
        }));
    }

    private async Task HandleDesignerMessageAsync(BridgeMessage message)
    {
        if (message.Type == "designer" && message.Event == "theme" && message.Payload is JsonElement themePayload
            && themePayload.ValueKind == JsonValueKind.Object && themePayload.TryGetProperty("theme", out var themeValue)) {
            var theme = themeValue.ValueKind == JsonValueKind.String ? themeValue.GetString() : null;
            if (theme is "light" or "dark") { _workspaceTheme = theme; foreach (var editor in _editors.Values) editor.ApplyTheme(theme); }
            return;
        }
        if (message.Type == "custom" && message.Event == "error" && _ready
            && message.Payload is JsonElement errorPayload && String(errorPayload, "message") is string errorMessage)
        {
            await StateAsync($"Component style error: {errorMessage}");
            return;
        }
        if (message.Type != "designer" || !_ready || _viewModel.Form is null || _bridge is null)
            return;
        try
        {
            var payload = message.Payload is JsonElement json && json.ValueKind == JsonValueKind.Object
                ? json : default;
            if (message.Event == "editor-bounds") {
                if (_componentEditor is not null && payload.ValueKind == JsonValueKind.Object) {
                    var scale = _preview.DeviceDpi / 96d * _preview.ZoomFactor;
                    int Pixel(string key) => (int)Math.Round(payload.GetProperty(key).GetDouble() * scale);
                    var x = Math.Clamp(Pixel("x"), 0, ClientSize.Width);
                    var y = Math.Clamp(Pixel("y"), 0, ClientSize.Height);
                    _componentEditor.Bounds = new Rectangle(x, y, Math.Clamp(Pixel("width"), 1, Math.Max(1, ClientSize.Width - x)), Math.Clamp(Pixel("height"), 1, Math.Max(1, ClientSize.Height - y)));
                    _componentEditor.BringToFront();
                    await _componentEditor.StartEditorAsync();
                }
                return;
            }
            if (message.Event == "editor-activate") {
                var key = String(payload, "key");
                if (key is not null && _editors.ContainsKey(key)) {
                    var owner = _viewModel.Forms.FirstOrDefault(form => key == "global:" + form.Id || Walk(form).Any(control => control.Id == key));
                    if (owner is not null && !key.StartsWith("global:", StringComparison.Ordinal)) await SwitchFormAsync(owner.Id);
                    ActivateEditor(key); await PublishEditorTabsAsync(true);
                }
                return;
            }
            if (message.Event == "editor-close-tab") {
                var key = String(payload, "key");
                if (key is not null && _editors.TryGetValue(key, out var editor)) editor.RequestClose();
                return;
            }
            if (message.Event == "editor-visibility") {
                _codeVisible = payload.GetProperty("visible").GetBoolean();
                if (_componentEditor is not null) _componentEditor.SetEditorVisible(_codeVisible);
                return;
            }
            if (message.Event == "explorer-action") { await HandleExplorerActionAsync(payload); return; }
            if (message.Event == "explorer-select") {
                var owner = _viewModel.Forms.FirstOrDefault(form => Walk(form).Any(control => control.Id == message.Id));
                if (owner is not null) {
                    await SwitchFormAsync(owner.Id);
                    _viewModel.ExecuteEdit("select", message.Id, default);
                    await _bridge.SendAsync(new { type = "designer", action = "form-selected" });
                    await StateAsync("Component selected from Solution Explorer");
                }
                return;
            }
            if (message.Event == "select-form") { var formId = String(payload, "id"); if (formId is not null) await SwitchFormAsync(formId); return; }
            if (message.Event == "preview")
            {
                await _bridge.SendAsync(new { type = "designer", action = "return-to-design" });
                if (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("enabled", out var enabled) && enabled.ValueKind == JsonValueKind.True)
                {
                    _runtimePreview?.Close();
                    _runtimePreview = new PreviewWindow(new PreviewSession(_viewModel));
                    _runtimePreview.Show(this);
                }
                await StateAsync("Preview opened in its own window");
                return;
            }
            if (message.Event == "command")
            {
                switch (String(payload, "command"))
                {
                    case "undo":
                    case "redo":
                        var undo = String(payload, "command") == "undo";
                        var snapshot = undo ? _viewModel.Undo() : _viewModel.Redo();
                        if (snapshot is not null) await RestoreHistoryAsync(snapshot, undo ? "Undone" : "Redone");
                        return;
                    case "save": SaveProject(); return;
                    case "save-as": SaveProject(true); return;
                    case "open": await OpenProjectAsync(); return;
                    case "new": await AddFormAsync(); return;
                    case "new-project": if (ConfirmDiscard()) await NewProjectAsync(); return;
                    case "about": ShowAbout(); return;
                    case "edit-characteristics":
                    case "edit-custom-properties":
                    case "edit-global-script":
                    case "view-css":
                    case "view-script":
                    case "view-events":
                    case "edit-event":
                    case "view-custom-properties":
                        await OpenComponentEditorAsync(String(payload, "command")!, message.Id, payload);
                        return;
                    case "exit": Close(); return;
                    case "show-dialog":
                        if (_viewModel.ShowDialog(_viewModel.SelectedControl?.Id)) await StateAsync("Dialog opened");
                        return;
                    case "choose-path":
                        if (_viewModel.SelectedControl is Forma.Core.Controls.PathPicker pathPicker)
                            await ChoosePathAsync(pathPicker, design: true);
                        return;
                    case "choose-image":
                        if (_viewModel.SelectedControl is Forma.Core.Controls.Image image
                            && !_viewModel.PreviewMode && !_viewModel.Appearance[image.Id].Locked)
                        {
                            using var picker = new OpenFileDialog
                            {
                                Title = "Choose image",
                                Filter = "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp;*.svg;*.ico|All files|*.*",
                                CheckFileExists = true
                            };
                            if (picker.ShowDialog(this) == DialogResult.OK)
                            {
                                var result = _viewModel.ExecuteEdit("image-source", image.Id,
                                    JsonSerializer.SerializeToElement(new { value = new Uri(picker.FileName).AbsoluteUri }));
                                await StateAsync(result.Status);
                            }
                        }
                        return;
                }
            }
            var edit = _viewModel.ExecuteEdit(message.Event ?? "", message.Id, payload);
            foreach (var added in edit.Added.SelectMany(Walk)) SubscribeControl(added);
            await StateAsync(edit.Status);
        }
        catch (Exception error)
        {
            if (IsDisposed) return;
            if (!await RollBackFailedEditAsync($"Designer error: {error.Message} (change reverted)"))
                await StateAsync($"Designer error: {error.Message}");
        }
    }

    /// <summary>Restores the design when a failed edit left partial changes behind.</summary>
    private async Task<bool> RollBackFailedEditAsync(string status)
    {
        if (_viewModel.TakeFailedEdit() is not { } before) return false;
        await RestoreHistoryAsync(before, status);
        return true;
    }
}
