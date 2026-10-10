using System.Reflection;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Forma.Core.Controls;
using Control = Forma.Core.Controls.Control;

namespace Forma.Builder;

/// <summary>Explicit script extensions. Inspector metadata validates catalog entries;
/// the same JSON catalog drives editor suggestions.</summary>
public static class RuntimePropertyService
{
    private sealed class Catalog
    {
        public string[] Appearance { get; set; } = [];
        public string[] Layout { get; set; } = [];
        public Dictionary<string, string[]> Specific { get; set; } = [];
        public Dictionary<string, string[]> ReadOnly { get; set; } = [];
    }
    private static readonly Catalog Properties = Load();
    private static readonly ConcurrentDictionary<string, string[]> AvailableCache = new();
    private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static Catalog Load()
    {
        using var stream = typeof(RuntimePropertyService).Assembly.GetManifestResourceStream("Forma.RuntimeScriptProperties.json")!;
        return JsonSerializer.Deserialize<Catalog>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
    private static string Name(string property) => char.ToUpperInvariant(property[0]) + property[1..];
    private static bool UsesAppearance(Control control, string property) => Properties.Appearance.Contains(property) && property is not ("x" or "y")
        || property == "style"
        || control is Forma.Core.Controls.TextBox && property is "placeholder" or "readOnly" or "password" or "maxLength";
    private static IEnumerable<string> Available(Control control) => AvailableCache.GetOrAdd(control.ControlType, kind =>
    {
        var inspector = InspectorCatalog.ForKind(kind).Select(p => p.Id).ToHashSet();
        return Properties.Appearance.Concat(Properties.Layout).Concat(Properties.Specific.GetValueOrDefault(kind) ?? [])
            .Where(inspector.Contains).Concat(Properties.ReadOnly.GetValueOrDefault(kind) ?? []).Distinct().ToArray();
    });
    public static void Project(Control control, Appearance appearance, Dictionary<string, object?> state)
    {
        var available = Available(control).ToArray(); state["scriptProperties"] = available;
        foreach (var property in available)
        {
            if (property is "x" or "y") { state[property] = property == "x" ? control.X ?? 0 : control.Y ?? 0; continue; }
            var source = UsesAppearance(control, property) ? (object)appearance : control;
            var member = source.GetType().GetProperty(Name(property));
            if (member?.CanRead == true)
            {
                var value = member.GetValue(source);
                state[property] = value is Array ? JsonSerializer.SerializeToElement(value, CamelCase) : value;
            }
        }
    }
    public static bool TrySet(Control control, Appearance appearance, string property, JsonElement value)
    {
        if (!Available(control).Contains(property)) return false;
        if ((Properties.ReadOnly.GetValueOrDefault(control.ControlType) ?? []).Contains(property)) throw new ArgumentException($"Property '{property}' is read-only.");
        var descriptor = InspectorCatalog.ForKind(control.ControlType).First(p => p.Id == property);
        if (descriptor.ReadOnly) throw new ArgumentException($"Property '{property}' is read-only.");
        var target = UsesAppearance(control, property) ? (object)appearance : control;
        var member = target.GetType().GetProperty(Name(property)) ?? throw new ArgumentException($"Property '{property}' has no runtime model.");
        var type = Nullable.GetUnderlyingType(member.PropertyType) ?? member.PropertyType;
        object converted;
        if (type.IsArray && value.ValueKind == JsonValueKind.Array)
        {
            if (type == typeof(string[]) && value.EnumerateArray().Any(entry => entry.ValueKind != JsonValueKind.String)) throw new ArgumentException($"'{property}' requires an array of strings.");
            try { converted = value.Deserialize(type, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!; }
            catch (JsonException error) { throw new ArgumentException($"Invalid '{property}' array.", error); }
        }
        else if (type == typeof(bool) && value.ValueKind is JsonValueKind.True or JsonValueKind.False) converted = value.GetBoolean();
        else if (type == typeof(int) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var integer)) converted = Math.Clamp(integer, (int)(descriptor.Min ?? int.MinValue), (int)(descriptor.Max ?? int.MaxValue));
        else if (type == typeof(double) && value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number) && double.IsFinite(number)) converted = Math.Clamp(number, descriptor.Min ?? double.MinValue, descriptor.Max ?? double.MaxValue);
        else if (type == typeof(string) && value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString()!;
            if (text.Length > 32767 || text.Contains('\0')) throw new ArgumentException($"Invalid '{property}' string.");
            if (descriptor.Options is not null && !descriptor.Options.Contains(text)) throw new ArgumentException($"Invalid '{property}' option.");
            if (descriptor.Editor == "color" && text != "transparent" && !Regex.IsMatch(text, "^#[0-9a-fA-F]{6}$")) throw new ArgumentException("Use a six-digit hex color, e.g. #2878ff, or transparent.");
            converted = text;
        }
        else throw new ArgumentException($"Property '{property}' requires a {type.Name} value.");
        if (property is "x" or "y" && (control.Parent is LinearLayout or Forma.Core.Controls.TableLayoutPanel || appearance.Dock != "none")) throw new ArgumentException("The parent layout or Dock controls this component's position.");
        if (property == "url" && converted is string url && (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http"))) throw new ArgumentException("Use an http or https URL.");
        try { member.SetValue(target, converted); }
        catch (TargetInvocationException error) when (error.InnerException is ArgumentException) { throw new ArgumentException(error.InnerException.Message); }
        if (property is "backColor" or "foreColor" or "borderColor") appearance.Style = "Custom";
        if (property == "style" && appearance.Style != "Custom")
        {
            appearance.ForeColor = appearance.Style is "Default" or "Warning" ? "#1f2937" : "#ffffff";
            appearance.BackColor = appearance.Style switch { "Primary" => "#2878ff", "Secondary" => "#64748b", "Success" => "#15803d", "Warning" => "#f59e0b", "Danger" => "#dc2626", _ => "#f3f4f6" };
            appearance.BorderColor = appearance.BackColor;
        }
        if (control is Forma.Core.Controls.TextBox textbox)
        {
            if (property == "readOnly") textbox.ReadOnly = appearance.ReadOnly;
            if (property == "password") textbox.Password = appearance.Password;
            if (property == "maxLength") textbox.MaxLength = appearance.MaxLength;
        }
        if (property is "width" or "height" or "minimumWidth" or "minimumHeight" or "maximumWidth" or "maximumHeight")
        {
            appearance.Width = Math.Clamp(appearance.Width, Math.Max(24, appearance.MinimumWidth), Math.Max(Math.Max(24, appearance.MinimumWidth), appearance.MaximumWidth == 0 ? 1600 : appearance.MaximumWidth));
            appearance.Height = Math.Clamp(appearance.Height, Math.Max(20, appearance.MinimumHeight), Math.Max(Math.Max(20, appearance.MinimumHeight), appearance.MaximumHeight == 0 ? 1200 : appearance.MaximumHeight));
            if (control is Forma.Core.Form form) { form.Width = appearance.Width = Math.Max(240, appearance.Width); form.Height = appearance.Height = Math.Max(160, appearance.Height); }
        }
        return true;
    }
}
