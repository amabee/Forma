namespace Forma.Core.Controls;

public sealed class Tooltip : Component
{
    private string _targetId = "";
    private int _initialDelay = 500, _showDuration = 5000;
    private string _placement = "top";
    public Tooltip() => Text = "Helpful tip";
    public string TargetId { get => _targetId; set { ArgumentNullException.ThrowIfNull(value); SetProperty(ref _targetId, value); } }
    public int InitialDelay { get => _initialDelay; set => SetProperty(ref _initialDelay, Math.Clamp(value, 0, 10000)); }
    public int ShowDuration { get => _showDuration; set => SetProperty(ref _showDuration, Math.Clamp(value, 500, 60000)); }
    public string Placement { get => _placement; set { if (value is "top" or "bottom" or "left" or "right") SetProperty(ref _placement, value); } }
}
