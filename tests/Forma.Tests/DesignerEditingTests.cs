using System.Text.Json;
using Forma.Builder;
using Forma.Core.Controls;

namespace Forma.Tests;

public class DesignerEditingTests
{
    [Fact]
    public void ManagedLayoutsSupportReorderingAndCellPlacementWithUndo()
    {
        var model = NewDesign();
        var flow = Add(model, "flowlayoutpanel");
        var first = Add(model, "button", flow);
        var second = Add(model, "label", flow);
        model.ExecuteEdit("move", first.Id, Payload(new { x = 0, y = 0, index = 1 }));
        Assert.Equal(new[] { second.Id, first.Id }, flow.Children.Select(c => c.Id));
        Restore(model, model.Undo()!);
        var restoredFlow = model.Form!.Children.Single();
        Assert.Equal(new[] { first.Id, second.Id }, restoredFlow.Children.Select(c => c.Id));

        var table = Add(model, "tablelayoutpanel");
        Property(model, table, "rowCount", 3);
        var child = model.ExecuteEdit("drop", table.Id, Payload(new { control = "button", x = 0, y = 0, layoutSlot = 6 })).AddedControl!;
        Assert.Equal(6, child.LayoutSlot);
        model.ExecuteEdit("move", child.Id, Payload(new { x = 0, y = 0, layoutSlot = 2 }));
        Assert.Equal(2, child.LayoutSlot);
        Assert.Equal(3, Assert.IsType<TableLayoutPanel>(table).RowCount);
        Restore(model, model.Undo()!);
        Assert.Equal(6, model.Form!.Children.Last().Children.Single().LayoutSlot);
        Assert.Equal(3, Assert.IsType<TableLayoutPanel>(model.Form.Children.Last()).RowCount);
    }

    [Fact]
    public void SplitPanePlacementAndVerticalTabAdditionAreUndoable()
    {
        var model = NewDesign();
        var split = Add(model, "splitcontainer");
        var child = model.ExecuteEdit("drop", split.Id, Payload(new { control = "button", x = 0, y = 0, layoutSlot = 2 })).AddedControl!;
        Assert.Equal(2, child.LayoutSlot);
        model.ExecuteEdit("move", child.Id, Payload(new { x = 0, y = 0, layoutSlot = 1 }));
        Assert.Equal(1, child.LayoutSlot);
        var tabs = Assert.IsType<TabControl>(Add(model, "tabcontrol"));
        Property(model, tabs, "orientation", "vertical");
        var count = tabs.Tabs.Length;
        model.ExecuteEdit("command", tabs.Id, Payload(new { command = "add-tab" }));
        Assert.Equal(count + 1, tabs.Tabs.Length);
        Assert.Equal(count, tabs.SelectedTab);
        Restore(model, model.Undo()!);
        var restored = Assert.IsType<TabControl>(model.Form!.Children.Last());
        Assert.Equal(count, restored.Tabs.Length);
        Assert.Equal("vertical", restored.Orientation);
    }

    private static JsonElement Payload(object value) => JsonSerializer.SerializeToElement(value);
    private static BuilderViewModel NewDesign()
    {
        var model = new BuilderViewModel();
        model.CreateNew();
        return model;
    }
    private static Control Add(BuilderViewModel model, string kind, Control? parent = null) =>
        model.ExecuteEdit("drop", (parent ?? model.Form!).Id,
            Payload(new { control = kind, x = 20, y = 30 })).AddedControl!;
    private static void Property(BuilderViewModel model, Control control, string property, object value) =>
        model.ExecuteEdit("property", control.Id, Payload(new { property, value }));
    private static void Restore(BuilderViewModel model, DesignSnapshot snapshot)
    {
        var restored = ProjectFile.Restore(ProjectFile.Parse(snapshot.Json));
        model.ApplyDocument(restored.Form, restored.Appearance.ToDictionary(
            pair => pair.Key, pair => pair.Value.Deserialize<Appearance>()!), snapshot.SelectedId);
    }

