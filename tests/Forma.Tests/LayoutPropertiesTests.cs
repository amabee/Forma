using System.Text.Json;
using Forma.Builder;
using Forma.Core.Controls;

namespace Forma.Tests;

public class LayoutPropertiesTests
{
    private static JsonElement Payload(object value) => JsonSerializer.SerializeToElement(value);
    private static Control Add(BuilderViewModel model, string kind, Control parent, int x = 0, int y = 0)
        => model.ExecuteEdit("drop", parent.Id, Payload(new { control = kind, x, y })).AddedControl!;
    private static void Set(BuilderViewModel model, Control control, string property, object value)
    {
        model.ExecuteEdit("select", control.Id, default);
        model.ExecuteEdit("property", control.Id, Payload(new { property, value }));
    }

    [Fact]
    public void ManagedResizeChangesSizeWithoutApplyingCornerCoordinates()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        foreach (var kind in new[] { "stackpanel", "flowlayoutpanel", "tablelayoutpanel", "sidebar", "appshell", "responsivepanel" })
        {
            var parent = Add(model, kind, model.Form);
            var child = Add(model, "button", parent, 17, 21);
            var x = child.X; var y = child.Y;
            model.ExecuteEdit("resize", child.Id, Payload(new { x = 99, y = 88, width = 150, height = 45 }));
            Assert.Equal(x, child.X); Assert.Equal(y, child.Y);
            Assert.InRange(model.Appearance[child.Id].Width, 121, 150);
        }
    }

    [Fact]
    public void DockReservesEdgesBeforeFillAndCanShrinkWithTheForm()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        var top = Add(model, "panel", model.Form); Set(model, top, "height", 40); Set(model, top, "dock", "top");
        var left = Add(model, "panel", model.Form); Set(model, left, "width", 180); Set(model, left, "dock", "left");
        var fill = Add(model, "panel", model.Form); Set(model, fill, "dock", "fill");
        Assert.Equal(0, top.Y); Assert.Equal(40, left.Y);
        Assert.Equal(180, fill.X); Assert.Equal(40, fill.Y);
        Assert.Equal(model.Appearance[model.Form.Id].Width - 180, model.Appearance[fill.Id].Width);
        model.ExecuteEdit("resize", model.Form.Id, Payload(new { width = 500, height = 300 }));
        Assert.Equal(500, model.Appearance[model.Form.Id].Width);
        Assert.Equal(500, model.Appearance[top.Id].Width);
        Assert.Equal(320, model.Appearance[fill.Id].Width);
        Assert.Equal(260, model.Appearance[fill.Id].Height);
        var saved = ProjectFile.Restore(ProjectFile.Parse(ProjectFile.Serialize(model.CaptureProject())));
        Assert.Equal("fill", saved.Appearance[fill.Id].Deserialize<Appearance>()!.Dock);
        var undone = ProjectFile.Restore(ProjectFile.Parse(model.Undo()!.Json));
        Assert.NotEqual(500, undone.Appearance[top.Id].Deserialize<Appearance>()!.Width);
    }

    [Fact]
    public void AnchorsFollowParentResizeWithoutAccumulatingDriftAndPreviewIsIsolated()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        var right = Add(model, "button", model.Form, 300, 50); Set(model, right, "anchor", "top,right");
        var stretch = Add(model, "textbox", model.Form, 30, 120); Set(model, stretch, "anchor", "top,left,right");
        var oldWidth = model.Appearance[model.Form.Id].Width;
        var originalWidth = model.Appearance[stretch.Id].Width;
        model.ExecuteEdit("resize", model.Form.Id, Payload(new { width = oldWidth + 100, height = 500 }));
        Assert.Equal(400, right.X); Assert.Equal(originalWidth + 100, model.Appearance[stretch.Id].Width);
        model.ExecuteEdit("resize", model.Form.Id, Payload(new { width = oldWidth, height = 440 }));
        Assert.Equal(300, right.X); Assert.Equal(originalWidth, model.Appearance[stretch.Id].Width);
        using var preview = new PreviewSession(model);
        preview.SetValue(right.Id, "anchor", Payload("bottom,right"));
        preview.SetValue(stretch.Id, "dock", Payload("fill"));
        Assert.Equal("top,right", model.Appearance[right.Id].Anchor);
        Assert.Equal("none", model.Appearance[stretch.Id].Dock);
        Assert.Throws<ArgumentException>(() => preview.SetValue(right.Id, "dock", Payload("oops")));
    }

    [Fact]
    public void NestedFreeContainersCarryAnchorsAndHiddenDocksReleaseSpace()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        foreach (var kind in new[] { "panel", "groupbox", "card" })
        {
            var parent = Add(model, kind, model.Form);
            var child = Add(model, "button", parent, 30, 30);
            Set(model, child, "anchor", "bottom,right");
            var original = (child.X, child.Y);
            var appearance = model.Appearance[parent.Id]; var originalWidth = appearance.Width; var originalHeight = appearance.Height;
            model.ExecuteEdit("resize", parent.Id, Payload(new { width = originalWidth + 80, height = originalHeight + 60 }));
            Assert.Equal(original.Item1 + 80, child.X); Assert.Equal(original.Item2 + 60, child.Y);
        }
        var top = Add(model, "panel", model.Form); Set(model, top, "dock", "top");
        var fill = Add(model, "panel", model.Form); Set(model, fill, "dock", "fill");
        Assert.True(fill.Y > 0);
        Set(model, top, "visible", false); Assert.Equal(0, fill.Y);
        Assert.Equal(model.Appearance[model.Form.Id].Height, model.Appearance[fill.Id].Height);
    }
}
