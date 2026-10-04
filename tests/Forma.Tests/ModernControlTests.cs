using System.Text.Json;
using Forma.Builder;
using Forma.Core.Controls;
using Forma.Core.Rendering;
using Forma.WebView2;

namespace Forma.Tests;

public class ModernControlTests
{
    private static JsonElement Payload(object value) => JsonSerializer.SerializeToElement(value);

    [Fact]
    public void ModernPropertiesAndCardChildrenRoundTripWithTransientToastClosed()
    {
        var design = new BuilderViewModel(); design.CreateNew();
        Control Add(string kind, Control? parent = null) => design.ExecuteEdit("drop", (parent ?? design.Form!).Id, Payload(new { control = kind, x = 0, y = 0 })).AddedControl!;
        void Set(Control control, string property, object value) { design.SelectedControl = control; design.ExecuteEdit("property", control.Id, Payload(new { property, value })); }
        var card = Assert.IsType<Card>(Add("card")); var button = Add("button", card);
        Set(card, "description", "Account details"); Set(card, "headerVisible", false);
        var avatar = Assert.IsType<Avatar>(Add("avatar")); Set(avatar, "initials", "AB"); Set(avatar, "shape", "square");
        var divider = Assert.IsType<Divider>(Add("divider")); Set(divider, "orientation", "vertical"); Set(divider, "thickness", 3); Set(divider, "lineStyle", "dashed");
        var badge = Assert.IsType<Badge>(Add("badge")); Set(badge, "variant", "success");
        var spinner = Assert.IsType<Spinner>(Add("spinner")); Set(spinner, "speed", 350); Set(spinner, "isActive", false);
        var overlay = Assert.IsType<LoadingOverlay>(Add("loadingoverlay")); Set(overlay, "targetId", card.Id); Set(overlay, "isActive", true);
        var toast = Assert.IsType<Toast>(Add("toast")); Set(toast, "duration", 900); Set(toast, "position", "top-left"); Set(toast, "dismissible", false); toast.Show();
        var restored = ProjectFile.Restore(ProjectFile.Capture(design.Form!, c => Payload(design.Appearance[c.Id]))).Form;
        var restoredCard = Assert.IsType<Card>(restored.Children[0]); Assert.False(restoredCard.HeaderVisible); Assert.Equal("Account details", restoredCard.Description);
        Assert.Equal(button.Id, restoredCard.Children.Single().Id); Assert.Same(restoredCard, restoredCard.Children.Single().Parent);
        Assert.Equal("AB", Assert.IsType<Avatar>(restored.Children[1]).Initials); Assert.Equal("square", Assert.IsType<Avatar>(restored.Children[1]).Shape);
        Assert.Equal("vertical", Assert.IsType<Divider>(restored.Children[2]).Orientation); Assert.Equal(3, Assert.IsType<Divider>(restored.Children[2]).Thickness);
        Assert.Equal("success", Assert.IsType<Badge>(restored.Children[3]).Variant);
        Assert.Equal(350, Assert.IsType<Spinner>(restored.Children[4]).Speed); Assert.False(Assert.IsType<Spinner>(restored.Children[4]).IsActive);
        Assert.Equal(card.Id, Assert.IsType<LoadingOverlay>(restored.Children[5]).TargetId);
        var restoredToast = Assert.IsType<Toast>(restored.Children[6]); Assert.False(restoredToast.IsOpen); Assert.False(restoredToast.Dismissible); Assert.Equal(900, restoredToast.Duration);
        using var preview = new PreviewSession(design);
        preview.SetValue(overlay.Id, "isActive", Payload(false));
        Assert.True(overlay.IsActive); Assert.False(preview.Controls.OfType<LoadingOverlay>().Single().IsActive);
        preview.SetValue(spinner.Id, "isActive", Payload(true)); Assert.False(spinner.IsActive);
    }

    [Fact]
    public async Task NonDismissibleToastOnlyClosesOnTimeoutAndRaisesClosedOnce()
    {
        var bridge = new FakeBridge(); var renderer = new WebView2Renderer(bridge);
        var toast = new Toast { Dismissible = false }; await renderer.RenderAsync(toast);
        int closed = 0; toast.Closed += (_, _) => closed++; toast.Show();
        void Close(object reason) => bridge.Receive(new BridgeMessage { Type = "event", Id = toast.Id, Event = "toast-close", Payload = Payload(new { reason }) });
        Close("dismiss"); Assert.True(toast.IsOpen);
        Close(3); Assert.True(toast.IsOpen);
        Close("timeout"); Assert.False(toast.IsOpen); Assert.Equal(1, closed);
        Close("timeout"); Assert.Equal(1, closed);
    }
    [Fact]
    public void LoadingOverlayIsNonvisualAndRejectsCanvasGeometryEdits()
    {
        var design = new BuilderViewModel(); design.CreateNew();
        var panel = design.ExecuteEdit("drop", design.Form!.Id, Payload(new { control = "panel", x = 0, y = 0 })).AddedControl!;
        var overlay = Assert.IsType<LoadingOverlay>(design.ExecuteEdit("drop", panel.Id,
            Payload(new { control = "loadingoverlay", x = 10, y = 10 })).AddedControl);
        Assert.IsAssignableFrom<INonvisualControl>(overlay); Assert.Same(design.Form, overlay.Parent);
        var properties = InspectorCatalog.ForKind("loadingoverlay").Select(p => p.Id).ToArray();
        Assert.Contains("targetId", properties); Assert.Contains("isActive", properties); Assert.Contains("enabled", properties);
        Assert.DoesNotContain("width", properties); Assert.DoesNotContain("height", properties); Assert.DoesNotContain("x", properties);
        design.ExecuteEdit("property", overlay.Id, Payload(new { property = "isActive", value = true }));
        Assert.True(overlay.IsActive); design.ClearHistory();
        var width = design.Appearance[overlay.Id].Width;
        design.ExecuteEdit("move", overlay.Id, Payload(new { x = 100, y = 100 }));
        design.ExecuteEdit("resize", overlay.Id, Payload(new { width = 400, height = 300 }));
        Assert.Equal(10, overlay.X); Assert.Equal(width, design.Appearance[overlay.Id].Width); Assert.False(design.CanUndo);
        var restored = ProjectFile.Restore(ProjectFile.Capture(design.Form, c => Payload(design.Appearance[c.Id])));
        var copy = Assert.IsType<LoadingOverlay>(restored.Form.Children.Last()); Assert.True(copy.IsActive);
        Assert.IsAssignableFrom<INonvisualControl>(copy);
    }

}
