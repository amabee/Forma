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
}
