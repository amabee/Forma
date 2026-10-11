using System.Text.Json;
using Forma.Builder;
using Forma.Core.Controls;

namespace Forma.Tests;

public class MultiSelectionTests
{
    private static JsonElement Payload(object value) => JsonSerializer.SerializeToElement(value);

    private static BuilderViewModel NewDesign()
    {
        var model = new BuilderViewModel();
        model.CreateNew();
        return model;
    }

    private static Control Add(BuilderViewModel model, string kind, Control? parent = null, int x = 20, int y = 30) =>
        model.ExecuteEdit("drop", (parent ?? model.Form!).Id, Payload(new { control = kind, x, y })).AddedControl!;

    private static void Command(BuilderViewModel model, string command, params Control[] controls) =>
        model.ExecuteEdit("command", model.Form!.Id, Payload(new { command, ids = controls.Select(c => c.Id).ToArray() }));

    [Fact]
    public void SelectionWithIdsKeepsEveryControlAndThePrimary()
    {
        var model = NewDesign();
        var first = Add(model, "button"); var second = Add(model, "label", x: 200);
        model.ExecuteEdit("select", second.Id, Payload(new { ids = new[] { first.Id, second.Id } }));
        Assert.Equal(new[] { first.Id, second.Id }, model.SelectedIds);
        Assert.Same(second, model.SelectedControl);
        model.ExecuteEdit("select", first.Id, default);
        Assert.Equal(new[] { first.Id }, model.SelectedIds);
    }

    [Fact]
    public void DeletingSeveralControlsIsOneUndoStep()
    {
        var model = NewDesign();
        var panel = Add(model, "panel"); var child = Add(model, "button", panel); var label = Add(model, "label", x: 300);
        // The child is covered by its selected panel and must not be removed twice.
        Command(model, "delete", panel, child, label);
        Assert.Empty(model.Form!.Children);
        Assert.DoesNotContain(child.Id, model.Appearance.Keys);
        var undo = model.Undo()!;
        Assert.Contains(child.Id, undo.Json);
        Assert.Contains(label.Id, undo.Json);
    }

    [Fact]
    public void LockedControlsAreLeftOutOfBatchCommands()
    {
        var model = NewDesign();
        var locked = Add(model, "button"); var free = Add(model, "label", x: 200);
        model.Appearance[locked.Id].Locked = true;
        Command(model, "delete", locked, free);
        Assert.Equal(new[] { locked.Id }, model.Form!.Children.Select(c => c.Id));
    }

    [Fact]
    public void MoveManyPositionsEveryFreeControlAndClampsToTheParent()
    {
        var model = NewDesign();
        var first = Add(model, "button"); var second = Add(model, "label", x: 200);
        model.ExecuteEdit("move-many", model.Form!.Id, Payload(new { moves = new object[] {
            new { id = first.Id, x = 40, y = 50 }, new { id = second.Id, x = 5000, y = 60 } } }));
        Assert.Equal((40, 50), (first.X, first.Y));
        Assert.Equal(model.Appearance[model.Form.Id].Width - model.Appearance[second.Id].Width, second.X);
        Assert.True(model.CanUndo);
    }

    [Fact]
    public void CopyAndPasteCreatesIndependentControlsWithNewIdsNamesAndOffset()
    {
        var model = NewDesign();
        var panel = Add(model, "panel"); var child = Add(model, "button", panel);
        child.Text = "Inner";
        model.ExecuteEdit("select", panel.Id, default);
        Command(model, "copy", panel);
        Assert.Single(model.Clipboard);
        model.ExecuteEdit("select", model.Form!.Id, default);
        var result = model.ExecuteEdit("command", model.Form.Id, Payload(new { command = "paste" }));
        var copy = Assert.Single(result.Added);
        Assert.NotEqual(panel.Id, copy.Id);
        Assert.NotEqual(panel.Name, copy.Name);
        Assert.Equal(panel.X + 16, copy.X);
        var copiedChild = Assert.Single(copy.Children);
        Assert.Equal("Inner", copiedChild.Text);
        Assert.NotEqual(child.Id, copiedChild.Id);
        Assert.Contains(copiedChild.Id, model.Appearance.Keys);
        Assert.Equal(new[] { copy.Id }, model.SelectedIds);
        // Pasting again makes another copy rather than reusing the first one's IDs.
        var again = Assert.Single(model.ExecuteEdit("command", model.Form.Id, Payload(new { command = "paste" })).Added);
        Assert.NotEqual(copy.Id, again.Id);
    }

    [Fact]
    public void DuplicateKeepsTargetsInsideTheCopyAndPastesNextToTheOriginal()
    {
        var model = NewDesign();
        var panel = Add(model, "panel"); var button = Add(model, "button", panel);
        var tooltip = Add(model, "tooltip", panel);
        Assert.IsType<Tooltip>(tooltip);
        var outsideTarget = Add(model, "label", x: 300);
        ((Tooltip)tooltip).TargetId = button.Id;
        model.ExecuteEdit("select", button.Id, default);
        var copies = model.ExecuteEdit("command", model.Form!.Id, Payload(new { command = "duplicate", ids = new[] { button.Id, outsideTarget.Id } })).Added;
        Assert.Equal(2, copies.Count);
        Assert.Same(panel, copies[0].Parent);
        Assert.Equal(2, model.SelectedIds.Count);
        Assert.Equal(button.Id, ((Tooltip)tooltip).TargetId);
    }

    [Fact]
    public void CutRemovesTheOriginalsAndPasteRestoresCopies()
    {
        var model = NewDesign();
        var label = Add(model, "label");
        Command(model, "cut", label);
        Assert.Empty(model.Form!.Children);
        var pasted = Assert.Single(model.ExecuteEdit("command", model.Form.Id, Payload(new { command = "paste" })).Added);
        Assert.Equal("label", pasted.ControlType);
    }

    [Fact]
    public void PropertyEditsApplyToEverySelectedControlThatHasTheProperty()
    {
        var model = NewDesign();
        var button = Add(model, "button"); var label = Add(model, "label", x: 200); var timer = Add(model, "timer");
        model.Appearance[label.Id].Locked = true;
        var other = Add(model, "button", x: 300);
        var ids = new[] { button.Id, label.Id, timer.Id, other.Id };
        model.ExecuteEdit("select", other.Id, Payload(new { ids }));
        model.ExecuteEdit("property", other.Id, Payload(new { property = "foreColor", value = "#ff0000", ids }));
        Assert.Equal("#ff0000", model.Appearance[button.Id].ForeColor);
        Assert.Equal("#ff0000", model.Appearance[other.Id].ForeColor);
        Assert.NotEqual("#ff0000", model.Appearance[label.Id].ForeColor);
        // Names stay unique: renaming never fans out to the whole selection.
        model.ExecuteEdit("property", other.Id, Payload(new { property = "name", value = "renamed", ids }));
        Assert.Equal("renamed", other.Name);
        Assert.NotEqual("renamed", button.Name);
        // One undo step restores every control.
        var undo = model.Undo()!;
        Assert.DoesNotContain("renamed", undo.Json);
    }
}
