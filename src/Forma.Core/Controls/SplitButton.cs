using Forma.Core.Rendering;

namespace Forma.Core.Controls;

public sealed class SplitButton : DropdownButton
{
    private bool _primaryEnabled = true;
    public SplitButton() => Text = "Run";
    public bool PrimaryEnabled { get => _primaryEnabled; set => SetProperty(ref _primaryEnabled, value); }
    public event EventHandler? PrimaryClick;
    public void InvokePrimary() { if (PrimaryEnabled) PrimaryClick?.Invoke(this, EventArgs.Empty); }

    protected override void WriteRenderState(IDictionary<string, object?> state)
    {
        base.WriteRenderState(state);
        state["primaryEnabled"] = PrimaryEnabled;
    }

    protected override void OnRuntimeEvent(RuntimeEvent e)
    {
        base.OnRuntimeEvent(e);
        if (e.Name == "primary-click") InvokePrimary();
    }
}
