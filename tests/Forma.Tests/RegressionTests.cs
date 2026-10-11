using System.Collections.Concurrent;
using System.Text.Json;
using Forma.Builder;
using Forma.Core.Controls;
using Forma.WebView2;

namespace Forma.Tests;

public class RegressionTests
{
    [Fact]
    public void GridPadsShortRowsWithEmptyCellsSoFilteringAndRowsStayValid()
    {
        var grid = new DataGridView { Columns = ["A", "B"], Rows = [["x"]], FilteringEnabled = true, ReadOnly = false };
        grid.SetCell(0, 0, "y");
        grid.FilterText = "q";
        Assert.Empty(grid.VisibleRowIndices);
        Assert.Equal(["y", ""], grid.Rows[0]);
        grid.Rows = grid.Rows;
    }

    [Fact]
    public void RadioButtonsAreExclusiveWhenSetFromCode()
    {
        var panel = new Panel();
        RadioButton first = new(), second = new();
        panel.Add(first); panel.Add(second);
        var otherPanel = new Panel(); var elsewhere = new RadioButton { Checked = true }; otherPanel.Add(elsewhere);
        first.Checked = true;
        second.Checked = true;
        Assert.False(first.Checked);
        Assert.True(second.Checked);
        Assert.True(elsewhere.Checked);
    }

    [Fact]
    public async Task ButtonEnabledNotifiesAndReachesTheRenderer()
    {
        var bridge = new FakeBridge(); var renderer = new WebView2Renderer(bridge);
        var button = new Button(); var changed = new List<string?>();
        button.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        await renderer.RenderAsync(button);
        button.Enabled = false;
        Assert.Contains(nameof(Button.Enabled), changed);
        Assert.Contains("\"enabled\":false", JsonSerializer.Serialize(bridge.Sent.Last()));
    }

    [Fact]
    public void TimerTicksThroughTheStartingContextAndNotAfterStop()
    {
        var context = new QueueContext(); var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            using var timer = new Forma.Core.Controls.Timer { Interval = 10 }; var ticks = 0;
            timer.Tick += (_, _) => ticks++;
            timer.Start();
            Assert.True(context.Posted.Wait(TimeSpan.FromSeconds(5)));
            context.RunAll();
            Assert.True(ticks > 0);
            context.Posted.Reset();
            Assert.True(context.Posted.Wait(TimeSpan.FromSeconds(5)));
            timer.Stop();
            var before = ticks;
            context.RunAll();
            Assert.Equal(before, ticks);
        }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
    }

    [Fact]
    public void MissingImagesStayLinkedInsteadOfBlockingCaptureAndSave()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        var missing = new Uri(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png")).AbsoluteUri;
        var image = Assert.IsType<Image>(model.ExecuteEdit("drop", model.Form.Id, JsonSerializer.SerializeToElement(new { control = "image", x = 8, y = 8 })).AddedControl);
        model.ExecuteEdit("image-source", image.Id, JsonSerializer.SerializeToElement(new { value = missing }));
        var skipped = new List<string>();
        var document = model.CaptureProject(embedImages: true, skipped);
        Assert.Equal([missing], skipped);
        Assert.Contains(missing, ProjectFile.Serialize(document));
        model.ExecuteEdit("move", image.Id, JsonSerializer.SerializeToElement(new { x = 20, y = 20 }));
        Assert.True(model.CanUndo);
    }

    [Fact]
    public void FailedEditsAreNotRecordedAndOnlyRollBackWhenStateChanged()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        var list = model.ExecuteEdit("drop", model.Form.Id, JsonSerializer.SerializeToElement(new { control = "checkedlistbox", x = 8, y = 8 })).AddedControl!;
        model.ClearHistory();
        Assert.ThrowsAny<FormatException>(() => model.ExecuteEdit("property", list.Id, JsonSerializer.SerializeToElement(new { property = "checkedIndices", value = "1,x" })));
        Assert.False(model.CanUndo);
        Assert.Null(model.TakeFailedEdit());
    }

    [Fact]
    public void ComponentCssTemplateDoesNotOverrideThePropertiesPanel()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        model.Appearance[model.Form.Id].BackColor = "#123456";
        var css = ComponentEditorService.Template(model.Form, model.Appearance[model.Form.Id]).Css;
        // Current values are listed for reference inside the leading comment only.
        Assert.Contains("#123456", css);
        var active = css[(css.IndexOf("*/", StringComparison.Ordinal) + 2)..];
        foreach (var declaration in new[] { "background-color", "color:", "padding", "border", "font-size" })
            Assert.DoesNotContain(declaration, active.Replace("/* Add a hover style here. */", ""));
    }

    [Fact]
    public void BorderSidesLimitTheBorderAndOnlyThoseEdgesTakeLayoutSpace()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        var panel = model.ExecuteEdit("drop", model.Form.Id, JsonSerializer.SerializeToElement(new { control = "panel", x = 0, y = 0 })).AddedControl!;
        void Set(string property, object value) => model.ExecuteEdit("property", panel.Id, JsonSerializer.SerializeToElement(new { property, value }));
        Set("borderWidth", 4);
        Set("borderSides", "bottom");
        var appearance = model.Appearance[panel.Id];
        Assert.Equal("bottom", appearance.BorderSides);
        Assert.Equal((0, 0, 4, 0), (appearance.BorderOn("top"), appearance.BorderOn("left"), appearance.BorderOn("bottom"), appearance.BorderOn("right")));
        // A docked child fills the full width because no side border takes horizontal space.
        var child = model.ExecuteEdit("drop", panel.Id, JsonSerializer.SerializeToElement(new { control = "button", x = 0, y = 0 })).AddedControl!;
        model.ExecuteEdit("property", child.Id, JsonSerializer.SerializeToElement(new { property = "dock", value = "fill" }));
        Assert.Equal(appearance.Width - appearance.PaddingLeft - appearance.PaddingRight, model.Appearance[child.Id].Width);
        Assert.Equal(appearance.Height - appearance.PaddingTop - appearance.PaddingBottom - 4, model.Appearance[child.Id].Height);
        // Unknown values are rejected and saved projects keep the setting.
        Set("borderSides", "diagonal");
        Assert.Equal("bottom", appearance.BorderSides);
        var json = ProjectFile.Serialize(model.CaptureProject());
        Assert.Contains("\"BorderSides\": \"bottom\"", json);
    }

    private sealed class QueueContext : SynchronizationContext
    {
        private readonly ConcurrentQueue<(SendOrPostCallback Callback, object? State)> _queue = new();
        public ManualResetEventSlim Posted { get; } = new();
        public override void Post(SendOrPostCallback d, object? state) { _queue.Enqueue((d, state)); Posted.Set(); }
        public void RunAll() { while (_queue.TryDequeue(out var item)) item.Callback(item.State); }
    }
}
