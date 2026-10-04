namespace Forma.Core.Controls;

/// <summary>A categorized name/value editor. Object binding is supplied by application code.</summary>
public sealed class PropertyGrid : Control
{
    private PropertyEntry[] _entries = [new("Name", "Example"), new("Enabled", "True")];
    private bool _readOnly;
    public PropertyEntry[] Entries
    {
        get => (PropertyEntry[])_entries.Clone();
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.Length > 200 || value.Any(entry => entry is null || string.IsNullOrWhiteSpace(entry.Name)
                || entry.Name.Length > 256 || entry.Value is null || entry.Value.Length > 32767
                || entry.Category is null || entry.Category.Length > 256))
                throw new ArgumentException("Invalid property entries or entry limit exceeded.");
            if (_entries.SequenceEqual(value)) return;
            SetProperty(ref _entries, (PropertyEntry[])value.Clone());
        }
    }
    public bool ReadOnly { get => _readOnly; set => SetProperty(ref _readOnly, value); }
    public event EventHandler<PropertyValueChangedEventArgs>? PropertyValueChanged;
    public void SetEntryValue(int index, string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (ReadOnly || index < 0 || index >= _entries.Length || _entries[index].ReadOnly
            || value.Length > 32767 || _entries[index].Value == value) return;
        var next = (PropertyEntry[])_entries.Clone();
        next[index] = next[index] with { Value = value };
        Entries = next;
        PropertyValueChanged?.Invoke(this, new(index, next[index].Name, value));
    }
}
