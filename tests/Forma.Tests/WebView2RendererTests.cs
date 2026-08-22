using Forma.Core;
using Forma.Core.Controls;
using Forma.Core.Rendering;
using Forma.WebView2;

namespace Forma.Tests;

public class WebView2RendererTests
{
    private static (WebView2Renderer Renderer, FakeBridge Bridge) CreateRenderer()
    {
        var bridge = new FakeBridge();

        return (new WebView2Renderer(bridge), bridge);
    }

    [Fact]
    public async Task RenderAsync_SendsCreateMessageDescribingTheControl()
    {
        var (renderer, bridge) = CreateRenderer();

        await renderer.RenderAsync(new Button { Id = "btn1", Text = "Save" });

        var message = bridge.Single();

        Assert.Equal("create", message.Str("type"));
        Assert.Equal("btn1", message.Str("id"));
        Assert.Equal("button", message.Str("control"));
        Assert.Equal("Save", message.Obj("properties").Str("text"));
    }

    [Fact]
    public async Task RenderAsync_MapsTextBoxToItsControlType()
    {
        var (renderer, bridge) = CreateRenderer();

        await renderer.RenderAsync(new TextBox { Id = "input1" });

        Assert.Equal("textbox", bridge.Single().Str("control"));
    }

    [Fact]
    public void RenderAsync_ThrowsForAnUnmappedControlType()
    {
        var (renderer, _) = CreateRenderer();

        // Form has no renderer mapping yet, so it must fail loudly rather than
        // silently producing an element the web runtime cannot build.
        Assert.Throws<NotSupportedException>(() =>
        {
            _ = renderer.RenderAsync(new Form { Id = "form1" });
        });
    }

    [Fact]
    public async Task ChangingText_SendsAnUpdateMessage()
    {
        var (renderer, bridge) = CreateRenderer();

        var button = new Button { Id = "btn1", Text = "Save" };

        await renderer.RenderAsync(button);

        bridge.Clear();

        button.Text = "Saved";

        var message = bridge.Single();

        Assert.Equal("update", message.Str("type"));
        Assert.Equal("btn1", message.Str("id"));
        Assert.Equal("Saved", message.Obj("properties").Str("text"));
    }

    [Fact]
    public async Task ChangingTextToTheSameValue_SendsNothing()
    {
        var (renderer, bridge) = CreateRenderer();

        var button = new Button { Id = "btn1", Text = "Save" };

        await renderer.RenderAsync(button);

        bridge.Clear();

        button.Text = "Save";

        Assert.Empty(bridge.Sent);
    }

    [Fact]
    public async Task RemoveAsync_SendsRemoveMessage()
    {
        var (renderer, bridge) = CreateRenderer();

        var button = new Button { Id = "btn1", Text = "Save" };

        await renderer.RenderAsync(button);

        bridge.Clear();

        await renderer.RemoveAsync(button);

        var message = bridge.Single();

        Assert.Equal("remove", message.Str("type"));
        Assert.Equal("btn1", message.Str("id"));
    }

    [Fact]
    public async Task ClickMessage_RaisesTheButtonClickEvent()
    {
        var (renderer, bridge) = CreateRenderer();

        var button = new Button { Id = "btn1", Text = "Save" };

        await renderer.RenderAsync(button);

        var clicked = false;

        button.Click += (_, _) => clicked = true;

        bridge.Receive(new BridgeMessage { Type = "event", Id = "btn1", Event = "click" });

        Assert.True(clicked);
    }

    [Fact]
    public async Task ClickMessage_ForAnUnknownId_IsIgnored()
    {
        var (renderer, bridge) = CreateRenderer();

        var button = new Button { Id = "btn1", Text = "Save" };

        await renderer.RenderAsync(button);

        var clicked = false;

        button.Click += (_, _) => clicked = true;

        bridge.Receive(new BridgeMessage { Type = "event", Id = "nope", Event = "click" });

        Assert.False(clicked);
    }

    [Fact]
    public async Task NonEventMessage_IsIgnored()
    {
        var (renderer, bridge) = CreateRenderer();

        var button = new Button { Id = "btn1", Text = "Save" };

        await renderer.RenderAsync(button);

        var clicked = false;

        button.Click += (_, _) => clicked = true;

        bridge.Receive(new BridgeMessage { Type = "log", Id = "btn1", Event = "click" });

        Assert.False(clicked);
    }

    // ---------------------------------------------------------------------
    // Known defects, scheduled for M2 (control tree work). These are written
    // out rather than left implicit so the fix has a target to turn green.
    // ---------------------------------------------------------------------

    [Fact(Skip = "M2: RemoveAsync does not unsubscribe PropertyChanged or drop the control from _controls")]
    public async Task RemovedControl_NoLongerSendsUpdates()
    {
        var (renderer, bridge) = CreateRenderer();

        var button = new Button { Id = "btn1", Text = "Save" };

        await renderer.RenderAsync(button);
        await renderer.RemoveAsync(button);

        bridge.Clear();

        button.Text = "Ghost";

        Assert.Empty(bridge.Sent);
    }

    [Fact(Skip = "M2: RenderAsync re-subscribes PropertyChanged, so a re-render duplicates every update")]
    public async Task RenderingTwice_DoesNotDuplicateUpdates()
    {
        var (renderer, bridge) = CreateRenderer();

        var button = new Button { Id = "btn1", Text = "Save" };

        await renderer.RenderAsync(button);
        await renderer.RenderAsync(button);

        bridge.Clear();

        button.Text = "Saved";

        Assert.Single(bridge.Sent);
    }
}
