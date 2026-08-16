using Forma.Core.Controls;
using Forma.Core.Rendering;

namespace Forma.WebView2;

public sealed class WebView2Renderer : IRenderer
{
    private readonly IBridge _bridge;

    public WebView2Renderer(IBridge bridge)
    {
        _bridge = bridge;
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task RenderAsync(Control control)
    {
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
}
