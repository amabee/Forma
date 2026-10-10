namespace Forma.Core.Controls;

public sealed class Rating : NumericControl
{
    private int _stars = 5;
    private bool _readOnly;
    public Rating() { Maximum = 5; }
    public override double Value { get => base.Value; set => base.Value = Math.Round(value, MidpointRounding.AwayFromZero); }
    public int Stars { get => _stars; set { SetProperty(ref _stars, Math.Clamp(value, 1, 10)); Maximum = _stars; } }
    public bool ReadOnly { get => _readOnly; set => SetProperty(ref _readOnly, value); }
}
