using Forma.Core.Rendering;
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

    protected override void WriteRenderState(IDictionary<string, object?> state)
    {
        base.WriteRenderState(state);
        state["enabled"] = Enabled;
    }

    protected override void OnRuntimeEvent(RuntimeEvent e)
    {
        base.OnRuntimeEvent(e);
        if (e.Name == "click") OnClick();
    }
}
