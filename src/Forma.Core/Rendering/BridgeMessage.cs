namespace Forma.Core.Rendering;

public sealed class BridgeMessage
{
    public string Type { get; set; } = string.Empty;
    public string? Id { get; set; }
    public string? Event { get; set; }
    public Object? Payload { get; set; }
}
