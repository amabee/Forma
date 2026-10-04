namespace Forma.Core.Controls;

public sealed class Skeleton : Control
{
    private string _shape = "text";
    private int _lines = 3;
    private bool _isActive = true;
    public Skeleton() => Text = "Loading content";
    public string Shape { get => _shape; set { if (value is "text" or "rectangle" or "circle") SetProperty(ref _shape, value); } }
    public int Lines { get => _lines; set => SetProperty(ref _lines, Math.Clamp(value, 1, 10)); }
    public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }
}
