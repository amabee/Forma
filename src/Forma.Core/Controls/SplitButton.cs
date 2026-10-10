namespace Forma.Core.Controls;

public sealed class SplitButton : DropdownButton
{
    private bool _primaryEnabled = true;
    public SplitButton() => Text = "Run";
    public bool PrimaryEnabled { get => _primaryEnabled; set => SetProperty(ref _primaryEnabled, value); }
    public event EventHandler? PrimaryClick;
    public void InvokePrimary() { if (PrimaryEnabled) PrimaryClick?.Invoke(this, EventArgs.Empty); }
}
