using Forma.Core.Events;

namespace Forma.Core.Controls;

public sealed class Button : Control
{
    public event EventHandler? Click;

    public bool Enabled { get; set; } = true;

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
