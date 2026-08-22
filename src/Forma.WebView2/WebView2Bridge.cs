using System.Text.Json;
using Forma.Core.Rendering;
using Microsoft.Web.WebView2.Core;

namespace Forma.WebView2;

/// <summary>
/// Transports Forma messages between C# and the web runtime hosted in a
/// WebView2 instance.
/// </summary>
/// <remarks>
/// Depends only on <see cref="CoreWebView2"/>, so it works with any WebView2
/// host (WinForms, WPF, WinUI) rather than being bound to one of them.
/// </remarks>
public sealed class WebView2Bridge : IBridge, IDisposable
{
    private readonly CoreWebView2 _coreWebView;

    /// <summary>
    /// The context that owns the WebView2 instance. <see cref="CoreWebView2"/>
    /// members must be called on the thread that created it, so sends
    /// originating elsewhere are marshalled back through this.
    /// </summary>
    private readonly SynchronizationContext? _uiContext;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private bool _disposed;

    public event EventHandler<BridgeMessage>? MessageReceived;

    public WebView2Bridge(CoreWebView2 coreWebView)
    {
        _coreWebView = coreWebView ?? throw new ArgumentNullException(nameof(coreWebView));
        _uiContext = SynchronizationContext.Current;

        _coreWebView.WebMessageReceived += OnWebMessageReceived;
    }

    private void OnWebMessageReceived(
        object? sender,
        CoreWebView2WebMessageReceivedEventArgs e
    )
    {
        BridgeMessage? message;

        try
        {
            message = JsonSerializer.Deserialize<BridgeMessage>(
                e.WebMessageAsJson,
                _jsonOptions
            );
        }
        catch (JsonException)
        {
            // The web runtime is the only expected sender, but a malformed
            // payload must not take down the host.
            return;
        }

        if (message is not null)
        {
            MessageReceived?.Invoke(this, message);
        }
    }

    public Task SendAsync(object message)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var json = JsonSerializer.Serialize(message);

        // PostWebMessageAsJson hands the payload to the web runtime as
        // structured data. Unlike ExecuteScriptAsync it never parses the
        // message as JavaScript, so control text cannot escape into code.
        if (_uiContext is null || SynchronizationContext.Current == _uiContext)
        {
            _coreWebView.PostWebMessageAsJson(json);

            return Task.CompletedTask;
        }

        var completion = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        _uiContext.Post(
            _ =>
            {
                try
                {
                    _coreWebView.PostWebMessageAsJson(json);

                    completion.SetResult();
                }
                catch (Exception ex)
                {
                    completion.SetException(ex);
                }
            },
            null
        );

        return completion.Task;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _coreWebView.WebMessageReceived -= OnWebMessageReceived;
    }
}
