using System.Text.Json;
using Forma.Builder;

namespace Forma.Tests;

public class ProjectExplorerTests
{
    [Fact]
    public void TreeIncludesEveryFormNestedComponentsSourcesAndGlobalScript()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        model.ProjectPath = "Example.forma";
        model.ExecuteEdit("drop", model.Form.Id, JsonSerializer.SerializeToElement(new { control = "panel", x = 20, y = 20 }));
        var panel = model.SelectedControl!;
        model.ExecuteEdit("drop", panel.Id, JsonSerializer.SerializeToElement(new { control = "button", x = 10, y = 10 }));
        var button = model.SelectedControl!;
        model.AddForm();
        var tree = ProjectExplorerService.Build(model);
        Assert.Equal("Example", tree.Name);
        var forms = tree.Children!.Single(node => node.Key == "forms");
        Assert.Equal(2, forms.Children!.Length);
        var nestedPanel = forms.Children[0].Children!.Single(node => node.ControlId == panel.Id);
        var nestedButton = nestedPanel.Children!.Single(node => node.ControlId == button.Id);
        Assert.Equal(new[] { "view-css", "view-script", "view-custom-properties" }, nestedButton.Children!.Select(node => node.Command));
        Assert.Equal("edit-global-script", tree.Children!.Single(node => node.Key == "global-script").Command);
        // The bridge uses default JSON naming; tree properties must still be camelCase.
        Assert.True(JsonSerializer.SerializeToElement(tree).TryGetProperty("children", out _));
    }

    [Fact]
    public void AssetsAreReferencesWithoutEmbeddingSourcePayloadsAndLockedSourcesAreMarked()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        model.ExecuteEdit("drop", model.Form.Id, JsonSerializer.SerializeToElement(new { control = "image", x = 20, y = 20 }));
        var image = (Forma.Core.Controls.Image)model.SelectedControl!;
        image.Source = "data:image/png;base64,AQID";
        model.Appearance[image.Id].Locked = true;
        var tree = ProjectExplorerService.Build(model);
        var asset = tree.Children!.Single(node => node.Key == "assets").Children!.Single();
        Assert.Equal(image.Id, asset.ControlId);
        Assert.Contains("Embedded image", asset.Name);
        Assert.DoesNotContain("AQID", JsonSerializer.Serialize(tree));
        var imageNode = tree.Children!.Single(node => node.Key == "forms").Children![0].Children!.Single(node => node.ControlId == image.Id);
        Assert.All(imageNode.Children!, source => Assert.True(source.Locked));
    }
}
