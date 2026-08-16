namespace Forma.Core.Controls;

public class Button : Control
{
    public event EventHandler? Click;

    public void RaiseClick()
    {
        Click?.Invoke(this, EventArgs.Empty);
    }
}
