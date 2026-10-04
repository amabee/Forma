using System.Text.Json;
using Forma.Builder;
using Forma.Core.Controls;
using Forma.Core.Rendering;
using Forma.WebView2;

namespace Forma.Tests;

public class PickerAndPropertyGridTests
{
    [Fact]
    public void PickersNotifyChangesAndValidateFiltersAndPaths()
    {
        var picker = new FilePicker();
        var selected = 0; var browse = 0;
        picker.SelectedPathChanged += (_, _) => selected++;
        picker.BrowseRequested += (_, _) => browse++;
        picker.SelectedPath = "C:\\example.txt";
        picker.SelectedPath = picker.SelectedPath;
        picker.RequestBrowse();
        Assert.Equal(1, selected);
        Assert.Equal(1, browse);
        picker.Filter = "Images|*.png;*.jpg|All files|*.*";
        Assert.Throws<ArgumentException>(() => picker.Filter = "Missing pattern");
        Assert.Throws<ArgumentException>(() => picker.Filter = "Images|");
        Assert.Throws<ArgumentException>(() => picker.SelectedPath = "invalid\0path");
        Assert.Equal("Choose folder", new FolderPicker().DialogTitle);
    }

    [Fact]
    public void PropertyGridCopiesEntriesAndHonorsReadOnlyFlagsAndBounds()
    {
        var grid = new PropertyGrid();
        var entries = new[] { new PropertyEntry("Name", "Old"), new PropertyEntry("ID", "123", ReadOnly: true) };
        grid.Entries = entries;
        entries[0] = new("Other", "Changed");
        var events = new List<PropertyValueChangedEventArgs>();
        grid.PropertyValueChanged += (_, e) => events.Add(e);
        grid.SetEntryValue(0, "New");
        grid.SetEntryValue(0, "New");
        grid.SetEntryValue(1, "Blocked");
        grid.SetEntryValue(-1, "Blocked");
        grid.ReadOnly = true;
        grid.SetEntryValue(0, "Blocked");
        Assert.Equal("New", grid.Entries[0].Value);
        Assert.Equal("123", grid.Entries[1].Value);
        var edit = Assert.Single(events);
        Assert.Equal("Name", edit.Name);
        Assert.Equal(0, edit.Index);
        Assert.Throws<ArgumentException>(() => grid.Entries = [new("", "Invalid")]);
        var copy = grid.Entries; copy[0] = new("Other", "Changed");
        Assert.Equal("Name", grid.Entries[0].Name);
    }

    [Fact]
    public async Task RendererRoutesBrowseAndPropertyValueEventsAndRejectsMalformedIndices()
    {
        var bridge = new FakeBridge(); var renderer = new WebView2Renderer(bridge);
        var form = new Forma.Core.Form(); var picker = new FilePicker(); var grid = new PropertyGrid();
        form.Add(picker); form.Add(grid);
        var browse = 0; picker.BrowseRequested += (_, _) => browse++;
        await renderer.InitializeAsync(); await renderer.RenderAsync(form);
        Assert.Contains(bridge.Sent, m => m.Str("id") == grid.Id && m.Obj("properties").TryGetProperty("entries", out _));
        bridge.Receive(new BridgeMessage { Type = "event", Id = picker.Id, Event = "browse", Payload = JsonSerializer.SerializeToElement(new { }) });
        bridge.Receive(new BridgeMessage { Type = "event", Id = grid.Id, Event = "property-value", Payload = JsonSerializer.SerializeToElement(new { index = 0, value = "Edited" }) });
        bridge.Receive(new BridgeMessage { Type = "event", Id = grid.Id, Event = "property-value", Payload = JsonSerializer.SerializeToElement(new { index = "wrong", value = "Ignored" }) });
        Assert.Equal(1, browse);
        Assert.Equal("Edited", grid.Entries[0].Value);
        grid.ReadOnly = true;
        bridge.Receive(new BridgeMessage { Type = "event", Id = grid.Id, Event = "property-value", Payload = JsonSerializer.SerializeToElement(new { index = 0, value = "Blocked" }) });
        Assert.Equal("Edited", grid.Entries[0].Value);
    }

    [Theory]
    [InlineData("filepicker", "selectedPath", "C:\\example.txt")]
    [InlineData("folderpicker", "selectedPath", "C:\\Example")]
    [InlineData("propertygrid", "entries", "[{\"Name\":\"Title\",\"Value\":\"Example\",\"Category\":\"General\",\"ReadOnly\":false}]")]
    public void DesignerEditsRoundTripAndUndoForNewControls(string kind, string property, string value)
    {
        var model = new BuilderViewModel(); model.CreateNew();
        var control = model.ExecuteEdit("drop", model.Form!.Id, JsonSerializer.SerializeToElement(new { control = kind, x = 20, y = 20 })).AddedControl!;
        var before = model.CaptureHistory();
        model.ExecuteEdit("property", control.Id, JsonSerializer.SerializeToElement(new { property, value }));
        var after = model.CaptureHistory();
        var restored = ProjectFile.Restore(ProjectFile.Parse(after.Json));
        var loaded = Assert.Single(restored.Form.Children);
        Assert.Equal(control.Id, loaded.Id);
        if (loaded is PathPicker picker) Assert.Equal(value, picker.SelectedPath);
        else Assert.Equal("Example", Assert.Single(((PropertyGrid)loaded).Entries).Value);
        Assert.Equal(before, model.Undo());
        Assert.Equal(after, model.Redo());
        Assert.Contains(InspectorCatalog.ForKind(kind), p => p.Id == property);
    }
}
