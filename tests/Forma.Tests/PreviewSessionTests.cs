using System.Text.Json;
using Forma.Builder;
using Forma.Core.Controls;

namespace Forma.Tests;

public class PreviewSessionTests
{
    [Fact]
    public void TimerIntervalIsTypedClampedAndRuntimeOnly()
    {
        var design = new BuilderViewModel(); design.CreateNew();
        var original = Assert.IsType<Forma.Core.Controls.Timer>(design.ExecuteEdit("drop", design.Form.Id,
            JsonSerializer.SerializeToElement(new { control = "timer", x = 0, y = 0 })).AddedControl);
        using var preview = new PreviewSession(design);
        var timer = Assert.IsType<Forma.Core.Controls.Timer>(preview.Controls.Single(c => c.Id == original.Id));
        preview.SetValue(timer.Id, "interval", JsonSerializer.SerializeToElement(250));
        Assert.Equal(250, timer.Interval); Assert.Equal(1000, original.Interval);
        var state = JsonSerializer.SerializeToElement(preview.State()).GetProperty("controls").EnumerateArray().Single(c => c.GetProperty("id").GetString() == timer.Id);
        Assert.Equal(250, state.GetProperty("interval").GetInt32());
        preview.SetValue(timer.Id, "enabled", JsonSerializer.SerializeToElement(true));
        Assert.True(timer.Enabled);
        preview.SetValue(timer.Id, "interval", JsonSerializer.SerializeToElement(2000));
        Assert.Equal(2000, timer.Interval); Assert.True(timer.Enabled);
        preview.SetValue(timer.Id, "enabled", JsonSerializer.SerializeToElement(false)); Assert.False(timer.Enabled);
        Assert.Throws<ArgumentException>(() => preview.SetValue(timer.Id, "interval", JsonSerializer.SerializeToElement("1000")));
        Assert.Throws<ArgumentException>(() => preview.SetValue(timer.Id, "interval", JsonSerializer.SerializeToElement(1000.5)));
        Assert.Equal(2000, timer.Interval);
        preview.SetValue(timer.Id, "interval", JsonSerializer.SerializeToElement(0)); Assert.Equal(10, timer.Interval);
        preview.SetValue(timer.Id, "interval", JsonSerializer.SerializeToElement(4000000)); Assert.Equal(3600000, timer.Interval);
    }

