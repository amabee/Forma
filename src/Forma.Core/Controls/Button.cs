namespace Forma.Core.Controls;

public sealed class Button : Control
{
    public event EventHandler? Click;

    public void RaiseClick()
    {
        Click?.Invoke(this, EventArgs.Empty);
    }
}
