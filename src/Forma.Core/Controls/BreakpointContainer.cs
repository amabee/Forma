namespace Forma.Core.Controls;

/// <summary>Free-position wide (slot 1) and narrow (slot 2) child layouts.</summary>
public sealed class BreakpointContainer : LayoutContainer
{
    private int _breakpoint = 600;
    public int Breakpoint
    {
        get => _breakpoint;
        set => SetProperty(ref _breakpoint, Math.Clamp(value, 100, 2400));
    }

    protected override void WriteRenderState(IDictionary<string, object?> state)
    {
        base.WriteRenderState(state);
        state["breakpoint"] = Breakpoint;
    }
}
