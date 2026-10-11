namespace Forma.Core.Controls;

public class Toolbar : CommandControl
{
    private string _orientation = "horizontal";
    public Toolbar() => Items = [new("new", "New"), new("open", "Open"), new("save", "Save")];
    public string Orientation
    {
        get => _orientation;
        set => SetProperty(ref _orientation, value == "vertical" ? "vertical" : "horizontal");
    }

    protected override void WriteRenderState(IDictionary<string, object?> state)
    {
        base.WriteRenderState(state);
        state["orientation"] = Orientation;
    }
}
