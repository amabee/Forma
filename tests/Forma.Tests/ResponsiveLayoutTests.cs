using System.Text.Json;
using Forma.Builder;
using Forma.Core.Controls;

namespace Forma.Tests;

public class ResponsiveLayoutTests
{
    private static JsonElement Value(object value) => JsonSerializer.SerializeToElement(value);
    private static Forma.Core.Controls.Control Drop(BuilderViewModel model, string kind, string? parent = null, int? slot = null) =>
        model.ExecuteEdit("drop", parent ?? model.Form!.Id, Value(new { control = kind, x = 0, y = 0, layoutSlot = slot })).AddedControl!;
    private static void Set(BuilderViewModel model, Forma.Core.Controls.Control control, string property, object value) {
        model.ExecuteEdit("select", control.Id, default);
        model.ExecuteEdit("property", control.Id, Value(new { property, value }));
    }

    [Fact]
    public void NewContainersPersistChildrenPropertiesAndUndoRedo()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        var grid = Assert.IsType<ResponsiveGrid>(Drop(model, "responsivegrid"));
        var stack = Assert.IsType<ResponsiveStack>(Drop(model, "responsivestack"));
        var panes = Assert.IsType<BreakpointContainer>(Drop(model, "breakpointcontainer"));
        var aspect = Assert.IsType<AspectRatioContainer>(Drop(model, "aspectratiocontainer"));
        foreach (var container in new Forma.Core.Controls.Control[] { grid, stack, panes, aspect }) Drop(model, "button", container.Id);
        Set(model, grid, "columns", 4); Set(model, grid, "narrowColumns", 2);
        Set(model, stack, "narrowOrientation", "horizontal"); Set(model, panes, "breakpoint", 480);
        Set(model, aspect, "aspectRatio", 2.5);
        Assert.Equal(2.5, aspect.AspectRatio);
        var undo = ProjectFile.Restore(ProjectFile.Parse(model.Undo()!.Json));
        Assert.Equal(16d / 9d, undo.Form.Children.OfType<AspectRatioContainer>().Single().AspectRatio);
        var redo = ProjectFile.Restore(ProjectFile.Parse(model.Redo()!.Json));
        Assert.Equal(2.5, redo.Form.Children.OfType<AspectRatioContainer>().Single().AspectRatio);
        var saved = ProjectFile.Restore(ProjectFile.Parse(ProjectFile.Serialize(model.CaptureProject())));
        Assert.Equal(4, saved.Form.Children.OfType<ResponsiveGrid>().Single().Columns);
        Assert.Equal(2, saved.Form.Children.OfType<ResponsiveGrid>().Single().NarrowColumns);
        Assert.Equal("horizontal", saved.Form.Children.OfType<ResponsiveStack>().Single().NarrowOrientation);
        Assert.Equal(480, saved.Form.Children.OfType<BreakpointContainer>().Single().Breakpoint);
        Assert.All(saved.Form.Children, control => Assert.Single(control.Children));
        Assert.Equal(2, saved.Form.Children.OfType<BreakpointContainer>().Single().Children[0].LayoutSlot);
    }

    [Fact]
    public void RuntimeSettingsAreTypedAndIndependentFromDesignAndRatioHeightIsDerived()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        var grid = Drop(model, "responsivegrid"); var stack = Drop(model, "responsivestack");
        var panes = Drop(model, "breakpointcontainer"); var aspect = Drop(model, "aspectratiocontainer");
        Set(model, aspect, "width", 360); Set(model, aspect, "aspectRatio", 2);
        Assert.Equal(180, model.Appearance[aspect.Id].Height);
        using var preview = new PreviewSession(model);
        preview.SetValue(grid.Id, "columns", Value(3)); preview.SetValue(grid.Id, "narrowColumns", Value(2));
        preview.SetValue(stack.Id, "narrowOrientation", Value("horizontal"));
        preview.SetValue(panes.Id, "breakpoint", Value(450));
        preview.SetValue(aspect.Id, "aspectRatio", Value(3));
        Assert.Equal(120, preview.Appearance[aspect.Id].Height);
        preview.SetValue(aspect.Id, "width", Value(300)); Assert.Equal(100, preview.Appearance[aspect.Id].Height);
        Assert.Equal(180, model.Appearance[aspect.Id].Height);
        Assert.Equal(2, ((ResponsiveGrid)grid).Columns); Assert.Equal("vertical", ((ResponsiveStack)stack).NarrowOrientation);
        Assert.Throws<ArgumentException>(() => preview.SetValue(grid.Id, "narrowColumns", Value("2")));
        Assert.Throws<ArgumentException>(() => preview.SetValue(stack.Id, "narrowOrientation", Value("diagonal")));
        Assert.Throws<ArgumentException>(() => preview.SetValue(aspect.Id, "height", Value(200)));
        var state = JsonSerializer.SerializeToElement(preview.State()).GetProperty("controls").EnumerateArray().ToArray();
        Assert.Equal(3, state.Single(item => item.GetProperty("id").GetString() == grid.Id).GetProperty("columns").GetInt32());
        Assert.Equal(450, state.Single(item => item.GetProperty("id").GetString() == panes.Id).GetProperty("breakpoint").GetInt32());
    }

    [Fact]
    public void BreakpointLayoutsKeepSeparateDockingAndRatioChildrenFollowWidth()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        var panes = Drop(model, "breakpointcontainer");
        var wide = Drop(model, "button", panes.Id, 1); var narrow = Drop(model, "button", panes.Id, 2);
        Set(model, wide, "dock", "top"); Set(model, narrow, "dock", "top");
        Assert.Equal(wide.Y, narrow.Y); // Inactive panes must not consume each other's dock space.
        var ratio = Drop(model, "aspectratiocontainer"); Set(model, ratio, "aspectRatio", 2);
        var child = Drop(model, "button", ratio.Id);
        Set(model, child, "anchor", "bottom,right"); var oldY = child.Y;
        using var preview = new PreviewSession(model);
        preview.SetValue(ratio.Id, "aspectRatio", Value(1));
        Assert.True(preview.Controls.Single(control => control.Id == child.Id).Y > oldY);
        Assert.Equal(oldY, child.Y);
        var dockModel = new BuilderViewModel(); dockModel.CreateNew();
        var dockFrame = Drop(dockModel, "aspectratiocontainer"); var dockButton = Drop(dockModel, "button");
        Set(dockModel, dockFrame, "aspectRatio", 2); Set(dockModel, dockFrame, "dock", "top");
        Set(dockModel, dockButton, "dock", "top");
        Assert.Equal(dockModel.Appearance[dockFrame.Id].Height, dockButton.Y);
        var coreRatio = new AspectRatioContainer(); Assert.Throws<ArgumentOutOfRangeException>(() => coreRatio.AspectRatio = double.NaN);
        Assert.Equal(12, new ResponsiveGrid { NarrowColumns = 100 }.NarrowColumns);
    }
}
