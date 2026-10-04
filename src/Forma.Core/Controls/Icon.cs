namespace Forma.Core.Controls;

public sealed class Icon : Control
{
    public static IReadOnlyList<string> Names { get; } = Array.AsReadOnly(new[] { "image", "search", "folder-open", "square-check", "circle", "house", "settings", "lock", "calendar", "file-plus", "list", "x" });
    private string _iconName = "image";
    private int _strokeWidth = 2;
    public Icon() => Text = "Icon";
    public string IconName { get => _iconName; set { if (Names.Contains(value)) SetProperty(ref _iconName, value); } }
    public int StrokeWidth { get => _strokeWidth; set => SetProperty(ref _strokeWidth, Math.Clamp(value, 1, 4)); }
}
