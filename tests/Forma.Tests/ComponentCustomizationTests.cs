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
        Assert.Contains("api.on", template.Behavior);
        model.ExecuteEdit("customize", button.Id, JsonSerializer.SerializeToElement(template));
        var copy = ProjectFile.Restore(ProjectFile.Capture(model.Form!, control => JsonSerializer.SerializeToElement(model.Appearance[control.Id])));
        var restored = copy.Appearance[button.Id].Deserialize<Appearance>()!;
        Assert.Equal(template.Css, restored.Customization!.Css);
        Assert.Equal(template.Behavior, restored.Customization.Behavior);
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
            ComponentEditorService.Write(folder, source);
            File.WriteAllText(Path.Combine(folder, "behavior.js"), "api.set(component.id, 'text', 'Updated');");
            var reloaded = ComponentEditorService.Read(folder);
            Assert.Equal(source.Css, reloaded.Css);
            Assert.Contains("Updated", reloaded.Behavior);
        }
        finally { Directory.Delete(folder, true); }
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
        Assert.Throws<ArgumentException>(() => preview.SetValue(button.Id, "width", JsonSerializer.SerializeToElement(30)));
    }
}
