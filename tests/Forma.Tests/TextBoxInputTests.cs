using System.Text.Json;
using Forma.Core.Controls;
using Forma.Core.Rendering;
using Forma.WebView2;

namespace Forma.Tests;

public class TextBoxInputTests
{
    private static BridgeMessage Input(string json) => new()
    {
        Type = "event",
        Id = "name",
        Event = "input",
        Payload = JsonSerializer.Deserialize<JsonElement>(json),
    };

    [Fact]
    public async Task BrowserInput_UpdatesTextBeforeNotifyingApplication()
    {
        var bridge = new FakeBridge();
        var renderer = new WebView2Renderer(bridge);
        var input = new TextBox { Id = "name" };
        string? observedText = null;
        var changes = 0;
        input.TextChanged += (_, _) => { observedText = input.Text; changes++; };
        await renderer.RenderAsync(input);
        bridge.Clear();

        bridge.Receive(Input("{\"text\":\"Angel\"}"));
        bridge.Receive(Input("{\"text\":\"Angel\"}"));

        Assert.Equal("Angel", observedText);
        Assert.Equal(1, changes);
        Assert.Equal("Angel", bridge.Single().Obj("properties").Str("text"));

        bridge.Receive(Input("{\"text\":\"\"}"));
        Assert.Equal("", input.Text);
        Assert.Equal(2, changes);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"text\":null}")]
    [InlineData("{\"text\":42}")]
    public async Task InvalidInput_DoesNotChangeText(string json)
    {
        var bridge = new FakeBridge();
        var renderer = new WebView2Renderer(bridge);
        var input = new TextBox { Id = "name", Text = "Original" };
        await renderer.RenderAsync(input);
        bridge.Clear();

        bridge.Receive(Input(json));

        Assert.Equal("Original", input.Text);
        Assert.Empty(bridge.Sent);
    }

    [Fact]
    public async Task RemovedInput_StopsReceivingBrowserEvents()
    {
        var bridge = new FakeBridge();
        var renderer = new WebView2Renderer(bridge);
        var input = new TextBox { Id = "name", Text = "Original" };
        await renderer.RenderAsync(input);
        await renderer.RemoveAsync(input);

        bridge.Receive(Input("{\"text\":\"Ghost\"}"));

        Assert.Equal("Original", input.Text);
    }
}
