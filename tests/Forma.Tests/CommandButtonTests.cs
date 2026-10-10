using System.Text.Json;
using Forma.Builder;
using Forma.Core.Controls;
using Forma.Core.Rendering;
using Forma.WebView2;

namespace Forma.Tests;

public class CommandButtonTests
{
    private static JsonElement Payload(object value) => JsonSerializer.SerializeToElement(value);
    [Fact]
    public void CommandButtonsPersistInspectorPropertiesAndSupportDynamicCommands()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        foreach (var kind in new[] { "dropdownbutton", "splitbutton", "commandbutton" })
            Assert.NotNull(model.ExecuteEdit("drop", model.Form.Id, Payload(new { control = kind, x = 10, y = 10 })).AddedControl);
        var split = model.Form.Children.OfType<SplitButton>().Single();
        model.ExecuteEdit("select", split.Id, default);
        model.ExecuteEdit("property", split.Id, Payload(new { property = "commandItems", value = "[{\"Id\":\"save\",\"Text\":\"Save\",\"CheckOnClick\":true}]" }));
        model.ExecuteEdit("property", split.Id, Payload(new { property = "primaryEnabled", value = false }));
        var undone = ProjectFile.Restore(ProjectFile.Parse(model.Undo()!.Json)).Form.Children.OfType<SplitButton>().Single(); Assert.True(undone.PrimaryEnabled);
        model.Redo();
        var command = model.Form.Children.OfType<CommandButton>().Single(); command.Description = "Save the current document"; command.IconName = "settings";
        var restored = ProjectFile.Restore(ProjectFile.Parse(ProjectFile.Serialize(model.CaptureProject()))).Form;
        var restoredSplit = restored.Children.OfType<SplitButton>().Single(); Assert.False(restoredSplit.PrimaryEnabled); Assert.Equal("save", restoredSplit.Items.Single().Id);
        Assert.Equal("Save the current document", restored.Children.OfType<CommandButton>().Single().Description);
        Assert.Contains("primary-click", ComponentEditorService.Template(split, model.Appearance[split.Id]).Behavior);
        var dropdown = model.Form.Children.OfType<DropdownButton>().First(control => control is not SplitButton);
        Assert.Contains("command-item", ComponentEditorService.Template(dropdown, model.Appearance[dropdown.Id]).Behavior);
        using var preview = new PreviewSession(model);
        preview.SetValue(split.Id, "commandItems", Payload(new[] { new { id = "open", text = "Open" } }));
        Assert.Equal("save", split.Items.Single().Id);
        var runtime = preview.Controls.OfType<SplitButton>().Single(); Assert.Equal("open", runtime.Items.Single().Id);
        Assert.Throws<ArgumentException>(() => preview.SetValue(split.Id, "commandItems", Payload(new[] { new { id = "duplicate", text = "A" }, new { id = "duplicate", text = "B" } })));
        Assert.Equal("open", runtime.Items.Single().Id);
        preview.SetValue(split.Id, "primaryEnabled", Payload(true)); Assert.True(runtime.PrimaryEnabled);
        preview.SetValue(command.Id, "description", Payload("Runtime description")); Assert.Equal("Save the current document", command.Description);
    }
    [Fact]
    public async Task SplitPrimaryAndMenuEventsRemainSeparateAndDisabledCommandsAreRejected()
    {
        var bridge = new FakeBridge(); var renderer = new WebView2Renderer(bridge);
        var split = new SplitButton { Items = [new("save", "Save", CheckOnClick: true), new("disabled", "Disabled", Enabled: false)] };
        await renderer.RenderAsync(split); var primary = 0; var commands = 0; var checkedState = false;
        split.PrimaryClick += (_, _) => primary++;
        split.ItemClicked += (_, e) => { commands++; checkedState = e.Checked; };
        void Send(string name, object value) => bridge.Receive(new BridgeMessage { Type = "event", Id = split.Id, Event = name, Payload = Payload(value) });
        Send("primary-click", new { }); Assert.Equal(1, primary); Assert.Equal(0, commands);
        split.PrimaryEnabled = false; Send("primary-click", new { }); Assert.Equal(1, primary);
        Send("command-item", new { itemId = "disabled" }); Assert.Equal(0, commands);
        Send("command-item", new { itemId = "save" }); Assert.Equal(1, commands); Assert.True(checkedState);
        Send("command-item", new { itemId = "save" }); Assert.Equal(2, commands); Assert.False(checkedState);
    }
}
