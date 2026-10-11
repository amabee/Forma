using System.Text.Json;
using Forma.Builder;
using Forma.Core.Controls;

namespace Forma.Tests;

public class ComponentCustomizationTests
{
    private static (BuilderViewModel Design, Control Button) Design()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        var button = model.ExecuteEdit("drop", model.Form.Id,
            JsonSerializer.SerializeToElement(new { control = "button", x = 20, y = 30 })).AddedControl!;
        return (model, button);
    }

    [Fact]
    public void ApplyingSourcesIsUndoablePortableAndBlockedForLockedControls()
    {
        var (model, button) = Design();
        var template = ComponentEditorService.Template(button, model.Appearance[button.Id]);
        Assert.Contains(model.Appearance[button.Id].BackColor, template.Css);
        Assert.Contains("forma.on", template.Behavior);
        template.Events = "export class ButtonEventHandler { static button1_Click(event) {} }";
        model.ExecuteEdit("customize", button.Id, JsonSerializer.SerializeToElement(template));
        var copy = ProjectFile.Restore(ProjectFile.Capture(model.Form!, control => JsonSerializer.SerializeToElement(model.Appearance[control.Id])));
        var restored = copy.Appearance[button.Id].Deserialize<Appearance>()!;
        Assert.Equal(template.Css, restored.Customization!.Css);
        Assert.Equal(template.Behavior, restored.Customization.Behavior);
        Assert.Equal(template.Events, restored.Customization.Events);
        Assert.Equal(template.Characteristics, restored.Customization.Characteristics);
        var previous = ProjectFile.Restore(ProjectFile.Parse(model.Undo()!.Json));
        Assert.Null(previous.Appearance[button.Id].Deserialize<Appearance>()!.Customization);
        model.Appearance[button.Id].Locked = true;
        model.ExecuteEdit("customize", button.Id, JsonSerializer.SerializeToElement(new ComponentCustomization { Css = ":host { color: red; }" }));
        Assert.Equal(template.Css, model.Appearance[button.Id].Customization!.Css);
    }

    [Fact]
    public void InvalidCustomValuesDoNotPartiallyApplyAndExternalFilesRoundTrip()
    {
        var (model, button) = Design();
        Assert.Throws<ArgumentException>(() => model.ExecuteEdit("customize", button.Id,
            JsonSerializer.SerializeToElement(new ComponentCustomization { Css = "new", Characteristics = "[]" })));
        Assert.Null(model.Appearance[button.Id].Customization);
        var folder = Path.Combine(Path.GetTempPath(), "forma-component-test-" + Guid.NewGuid());
        try
        {
            var source = ComponentEditorService.Template(button, model.Appearance[button.Id]);
            source.Events = "export class ButtonEventHandler {}";
            ComponentEditorService.Write(folder, source);
            Assert.True(File.Exists(Path.Combine(folder, "script.js")));
            Assert.True(File.Exists(Path.Combine(folder, "event.js")));
            File.WriteAllText(Path.Combine(folder, "script.js"), "api.set(component.id, 'text', 'Updated');");
            var reloaded = ComponentEditorService.Read(folder);
            Assert.Equal(source.Css, reloaded.Css);
            Assert.Equal(source.Events, reloaded.Events);
            Assert.Contains("Updated", reloaded.Behavior);
            File.Delete(Path.Combine(folder, "event.js"));
            Assert.Equal("", ComponentEditorService.Read(folder).Events);
        }
        finally { Directory.Delete(folder, true); }
    }

    [Fact]
    public void PropertyOriginsDescribeAcceptedCoalescedWritesAndAreDrainedWithState()
    {
        var (model, button) = Design();
        using var preview = new PreviewSession(model);
        preview.SetValue(button.Id, "text", JsonSerializer.SerializeToElement("First"));
        preview.SetValue(button.Id, "text", JsonSerializer.SerializeToElement("Final"), "binding");
        var state = JsonSerializer.SerializeToElement(preview.State());
        var origins = state.GetProperty("propertyOrigins");
        Assert.Equal(1, origins.GetArrayLength());
        Assert.Equal(button.Id, origins[0].GetProperty("id").GetString());
        Assert.Equal("text", origins[0].GetProperty("property").GetString());
        Assert.Equal("binding", origins[0].GetProperty("origin").GetString());
        Assert.Equal(0, JsonSerializer.SerializeToElement(preview.State()).GetProperty("propertyOrigins").GetArrayLength());
        Assert.Throws<ArgumentException>(() => preview.SetValue(button.Id, "text", JsonSerializer.SerializeToElement("Rejected"), "user"));
        Assert.Throws<ArgumentException>(() => preview.SetValue(button.Id, "text", JsonSerializer.SerializeToElement(3), "binding"));
        Assert.Equal("Final", preview.Controls.Single(control => control.Id == button.Id).Text);
        Assert.Equal(0, JsonSerializer.SerializeToElement(preview.State()).GetProperty("propertyOrigins").GetArrayLength());
    }

    [Fact]
    public void ScriptCommandsMarkTheirResultingPropertyChangesAsProgrammatic()
    {
        var (model, _) = Design();
        string Add(string kind) => model.ExecuteEdit("drop", model.Form!.Id,
            JsonSerializer.SerializeToElement(new { control = kind, x = 20, y = 30 })).AddedControl!.Id;
        var toast = Add("toast"); var dialog = Add("dialog"); var grid = Add("datagridview");
        using var preview = new PreviewSession(model);
        preview.ShowToast(toast, JsonSerializer.SerializeToElement(new { duration = 0 }));
        preview.ShowDialog(dialog);
        preview.EditGrid(grid, "addRow", JsonSerializer.SerializeToElement(new { row = new[] { "Alice", "Engineering" } }));
        var origins = JsonSerializer.SerializeToElement(preview.State()).GetProperty("propertyOrigins").EnumerateArray().ToArray();
        Assert.Contains(origins, change => change.GetProperty("id").GetString() == toast && change.GetProperty("property").GetString() == "isOpen");
        Assert.Contains(origins, change => change.GetProperty("id").GetString() == dialog && change.GetProperty("property").GetString() == "isOpen");
        Assert.Contains(origins, change => change.GetProperty("id").GetString() == grid && change.GetProperty("property").GetString() == "rows");
        Assert.All(origins, change => Assert.Equal("programmatic", change.GetProperty("origin").GetString()));
        preview.CloseToast(toast);
        Assert.Equal("programmatic", JsonSerializer.SerializeToElement(preview.State()).GetProperty("propertyOrigins")[0].GetProperty("origin").GetString());
    }

    [Fact]
    public void ScriptSettersChangeOnlyTheRuntimeModelAndRejectWrongTypes()
    {
        var (model, button) = Design();
        using var preview = new PreviewSession(model);
        preview.SetValue(button.Id, "text", JsonSerializer.SerializeToElement("From script"));
        preview.SetValue(button.Id, "visible", JsonSerializer.SerializeToElement(false));
        Assert.Equal("From script", preview.Controls.Single(control => control.Id == button.Id).Text);
        Assert.Equal("Continue", button.Text);
        Assert.True(model.Appearance[button.Id].Visible);
        Assert.False(preview.Appearance[button.Id].Visible);
        Assert.Throws<ArgumentException>(() => preview.SetValue(button.Id, "text", JsonSerializer.SerializeToElement(3)));
        preview.SetValue(button.Id, "width", JsonSerializer.SerializeToElement(30));
        Assert.Equal(30, preview.Appearance[button.Id].Width);
        Assert.NotEqual(30, model.Appearance[button.Id].Width);
        Assert.Throws<ArgumentException>(() => preview.SetValue(button.Id, "width", JsonSerializer.SerializeToElement("30")));
    }
}
