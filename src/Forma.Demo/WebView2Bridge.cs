using System.Text.Json;
using Forma.Core.Rendering;
using WebView2Control = Microsoft.Web.WebView2.WinForms.WebView2;

namespace Forma.Demo;

public sealed class WebView2Bridge : IBridge
{
    private readonly WebView2Control _webView;

    public event EventHandler<BridgeMessage>? MessageReceived;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public WebView2Bridge(WebView2Control webView)
    {
        _webView = webView;

        _webView.WebMessageReceived += OnWebMessageReceived;
    }

    private void OnWebMessageReceived(
        object? sender,
        Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e
    )
    {
        try
        {
            var json = e.WebMessageAsJson;

            Console.WriteLine($"Raw browser message: {json}");

            var message = JsonSerializer.Deserialize<BridgeMessage>(
                json,
                _jsonOptions
            );

            if (message is not null)
            {
                MessageReceived?.Invoke(this, message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Bridge error: {ex}");
        }
    }

    public async Task SendAsync(object message)
    {
        var json = JsonSerializer.Serialize(message);

        await _webView.ExecuteScriptAsync($"window.forma.receive({json})");
    }
}
