namespace Forma.Core.Controls;

public sealed class StatusBar : Control
{
    private string _rightText = "";
    public StatusBar() => Text = "Ready";
    public string RightText
    {
        get => _rightText;
        set { ArgumentNullException.ThrowIfNull(value); SetProperty(ref _rightText, value); }
    }
}
