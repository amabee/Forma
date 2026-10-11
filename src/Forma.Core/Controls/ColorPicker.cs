using Forma.Core.Rendering;

namespace Forma.Core.Controls;

public sealed class ColorPicker : Control
{
    private string _color = "#2878ff";
    public string Color
    {
        get => _color;
        set
        {
            if (
                value is not { Length: 7 }
                || value[0] != '#'
                || !value.Skip(1).All(Uri.IsHexDigit)
                || value == _color
            )
                return;
            SetProperty(ref _color, value);
            ColorChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler? ColorChanged;

    protected override void WriteRenderState(IDictionary<string, object?> state)
    {
        base.WriteRenderState(state);
        state["color"] = Color;
    }

    protected override void OnRuntimeEvent(RuntimeEvent e)
    {
        base.OnRuntimeEvent(e);
        if (e.Name == "value" && e.TryGetString("value", out var value)) Color = value;
    }
}
