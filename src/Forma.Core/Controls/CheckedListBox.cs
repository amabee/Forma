namespace Forma.Core.Controls;

public sealed class CheckedListBox : Control
{
    private string[] _items = ["Item 1", "Item 2", "Item 3"];
    private int[] _checkedIndices = [];
    public string[] Items { get => (string[])_items.Clone(); set { ArgumentNullException.ThrowIfNull(value); SetProperty(ref _items, (string[])value.Clone()); CheckedIndices = _checkedIndices; } }
    public int[] CheckedIndices { get => (int[])_checkedIndices.Clone(); set { ArgumentNullException.ThrowIfNull(value); var next = value.Where(i => i >= 0 && i < _items.Length).Distinct().Order().ToArray(); if (!_checkedIndices.SequenceEqual(next)) SetProperty(ref _checkedIndices, next); } }
    public event EventHandler<ItemCheckEventArgs>? ItemCheck;
    public void SetItemChecked(int index, bool value) {
        if (index < 0 || index >= _items.Length || _checkedIndices.Contains(index) == value) return;
        CheckedIndices = value ? [.._checkedIndices, index] : _checkedIndices.Where(i => i != index).ToArray();
        ItemCheck?.Invoke(this, new(index, value));
    }
}
