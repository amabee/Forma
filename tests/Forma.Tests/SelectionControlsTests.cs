using System.Text.Json;
using Forma.Builder;
using Forma.Core.Controls;
using Forma.Core.Rendering;
using Forma.WebView2;

namespace Forma.Tests;

public class SelectionControlsTests
{
    private static JsonElement Payload(object value) => JsonSerializer.SerializeToElement(value);
    [Fact]
    public void ChipAndButtonModelsPersistSettingsAndRuntimeChangesStayIsolated()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        foreach (var kind in new[] { "chip", "chipgroup", "buttongroup", "iconbutton", "floatingactionbutton" })
            Assert.NotNull(model.ExecuteEdit("drop", model.Form.Id, Payload(new { control = kind, x = 5, y = 5 })).AddedControl);
        var chip = model.Form.Children.OfType<Chip>().Single();
        model.ExecuteEdit("select", chip.Id, default);
        model.ExecuteEdit("property", chip.Id, Payload(new { property = "removable", value = true }));
        model.ExecuteEdit("property", chip.Id, Payload(new { property = "variant", value = "success" }));
        chip.Checked = true;
        var group = model.Form.Children.OfType<ChipGroup>().Single(); group.Items = ["A", "B"]; group.CheckedIndices = [1]; group.Orientation = "vertical";
        var button = model.Form.Children.OfType<IconButton>().First(b => b is not FloatingActionButton); button.IconName = "settings"; button.ShowText = true;
        chip.Remove(); Assert.True(chip.IsRemoved);
        var restored = ProjectFile.Restore(ProjectFile.Parse(ProjectFile.Serialize(model.CaptureProject()))).Form;
        var copy = restored.Children.OfType<Chip>().Single();
        Assert.False(copy.IsRemoved); Assert.True(copy.Checked); Assert.True(copy.Removable); Assert.Equal("success", copy.Variant);
        var groupCopy = restored.Children.OfType<ChipGroup>().Single(); Assert.Equal(new[] { 1 }, groupCopy.CheckedIndices); Assert.Equal("vertical", groupCopy.Orientation);
        var iconCopy = restored.Children.OfType<IconButton>().First(b => b is not FloatingActionButton); Assert.Equal("settings", iconCopy.IconName); Assert.True(iconCopy.ShowText);
        using var preview = new PreviewSession(model);
        preview.SetValue(chip.Id, "checked", Payload(false)); preview.SetValue(chip.Id, "isRemoved", Payload(true));
        preview.SetValue(button.Id, "iconName", Payload("house")); preview.SetValue(button.Id, "showText", Payload(false));
        Assert.True(chip.Checked); Assert.Equal("settings", button.IconName);
        Assert.True(preview.Controls.OfType<Chip>().Single().IsRemoved);
        preview.SetValue(chip.Id, "isRemoved", Payload(false)); Assert.False(preview.Controls.OfType<Chip>().Single().IsRemoved);
        Assert.Throws<ArgumentException>(() => preview.SetValue(button.Id, "iconName", Payload("unknown")));
        Assert.Contains("removable", InspectorCatalog.ForKind("chip").Select(p => p.Id));
        Assert.Contains("showText", InspectorCatalog.ForKind("floatingactionbutton").Select(p => p.Id));
    }
    [Fact]
    public async Task ChipRemovalAndIconButtonClicksReachCoreEvents()
    {
        var bridge = new FakeBridge(); var renderer = new WebView2Renderer(bridge);
        var chip = new Chip(); var button = new IconButton();
        await renderer.RenderAsync(chip); await renderer.RenderAsync(button);
        var removed = 0; var clicks = 0; chip.Removed += (_, _) => removed++; button.Click += (_, _) => clicks++;
        void Send(Control control, string name) => bridge.Receive(new BridgeMessage { Type = "event", Id = control.Id, Event = name, Payload = Payload(new { }) });
        Send(chip, "chip-remove"); Assert.False(chip.IsRemoved);
        chip.Removable = true; Send(chip, "chip-remove"); Send(chip, "chip-remove"); Assert.Equal(1, removed);
        Send(button, "click"); Assert.Equal(1, clicks); button.Enabled = false; Send(button, "click"); Assert.Equal(1, clicks);
    }
    [Fact]
    public void NewSelectionControlsPersistInspectorEditsAndUndoRedo()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        foreach (var kind in new[] { "radiogroup", "checkboxgroup", "segmentedcontrol", "rating" })
            Assert.NotNull(model.ExecuteEdit("drop", model.Form.Id, Payload(new { control = kind, x = 10, y = 10 })).AddedControl);
        var radio = model.Form.Children.OfType<RadioGroup>().Single();
        model.ExecuteEdit("select", radio.Id, default);
        model.ExecuteEdit("property", radio.Id, Payload(new { property = "items", value = "Apple\nPear" }));
        model.ExecuteEdit("property", radio.Id, Payload(new { property = "orientation", value = "vertical" }));
        model.ExecuteEdit("property", radio.Id, Payload(new { property = "selectedIndex", value = 1 }));
        var undone = ProjectFile.Restore(ProjectFile.Parse(model.Undo()!.Json));
        Assert.Equal(0, undone.Form.Children.OfType<RadioGroup>().Single().SelectedIndex);
        var redone = ProjectFile.Restore(ProjectFile.Parse(model.Redo()!.Json));
        Assert.Equal(1, redone.Form.Children.OfType<RadioGroup>().Single().SelectedIndex);
        var checks = model.Form.Children.OfType<CheckBoxGroup>().Single();
        model.ExecuteEdit("select", checks.Id, default);
        model.ExecuteEdit("property", checks.Id, Payload(new { property = "checkedIndices", value = "2, 0, 2" }));
        var rating = model.Form.Children.OfType<Rating>().Single();
        model.ExecuteEdit("select", rating.Id, default);
        model.ExecuteEdit("property", rating.Id, Payload(new { property = "stars", value = 3 }));
        model.ExecuteEdit("property", rating.Id, Payload(new { property = "number", value = 9 }));
        model.ExecuteEdit("property", rating.Id, Payload(new { property = "readOnly", value = true }));
        var saved = ProjectFile.Capture(model.Form, c => Payload(model.Appearance[c.Id]));
        var restored = ProjectFile.Restore(ProjectFile.Parse(ProjectFile.Serialize(saved))).Form;
        var restoredRadio = restored.Children.OfType<RadioGroup>().Single();
        Assert.Equal(new[] { "Apple", "Pear" }, restoredRadio.Items); Assert.Equal(1, restoredRadio.SelectedIndex); Assert.Equal("vertical", restoredRadio.Orientation);
        Assert.Equal(new[] { 0, 2 }, restored.Children.OfType<CheckBoxGroup>().Single().CheckedIndices);
        var restoredRating = restored.Children.OfType<Rating>().Single();
        Assert.Equal(3, restoredRating.Stars); Assert.Equal(3, restoredRating.Value); Assert.True(restoredRating.ReadOnly);
        using var preview = new PreviewSession(model);
        preview.SetValue(radio.Id, "items", Payload(new[] { "New" }));
        preview.SetValue(checks.Id, "checkedIndices", Payload(new[] { 1 }));
        preview.SetValue(rating.Id, "value", Payload(1.7));
        Assert.Equal(2, preview.Controls.OfType<Rating>().Single().Value);
        Assert.Equal(new[] { "Apple", "Pear" }, radio.Items);
        Assert.Contains("items", InspectorCatalog.ForKind("radiogroup").Select(p => p.Id));
        Assert.Contains("stars", InspectorCatalog.ForKind("rating").Select(p => p.Id));
    }
    [Fact]
    public async Task RendererRoutesGroupAndRatingInteractionsToModelAndHonorsRatingReadOnly()
    {
        var bridge = new FakeBridge(); var renderer = new WebView2Renderer(bridge);
        var radio = new RadioGroup(); var checks = new CheckBoxGroup(); var rating = new Rating();
        await renderer.RenderAsync(radio); await renderer.RenderAsync(checks); await renderer.RenderAsync(rating);
        void Send(Control control, string name, object payload) => bridge.Receive(new BridgeMessage { Type = "event", Id = control.Id, Event = name, Payload = Payload(payload) });
        Send(radio, "selection", new { selectedIndex = 2 }); Assert.Equal(2, radio.SelectedIndex);
        var changed = 0; checks.ItemCheck += (_, _) => changed++;
        Send(checks, "item-check", new { index = 1, @checked = true }); Assert.Equal(new[] { 1 }, checks.CheckedIndices); Assert.Equal(1, changed);
        Send(rating, "value", new { value = 4 }); Assert.Equal(4, rating.Value);
        rating.ReadOnly = true; Send(rating, "value", new { value = 1 }); Assert.Equal(4, rating.Value);
        rating.Stars = 2; Assert.Equal(2, rating.Value);
    }
}
