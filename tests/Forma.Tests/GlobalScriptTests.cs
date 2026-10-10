using System.Text.Json;
using Forma.Builder;

namespace Forma.Tests;

public class GlobalScriptTests
{
    [Fact]
    public void GlobalScriptPersistsWithProjectParticipatesInHistoryAndIsCopiedIntoPreview()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        var source = "forma.provide('app', { count: forma.ref(0) });";
        Assert.Equal(new[] { "app" }, ComponentEditorService.ModuleNames(source));
        model.ExecuteEdit("global-script", model.Form.Id, JsonSerializer.SerializeToElement(new { script = source }));
        Assert.True(model.HasUnsavedChanges);
        var saved = ProjectFile.Restore(ProjectFile.Parse(ProjectFile.Serialize(model.CaptureProject())));
        Assert.Equal(source, saved.Appearance[model.Form.Id].Deserialize<Appearance>()!.GlobalScript);
        var before = ProjectFile.Restore(ProjectFile.Parse(model.Undo()!.Json));
        Assert.Equal("", before.Appearance[model.Form.Id].Deserialize<Appearance>()!.GlobalScript);
        using var preview = new PreviewSession(model);
        Assert.Equal(source, JsonSerializer.SerializeToElement(preview.State()).GetProperty("globalScript").GetString());
        model.ExecuteEdit("global-script", model.Form.Id, JsonSerializer.SerializeToElement(new { script = "// changed" }));
        Assert.Equal(source, JsonSerializer.SerializeToElement(preview.State()).GetProperty("globalScript").GetString());
        Assert.Throws<ArgumentException>(() => model.ExecuteEdit("global-script", model.Form.Id, JsonSerializer.SerializeToElement(new { script = new string('x', 200001) })));
        Assert.Equal("// changed", model.Appearance[model.Form.Id].GlobalScript);
    }

    [Fact]
    public void LegacyBehaviorFilesStillReadAndNewSavesPreferScriptFile()
    {
        var folder = Path.Combine(Path.GetTempPath(), "forma-script-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "component.css"), "");
            File.WriteAllText(Path.Combine(folder, "behavior.js"), "// legacy");
            File.WriteAllText(Path.Combine(folder, "custom-properties.json"), "{}");
            Assert.Equal("// legacy", ComponentEditorService.Read(folder).Behavior);
            ComponentEditorService.Write(folder, new() { Behavior = "// current" });
            Assert.Equal("// current", ComponentEditorService.Read(folder).Behavior);
            Assert.Equal("// current", File.ReadAllText(Path.Combine(folder, "script.js")));
            Assert.Equal("// legacy", File.ReadAllText(Path.Combine(folder, "behavior.js")));
        }
        finally { Directory.Delete(folder, true); }
    }
}
