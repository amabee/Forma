using System.Text.Json;
using Forma.Builder;
using Forma.Core.Controls;
using Forma.WebView2;

namespace Forma.Tests;

public class DisplayControlTests
{
    private static JsonElement Payload(object value) => JsonSerializer.SerializeToElement(value);

    [Fact]
    public async Task DisplayPropertiesReachRendererAndSurviveSaveOpenAndUndo()
    {
        var design = new BuilderViewModel(); design.CreateNew();
        Control Add(string kind) => design.ExecuteEdit("drop", design.Form!.Id, Payload(new { control = kind, x = 0, y = 0 })).AddedControl!;
        void Set(Control c, string property, object value) { design.SelectedControl = c; design.ExecuteEdit("property", c.Id, Payload(new { property, value })); }
        var icon = Assert.IsType<Icon>(Add("icon")); var empty = Assert.IsType<EmptyState>(Add("emptystate")); var skeleton = Assert.IsType<Skeleton>(Add("skeleton"));
        var bridge = new FakeBridge(); var renderer = new WebView2Renderer(bridge); await renderer.RenderAsync(design.Form!); bridge.Clear();
        Set(icon, "iconName", "search"); Set(icon, "strokeWidth", 3);
        Set(empty, "description", "Try another search"); Set(empty, "iconName", "folder-open");
        Set(skeleton, "shape", "circle"); Set(skeleton, "lines", 5); Set(skeleton, "isActive", false);
        Assert.Contains(bridge.Sent, m => m.Str("id") == icon.Id && m.Obj("properties").Str("iconName") == "search");
        Assert.Contains(bridge.Sent, m => m.Str("id") == skeleton.Id && !m.Obj("properties").GetProperty("isActive").GetBoolean());
        var restored = ProjectFile.Restore(ProjectFile.Capture(design.Form!, c => Payload(design.Appearance[c.Id]))).Form;
        Assert.Equal("search", Assert.IsType<Icon>(restored.Children[0]).IconName); Assert.Equal(3, Assert.IsType<Icon>(restored.Children[0]).StrokeWidth);
        Assert.Equal("Try another search", Assert.IsType<EmptyState>(restored.Children[1]).Description);
        var restoredSkeleton = Assert.IsType<Skeleton>(restored.Children[2]); Assert.Equal("circle", restoredSkeleton.Shape); Assert.Equal(5, restoredSkeleton.Lines); Assert.False(restoredSkeleton.IsActive);
        var undone = ProjectFile.Restore(ProjectFile.Parse(design.Undo()!.Json)).Form;
        Assert.True(Assert.IsType<Skeleton>(undone.Children[2]).IsActive);
        var redone = ProjectFile.Restore(ProjectFile.Parse(design.Redo()!.Json)).Form;
        Assert.False(Assert.IsType<Skeleton>(redone.Children[2]).IsActive);
        using var preview = new PreviewSession(design);
        Assert.Equal("circle", preview.Controls.OfType<Skeleton>().Single().Shape);
        Assert.Equal("search", preview.Controls.OfType<Icon>().Single().IconName);
        Assert.Contains("iconName", InspectorCatalog.ForKind("icon").Select(p => p.Id));
        Assert.Single(InspectorCatalog.ForKind("skeleton"), p => p.Id == "shape");
    }

    [Fact]
    public void DisplayModelsRejectInvalidIconsAndClampPlaceholderSettings()
    {
        var icon = new Icon { IconName = "search", StrokeWidth = 99 }; icon.IconName = "../remote.svg";
        Assert.Equal("search", icon.IconName); Assert.Equal(4, icon.StrokeWidth);
        var empty = new EmptyState { IconName = "lock" }; empty.IconName = "javascript:alert(1)"; Assert.Equal("lock", empty.IconName);
        var skeleton = new Skeleton { Lines = 99, Shape = "circle" }; Assert.Equal(10, skeleton.Lines);
        skeleton.Lines = -1; skeleton.Shape = "invalid"; Assert.Equal(1, skeleton.Lines); Assert.Equal("circle", skeleton.Shape);
    }
}
