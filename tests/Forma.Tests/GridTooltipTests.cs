using System.Text.Json;
using Forma.Builder;
using Forma.Core.Controls;
using Forma.Core.Rendering;
using Forma.WebView2;

namespace Forma.Tests;

public class GridTooltipTests
{
    [Fact]
    public void InspectorColumnsAndRowsSurviveSelectionSaveOpenAndUndo()
    {
        var design = new BuilderViewModel(); design.CreateNew();
        var grid = Assert.IsType<DataGridView>(design.ExecuteEdit("drop", design.Form.Id,
            JsonSerializer.SerializeToElement(new { control = "datagridview", x = 0, y = 0 })).AddedControl);
        design.ExecuteEdit("property", grid.Id, JsonSerializer.SerializeToElement(new { property = "gridColumns", value = "Employee\nDepartment\nStatus" }));
        design.ExecuteEdit("property", grid.Id, JsonSerializer.SerializeToElement(new { property = "gridRows", value = "[[\"Angel\",\"Engineering\",\"Active\"]]" }));
        design.ExecuteEdit("select", design.Form.Id, JsonSerializer.SerializeToElement(new { }));
        var restored = ProjectFile.Restore(ProjectFile.Parse(ProjectFile.Serialize(design.CaptureProject())));
        var copy = Assert.IsType<DataGridView>(Assert.Single(restored.Form.Children));
        Assert.Equal(new[] { "Employee", "Department", "Status" }, copy.Columns);
        Assert.Equal(new[] { "Angel", "Engineering", "Active" }, Assert.Single(copy.Rows));
        var undone = ProjectFile.Restore(ProjectFile.Parse(design.Undo()!.Json));
        Assert.Equal("First row", Assert.IsType<DataGridView>(Assert.Single(undone.Form.Children)).Rows[0][0]);
        var redone = ProjectFile.Restore(ProjectFile.Parse(design.Redo()!.Json));
        Assert.Equal("Angel", Assert.IsType<DataGridView>(Assert.Single(redone.Form.Children)).Rows[0][0]);
    }

    [Fact]
    public void GridSortsNumbersFiltersAndRetainsOriginalRowIdentity()
    {
        var grid = new DataGridView { Columns = ["Name", "Number"], Rows = [["Ten", "10"], ["Two", "2"], ["Another two", "2"]], SortColumn = 1, ReadOnly = false };
        Assert.Equal(new[] { 1, 2, 0 }, grid.VisibleRowIndices);
        grid.SelectedRow = 1; grid.SortDirection = "descending";
        Assert.Equal(new[] { 0, 1, 2 }, grid.VisibleRowIndices);
        Assert.Equal(1, grid.SelectedRow);
        grid.FilterText = "TWO";
        Assert.Equal(new[] { 1, 2 }, grid.VisibleRowIndices);
        grid.SetCell(1, 1, "20");
        Assert.Equal("20", grid.Rows[1][1]);
        grid.FilteringEnabled = false; grid.SortingEnabled = false;
        Assert.Equal(new[] { 0, 1, 2 }, grid.VisibleRowIndices);
        grid.Rows = []; Assert.Equal(-1, grid.SelectedRow);
        grid.Columns = []; Assert.Equal(-1, grid.SortColumn);
    }

    [Fact]
    public async Task GridBridgeHonorsFeatureFlagsAndSelectsOnlyVisibleSourceRows()
    {
        var bridge = new FakeBridge(); var renderer = new WebView2Renderer(bridge);
        var grid = new DataGridView { Columns = ["Name"], Rows = [["B"], ["A"]] };
        await renderer.RenderAsync(grid);
        BridgeMessage Event(string action, object payload) => new() { Type = "event", Id = grid.Id, Event = action, Payload = JsonSerializer.SerializeToElement(payload) };
        bridge.Receive(Event("grid-sort", new { column = 0 }));
        Assert.Equal(new[] { 1, 0 }, grid.VisibleRowIndices);
        bridge.Receive(Event("grid-sort", new { column = 0 })); Assert.Equal("descending", grid.SortDirection);
        bridge.Receive(Event("grid-filter", new { text = "A" })); Assert.Equal(new[] { 1 }, grid.VisibleRowIndices);
        bridge.Receive(Event("grid-select", new { row = 0 })); Assert.Equal(-1, grid.SelectedRow);
        bridge.Receive(Event("grid-select", new { row = 1 })); Assert.Equal(1, grid.SelectedRow);
        grid.SortingEnabled = false; grid.FilteringEnabled = false;
        bridge.Receive(Event("grid-sort", new { column = 0 })); bridge.Receive(Event("grid-filter", new { text = "B" }));
        Assert.Equal("descending", grid.SortDirection); Assert.Equal("A", grid.FilterText);
    }

    [Fact]
    public void TooltipTargetAndGridSettingsRoundTripAndDeletionCanBeUndone()
    {
        var design = new BuilderViewModel(); design.CreateNew();
        Control Add(string kind) => design.ExecuteEdit("drop", design.Form.Id, JsonSerializer.SerializeToElement(new { control = kind, x = 0, y = 0 })).AddedControl!;
        var button = Add("button"); var tooltip = Assert.IsType<Tooltip>(Add("tooltip"));
        design.ExecuteEdit("property", tooltip.Id, JsonSerializer.SerializeToElement(new { property = "targetId", value = button.Id }));
        tooltip.InitialDelay = 25; tooltip.ShowDuration = 900; tooltip.Placement = "right";
        var grid = Assert.IsType<DataGridView>(Add("datagridview")); grid.SortColumn = 0; grid.FilterText = "First"; grid.SelectedRow = 0;
        var copy = ProjectFile.Restore(ProjectFile.Capture(design.Form, control => JsonSerializer.SerializeToElement(design.Appearance[control.Id])));
        var restored = Assert.IsType<Tooltip>(copy.Form.Children[1]);
        Assert.Equal(button.Id, restored.TargetId); Assert.Equal(25, restored.InitialDelay); Assert.Equal("right", restored.Placement);
        var restoredGrid = Assert.IsType<DataGridView>(copy.Form.Children[2]); Assert.Equal(0, restoredGrid.SortColumn); Assert.Equal("First", restoredGrid.FilterText); Assert.Equal(0, restoredGrid.SelectedRow);
        design.SelectedControl = button;
        design.ExecuteEdit("command", button.Id, JsonSerializer.SerializeToElement(new { command = "delete" })); Assert.Equal("", tooltip.TargetId);
        var previous = ProjectFile.Restore(ProjectFile.Parse(design.Undo()!.Json));
        Assert.Equal(button.Id, Assert.IsType<Tooltip>(previous.Form.Children[1]).TargetId);
    }
}
