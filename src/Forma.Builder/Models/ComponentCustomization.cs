using System.Text.Json;

namespace Forma.Builder;

public sealed class ComponentCustomization
{
    public string Css { get; set; } = "";
    public string Behavior { get; set; } = "";
    public string Characteristics { get; set; } = "{}";

    public static ComponentCustomization Validate(ComponentCustomization value)
    {
        if (value.Css is null || value.Behavior is null || value.Characteristics is null)
            throw new ArgumentException("Component sources cannot be null.");
        if (value.Css.Length > 200_000 || value.Behavior.Length > 200_000 || value.Characteristics.Length > 200_000)
            throw new ArgumentException("Each component file must be under 200,000 characters.");
        using var json = JsonDocument.Parse(value.Characteristics);
        if (json.RootElement.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Custom values must be a JSON object.");
        return new() { Css = value.Css, Behavior = value.Behavior, Characteristics = value.Characteristics };
    }
}
