namespace Forma.Core.Controls;

public sealed class Chip : CheckBox
{
    private bool _removable, _isRemoved;
    private string _variant = "neutral";
    public Chip() => Text = "Chip";
    public bool Removable { get => _removable; set => SetProperty(ref _removable, value); }
    public bool IsRemoved => _isRemoved;
    public string Variant { get => _variant; set { if (value is "neutral" or "info" or "success" or "warning" or "danger") SetProperty(ref _variant, value); } }
    public event EventHandler? Removed;
    public void Remove() { if (!Removable || _isRemoved) return; SetProperty(ref _isRemoved, true, nameof(IsRemoved)); Removed?.Invoke(this, EventArgs.Empty); }
    public void Restore() => SetProperty(ref _isRemoved, false, nameof(IsRemoved));
}
