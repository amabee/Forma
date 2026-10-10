namespace Forma.Core.Controls;

public abstract class ChoiceControl : Control
{
    private string[] _items = ["Item 1", "Item 2", "Item 3"];
    private int _selectedIndex;
    public string[] Items
    {
        get => (string[])_items.Clone();
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.Any(item => item is null)) throw new ArgumentException("Items cannot be null.");
            SetProperty(ref _items, (string[])value.Clone());
            if (_selectedIndex >= value.Length)
                SelectedIndex = value.Length - 1;
        }
    }
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var index = Math.Clamp(value, -1, Math.Max(-1, _items.Length - 1));
            if (_selectedIndex == index)
                return;
            SetProperty(ref _selectedIndex, index);
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler? SelectedIndexChanged;
}
