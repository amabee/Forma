using System.Text.Json;
using Forma.Builder;
using Forma.Core.Controls;

namespace Forma.Tests;

public class MultiFormTests
{
    private static JsonElement Payload(object value) => JsonSerializer.SerializeToElement(value);

    [Fact]
    public void AddingFormRetainsProjectPathDesignAndGlobalScriptAndSwitchingIsNotAnEdit()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        var first = model.Form;
        model.ExecuteEdit("drop", first.Id, Payload(new { control = "button", x = 20, y = 20 }));
        var button = first.Children.Single();
        model.ExecuteEdit("global-script", first.Id, Payload(new { script = "forma.provide('app', {});" }));
        model.ProjectPath = Path.Combine(Path.GetTempPath(), "Multi.forma"); model.MarkSaved();
        model.AddForm(); var second = model.Form;
        Assert.Equal(2, model.Forms.Count);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal("form2", second.Name);
        Assert.Same(button, first.Children.Single());
        Assert.EndsWith("Multi.forma", model.ProjectPath);
        Assert.True(model.HasUnsavedChanges);
        model.MarkSaved();
        Assert.True(model.SelectForm(first.Id));
        Assert.Same(button, model.SelectedControl);
        Assert.False(model.HasUnsavedChanges);
        Assert.True(model.SelectForm(second.Id));
        Assert.False(model.HasUnsavedChanges);
        using var preview = new PreviewSession(model);
        Assert.Equal(second.Id, preview.Form.Id);
        Assert.Equal(model.GlobalScript, preview.Appearance[preview.Form.Id].GlobalScript);
        Assert.Single(preview.Controls);
    }

    [Fact]
    public void SaveAndRestorePreservesEveryFormComponentAndScript()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        model.ExecuteEdit("drop", model.Form.Id, Payload(new { control = "label", x = 20, y = 20 }));
        var first = model.Form;
        model.ExecuteEdit("customize", first.Children[0].Id, Payload(new ComponentCustomization { Behavior = "// first script", Characteristics = "{}" }));
        model.AddForm(); var second = model.Form;
        model.ExecuteEdit("drop", second.Id, Payload(new { control = "textbox", x = 30, y = 30 }));
        model.ExecuteEdit("global-script", second.Id, Payload(new { script = "// project global" }));
        var project = model.CaptureProject(); Assert.Equal(2, project.Version);
        var restored = ProjectFile.RestoreProject(ProjectFile.Parse(ProjectFile.Serialize(project)));
        Assert.Equal(new[] { first.Id, second.Id }, restored.Forms.Select(form => form.Id));
        Assert.IsType<Label>(restored.Forms[0].Children[0]);
        Assert.IsType<TextBox>(restored.Forms[1].Children[0]);
        Assert.Equal("// first script", restored.Appearance[first.Children[0].Id].Deserialize<Appearance>()!.Customization!.Behavior);
        var reopened = new BuilderViewModel(); reopened.ApplyProject(restored.Forms, restored.Appearance.ToDictionary(pair => pair.Key, pair => pair.Value.Deserialize<Appearance>()!));
        Assert.Equal("// project global", reopened.GlobalScript);
        Assert.Throws<InvalidDataException>(() => ProjectFile.Restore(project));
    }

    [Fact]
    public void FormCreationUndoRedoRestoresWholeProjectAndActiveForm()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        var firstId = model.Form.Id;
        model.ProjectPath = "Existing.forma";
        model.AddForm(); var secondId = model.Form.Id;
        Restore(model, model.Undo()!);
        Assert.Single(model.Forms); Assert.Equal(firstId, model.Form.Id);
        Assert.Equal("Existing.forma", model.ProjectPath);
        Restore(model, model.Redo()!);
        Assert.Equal(2, model.Forms.Count); Assert.Equal(secondId, model.Form.Id);
        model.SelectForm(firstId);
        model.ExecuteEdit("drop", firstId, Payload(new { control = "button", x = 20, y = 20 }));
        model.SelectForm(secondId);
        Restore(model, model.Undo()!);
        Assert.Equal(firstId, model.Form.Id);
        Assert.Empty(model.Forms[0].Children);
        Assert.Equal(2, model.Forms.Count);
    }

    [Fact]
    public void InvalidMultiFormProjectsRejectDuplicateIdsAndNamesAndEmptyCollections()
    {
        var model = new BuilderViewModel(); model.CreateNew(); model.AddForm();
        var project = model.CaptureProject();
        project.Forms[1].Id = project.Forms[0].Id;
        Assert.Throws<InvalidDataException>(() => ProjectFile.RestoreProject(project));
        project = model.CaptureProject();
        project.Forms[1].Properties["Name"] = project.Forms[0].Properties["Name"];
        Assert.Throws<InvalidDataException>(() => ProjectFile.RestoreProject(project));
        Assert.Throws<InvalidDataException>(() => ProjectFile.RestoreProject(new ProjectDocument { Version = 2 }));
        Assert.Throws<ArgumentException>(() => model.ExecuteEdit("property", model.Form.Id, Payload(new { property = "name", value = "form1" })));
    }

    [Fact]
    public void LegacyFilesRemainSingleFormProjectsAndNewProjectResetsAllForms()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        var legacy = model.CaptureProject(); Assert.Equal(1, legacy.Version);
        var restored = ProjectFile.RestoreProject(legacy); Assert.Single(restored.Forms);
        model.AddForm(); model.ProjectPath = "Existing.forma"; model.CreateNew();
        Assert.Single(model.Forms); Assert.Single(model.Appearance);
        Assert.Null(model.ProjectPath); Assert.False(model.HasUnsavedChanges); Assert.False(model.CanUndo);
    }

    private static void Restore(BuilderViewModel model, DesignSnapshot snapshot)
    {
        var restored = ProjectFile.RestoreProject(ProjectFile.Parse(snapshot.Json));
        model.ApplyProject(restored.Forms, restored.Appearance.ToDictionary(pair => pair.Key, pair => pair.Value.Deserialize<Appearance>()!), snapshot.ActiveFormId, snapshot.SelectedId);
    }
}
