using System.Text.Json;
using Forma.Core.Controls;
using Forma.Core.Rendering;
using Forma.WebView2;

namespace Forma.Tests;

public class ToolboxTests
{
    private static BridgeMessage Event(string id, string name, object payload) => new()
    { Type = "event", Id = id, Event = name, Payload = JsonSerializer.SerializeToElement(payload) };

    [Fact]
    public async Task RadioSelectionOnlyClearsSiblings()
    {
        var bridge = new FakeBridge(); var renderer = new WebView2Renderer(bridge);
        var root = new Panel(); var group = new GroupBox(); root.Add(group);
        var first = new RadioButton { Checked = true }; var second = new RadioButton();
        var separate = new RadioButton { Checked = true };
        group.Add(first); group.Add(second); root.Add(separate);
        await renderer.RenderAsync(root);
        bridge.Receive(Event(second.Id, "checked", new { @checked = true }));
        Assert.False(first.Checked); Assert.True(second.Checked); Assert.True(separate.Checked);
    }

    [Fact]
    public async Task ChoiceSelectionIsClampedAndItemsAreCopied()
    {
        var bridge = new FakeBridge(); var renderer = new WebView2Renderer(bridge);
        var source = new[] { "A", "B" }; var choice = new ComboBox { Items = source };
        source[0] = "mutated";
        await renderer.RenderAsync(choice);
        Assert.Equal("A", bridge.Single().Obj("properties").Obj("items")[0].GetString());
        bridge.Receive(Event(choice.Id, "selection", new { selectedIndex = 100 }));
        Assert.Equal(1, choice.SelectedIndex);
        choice.Items = [];
        Assert.Equal(-1, choice.SelectedIndex);
    }

    [Fact]
    public async Task GridIgnoresReadOnlyAndInvalidEditsThenAcceptsValidEdit()
    {
        var bridge = new FakeBridge(); var renderer = new WebView2Renderer(bridge);
        var grid = new DataGridView { Columns = ["Name"], Rows = [["Original"]] };
        await renderer.RenderAsync(grid);
        bridge.Receive(Event(grid.Id, "cell", new { row = 0, column = 0, value = "Ignored" }));
        Assert.Equal("Original", grid.Rows[0][0]);
        grid.ReadOnly = false;
        bridge.Receive(Event(grid.Id, "cell", new { row = -1, column = 0, value = "Ignored" }));
        bridge.Receive(Event(grid.Id, "cell", new { row = 0, column = 0, value = "Edited" }));
        Assert.Equal("Edited", grid.Rows[0][0]);
    }

    [Fact]
    public async Task TabAndLayoutPropertiesRoundTrip()
    {
        var bridge = new FakeBridge(); var renderer = new WebView2Renderer(bridge);
        var tabs = new TabControl { Tabs = ["First", "Second"] };
        var child = new Button { LayoutSlot = 2 }; tabs.Add(child);
        await renderer.RenderAsync(tabs);
        Assert.Equal(tabs.Id, bridge.Last().Str("parentId"));
        Assert.Equal(2, bridge.Last().Obj("properties").Obj("layoutSlot").GetInt32());
        bridge.Receive(Event(tabs.Id, "tab", new { selectedTab = 1 }));
        Assert.Equal(1, tabs.SelectedTab);
        tabs.Tabs = ["Only"];
        Assert.Equal(0, tabs.SelectedTab);
    }

    [Fact]
    public async Task TimerTicksWhenStartedAndCanStop()
    {
        using var timer = new Forma.Core.Controls.Timer { Interval = 10 };
        var tick = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        timer.Tick += (_, _) => tick.TrySetResult();
        Assert.False(timer.Enabled); timer.Start();
        await tick.Task.WaitAsync(TimeSpan.FromSeconds(5));
        timer.Stop(); Assert.False(timer.Enabled);
    }

    [Fact]
    public async Task WorkerSupportsCancellationAndReportsCompletion()
    {
        using var worker = new BackgroundWorker();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        WorkerCompletedEventArgs? result = null;
        worker.RunWorkerCompleted += (_, args) => result = args;
        var task = worker.RunAsync(async (token, progress) => {
            started.SetResult(); await Task.Delay(Timeout.Infinite, token);
        });
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(worker.IsBusy); worker.CancelAsync(); await task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(worker.IsBusy); Assert.True(result!.Cancelled); Assert.Null(result.Error);
    }
}
