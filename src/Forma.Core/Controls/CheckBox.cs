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
            _checked = value;
            OnCheckedChanged();
        }
    }
    public event EventHandler? CheckedChanged;

    protected virtual void OnCheckedChanged()
    {
        OnPropertyChanged(nameof(Checked));
        CheckedChanged?.Invoke(this, EventArgs.Empty);
    }
}
