using System.Text.Json;
using Forma.Core.Rendering;

namespace Forma.Tests;

/// <summary>
/// In-memory <see cref="IBridge"/> that records what the renderer sends and
/// lets a test push messages back, so renderer behaviour can be asserted
/// without standing up a WebView2 instance.
/// </summary>
public sealed class FakeBridge : IBridge
{
    private readonly List<JsonElement> _sent = [];

    /// <summary>
    /// Messages the renderer has sent, captured as JSON so assertions see
    /// exactly what would go over the wire.
    /// </summary>
    public IReadOnlyList<JsonElement> Sent => _sent;

    public event EventHandler<BridgeMessage>? MessageReceived;

    public Task SendAsync(object message)
    {
        var json = JsonSerializer.Serialize(message);

        _sent.Add(JsonDocument.Parse(json).RootElement.Clone());

        return Task.CompletedTask;
    }

    /// <summary>Simulates a message arriving from the web runtime.</summary>
    public void Receive(BridgeMessage message)
    {
        MessageReceived?.Invoke(this, message);
    }

    public JsonElement Single()
    {
        return Assert.Single(_sent);
    }

    public JsonElement Last()
    {
        Assert.NotEmpty(_sent);

        return _sent[^1];
    }

    public void Clear()
    {
        _sent.Clear();
    }
}

internal static class JsonElementExtensions
{
    public static string? Str(this JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value)
            ? value.GetString()
            : null;
    }

    public static JsonElement Obj(this JsonElement element, string propertyName)
    {
        Assert.True(
            element.TryGetProperty(propertyName, out var value),
            $"Expected property '{propertyName}' on {element}"
        );

        return value;
    }
}