    [Fact]
    public void AddingAndMovingControlsRecordsUndoAndRespectsBounds()
    {
        var model = NewDesign();
        var button = Add(model, "button");
        Assert.Same(button, model.SelectedControl);
        Assert.Same(model.Form, button.Parent);
        Assert.True(model.CanUndo);
        model.ExecuteEdit("move", button.Id, Payload(new { x = 900, y = -10 }));
        Assert.Equal(520, button.X);
        Assert.Equal(0, button.Y);
        Restore(model, model.Undo()!);
        Assert.Equal(button.Id, model.SelectedControl!.Id);
        Assert.Equal(20, model.SelectedControl.X);
        Restore(model, model.Undo()!);
        Assert.Empty(model.Form!.Children);
        Assert.False(model.HasUnsavedChanges);
        Restore(model, model.Redo()!);
        Assert.Single(model.Form!.Children);
    }

    [Fact]
    public void NestedResizingHonorsParentPaddingAndConfiguredLimits()
    {
        var model = NewDesign();
        var panel = Add(model, "panel");
        var button = Add(model, "button", panel);
        Property(model, button, "minimumWidth", 80);
        Property(model, button, "maximumWidth", 140);
        model.ExecuteEdit("resize", button.Id, Payload(new { width = 800, height = 600, x = 900, y = 900 }));
        Assert.Equal(140, model.Appearance[button.Id].Width);
        Assert.Equal(122, model.Appearance[button.Id].Height);
        Assert.Equal(62, button.X);
        Assert.Equal(0, button.Y);
        model.ExecuteEdit("resize", button.Id, Payload(new { width = 1, height = 1 }));
        Assert.Equal(80, model.Appearance[button.Id].Width);
        Assert.Equal(20, model.Appearance[button.Id].Height);
    }

    [Fact]
    public void LockedAndPreviewControlsRejectEditsWithoutAddingHistory()
    {
        var model = NewDesign();
        var button = Add(model, "button");
        Property(model, button, "locked", true);
        model.ClearHistory();
        model.ExecuteEdit("move", button.Id, Payload(new { x = 100, y = 100 }));
        model.ExecuteEdit("resize", button.Id, Payload(new { width = 200, height = 100 }));
        Property(model, button, "text", "Blocked");
        model.ExecuteEdit("command", button.Id, Payload(new { command = "delete" }));
        Assert.Equal("Continue", button.Text);
        Assert.Equal(20, button.X);
        Assert.Single(model.Form!.Children);
        Assert.False(model.CanUndo);
        Property(model, button, "locked", false);
        model.ClearHistory();
        model.ExecuteEdit("preview", model.Form.Id, Payload(new { enabled = true }));
        model.ExecuteEdit("move", button.Id, Payload(new { x = 100, y = 100 }));
        Property(model, button, "text", "Blocked");
        Assert.Null(model.ExecuteEdit("drop", model.Form.Id, Payload(new { control = "button", x = 20, y = 30 })).AddedControl);
        model.ExecuteEdit("preview", model.Form.Id, Payload(new { enabled = false }));
        Assert.False(model.CanUndo);
        Assert.Equal("Continue", button.Text);
    }

    [Fact]
    public void DeletingAContainerRemovesItsMetadataAndUndoRestoresTheTree()
    {
        var model = NewDesign();
        var panel = Add(model, "panel");
        var button = Add(model, "button", panel);
        model.ExecuteEdit("select", panel.Id, default);
        model.ExecuteEdit("command", panel.Id, Payload(new { command = "delete" }));
        Assert.Empty(model.Form!.Children);
        Assert.Single(model.Appearance);
        Assert.Same(model.Form, model.SelectedControl);
        Restore(model, model.Undo()!);
        Assert.Equal(panel.Id, model.SelectedControl!.Id);
        Assert.Equal(button.Id, Assert.Single(model.SelectedControl.Children).Id);
        Assert.Equal(3, model.Appearance.Count);
    }

