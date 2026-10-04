namespace Forma.Core.Controls;

public class CheckBox : Control
{
    private bool _checked;
    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value)
                return;
            SetProperty(ref _checked, value);
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler? CheckedChanged;
}
