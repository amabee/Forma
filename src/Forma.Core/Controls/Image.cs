namespace Forma.Core.Controls;

public class Image : Control
{
    private string _source = "";
    private string _sizeMode = "contain";
    public string Source
    {
        get => _source;
        set => SetProperty(ref _source, value);
    }
    public string SizeMode
    {
        get => _sizeMode;
        set =>
            SetProperty(ref _sizeMode, value is "contain" or "cover" or "fill" ? value : "contain");
    }

    protected override void WriteRenderState(IDictionary<string, object?> state)
    {
        base.WriteRenderState(state);
        state["source"] = Source; state["sizeMode"] = SizeMode;
    }
}
