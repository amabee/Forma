using System.Text.Json;
using Forma.Builder;
using Forma.Core.Controls;

namespace Forma.Tests;

public class PreviewSessionTests
{
    [Theory]
    [InlineData("avatar")]
    [InlineData("image")]
    [InlineData("picturebox")]
    public void RuntimeImageSourceAcceptsPathsAndUrisWithoutChangingDesign(string kind)
    {
        var design = new BuilderViewModel(); design.CreateNew();
        var original = design.ExecuteEdit("drop", design.Form.Id,
            JsonSerializer.SerializeToElement(new { control = kind, x = 20, y = 30 })).AddedControl!;
        using var preview = new PreviewSession(design);
        var image = Assert.IsAssignableFrom<Forma.Core.Controls.Image>(preview.Controls.Single(c => c.Id == original.Id));
        var initial = Assert.IsAssignableFrom<Forma.Core.Controls.Image>(original).Source;
        foreach (var source in new[] { @"C:\Users\Angelzm\Pictures\My avatar.png", "https://example.com/avatar.png", "file:///C:/Pictures/avatar.png", "data:image/png;base64,AAAA", "" }) {
            preview.SetValue(image.Id, "source", JsonSerializer.SerializeToElement(source));
            Assert.Equal(Path.IsPathFullyQualified(source) ? new Uri(source).AbsoluteUri : source, image.Source);
            var state = JsonSerializer.SerializeToElement(preview.State());
            Assert.Equal(image.Source, state.GetProperty("controls").EnumerateArray().Single(c => c.GetProperty("id").GetString() == image.Id).GetProperty("source").GetString());
        }
        Assert.Equal(initial, Assert.IsAssignableFrom<Forma.Core.Controls.Image>(original).Source);
        preview.SetValue(image.Id, "text", JsonSerializer.SerializeToElement("New initials"));
        Assert.Equal("", image.Source);
        foreach (var invalid in new[] { "javascript:alert(1)", "data:text/html,test", "relative.png", "bad\0path" })
            Assert.Throws<ArgumentException>(() => preview.SetValue(image.Id, "source", JsonSerializer.SerializeToElement(invalid)));
        Assert.Throws<ArgumentException>(() => preview.SetValue(image.Id, "source", JsonSerializer.SerializeToElement(42)));
    }

    [Fact]
    public void RuntimeChangesAndAppearanceAreIsolatedFromTheDesign()
    {
        var design = new BuilderViewModel(); design.CreateNew();
        var button = design.ExecuteEdit("drop", design.Form.Id,
            JsonSerializer.SerializeToElement(new { control = "button", x = 20, y = 30 })).AddedControl!;
        design.MarkSaved();
        using var preview = new PreviewSession(design);
        var copy = preview.Controls.Single(control => control.Id == button.Id);
        Assert.NotSame(button, copy);
        Assert.Equal(button.X, copy.X);
        copy.Text = "Runtime change";
        preview.Appearance[button.Id].Width = 240;
        Assert.Equal("Continue", button.Text);
        Assert.Equal(120, design.Appearance[button.Id].Width);
        Assert.False(design.HasUnsavedChanges);
        Assert.False(design.PreviewMode);
    }

    [Fact]
    public void PreviewProjectsTabVisibilityAndStopsItsOwnTimers()
    {
        var design = new BuilderViewModel(); design.CreateNew();
        var tabs = design.ExecuteEdit("drop", design.Form.Id,
            JsonSerializer.SerializeToElement(new { control = "tabcontrol", x = 0, y = 0 })).AddedControl!;
        var timer = design.ExecuteEdit("drop", design.Form.Id,
            JsonSerializer.SerializeToElement(new { control = "timer", x = 0, y = 0 })).AddedControl!;
        design.Appearance[timer.Id].Enabled = true;
        using var preview = new PreviewSession(design);
        var runtimeTabs = Assert.IsType<TabControl>(preview.Controls.Single(control => control.Id == tabs.Id));
        runtimeTabs.SelectedTab = 1;
        var state = JsonSerializer.SerializeToElement(preview.State());
        var projectedTabs = state.GetProperty("controls").EnumerateArray().Single(control => control.GetProperty("id").GetString() == tabs.Id);
        Assert.Equal(1, projectedTabs.GetProperty("selectedTab").GetInt32());
        Assert.Equal("runtime-preview", state.GetProperty("action").GetString());
        preview.Start();
        var runtimeTimer = Assert.IsType<Forma.Core.Controls.Timer>(preview.Controls.Single(control => control.Id == timer.Id));
        Assert.True(runtimeTimer.Enabled);
        Assert.False(Assert.IsType<Forma.Core.Controls.Timer>(timer).Enabled);
        preview.Dispose();
        Assert.False(runtimeTimer.Enabled);
    }
}
