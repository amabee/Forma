namespace Forma.Core.Controls;

public sealed class Spinner : Control
{
    private bool _isActive = true;
    private int _speed = 800;
    public Spinner() => Text = "Loading…";
    public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }
    public int Speed { get => _speed; set => SetProperty(ref _speed, Math.Clamp(value, 100, 5000)); }
}
