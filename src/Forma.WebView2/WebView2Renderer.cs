using Forma.Core.Controls;
using Forma.Core.Rendering;

namespace Forma.WebView2;

public sealed class WebView2Renderer : IRenderer
{
    private readonly IBridge _bridge;

    private readonly Dictionary<string, Control> _controls = new();

    public WebView2Renderer(IBridge bridge)
    {
        _bridge = bridge;

        _bridge.MessageReceived += OnMessageReceived;
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task RenderAsync(Control control)
    {
        _controls[control.Id] = control;
        control.PropertyChanged += async (_, _) =>
        {
            try
            {
                await UpdateAsync(control);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating control: {ex.Message}");
            }
        };

        return _bridge.SendAsync(
            new
            {
                type = "create",
                id = control.Id,
                control = GetControlType(control),
                properties = new { text = control.Text },
            }
        );
    }

    public Task UpdateAsync(Control control)
    {
        return _bridge.SendAsync(
            new
            {
                type = "update",
                id = control.Id,
                properties = new { text = control.Text },
            }
        );
    }

    public Task RemoveAsync(Control control)
    {
        return _bridge.SendAsync(new { type = "remove", id = control.Id });
    }

    private static string GetControlType(Control control)
    {
        return control switch
        {
            Button => "button",
            TextBox => "textbox",

            _ => throw new NotSupportedException($"Unsupported control: {control.GetType().Name}"),
        };
    }

    private void OnMessageReceived(object? sender, BridgeMessage message)
    {
        if (message.Type != "event")
            return;

        if (message.Id is null)
            return;

        if (!_controls.TryGetValue(message.Id, out var control))
        {
            return;
        }

        if (control is Button button && message.Event == "click")
        {
            button.RaiseClick();
        }
    }
}
