using Forma.Core;
using Forma.Core.Controls;
using Forma.WebView2;

namespace Forma.Tests;

public class RendererTreeTests
{
    private static (WebView2Renderer Renderer, FakeBridge Bridge) CreateRenderer()
    {
        var bridge = new FakeBridge();

        return (new WebView2Renderer(bridge), bridge);
    }

    [Fact]
    public async Task RenderAsync_RendersTheWholeSubtreeParentsFirst()
    {
        var (renderer, bridge) = CreateRenderer();

        var form = new Form { Id = "form1", Title = "Demo" };
        var panel = new Form { Id = "panel1" };
        var save = new Button { Id = "save", Text = "Save" };
        var cancel = new Button { Id = "cancel", Text = "Cancel" };

        panel.Add(save);
        panel.Add(cancel);
        form.Add(panel);

        await renderer.RenderAsync(form);

        var ids = bridge.Sent.Select(m => m.Str("id")!).ToArray();

        // Depth-first, parent before child: the runtime can only attach an
        // element once its parent exists.
        Assert.Equal(["form1", "panel1", "save", "cancel"], ids);
        Assert.All(bridge.Sent, m => Assert.Equal("create", m.Str("type")));
    }

    [Fact]
    public async Task RenderAsync_SendsTheParentIdOnEachChild()
    {
        var (renderer, bridge) = CreateRenderer();

        var form = new Form { Id = "form1" };
        var button = new Button { Id = "btn1" };

        form.Add(button);

        await renderer.RenderAsync(form);

        Assert.Null(bridge.Sent[0].Str("parentId"));
        Assert.Equal("form1", bridge.Sent[1].Str("parentId"));
    }

    [Fact]
    public async Task AddingAChildAfterRender_RendersItIncrementally()
    {
        var (renderer, bridge) = CreateRenderer();

        var form = new Form { Id = "form1" };

        await renderer.RenderAsync(form);

        bridge.Clear();

        form.Add(new Button { Id = "btn1", Text = "Save" });

        var message = bridge.Single();

        Assert.Equal("create", message.Str("type"));
        Assert.Equal("btn1", message.Str("id"));
        Assert.Equal("form1", message.Str("parentId"));
    }

    [Fact]
    public async Task AddingASubtreeAfterRender_RendersEveryDescendant()
    {
        var (renderer, bridge) = CreateRenderer();

        var form = new Form { Id = "form1" };

        await renderer.RenderAsync(form);

        bridge.Clear();

        var panel = new Form { Id = "panel1" };

        panel.Add(new Button { Id = "btn1" });

        form.Add(panel);

        Assert.Equal(["panel1", "btn1"], bridge.Sent.Select(m => m.Str("id")!));
    }

    [Fact]
    public async Task RemovingAChildAfterRender_SendsOneRemoveForTheSubtree()
    {
        var (renderer, bridge) = CreateRenderer();

        var form = new Form { Id = "form1" };
        var panel = new Form { Id = "panel1" };

        panel.Add(new Button { Id = "btn1" });
        form.Add(panel);

        await renderer.RenderAsync(form);

        bridge.Clear();

        form.Remove(panel);

        // Detaching the parent element takes its descendants with it, so one
        // message is enough.
        var message = bridge.Single();

        Assert.Equal("remove", message.Str("type"));
        Assert.Equal("panel1", message.Str("id"));
    }

    [Fact]
    public async Task RemovingASubtree_UnregistersDescendants()
    {
        var (renderer, bridge) = CreateRenderer();

        var form = new Form { Id = "form1" };
        var panel = new Form { Id = "panel1" };
        var button = new Button { Id = "btn1" };

        panel.Add(button);
        form.Add(panel);

        await renderer.RenderAsync(form);

        form.Remove(panel);

        bridge.Clear();

        // The grandchild was never removed directly; its subscription must
        // still have gone with the subtree.
        button.Text = "Ghost";

        Assert.Empty(bridge.Sent);
    }

    [Fact]
    public async Task RemovedSubtree_StopsRoutingEventsToDescendants()
    {
        var (renderer, bridge) = CreateRenderer();

        var form = new Form { Id = "form1" };
        var panel = new Form { Id = "panel1" };
        var button = new Button { Id = "btn1" };

        panel.Add(button);
        form.Add(panel);

        await renderer.RenderAsync(form);

        form.Remove(panel);

        var clicked = false;

        button.Click += (_, _) => clicked = true;

        bridge.Receive(new Core.Rendering.BridgeMessage
        {
            Type = "event",
            Id = "btn1",
            Event = "click",
        });

        Assert.False(clicked);
    }

    [Fact]
    public async Task ChildrenAddedToAnUnrenderedControl_AreRenderedWithIt()
    {
        var (renderer, bridge) = CreateRenderer();

        var form = new Form { Id = "form1" };

        // Adding before the renderer knows about the tree must not be lost.
        form.Add(new Button { Id = "btn1" });

        await renderer.RenderAsync(form);

        Assert.Equal(["form1", "btn1"], bridge.Sent.Select(m => m.Str("id")!));
    }
}