    [Fact]
    public void NumericRangesAndLoadingPropertiesHaveRuntimeAccess()
    {
        var design = new BuilderViewModel(); design.CreateNew();
        foreach (var kind in new[] { "numericupdown", "spinner", "skeleton" }) design.ExecuteEdit("drop", design.Form.Id, JsonSerializer.SerializeToElement(new { control = kind, x = 0, y = 0 }));
        using var preview = new PreviewSession(design);
        var numeric = preview.Controls.OfType<NumericUpDown>().Single();
        preview.SetValue(numeric.Id, "maximum", JsonSerializer.SerializeToElement(200));
        preview.SetValue(numeric.Id, "minimum", JsonSerializer.SerializeToElement(20));
        preview.SetValue(numeric.Id, "increment", JsonSerializer.SerializeToElement(5));
        preview.SetValue(numeric.Id, "value", JsonSerializer.SerializeToElement(10));
        Assert.Equal(20, numeric.Value); Assert.Equal(5, numeric.Increment);
        Assert.Throws<ArgumentException>(() => preview.SetValue(numeric.Id, "increment", JsonSerializer.SerializeToElement(0)));
        var spinner = preview.Controls.OfType<Spinner>().Single();
        preview.SetValue(spinner.Id, "speed", JsonSerializer.SerializeToElement(1200)); Assert.Equal(1200, spinner.Speed);
        var skeleton = preview.Controls.OfType<Skeleton>().Single();
        preview.SetValue(skeleton.Id, "lines", JsonSerializer.SerializeToElement(5));
        preview.SetValue(skeleton.Id, "shape", JsonSerializer.SerializeToElement("rectangle"));
        preview.SetValue(skeleton.Id, "isActive", JsonSerializer.SerializeToElement(false));
        Assert.Equal(5, skeleton.Lines); Assert.Equal("rectangle", skeleton.Shape); Assert.False(skeleton.IsActive);
    }
    [Fact]
    public void ScriptGridOperationsUseLatestRowsPreserveSelectionAndLeaveDesignUntouched()
    {
        var design = new BuilderViewModel(); design.CreateNew();
        var original = Assert.IsType<Forma.Core.Controls.DataGridView>(design.ExecuteEdit("drop", design.Form.Id,
            JsonSerializer.SerializeToElement(new { control = "datagridview", x = 20, y = 30 })).AddedControl!);
        using var preview = new PreviewSession(design);
        var grid = Assert.IsType<Forma.Core.Controls.DataGridView>(preview.Controls.Single(c => c.Id == original.Id));
        JsonElement Payload(object value) => JsonSerializer.SerializeToElement(value);
        preview.SetValue(grid.Id, "columns", Payload(new[] { "Name", "Department" }));
        preview.SetValue(grid.Id, "rows", Payload(Array.Empty<string[]>()));
        preview.EditGrid(grid.Id, "addRow", Payload(new { row = new[] { "Angel", "Engineering" } }));
        preview.EditGrid(grid.Id, "addRow", Payload(new { row = new[] { "Jane", "Design" } }));
        preview.EditGrid(grid.Id, "addRow", Payload(new { row = new[] { "John" } }));
        grid.SelectedRow = 2;
        preview.EditGrid(grid.Id, "updateRow", Payload(new { index = 1, row = new[] { "Jane", "HR" } }));
        preview.EditGrid(grid.Id, "setCell", Payload(new { rowIndex = 2, columnIndex = 1, value = "Operations" }));
        Assert.True(grid.ReadOnly); // Programmatic data changes are allowed in a read-only grid.
        Assert.Equal("Operations", grid.Rows[2][1]);
        preview.EditGrid(grid.Id, "removeRow", Payload(new { index = 0 }));
        Assert.Equal(1, grid.SelectedRow);
        Assert.Equal("Jane", grid.Rows[0][0]);
        preview.SetValue(grid.Id, "sortingEnabled", Payload(false));
        preview.SetValue(grid.Id, "filteringEnabled", Payload(false));
        preview.SetValue(grid.Id, "readOnly", Payload(false));
        var state = Payload(preview.State()).GetProperty("controls").EnumerateArray().Single(c => c.GetProperty("id").GetString() == grid.Id);
        Assert.Equal("Department", state.GetProperty("columns")[1].GetString());
        Assert.Equal("HR", state.GetProperty("rows")[0][1].GetString());
        Assert.False(state.GetProperty("sortingEnabled").GetBoolean());
        Assert.False(state.GetProperty("filteringEnabled").GetBoolean());
        Assert.False(state.GetProperty("readOnly").GetBoolean());
        Assert.Throws<ArgumentException>(() => preview.SetValue(grid.Id, "rows", Payload(new object[] { new object[] { "Valid", 1 } })));
        Assert.Throws<ArgumentException>(() => preview.EditGrid(grid.Id, "removeRow", Payload(new { index = 99 })));
        Assert.Throws<ArgumentException>(() => preview.EditGrid(grid.Id, "setCell", Payload(new { rowIndex = 0, columnIndex = -1, value = "Bad" })));
        Assert.Throws<ArgumentException>(() => preview.EditGrid(grid.Id, "updateRow", Payload(new { index = 0, row = new object[] { "Bad", null! } })));
        Assert.Equal("HR", grid.Rows[0][1]);
        preview.EditGrid(grid.Id, "removeRow", Payload(new { index = 1 })); Assert.Equal(-1, grid.SelectedRow);
        preview.EditGrid(grid.Id, "clearRows", Payload(new { })); Assert.Empty(grid.Rows);
        Assert.Equal(new[] { "Name", "Value" }, original.Columns);
        Assert.Equal(2, original.Rows.Length);
    }

