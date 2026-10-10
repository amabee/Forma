namespace Forma.Core.Controls;

public abstract class NumericControl : Control
{
    private double _minimum,
        _maximum = 100,
        _value,
        _increment = 1;
    public double Minimum
    {
        get => _minimum;
        set
        {
            if (!double.IsFinite(value))
                return;
            SetProperty(ref _minimum, Math.Min(value, _maximum));
            Value = _value;
        }
    }
    public double Maximum
    {
        get => _maximum;
        set
        {
            if (!double.IsFinite(value))
                return;
            SetProperty(ref _maximum, Math.Max(value, _minimum));
            Value = _value;
        }
    }
    public double Increment
    {
        get => _increment;
        set
        {
            if (double.IsFinite(value) && value > 0)
                SetProperty(ref _increment, value);
        }
    }

    // A numeric value is separate from Control.Value's text alias.
    public new virtual double Value
    {
        get => _value;
        set
        {
            if (!double.IsFinite(value))
                return;
            var next = Math.Clamp(value, _minimum, _maximum);
            if (next == _value)
                return;
            SetProperty(ref _value, next);
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler? ValueChanged;
}
