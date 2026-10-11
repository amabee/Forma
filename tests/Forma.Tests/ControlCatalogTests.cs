using System.Text.RegularExpressions;
using Forma.Builder;

namespace Forma.Tests;

public class ControlCatalogTests
{
    [Fact]
    public void EveryPlaceableKindCreatesItsOwnType()
    {
        foreach (var entry in ControlCatalog.All.Where(entry => entry.Create is not null))
        {
            var control = entry.Create!();
            Assert.IsType(entry.Type, control);
            Assert.Equal(entry.Kind, control.ControlType);
        }
        Assert.Null(ControlCatalog.Create("form"));
        Assert.Null(ControlCatalog.Create("unknown"));
    }

    [Fact]
    public void ToolboxKindsAllExistInTheCatalog()
    {
        var html = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Forma.Builder", "DesignerWeb", "index.html"));
        var kinds = Regex.Matches(html, "data-kind=\"([a-z]+)\"").Select(match => match.Groups[1].Value).Distinct().ToList();
        Assert.NotEmpty(kinds);
        Assert.All(kinds, kind => Assert.True(ControlCatalog.TryGet(kind, out var entry) && entry.Create is not null, kind));
    }

    [Fact]
    public void DefaultAppearancesKeepTheirToolboxPresets()
    {
        Appearance Of(string kind) { ControlCatalog.TryGet(kind, out var entry); return entry.DefaultAppearance(); }
        Assert.Equal(22, Of("label").FontSize);
        Assert.Equal((10, "Small", 0), (Of("card").BorderRadius, Of("card").Shadow, Of("card").PaddingTop));
        Assert.Equal((120, 120, 100), (Of("circularprogress").Width, Of("circularprogress").Height, Of("circularprogress").BorderRadius));
        Assert.True(Of("passwordbox").Password);
        Assert.False(Of("timer").Enabled);
        Assert.Equal(("transparent", 32), (Of("icon").BackColor, Of("icon").Width));
        // Each call returns a fresh instance so edits never leak between controls.
        Assert.NotSame(Of("panel"), Of("panel"));
    }

    [Fact]
    public void ContainerAndComponentRulesMatchTheControls()
    {
        Assert.True(ControlCatalog.TryGet("panel", out var panel) && panel.CanContainChildren);
        Assert.True(ControlCatalog.TryGet("button", out var button) && !button.CanContainChildren);
        Assert.True(ControlCatalog.TryGet("timer", out var timer) && timer.FormOnly);
        Assert.True(ControlCatalog.TryGet("dialog", out var dialog) && !dialog.FormOnly);
    }
}
