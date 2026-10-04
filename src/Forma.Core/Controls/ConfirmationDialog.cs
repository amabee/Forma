namespace Forma.Core.Controls;

public sealed class ConfirmationDialog : Dialog
{
    public ConfirmationDialog() { DialogTitle = "Confirm"; Message = "Do you want to continue?"; Buttons = "YesNo"; }
}
