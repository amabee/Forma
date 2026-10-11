using Forma.Core.Events;

namespace Forma.Core.Controls;

public class Button : Control
{
    public event EventHandler? Click;

    private bool _enabled = true;
    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }

    public event EventHandler<MouseEventArgs>? MouseDown;
    public event EventHandler<MouseEventArgs>? MouseUp;

    public void OnClick()
    {
        if(!Enabled)
        {
            return;
        }

        Click?.Invoke(this, EventArgs.Empty);
    }
}
