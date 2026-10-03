using System.Text.Json;
using Forma.Core.Controls;
using Forma.Core.Rendering;
using Forma.WebView2;

namespace Forma.Tests;
public class InputControlsTests
{
    [Fact]
    public void NumericBoundsRejectInvalidValuesAndClamp()
    {
        var input = new NumericUpDown { Minimum = -10, Maximum = 20, Value = 100 };
        Assert.Equal(20, input.Value);
        input.Value = double.NaN; Assert.Equal(20, input.Value);
        input.Maximum = 5; Assert.Equal(5, input.Value);
        input.Minimum = 10; Assert.Equal(5, input.Minimum);
        input.Increment = 0; Assert.Equal(1, input.Increment);
    }
    [Fact]
    public void DatesRejectInvalidValuesAndAllowClearing()
    {
        var date = new DatePicker { DateValue = "2026-10-03" };
        date.DateValue = "2026-02-30"; Assert.Equal("2026-10-03", date.DateValue);
        date.DateValue = ""; Assert.Equal("", date.DateValue);
        var time = new TimePicker { DateValue = "12:45" };
        time.DateValue = "25:00"; Assert.Equal("12:45", time.DateValue);
    }
    [Fact]
    public async Task NativeInputUpdatesNumericDateAndColorButProgressIsReadOnly()
    {
        var bridge = new FakeBridge(); var renderer = new WebView2Renderer(bridge);
        var slider = new Slider(); var progress = new ProgressBar(); var date = new DatePicker(); var color = new ColorPicker();
        foreach (var control in new Control[] { slider, progress, date, color }) await renderer.RenderAsync(control);
        void Send(Control control, object value) => bridge.Receive(new BridgeMessage {
            Type = "event", Id = control.Id, Event = "value", Payload = JsonSerializer.SerializeToElement(new { value }) });
        Send(slider, 200); Assert.Equal(100, slider.Value);
        Send(progress, 80); Assert.Equal(0, progress.Value);
        Send(date, "2026-10-03"); Assert.Equal("2026-10-03", date.DateValue);
        Send(color, "#123abc"); Assert.Equal("#123abc", color.Color);
        Send(color, "invalid"); Assert.Equal("#123abc", color.Color);
    }
    [Fact]
    public async Task PasswordAndTogglePropertiesRenderAndRoute()
    {
        var bridge = new FakeBridge(); var renderer = new WebView2Renderer(bridge);
        await renderer.RenderAsync(new PasswordBox()); Assert.True(bridge.Last().Obj("properties").Obj("password").GetBoolean());
        var toggle = new ToggleButton(); await renderer.RenderAsync(toggle);
        bridge.Receive(new BridgeMessage { Type = "event", Id = toggle.Id, Event = "checked", Payload = JsonSerializer.SerializeToElement(new { @checked = true }) });
        Assert.True(toggle.Checked);
    }
}
