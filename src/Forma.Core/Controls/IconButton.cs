namespace Forma.Core.Controls;

public class IconButton : Button
{
    private string _iconName = "search";
    private bool _showText;
    public string IconName { get => _iconName; set { if (Icon.Names.Contains(value)) SetProperty(ref _iconName, value); } }
    public bool ShowText { get => _showText; set => SetProperty(ref _showText, value); }
    public IconButton() => Text = "Search";
}
