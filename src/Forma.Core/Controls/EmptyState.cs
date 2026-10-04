namespace Forma.Core.Controls;

public sealed class EmptyState : Control
{
    private string _description = "Add items to see them here.", _iconName = "folder-open";
    public EmptyState() => Text = "Nothing here yet";
    public string Description { get => _description; set { ArgumentNullException.ThrowIfNull(value); SetProperty(ref _description, value); } }
    public string IconName { get => _iconName; set { if (Icon.Names.Contains(value)) SetProperty(ref _iconName, value); } }
}