    [Theory]
    [InlineData("avatar")]
    [InlineData("image")]
    [InlineData("picturebox")]
    public void RuntimeImageSourceAcceptsPathsAndUrisWithoutChangingDesign(string kind)
    {
        var design = new BuilderViewModel(); design.CreateNew();
        var original = design.ExecuteEdit("drop", design.Form.Id,
            JsonSerializer.SerializeToElement(new { control = kind, x = 20, y = 30 })).AddedControl!;
        using var preview = new PreviewSession(design);
        var image = Assert.IsAssignableFrom<Forma.Core.Controls.Image>(preview.Controls.Single(c => c.Id == original.Id));
        var initial = Assert.IsAssignableFrom<Forma.Core.Controls.Image>(original).Source;
        foreach (var source in new[] { @"C:\Users\Angelzm\Pictures\My avatar.png", "https://example.com/avatar.png", "file:///C:/Pictures/avatar.png", "data:image/png;base64,AAAA", "" }) {
            preview.SetValue(image.Id, "source", JsonSerializer.SerializeToElement(source));
            Assert.Equal(Path.IsPathFullyQualified(source) ? new Uri(source).AbsoluteUri : source, image.Source);
            var state = JsonSerializer.SerializeToElement(preview.State());
            Assert.Equal(image.Source, state.GetProperty("controls").EnumerateArray().Single(c => c.GetProperty("id").GetString() == image.Id).GetProperty("source").GetString());
        }
        Assert.Equal(initial, Assert.IsAssignableFrom<Forma.Core.Controls.Image>(original).Source);
        preview.SetValue(image.Id, "text", JsonSerializer.SerializeToElement("New initials"));
        Assert.Equal("", image.Source);
        foreach (var invalid in new[] { "javascript:alert(1)", "data:text/html,test", "relative.png", "bad\0path" })
            Assert.Throws<ArgumentException>(() => preview.SetValue(image.Id, "source", JsonSerializer.SerializeToElement(invalid)));
        Assert.Throws<ArgumentException>(() => preview.SetValue(image.Id, "source", JsonSerializer.SerializeToElement(42)));
    }

    [Fact]
    public void RuntimeChangesAndAppearanceAreIsolatedFromTheDesign()
    {
        var design = new BuilderViewModel(); design.CreateNew();
        var button = design.ExecuteEdit("drop", design.Form.Id,
            JsonSerializer.SerializeToElement(new { control = "button", x = 20, y = 30 })).AddedControl!;
        design.MarkSaved();
        using var preview = new PreviewSession(design);
        var copy = preview.Controls.Single(control => control.Id == button.Id);
        Assert.NotSame(button, copy);
        Assert.Equal(button.X, copy.X);
        copy.Text = "Runtime change";
        preview.Appearance[button.Id].Width = 240;
        Assert.Equal("Continue", button.Text);
        Assert.Equal(120, design.Appearance[button.Id].Width);
        Assert.False(design.HasUnsavedChanges);
        Assert.False(design.PreviewMode);
    }

    [Fact]
    public void PreviewProjectsTabVisibilityAndStopsItsOwnTimers()
    {
        var design = new BuilderViewModel(); design.CreateNew();
        var tabs = design.ExecuteEdit("drop", design.Form.Id,
            JsonSerializer.SerializeToElement(new { control = "tabcontrol", x = 0, y = 0 })).AddedControl!;
        var timer = design.ExecuteEdit("drop", design.Form.Id,
            JsonSerializer.SerializeToElement(new { control = "timer", x = 0, y = 0 })).AddedControl!;
        design.Appearance[timer.Id].Enabled = true;
        using var preview = new PreviewSession(design);
        var runtimeTabs = Assert.IsType<TabControl>(preview.Controls.Single(control => control.Id == tabs.Id));
        runtimeTabs.SelectedTab = 1;
        var state = JsonSerializer.SerializeToElement(preview.State());
        var projectedTabs = state.GetProperty("controls").EnumerateArray().Single(control => control.GetProperty("id").GetString() == tabs.Id);
        Assert.Equal(1, projectedTabs.GetProperty("selectedTab").GetInt32());
        Assert.Equal("runtime-preview", state.GetProperty("action").GetString());
        preview.Start();
        var runtimeTimer = Assert.IsType<Forma.Core.Controls.Timer>(preview.Controls.Single(control => control.Id == timer.Id));
        Assert.True(runtimeTimer.Enabled);
        Assert.False(Assert.IsType<Forma.Core.Controls.Timer>(timer).Enabled);
        preview.Dispose();
        Assert.False(runtimeTimer.Enabled);
    }
}
