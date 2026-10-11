namespace Forma.Core.Controls;
public sealed class ScrollablePanel : LayoutContainer
{
    private string _scrollDirection = "both";
    public string ScrollDirection { get => _scrollDirection; set { if (value is "both" or "horizontal" or "vertical") SetProperty(ref _scrollDirection, value); } }

    protected override void WriteRenderState(IDictionary<string, object?> state)
    {
        base.WriteRenderState(state);
        state["scrollDirection"] = ScrollDirection;
    }
}
