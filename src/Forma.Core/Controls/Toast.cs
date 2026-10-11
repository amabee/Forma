using Forma.Core.Rendering;

namespace Forma.Core.Controls;

public sealed class Toast : Component
{
    public static IReadOnlyList<string> Variants { get; } = Array.AsReadOnly(new[] { "neutral", "info", "success", "warning", "caution", "error", "danger" });
    private string _variant = "info", _position = "bottom-right";
    private int _duration = 4000;
    private bool _dismissible = true, _isOpen;
    public Toast() => Text = "Notification message";
    public string Variant { get => _variant; set { if (Variants.Contains(value)) SetProperty(ref _variant, value); } }
    public string Position { get => _position; set { if (value is "top-right" or "top-left" or "bottom-right" or "bottom-left") SetProperty(ref _position, value); } }
    public int Duration { get => _duration; set => SetProperty(ref _duration, Math.Clamp(value, 500, 60000)); }
    public bool Dismissible { get => _dismissible; set => SetProperty(ref _dismissible, value); }
    public bool IsOpen => _isOpen;
    public event EventHandler? Closed;
    public void Show() => SetProperty(ref _isOpen, true, nameof(IsOpen));
    public void Close() { if (!_isOpen) return; SetProperty(ref _isOpen, false, nameof(IsOpen)); Closed?.Invoke(this, EventArgs.Empty); }

    protected override void WriteRenderState(IDictionary<string, object?> state)
    {
        base.WriteRenderState(state);
        state["variant"] = Variant; state["position"] = Position; state["duration"] = Duration; state["dismissible"] = Dismissible; state["isOpen"] = IsOpen;
    }

    protected override void OnRuntimeEvent(RuntimeEvent e)
    {
        base.OnRuntimeEvent(e);
        // A timeout closes even non-dismissible toasts; users can only dismiss dismissible ones.
        if (e.Name == "toast-close" && (Dismissible || e.TryGetString("reason", out var reason) && reason == "timeout")) Close();
    }
}
