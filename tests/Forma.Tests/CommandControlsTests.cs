using System.Text.Json;
using Forma.Builder;
using Forma.Core.Controls;
using Forma.Core.Rendering;
using Forma.WebView2;

namespace Forma.Tests;

public class CommandControlsTests
{
    [Fact]
    public void CommandTreesAreCopiedAndRejectDuplicateIdsAndExcessiveDepth()
    {
        var children = new[] { new CommandItem("open", "Open") };
        var menu = new MenuStrip { Items = [new("file", "File", Items: children)] };
        children[0] = new("changed", "Changed");
        Assert.Equal("open", menu.Items[0].Items![0].Id);
        var copy = menu.Items; copy[0].Items![0] = new("changed", "Changed");
        Assert.Equal("open", menu.Items[0].Items![0].Id);
        Assert.Throws<ArgumentException>(() => menu.Items = [new("same", "A"), new("same", "B")]);
        CommandItem tree = new("leaf", "Leaf");
        for (var index = 0; index < 8; index++) tree = new($"parent{index}", "Parent", Items: [tree]);
        Assert.Throws<ArgumentException>(() => menu.Items = [tree]);
        Assert.Throws<ArgumentException>(() => menu.Items = [new("separator", "", Separator: true, Items: [new("child", "Child")])]);
    }

    [Fact]
    public void DisabledParentsSeparatorsAndUnknownCommandsDoNotRaiseEvents()
    {
        var menu = new MenuStrip { Items = [
            new("disabled", "Disabled", Enabled: false, Items: [new("blocked", "Blocked")]),
            new("separator", "", Separator: true),
            new("toggle", "Show grid", CheckOnClick: true)
        ] };
        var clicked = new List<CommandItemClickedEventArgs>();
        menu.ItemClicked += (_, item) => clicked.Add(item);
        menu.InvokeItem("blocked"); menu.InvokeItem("separator"); menu.InvokeItem("unknown");
        Assert.Empty(clicked);
        menu.InvokeItem("toggle");
        Assert.True(Assert.Single(clicked).Checked);
        Assert.True(menu.Items[2].Checked);
        menu.InvokeItem("toggle");
        Assert.False(menu.Items[2].Checked);
        Assert.Equal(2, clicked.Count);
    }

    [Fact]
    public async Task RendererRoutesMenuClicksAndPublishesCheckChanges()
    {
        var menu = new MenuStrip { Items = [new("toggle", "Grid", CheckOnClick: true)] };
        var bridge = new FakeBridge(); var renderer = new WebView2Renderer(bridge);
        await renderer.InitializeAsync(); await renderer.RenderAsync(menu);
        bridge.Clear();
        bridge.Receive(new BridgeMessage { Type = "event", Id = menu.Id, Event = "command-item",
            Payload = JsonSerializer.SerializeToElement(new { itemId = "toggle" }) });
        Assert.True(menu.Items[0].Checked);
        Assert.True(bridge.Last().Obj("properties").GetProperty("commandItems")[0].GetProperty("Checked").GetBoolean());
    }

    [Theory]
    [InlineData("menustrip")]
    [InlineData("toolbar")]
    [InlineData("toolstrip")]
    public void DesignerCommandItemsPersistAndEditsAreUndoable(string kind)
    {
        var model = new BuilderViewModel(); model.CreateNew();
        var commands = (CommandControl)model.ExecuteEdit("drop", model.Form!.Id,
            JsonSerializer.SerializeToElement(new { control = kind, x = 20, y = 20 })).AddedControl!;
        var before = model.CaptureHistory();
        var items = new[] { new CommandItem("tools", "Tools", Items: [new("grid", "Grid", CheckOnClick: true, Checked: true)]) };
        model.ExecuteEdit("property", commands.Id, JsonSerializer.SerializeToElement(new { property = "commandItems", value = JsonSerializer.Serialize(items) }));
        var after = model.CaptureHistory();
        var restored = ProjectFile.Restore(ProjectFile.Parse(after.Json));
        var loaded = (CommandControl)Assert.Single(restored.Form.Children);
        Assert.Equal("grid", loaded.Items[0].Items![0].Id);
        Assert.True(loaded.Items[0].Items![0].Checked);
        Assert.Equal(before, model.Undo()); Assert.Equal(after, model.Redo());
        Assert.Contains(InspectorCatalog.ForKind(kind), p => p.Id == "commandItems");
    }

    [Fact]
    public void ToolbarOrientationAndStatusTextRoundTrip()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        var toolbar = (Toolbar)model.ExecuteEdit("drop", model.Form!.Id,
            JsonSerializer.SerializeToElement(new { control = "toolbar", x = 20, y = 20 })).AddedControl!;
        model.ExecuteEdit("property", toolbar.Id, JsonSerializer.SerializeToElement(new { property = "orientation", value = "vertical" }));
        var status = (StatusBar)model.ExecuteEdit("drop", model.Form.Id,
            JsonSerializer.SerializeToElement(new { control = "statusbar", x = 20, y = 60 })).AddedControl!;
        model.ExecuteEdit("property", status.Id, JsonSerializer.SerializeToElement(new { property = "rightText", value = "Ln 1" }));
        var restored = ProjectFile.Restore(model.CaptureProject());
        Assert.Equal("vertical", ((Toolbar)restored.Form.Children[0]).Orientation);
        Assert.Equal("Ready", restored.Form.Children[1].Text);
        Assert.Equal("Ln 1", ((StatusBar)restored.Form.Children[1]).RightText);
    }
}
