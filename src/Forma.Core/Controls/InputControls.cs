using System.Globalization;

namespace Forma.Core.Controls;

public abstract class NumericControl : Control
{
    private double _minimum, _maximum = 100, _value, _increment = 1;
    public double Minimum { get => _minimum; set { if (!double.IsFinite(value)) return; SetProperty(ref _minimum, Math.Min(value, _maximum)); Value = _value; } }
    public double Maximum { get => _maximum; set { if (!double.IsFinite(value)) return; SetProperty(ref _maximum, Math.Max(value, _minimum)); Value = _value; } }
    public double Increment { get => _increment; set { if (double.IsFinite(value) && value > 0) SetProperty(ref _increment, value); } }
    // A numeric value is separate from Control.Value's text alias.
    public new double Value { get => _value; set { if (!double.IsFinite(value)) return; var next = Math.Clamp(value, _minimum, _maximum); if (next == _value) return; SetProperty(ref _value, next); ValueChanged?.Invoke(this, EventArgs.Empty); } }
    public event EventHandler? ValueChanged;
}
public sealed class NumericUpDown : NumericControl { }
public sealed class Slider : NumericControl { }
public sealed class ProgressBar : NumericControl { }
public sealed class CircularProgress : NumericControl { }
public sealed class ToggleSwitch : CheckBox { }
public sealed class ToggleButton : CheckBox { }
public sealed class SearchBox : TextBox { }
public sealed class PasswordBox : TextBox { public PasswordBox() => Password = true; }
public sealed class TextArea : TextBox { public TextArea() => Multiline = true; }

public abstract class DateTimeInput : Control
{
    private string _dateValue = "";
    protected abstract string Format { get; }
    public string DateValue
    {
        get => _dateValue;
        set {
            if (value != "" && !DateTime.TryParseExact(value, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) return;
            if (_dateValue == value) return;
            SetProperty(ref _dateValue, value); ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler? ValueChanged;
}
public sealed class DatePicker : DateTimeInput { protected override string Format => "yyyy-MM-dd"; }
public sealed class TimePicker : DateTimeInput { protected override string Format => "HH:mm"; }
public sealed class DateTimePicker : DateTimeInput { protected override string Format => "yyyy-MM-dd'T'HH:mm"; }
public sealed class ColorPicker : Control
{
    private string _color = "#2878ff";
    public string Color { get => _color; set { if (value is not { Length: 7 } || value[0] != '#' || !value.Skip(1).All(Uri.IsHexDigit) || value == _color) return; SetProperty(ref _color, value); ColorChanged?.Invoke(this, EventArgs.Empty); } }
    public event EventHandler? ColorChanged;
}
