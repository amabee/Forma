using System.Text.Json;
using Forma.Builder;
using Forma.Core.Controls;
using Forma.Core.Rendering;
using Forma.WebView2;

namespace Forma.Tests;

public class ContextAndDialogTests
{
    private static JsonElement Payload(object value) => JsonSerializer.SerializeToElement(value);
    private static Control Add(BuilderViewModel model, string kind) => model.ExecuteEdit("drop", model.Form!.Id,
        Payload(new { control = kind, x = 20, y = 20 })).AddedControl!;

    [Theory]
    [InlineData("contextmenu")]
    [InlineData("contextmenustrip")]
    public void ContextMenuTargetsPersistAndDeletingATargetResetsTheAssignment(string kind)
    {
        var model = new BuilderViewModel(); model.CreateNew();
        var button = Add(model, "button"); var menu = (ContextMenu)Add(model, kind);
        Assert.IsAssignableFrom<INonvisualControl>(menu);
        model.ExecuteEdit("property", menu.Id, Payload(new { property = "targetId", value = button.Id }));
        Assert.Equal(button.Id, menu.TargetId);
        var restored = ProjectFile.Restore(model.CaptureProject());
        Assert.Equal(button.Id, ((ContextMenu)restored.Form.Children[1]).TargetId);
        Assert.Throws<ArgumentException>(() => model.ExecuteEdit("property", menu.Id, Payload(new { property = "targetId", value = menu.Id })));
        model.ExecuteEdit("select", button.Id, default);
        model.ExecuteEdit("command", button.Id, Payload(new { command = "delete" }));
        Assert.Equal("", menu.TargetId);
        restored = ProjectFile.Restore(ProjectFile.Parse(model.Undo()!.Json));
        Assert.Equal(button.Id, ((ContextMenu)restored.Form.Children[1]).TargetId);
    }

    [Fact]
    public void DialogStateIsTransientAndClosingValidatesResults()
    {
        var dialog = new ConfirmationDialog(); var results = new List<string>();
        dialog.Closed += (_, e) => results.Add(e.Result);
        dialog.Show(); dialog.Show(); dialog.Close("Wrong");
        Assert.True(dialog.IsOpen); Assert.Empty(results);
        dialog.Close("Yes"); dialog.Close("No");
        Assert.False(dialog.IsOpen); Assert.Equal("Yes", dialog.Result); Assert.Equal(["Yes"], results);
        dialog.Show(); Assert.Equal("", dialog.Result);
        var form = new Forma.Core.Form(); form.Add(dialog);
        var restored = ProjectFile.Restore(ProjectFile.Capture(form, _ => Payload(new { Width = 100, Height = 100 })));
        var loaded = (ConfirmationDialog)Assert.Single(restored.Form.Children);
        Assert.False(loaded.IsOpen); Assert.Equal("", loaded.Result);
        Assert.Equal("YesNo", loaded.Buttons);
        Assert.Throws<ArgumentException>(() => dialog.Buttons = "Wrong");
    }

    [Fact]
    public void DesignerOnlyShowsEnabledDialogsInPreviewAndStopsThemOnExit()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        var dialog = (Dialog)Add(model, "dialog");
        model.MarkSaved(); model.ClearHistory();
        Assert.False(model.ShowDialog(dialog.Id));
        model.ExecuteEdit("preview", model.Form!.Id, Payload(new { enabled = true }));
        Assert.True(model.ShowDialog(dialog.Id)); Assert.True(dialog.IsOpen);
        Assert.False(model.HasUnsavedChanges); Assert.False(model.CanUndo);
        model.ExecuteEdit("preview", model.Form.Id, Payload(new { enabled = false }));
        Assert.False(dialog.IsOpen); Assert.False(model.HasUnsavedChanges);
        model.Appearance[dialog.Id].Enabled = false;
        model.ExecuteEdit("preview", model.Form.Id, Payload(new { enabled = true }));
        Assert.False(model.ShowDialog(dialog.Id));
    }

    [Fact]
    public async Task RendererPublishesDialogOpenStateAndRoutesResultMessages()
    {
        var dialog = new Dialog { CanCancel = false, Buttons = "OK" };
        var bridge = new FakeBridge(); var renderer = new WebView2Renderer(bridge);
        await renderer.InitializeAsync(); await renderer.RenderAsync(dialog);
        dialog.Show();
        Assert.True(bridge.Last().Obj("properties").GetProperty("isOpen").GetBoolean());
        bridge.Receive(new BridgeMessage { Type = "event", Id = dialog.Id, Event = "dialog-result", Payload = Payload(new { result = "Cancel" }) });
        Assert.True(dialog.IsOpen);
        bridge.Receive(new BridgeMessage { Type = "event", Id = dialog.Id, Event = "dialog-result", Payload = Payload(new { result = "OK" }) });
        Assert.False(dialog.IsOpen); Assert.Equal("OK", dialog.Result);
    }
}
