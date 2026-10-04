namespace Forma.Core.Controls;

public sealed class Toast : Component
{
    private string _variant = "info", _position = "bottom-right";
    private int _duration = 4000;
    private bool _dismissible = true, _isOpen;
    public Toast() => Text = "Notification message";
    public string Variant { get => _variant; set { if (value is "neutral" or "info" or "success" or "warning" or "danger") SetProperty(ref _variant, value); } }
    public string Position { get => _position; set { if (value is "top-right" or "top-left" or "bottom-right" or "bottom-left") SetProperty(ref _position, value); } }
    public int Duration { get => _duration; set => SetProperty(ref _duration, Math.Clamp(value, 500, 60000)); }
    public bool Dismissible { get => _dismissible; set => SetProperty(ref _dismissible, value); }
    public bool IsOpen => _isOpen;
    public event EventHandler? Closed;
    public void Show() => SetProperty(ref _isOpen, true, nameof(IsOpen));
    public void Close() { if (!_isOpen) return; SetProperty(ref _isOpen, false, nameof(IsOpen)); Closed?.Invoke(this, EventArgs.Empty); }
}
