using System.Text.Json;

namespace Forma.Core.Rendering;

/// <summary>
/// A user-interface event a renderer received for one control, such as a click or an edit.
/// The payload comes from the rendered page and is untrusted, so values are read through
/// typed accessors that reject missing or mistyped fields.
/// </summary>
public readonly struct RuntimeEvent
{
    private readonly JsonElement _payload;

    public RuntimeEvent(string name, object? payload = null)
    {
        Name = name;
        _payload = payload is JsonElement { ValueKind: JsonValueKind.Object } json ? json : default;
    }

    public string Name { get; }

    public bool TryGetElement(string key, out JsonElement value)
    {
        value = default;
        return _payload.ValueKind == JsonValueKind.Object && _payload.TryGetProperty(key, out value);
    }

    public bool TryGetString(string key, out string value)
    {
        value = "";
        if (!TryGetElement(key, out var element) || element.ValueKind != JsonValueKind.String) return false;
        value = element.GetString()!;
        return true;
    }

    public bool TryGetInt(string key, out int value)
    {
        value = 0;
        return TryGetElement(key, out var element) && element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out value);
    }

    public bool TryGetDouble(string key, out double value)
    {
        value = 0;
        return TryGetElement(key, out var element) && element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out value);
    }

    public bool TryGetBool(string key, out bool value)
    {
        value = false;
        if (!TryGetElement(key, out var element) || element.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = element.GetBoolean();
        return true;
    }
}
