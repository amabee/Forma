using System.Text.Json;
using FControl = Forma.Core.Controls.Control;

namespace Forma.Builder;

// Edits that act on several selected controls at once: delete, move, copy, paste, duplicate.
public sealed partial class DesignerEditingService
{
    private const int PasteOffset = 16;

    /// <summary>
    /// The editable controls a command targets: the "ids" in the payload, or the selected control.
    /// The form, locked controls and controls whose ancestor is also targeted are left out,
    /// so a selected panel and its child are not handled twice.
    /// </summary>
    private List<FControl> Targets(JsonElement payload)
    {
        var form = _viewModel.Form!;
        var ids = IdList(payload) ?? (_viewModel.SelectedControl is { } selected ? [selected.Id] : []);
        var controls = Walk(form).Where(control => ids.Contains(control.Id) && control != form
            && _viewModel.Appearance.TryGetValue(control.Id, out var appearance) && !appearance.Locked).ToList();
        return controls.Where(control => !Ancestors(control).Any(controls.Contains)).ToList();
    }

    private static HashSet<string>? IdList(JsonElement payload) =>
        payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("ids", out var list) && list.ValueKind == JsonValueKind.Array
            ? list.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()!).Take(5000).ToHashSet()
            : null;

    private static IEnumerable<FControl> Ancestors(FControl control)
    {
        for (var parent = control.Parent; parent is not null; parent = parent.Parent) yield return parent;
    }

    private string DeleteControls(IReadOnlyList<FControl> targets)
    {
        var form = _viewModel.Form!;
        if (targets.Count == 0) return "Ready";
        var removed = targets.SelectMany(Walk).ToHashSet();
        foreach (var control in targets) control.Parent?.Remove(control);
        foreach (var child in removed)
        {
            _viewModel.Appearance.Remove(child.Id);
            (child as IDisposable)?.Dispose();
        }
        bool Gone(string id) => removed.Any(item => item.Id == id);
        foreach (var menu in Walk(form).OfType<Forma.Core.Controls.ContextMenu>())
            if (Gone(menu.TargetId)) menu.TargetId = "";
        foreach (var tooltip in Walk(form).OfType<Forma.Core.Controls.Tooltip>())
            if (Gone(tooltip.TargetId)) tooltip.TargetId = "";
        foreach (var overlay in Walk(form).OfType<Forma.Core.Controls.LoadingOverlay>())
            if (Gone(overlay.TargetId)) overlay.TargetId = "";
        _viewModel.SelectedControl = form;
        return targets.Count == 1 ? "Control deleted" : $"{targets.Count} controls deleted";
    }

    /// <summary>Moves several controls in one edit; positions are clamped like single moves.</summary>
    private string MoveControls(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty("moves", out var moves) || moves.ValueKind != JsonValueKind.Array)
            return "Ready";
        var form = _viewModel.Form!;
        var moved = 0;
        foreach (var move in moves.EnumerateArray().Take(5000))
        {
            if (String(move, "id") is not string id || Number(move, "x") is not int x || Number(move, "y") is not int y) continue;
            var control = Walk(form).FirstOrDefault(item => item.Id == id);
            if (control is null || control == form || control is Forma.Core.Controls.INonvisualControl
                || control.Parent is Forma.Core.Controls.LinearLayout or Forma.Core.Controls.TableLayoutPanel
                || !_viewModel.Appearance.TryGetValue(id, out var appearance) || appearance.Locked || appearance.Dock != "none")
                continue;
            _layout.Position(control, x, y);
            moved++;
        }
        return moved == 0 ? "Ready" : $"Moved {moved} control{(moved == 1 ? "" : "s")}";
    }

    /// <summary>Captures controls, their appearance and their children for pasting later.</summary>
    private List<ProjectNode> CopyNodes(IReadOnlyList<FControl> targets) =>
        targets.Select(control => ProjectFile.Capture(control, item => JsonSerializer.SerializeToElement(_viewModel.Appearance[item.Id]), embedImages: true).Root).ToList();

    /// <summary>
    /// Adds copies of <paramref name="nodes"/> with new IDs and names, offset from the originals.
    /// Copies go into the selected container, or next to the selected control.
    /// </summary>
    private IReadOnlyList<FControl> PasteNodes(IReadOnlyList<ProjectNode> nodes)
    {
        var form = _viewModel.Form!;
        if (nodes.Count == 0) return [];
        var selected = _viewModel.SelectedControl ?? form;
        var target = selected == form || selected is Forma.Core.Controls.Panel or Forma.Core.Controls.LayoutContainer
            ? selected : selected.Parent ?? form;
        if (_viewModel.Appearance[target.Id].Locked) target = form;

        // Fresh IDs for every copied node; targets that point inside the copy follow it.
        var ids = new Dictionary<string, string>();
        var copies = nodes.Select(node => CloneWithNewIds(node, ids)).ToList();
        foreach (var node in copies.SelectMany(Flatten))
            if (node.Properties.TryGetValue("TargetId", out var targetId) && targetId.ValueKind == JsonValueKind.String
                && ids.TryGetValue(targetId.GetString()!, out var copiedTarget))
                node.Properties["TargetId"] = JsonSerializer.SerializeToElement(copiedTarget);

        // Build through the project-file validator so a paste can never create an invalid tree.
        var holder = new ProjectNode
        {
            Kind = "form", Id = "paste-" + Guid.NewGuid().ToString("N"),
            Appearance = JsonSerializer.SerializeToElement(new Appearance { Width = 1600, Height = 1200 }),
            Children = copies,
        };
        var (built, appearances) = ProjectFile.Restore(new ProjectDocument { Root = holder });
        appearances.Remove(holder.Id);
        foreach (var (id, json) in appearances)
            _viewModel.Appearance[id] = json.Deserialize<Appearance>() ?? throw new InvalidDataException("Invalid appearance.");

        var pasted = built.Children.ToList();
        foreach (var control in pasted)
        {
            built.Remove(control);
            foreach (var item in Walk(control)) item.Name = UniqueName(item.ControlType);
            var parent = control is Forma.Core.Controls.INonvisualControl ? form : target;
            if (parent is Forma.Core.Controls.TabControl tabs) control.LayoutSlot = tabs.SelectedTab + 1;
            parent.Add(control);
            if (control is Forma.Core.Controls.INonvisualControl) continue;
            var appearance = _viewModel.Appearance[control.Id];
            _layout.ResizeControl(control, appearance.Width, appearance.Height);
            if (parent is not Forma.Core.Controls.LinearLayout and not Forma.Core.Controls.TableLayoutPanel && appearance.Dock == "none")
                _layout.Position(control, (control.X ?? 0) + PasteOffset, (control.Y ?? 0) + PasteOffset);
        }
        _viewModel.SetSelection(pasted);
        return pasted;
    }

    private string UniqueName(string kind)
    {
        string name;
        do { name = $"{kind}{++_viewModel.ControlSequence}"; } while (_viewModel.ProjectControls.Any(control => control.Name == name));
        return name;
    }

    private static ProjectNode CloneWithNewIds(ProjectNode node, Dictionary<string, string> ids)
    {
        var id = Guid.NewGuid().ToString();
        ids[node.Id] = id;
        return new ProjectNode
        {
            Kind = node.Kind,
            Id = id,
            Properties = new(node.Properties),
            Appearance = node.Appearance.Clone(),
            Children = node.Children.Select(child => CloneWithNewIds(child, ids)).ToList(),
        };
    }

    private static IEnumerable<ProjectNode> Flatten(ProjectNode node) =>
        node.Children.SelectMany(Flatten).Prepend(node);
}
