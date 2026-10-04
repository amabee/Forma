namespace Forma.Core.Controls;

public sealed class Badge : Control
{
    private string _variant = "info";
    public Badge() => Text = "Badge";
    public string Variant { get => _variant; set { if (value is "neutral" or "info" or "success" or "warning" or "danger") SetProperty(ref _variant, value); } }
}
