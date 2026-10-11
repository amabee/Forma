using System.Text.Json;
using Forma.Core.Controls;
using Forma.Core.Rendering;
using Forma.WebView2;

namespace Forma.Tests;

public class RenderStateTests
{
    private sealed class Gauge : Control
    {
        public int Level { get; set; } = 3;
        public int Bumps { get; private set; }

        protected override void WriteRenderState(IDictionary<string, object?> state)
        {
            base.WriteRenderState(state);
            state["level"] = Level;
        }

        protected override void OnRuntimeEvent(RuntimeEvent e)
        {
            if (e.Name == "bump" && e.TryGetInt("by", out var by)) Bumps += by;
        }
    }

    [Fact]
    public async Task NewControlTypesRenderAndReceiveEventsWithoutRendererChanges()
    {
        var bridge = new FakeBridge(); var renderer = new WebView2Renderer(bridge);
        var gauge = new Gauge();
        await renderer.RenderAsync(gauge);
        var created = bridge.Sent.Last();
        Assert.Equal("gauge", created.GetProperty("control").GetString());
        Assert.Equal(3, created.GetProperty("properties").GetProperty("level").GetInt32());

        void Send(object payload) => bridge.Receive(new BridgeMessage { Type = "event", Id = gauge.Id, Event = "bump", Payload = JsonSerializer.SerializeToElement(payload) });
        Send(new { by = 2 });
        Send(new { by = "two" });
        Assert.Equal(2, gauge.Bumps);
    }

    [Fact]
    public void DerivedControlsLayerTheirStateOnTheBase()
    {
        var state = new CommandButton { Text = "Go", Description = "Runs it" }.GetRenderState();
        Assert.Equal("Go", state["text"]);
        Assert.Equal(true, state["enabled"]);
        Assert.True(state.ContainsKey("iconName"));
        Assert.Equal("Runs it", state["description"]);
    }

    [Fact]
    public void RuntimeEventsIgnoreMissingAndMistypedPayloads()
    {
        var slider = new Slider();
        slider.HandleRuntimeEvent(new RuntimeEvent("value", JsonSerializer.SerializeToElement(new { value = "high" })));
        slider.HandleRuntimeEvent(new RuntimeEvent("value", null));
        Assert.Equal(0, slider.Value);
        slider.HandleRuntimeEvent(new RuntimeEvent("value", JsonSerializer.SerializeToElement(new { value = 40 })));
        Assert.Equal(40, slider.Value);

        // Progress bars display a value; users cannot set it.
        var progress = new ProgressBar();
        progress.HandleRuntimeEvent(new RuntimeEvent("value", JsonSerializer.SerializeToElement(new { value = 40 })));
        Assert.Equal(0, progress.Value);
    }
}