    [Fact]
    public void PropertyEditsValidateSelectionNamesAndNumericValues()
    {
        var model = NewDesign();
        var first = Add(model, "button");
        var numeric = (NumericUpDown)Add(model, "numericupdown");
        Property(model, first, "text", "Stale selection");
        Assert.Equal("Continue", first.Text);
        Assert.Throws<ArgumentException>(() => Property(model, numeric, "name", first.Name!));
        Property(model, numeric, "number", 12.5);
        Assert.Equal(12.5, numeric.Value);
        Restore(model, model.Undo()!);
        Assert.Equal(0, ((NumericUpDown)model.SelectedControl!).Value);
    }

    [Fact]
    public void LayerOrderingIsUndoableAndInvalidDropsDoNotCreateEdits()
    {
        var model = NewDesign();
        Assert.Null(model.ExecuteEdit("drop", model.Form!.Id, Payload(new { control = "unknown", x = 20, y = 30 })).AddedControl);
        Assert.False(model.CanUndo);
        var button = Add(model, "button");
        var panel = Add(model, "panel");
        Assert.True(model.Appearance[button.Id].ZIndex > model.Appearance[panel.Id].ZIndex);
        model.ExecuteEdit("command", panel.Id, Payload(new { command = "bring-front" }));
        Assert.True(model.Appearance[panel.Id].ZIndex > model.Appearance[button.Id].ZIndex);
        Restore(model, model.Undo()!);
        Assert.True(model.Appearance[button.Id].ZIndex > model.Appearance[panel.Id].ZIndex);
    }

    [Fact]
    public void MovingIntoAndOutOfPanelsChangesTheTreeAndUndoRestoresGrouping()
    {
        var model = NewDesign();
        var button = Add(model, "button");
        var panel = Add(model, "panel");
        model.ExecuteEdit("move", button.Id, Payload(new { parentId = panel.Id, x = 30, y = 40 }));
        Assert.Same(panel, button.Parent);
        Assert.Equal(30, button.X);
        Assert.Equal(40, button.Y);
        model.ExecuteEdit("move", panel.Id, Payload(new { x = 200, y = 150 }));
        Assert.Equal(30, button.X);
        Assert.Equal(40, button.Y);
        Assert.Equal(230, panel.X + button.X);
        model.ExecuteEdit("move", button.Id, Payload(new { parentId = model.Form!.Id, x = 300, y = 200 }));
        Assert.Same(model.Form, button.Parent);
        Restore(model, model.Undo()!);
        var restoredPanel = model.Form!.Children.Single(control => control.Id == panel.Id);
        var restoredButton = Assert.Single(restoredPanel.Children);
        Assert.Equal(button.Id, restoredButton.Id);
        Assert.Equal(30, restoredButton.X);
        Restore(model, model.Redo()!);
        Assert.Same(model.Form, model.SelectedControl!.Parent);
        Assert.Equal(300, model.SelectedControl.X);
    }

    [Fact]
    public void ReparentingContainersPreservesChildrenAndRejectsCyclesAndLockedTargets()
    {
        var model = NewDesign();
        var outer = Add(model, "panel");
        model.ExecuteEdit("resize", outer.Id, Payload(new { width = 400, height = 300 }));
        var inner = Add(model, "panel");
        var button = Add(model, "button", inner);
        model.ExecuteEdit("move", inner.Id, Payload(new { parentId = outer.Id, x = 40, y = 50 }));
        Assert.Same(outer, inner.Parent);
        Assert.Same(inner, button.Parent);
        model.ClearHistory();
        model.ExecuteEdit("move", outer.Id, Payload(new { parentId = inner.Id, x = 1, y = 1 }));
        Assert.Same(model.Form, outer.Parent);
        Assert.False(model.CanUndo);
        model.ExecuteEdit("move", button.Id, Payload(new { parentId = button.Id, x = 1, y = 1 }));
        Assert.Same(inner, button.Parent);
        model.Appearance[outer.Id].Locked = true;
        model.ExecuteEdit("move", button.Id, Payload(new { parentId = outer.Id, x = 1, y = 1 }));
        Assert.Same(inner, button.Parent);
        Assert.False(model.CanUndo);
    }

