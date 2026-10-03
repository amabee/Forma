using System.Text.Json;
using Forma.Builder;
using Forma.Core.Controls;
using Forma.Core.Rendering;
using Forma.WebView2;

namespace Forma.Tests;
public class BasicControlsTests
{
    [Fact]
    public void MaskFiltersTokensKeepsLiteralsAndReportsCompletion()
    {
        var masked = new MaskedTextBox(); masked.SetMaskedText("abc1234567");
        Assert.Equal("123-4567", masked.Text); Assert.True(masked.MaskCompleted);
        masked.SetMaskedText("12"); Assert.Equal("12_-____", masked.Text); Assert.False(masked.MaskCompleted);
        masked.Mask = "LL-00"; masked.SetMaskedText("ab34"); Assert.Equal("ab-34", masked.Text);
        masked.Mask = "0_0"; masked.SetMaskedText("1_2"); Assert.True(masked.MaskCompleted);
    }
    [Fact]
    public void CheckedRowsAreCopiedDeduplicatedAndPruned()
    {
        var list = new CheckedListBox { Items = ["A", "B", "C"], CheckedIndices = [2, 0, 2, -1, 10] };
        Assert.Equal(new[] { 0, 2 }, list.CheckedIndices);
        var copy = list.CheckedIndices; copy[0] = 1; Assert.Equal(0, list.CheckedIndices[0]);
        list.Items = ["A"]; Assert.Equal(new[] { 0 }, list.CheckedIndices);
        var count = 0; list.ItemCheck += (_, _) => count++;
        list.SetItemChecked(0, false); list.SetItemChecked(0, false); list.SetItemChecked(9, true);
        Assert.Equal(1, count); Assert.Empty(list.CheckedIndices);
    }
    [Fact]
    public async Task RendererRoutesMaskedCheckedAndLinkEvents()
    {
        var bridge = new FakeBridge(); var renderer = new WebView2Renderer(bridge);
        var masked = new MaskedTextBox(); var list = new CheckedListBox(); var link = new LinkLabel();
        foreach (var control in new Control[] { masked, list, link }) await renderer.RenderAsync(control);
        void Send(Control c, string name, object payload) => bridge.Receive(new BridgeMessage { Type = "event", Id = c.Id, Event = name, Payload = JsonSerializer.SerializeToElement(payload) });
        Send(masked, "input", new { text = "1234567" }); Assert.Equal("123-4567", masked.Text);
        Send(list, "item-check", new { index = 1, @checked = true }); Assert.Equal(new[] { 1 }, list.CheckedIndices);
        Send(list, "item-check", new { index = "bad", @checked = true }); Assert.Equal(new[] { 1 }, list.CheckedIndices);
        var clicked = false; link.LinkClicked += (_, _) => clicked = true;
        Send(link, "link", new { }); Assert.True(clicked); Assert.True(link.Visited);
        link.Url = "javascript:alert(1)"; Assert.Equal("https://example.com/", link.Url);
    }
    [Fact]
    public void NewControlsRoundTripThroughProjectsAndHistory()
    {
        var form = new Forma.Core.Form(); var masked = new MaskedTextBox { Mask = "LL-00" }; masked.SetMaskedText("AB12");
        form.Add(masked); form.Add(new CheckedListBox { Items = ["A", "B"], CheckedIndices = [1] }); form.Add(new LinkLabel { Url = "https://example.org/", Text = "Website" });
        var project = ProjectFile.Capture(form, _ => JsonSerializer.SerializeToElement(new { Width = 180, Height = 100 }));
        var loaded = ProjectFile.Restore(ProjectFile.Parse(ProjectFile.Serialize(project))).Form;
        Assert.Equal("AB-12", Assert.IsType<MaskedTextBox>(loaded.Children[0]).Text);
        Assert.Equal(new[] { 1 }, Assert.IsType<CheckedListBox>(loaded.Children[1]).CheckedIndices);
        Assert.Equal("https://example.org/", Assert.IsType<LinkLabel>(loaded.Children[2]).Url);
    }
}
