namespace Forma.Core.Controls;

/// <summary>A non-wrapping stack with a separate direction below the breakpoint.</summary>
public sealed class ResponsiveStack : ResponsiveLayout
{
    private string _narrowOrientation = "vertical";
    public string NarrowOrientation
    {
        get => _narrowOrientation;
        set => SetProperty(ref _narrowOrientation, value == "horizontal" ? value : "vertical");
    }

    protected override void WriteRenderState(IDictionary<string, object?> state)
    {
        base.WriteRenderState(state);
        state["narrowOrientation"] = NarrowOrientation;
    }
}
