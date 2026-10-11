namespace Forma.Core.Controls;

public abstract class ResponsiveLayout : LinearLayout
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
