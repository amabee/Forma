namespace Forma.Core.Controls;

public sealed class Divider : Control
{
    private string _orientation = "horizontal", _lineStyle = "solid";
    private int _thickness = 1;
    public string Orientation { get => _orientation; set { if (value is "horizontal" or "vertical") SetProperty(ref _orientation, value); } }
    public int Thickness { get => _thickness; set => SetProperty(ref _thickness, Math.Clamp(value, 1, 12)); }
    public string LineStyle { get => _lineStyle; set { if (value is "solid" or "dashed" or "dotted") SetProperty(ref _lineStyle, value); } }
}
