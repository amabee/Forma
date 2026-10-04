namespace Forma.Core.Controls;

public sealed class Avatar : Image
{
    private string _initials = "", _shape = "circle";
    public Avatar() { Text = "User"; SizeMode = "cover"; }
    public string Initials { get => _initials; set { ArgumentNullException.ThrowIfNull(value); SetProperty(ref _initials, value.Length > 4 ? value[..4] : value); } }
    public string Shape { get => _shape; set { if (value is "circle" or "rounded" or "square") SetProperty(ref _shape, value); } }
}