    [Fact]
    public async Task ReparentedSubtreesAreRenderedUnderTheirNewParent()
    {
        var model = NewDesign();
        var target = Add(model, "panel");
        model.ExecuteEdit("resize", target.Id, Payload(new { width = 400, height = 300 }));
        var panel = Add(model, "panel");
        var button = Add(model, "button", panel);
        var bridge = new FakeBridge();
        var renderer = new Forma.WebView2.WebView2Renderer(bridge);
        await renderer.RenderAsync(model.Form!);
        bridge.Clear();
        model.ExecuteEdit("move", panel.Id, Payload(new { parentId = target.Id, x = 10, y = 10 }));
        Assert.Contains(bridge.Sent, message => message.Str("type") == "create"
            && message.Str("id") == panel.Id && message.Str("parentId") == target.Id);
        Assert.Contains(bridge.Sent, message => message.Str("type") == "create"
            && message.Str("id") == button.Id && message.Str("parentId") == panel.Id);
        bridge.Clear();
        button.Text = "Still observed";
        Assert.Single(bridge.Sent, message => message.Str("id") == button.Id);
    }
    [Theory]
    [InlineData("avatar")]
    [InlineData("image")]
    [InlineData("picturebox")]
    public async Task ImagePickerSelectionUpdatesRendererAndSupportsHistoryAndPersistence(string kind)
    {
        var model = NewDesign(); var image = Assert.IsAssignableFrom<Image>(Add(model, kind));
        var bridge = new FakeBridge(); var renderer = new Forma.WebView2.WebView2Renderer(bridge);
        await renderer.RenderAsync(model.Form!); bridge.Clear(); model.ClearHistory();
        var source = new Uri(Path.Combine(Path.GetTempPath(), "portrait with spaces.png")).AbsoluteUri;
        // The source field stays read-only; the picker uses an explicit design action.
        Property(model, image, "source", source); Assert.Equal("", image.Source); Assert.False(model.CanUndo);
        model.ExecuteEdit("image-source", image.Id, Payload(new { value = source }));
        Assert.Equal(source, image.Source); Assert.True(model.CanUndo);
        Assert.Contains(bridge.Sent, message => message.Str("id") == image.Id
            && message.Obj("properties").Str("source") == source);
        using (var preview = new PreviewSession(model))
            Assert.Equal(source, Assert.IsAssignableFrom<Image>(preview.Controls.Single(c => c.Id == image.Id)).Source);
        var copy = ProjectFile.Restore(ProjectFile.Capture(model.Form!, c => Payload(model.Appearance[c.Id])));
        Assert.Equal(source, Assert.IsAssignableFrom<Image>(copy.Form.Children.Single()).Source);
        Restore(model, model.Undo()!); Assert.Equal("", Assert.IsAssignableFrom<Image>(model.SelectedControl).Source);
        Restore(model, model.Redo()!); Assert.Equal(source, Assert.IsAssignableFrom<Image>(model.SelectedControl).Source);
        model.ClearHistory(); model.Appearance[image.Id].Locked = true;
        model.ExecuteEdit("image-source", image.Id, Payload(new { value = "other.png" }));
        Assert.Equal(source, Assert.IsAssignableFrom<Image>(model.SelectedControl).Source); Assert.False(model.CanUndo);
        model.Appearance[image.Id].Locked = false; model.PreviewMode = true;
        model.ExecuteEdit("image-source", image.Id, Payload(new { value = "other.png" }));
        Assert.Equal(source, Assert.IsAssignableFrom<Image>(model.SelectedControl).Source); Assert.False(model.CanUndo);
    }

}
