namespace Forma.Core.Controls;

public sealed class CommandButton : IconButton
{
    private string _description = "Perform an action";
    public CommandButton() { Text = "Command"; IconName = "file-plus"; ShowText = true; }
    public string Description { get => _description; set { ArgumentNullException.ThrowIfNull(value); SetProperty(ref _description, value); } }
}
