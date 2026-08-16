namespace Forma.Core.Rendering;

public sealed class BridgeMessage
{
    public string Type { get; init; } = string.Empty;
    public string? Id {get; init; }
    public Object? Payload { get; init; }
}


