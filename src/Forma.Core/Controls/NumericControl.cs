using Forma.Core.Rendering;

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

    protected override void WriteRenderState(IDictionary<string, object?> state)
    {
        base.WriteRenderState(state);
        state["minimum"] = Minimum; state["maximum"] = Maximum; state["increment"] = Increment; state["number"] = Value;
    }

    /// <summary>Whether users can change <see cref="Value"/> from the rendered control.</summary>
    protected virtual bool AcceptsUserValue => false;

    protected override void OnRuntimeEvent(RuntimeEvent e)
    {
        base.OnRuntimeEvent(e);
        if (e.Name == "value" && AcceptsUserValue && e.TryGetDouble("value", out var value)) Value = value;
    }
}
