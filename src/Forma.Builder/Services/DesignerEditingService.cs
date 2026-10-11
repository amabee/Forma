using System.Text.Json;
using FControl = Forma.Core.Controls.Control;

namespace Forma.Builder;

/// <summary>Editing rules for the designer, independent of native and browser views.</summary>
public sealed partial class DesignerEditingService(BuilderViewModel viewModel)
{
    private readonly BuilderViewModel _viewModel = viewModel;
    private readonly DesignerLayout _layout = new(viewModel);

    public void ReflowLayouts() => _layout.ReflowLayouts();

    public DesignerEditResult Execute(string action, string? id, JsonElement payload)
    {
        var form = _viewModel.Form;
        if (form is null) return new("Ready");
        var control = Walk(form).FirstOrDefault(item => item.Id == id);
        if (action == "select" && control is not null)
        {
            _viewModel.SelectedControl = control;
            // Ctrl/Shift-click and marquee selection send every selected ID; the clicked one is primary.
            var ids = IdList(payload) ?? [];
            _viewModel.SetSelection(Walk(form).Where(item => item != form && ids.Contains(item.Id)).Append(control));
            return new("Ready");
        }
        if (action == "preview")
        {
            _viewModel.PreviewMode = Boolean(payload, "enabled", false);
            foreach (var timer in Walk(form).OfType<Forma.Core.Controls.Timer>())
                timer.Enabled = _viewModel.PreviewMode && _viewModel.Appearance[timer.Id].Enabled;
            if (!_viewModel.PreviewMode) foreach (var dialog in Walk(form).OfType<Forma.Core.Controls.Dialog>()) dialog.Close("Cancel");
            return new("Ready");
        }
        // Editing guards belong here as well as in the browser UI.
        if (_viewModel.PreviewMode) return new("Ready");
        if (action == "global-script" && payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("script", out var global))
        {
            if (global.ValueKind != JsonValueKind.String || global.GetString()!.Length > 200_000) throw new ArgumentException("Global script must be under 200,000 characters.");
            _viewModel.GlobalScript = global.GetString()!;
            return new("Global script saved");
        }
        if (action == "customize" && control is not null && !_viewModel.Appearance[control.Id].Locked)
        {
            var customization = payload.Deserialize<ComponentCustomization>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new ArgumentException("Missing custom properties.");
            _viewModel.Appearance[control.Id].Customization = ComponentCustomization.Validate(customization);
            return new("Custom properties applied");
        }
        if (action == "drop" && control is not null
            && (control == form || control is Forma.Core.Controls.LayoutContainer or Forma.Core.Controls.Panel)
            && Number(payload, "x") is int x && Number(payload, "y") is int y)
        {
            var added = AddControl(String(payload, "control"), x, y, control, Number(payload, "layoutSlot"), Number(payload, "index"));
            return new(added is null ? "Ready" : $"Added {added.Name}", added);
        }
        if (action == "move-many") return new(MoveControls(payload));
        if (action == "command")
        {
            switch (String(payload, "command"))
            {
                case "delete":
                    return new(DeleteControls(Targets(payload)));
                case "copy":
                case "cut":
                    var copied = Targets(payload);
                    if (copied.Count == 0) return new("Nothing to copy");
                    _viewModel.Clipboard = CopyNodes(copied);
                    if (String(payload, "command") == "copy") return new($"Copied {Describe(copied.Count)}");
                    DeleteControls(copied);
                    return new($"Cut {Describe(copied.Count)}");
                case "paste":
                    var pasted = PasteNodes(_viewModel.Clipboard);
                    return new(pasted.Count == 0 ? "Nothing to paste" : $"Pasted {Describe(pasted.Count)}", pasted.FirstOrDefault(), pasted);
                case "duplicate":
                    var duplicates = PasteNodes(CopyNodes(Targets(payload)));
                    return new(duplicates.Count == 0 ? "Ready" : $"Duplicated {Describe(duplicates.Count)}", duplicates.FirstOrDefault(), duplicates);
            }
            if (String(payload, "command") == "add-tab" && control is Forma.Core.Controls.TabControl tabControl
                && !_viewModel.Appearance[control.Id].Locked)
            {
                var label = tabControl is Forma.Core.Controls.Accordion ? "Section" : "Tab";
                tabControl.Tabs = [..tabControl.Tabs, $"{label} {tabControl.Tabs.Length + 1}"];
                tabControl.SelectedTab = tabControl.Tabs.Length - 1;
                _viewModel.SelectedControl = tabControl;
                return new($"{label} added");
            }
            var selected = _viewModel.SelectedControl;
            if (selected is null || selected == form || !_viewModel.Appearance.TryGetValue(selected.Id, out var appearance)
                || appearance.Locked || !Walk(form).Contains(selected)) return new("Ready");
            switch (String(payload, "command"))
            {
                case "bring-front":
                case "send-back":
                    var siblings = selected.Parent!.Children.OrderBy(item => _viewModel.Appearance[item.Id].ZIndex).ToList();
                    siblings.Remove(selected);
                    if (String(payload, "command") == "bring-front") siblings.Add(selected);
                    else siblings.Insert(0, selected);
                    for (var index = 0; index < siblings.Count; index++) _viewModel.Appearance[siblings[index].Id].ZIndex = index;
                    return new("Layer order updated");
            }
        }
        if (control is null || !_viewModel.Appearance.TryGetValue(control.Id, out var bounds)) return new("Ready");
        switch (action)
        {
            case "move" when control != form && !bounds.Locked
                && Number(payload, "x") is int left && Number(payload, "y") is int top:
                if (control is Forma.Core.Controls.INonvisualControl) return new("Ready");
                var parentId = String(payload, "parentId");
                if (parentId is not null && parentId != control.Parent?.Id)
                {
                    var target = Walk(form).FirstOrDefault(item => item.Id == parentId);
                    if (target is null || !(target == form || target is Forma.Core.Controls.Panel or Forma.Core.Controls.LayoutContainer)
                        || _viewModel.Appearance[target.Id].Locked || Walk(control).Contains(target))
                        return new("Ready");
                    // Reparent the same model instance; descendants and identities stay intact.
                    control.Parent?.Remove(control);
                    control.LayoutSlot = target is Forma.Core.Controls.TabControl tabs ? tabs.SelectedTab + 1
                        : target is Forma.Core.Controls.BreakpointContainer responsiveTarget ? (_viewModel.Appearance[target.Id].Width < responsiveTarget.Breakpoint ? 2 : 1)
                        : target is Forma.Core.Controls.SplitContainer ? Math.Min(2, target.Children.Count + 1)
                        : target is Forma.Core.Controls.TableLayoutPanel ? target.Children.Count + 1 : 1;
                    control.X = left;
                    control.Y = top;
                    target.Add(control);
                    _layout.ResizeControl(control, bounds.Width, bounds.Height);
                }
                _layout.Position(control, left, top);
                if (Number(payload, "layoutSlot") is int slot && control.Parent is Forma.Core.Controls.SplitContainer or Forma.Core.Controls.BreakpointContainer or Forma.Core.Controls.TableLayoutPanel)
                {
                    control.LayoutSlot = control.Parent is Forma.Core.Controls.SplitContainer or Forma.Core.Controls.BreakpointContainer ? Math.Clamp(slot, 1, 2) : Math.Clamp(slot, 1, 1200);
                    _layout.ResizeControl(control, bounds.Width, bounds.Height);
                }
                if (Number(payload, "index") is int insertionIndex && control.Parent is Forma.Core.Controls.LinearLayout flow)
                    flow.MoveChild(control, insertionIndex);
                _viewModel.SelectedControl = control;
                return new("Control moved");
            case "resize" when !bounds.Locked && control is not Forma.Core.Controls.INonvisualControl
                && Number(payload, "width") is int width && Number(payload, "height") is int height:
                _layout.ResizeControl(control, width, height);
                if (control != form && control.Parent is not Forma.Core.Controls.LinearLayout and not Forma.Core.Controls.TableLayoutPanel
                    && bounds.Dock == "none" && Number(payload, "x") is int rx && Number(payload, "y") is int ry)
                    _layout.Position(control, rx, ry);
                _viewModel.SelectedControl = control;
                return new("Control resized");
            case "image-source" when control == _viewModel.SelectedControl && !bounds.Locked
                && control is Forma.Core.Controls.Image selectedImage && String(payload, "value") is string source:
                selectedImage.Source = Path.IsPathFullyQualified(source) ? new Uri(source).AbsoluteUri : source;
                return new("Image updated");
            case "path-source" when control == _viewModel.SelectedControl && !bounds.Locked
                && control is Forma.Core.Controls.PathPicker selectedPicker && String(payload, "value") is string path:
                selectedPicker.SelectedPath = path;
                return new("Path selected");
            case "property" when control == _viewModel.SelectedControl:
                ApplyProperty(control, payload);
                return new("Property updated");
        }
        return new("Ready");
    }
    private FControl? AddControl(string? kind, int x, int y, FControl parent, int? layoutSlot = null, int? index = null)
    {
        if (
            _viewModel.Form is null
            || _viewModel.Appearance[parent.Id].Locked
            || x < 0
            || y < 0
            || x >= _viewModel.Appearance[parent.Id].Width
            || y >= _viewModel.Appearance[parent.Id].Height
        )
            return null;
        if (!ControlCatalog.TryGet(kind, out var entry) || entry.Create is null)
            return null;
        var control = entry.Create();
        do { control.Name = $"{kind}{++_viewModel.ControlSequence}"; } while (Walk(_viewModel.Form).Any(c => c.Name == control.Name));
        var appearance = entry.DefaultAppearance();
        appearance.ZIndex = control is Forma.Core.Controls.Panel or Forma.Core.Controls.LayoutContainer ? 0 : 10;
        _viewModel.Appearance[control.Id] = appearance;
        if (control is Forma.Core.Controls.INonvisualControl)
            parent = _viewModel.Form;
        control.X = x;
        control.Y = y;
        if (parent is Forma.Core.Controls.TabControl tabParent)
            control.LayoutSlot = tabParent.SelectedTab + 1;
        else if (parent is Forma.Core.Controls.BreakpointContainer breakpointParent)
            control.LayoutSlot = _viewModel.Appearance[parent.Id].Width < breakpointParent.Breakpoint ? 2 : 1;
        else if (parent is Forma.Core.Controls.SplitContainer)
            control.LayoutSlot = Math.Min(2, parent.Children.Count + 1);
        else if (parent is Forma.Core.Controls.TableLayoutPanel)
            control.LayoutSlot = parent.Children.Count + 1;
        if (layoutSlot is int slot && parent is Forma.Core.Controls.SplitContainer or Forma.Core.Controls.BreakpointContainer or Forma.Core.Controls.TableLayoutPanel)
            control.LayoutSlot = parent is Forma.Core.Controls.SplitContainer or Forma.Core.Controls.BreakpointContainer ? Math.Clamp(slot, 1, 2) : Math.Clamp(slot, 1, 1200);
        if (parent is Forma.Core.Controls.LinearLayout && index is int insertionIndex)
            parent.Insert(Math.Clamp(insertionIndex, 0, parent.Children.Count), control);
        else parent.Add(control);
        if (control is not Forma.Core.Controls.INonvisualControl)
            _layout.ResizeControl(control, _viewModel.Appearance[control.Id].Width, _viewModel.Appearance[control.Id].Height);
        _viewModel.SelectedControl = control;
        return control;
    }

    private static string Describe(int count) => count == 1 ? "1 control" : $"{count} controls";

    private static string[] Lines(string text) =>
        text.Split('\n').Select(s => s.TrimEnd('\r')).Where(s => s.Length > 0).ToArray();

    private static string? String(JsonElement json, string name) =>
        json.ValueKind == JsonValueKind.Object
        && json.TryGetProperty(name, out var v)
        && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    private static int? Number(JsonElement json, string name) =>
        json.ValueKind == JsonValueKind.Object
        && json.TryGetProperty(name, out var v)
        && v.ValueKind == JsonValueKind.Number
        && v.TryGetInt32(out var n)
            ? n
            : null;

    private static bool Boolean(JsonElement json, string name, bool fallback) =>
        json.ValueKind == JsonValueKind.Object
        && json.TryGetProperty(name, out var v)
        && v.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? v.GetBoolean()
            : fallback;

    private static bool IsColor(string? value) =>
        value is { Length: 7 } && value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit);

    private static IEnumerable<FControl> Walk(FControl root)
    {
        yield return root;
        foreach (var child in root.Children)
        foreach (var item in Walk(child)) yield return item;
    }
}
