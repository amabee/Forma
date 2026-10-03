using Forma.Core.Controls;
using Forma.WebView2;

namespace Forma.Tests;

public class ControlPositionTests
{
    [Fact]
    public async Task Position_RendersAndUpdatesWithoutRecreatingTheControl()
    {
        var bridge = new FakeBridge();
        var renderer = new WebView2Renderer(bridge);
        var button = new Button { X = 40, Y = 60 };
        await renderer.RenderAsync(button);
        Assert.Equal(40, bridge.Single().Obj("properties").GetProperty("x").GetInt32());
        Assert.Equal(60, bridge.Single().Obj("properties").GetProperty("y").GetInt32());
        bridge.Clear();

        button.X = 120;
        button.X = 120;

        Assert.Equal("update", bridge.Single().Str("type"));
        Assert.Equal(120, bridge.Single().Obj("properties").GetProperty("x").GetInt32());
        Assert.Equal(60, bridge.Single().Obj("properties").GetProperty("y").GetInt32());
        bridge.Clear();

        button.X = null;
        button.Y = null;

        var position = bridge.Last().Obj("properties");
        Assert.Equal(System.Text.Json.JsonValueKind.Null, position.GetProperty("x").ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, position.GetProperty("y").ValueKind);
    }
}
