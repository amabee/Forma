using Forma.Core.Rendering;
using System.Globalization;

namespace Forma.Core.Controls;

public abstract class DateTimeInput : Control
{
    private string _dateValue = "";
    protected abstract string Format { get; }
    public string DateValue
    {
        get => _dateValue;
        set
        {
            if (
                value != ""
                && !DateTime.TryParseExact(
                    value,
                    Format,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out _
                )
            )
                return;
            if (_dateValue == value)
                return;
            SetProperty(ref _dateValue, value);
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler? ValueChanged;

    protected override void WriteRenderState(IDictionary<string, object?> state)
    {
        base.WriteRenderState(state);
        state["dateValue"] = DateValue;
    }

    protected override void OnRuntimeEvent(RuntimeEvent e)
    {
        base.OnRuntimeEvent(e);
        if (e.Name == "value" && e.TryGetString("value", out var value)) DateValue = value;
    }
}
