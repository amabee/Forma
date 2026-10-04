namespace Forma.Core.Controls;

/// <summary>Validated command trees shared by menus and toolbars.</summary>
public abstract class CommandControl : Control
{
    private CommandItem[] _items = [new("action", "Action")];
    public CommandItem[] Items
    {
        get => Copy(_items);
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var count = 0;
            void Validate(CommandItem[] items, int depth)
            {
                if (depth > 8) throw new ArgumentException("Command trees support at most eight levels.");
                foreach (var item in items)
                {
                    if (++count > 300 || item is null || string.IsNullOrWhiteSpace(item.Id) || item.Id.Length > 128
                        || !ids.Add(item.Id) || item.Text is null || item.Text.Length > 1024)
                        throw new ArgumentException("Command items require unique IDs and valid text (maximum 300 items).");
                    if (item.Items is { Length: > 0 } children)
                    {
                        if (item.Separator) throw new ArgumentException("A separator cannot contain commands.");
                        Validate(children, depth + 1);
                    }
                }
            }
            Validate(value, 1);
            SetProperty(ref _items, Copy(value));
        }
    }
    public event EventHandler<CommandItemClickedEventArgs>? ItemClicked;

    public void InvokeItem(string id)
    {
        CommandItem? found = null;
        CommandItem[] Visit(CommandItem[] items, bool parentEnabled)
        {
            return items.Select(item =>
            {
                var enabled = parentEnabled && item.Enabled && !item.Separator;
                if (item.Id == id && enabled && (item.Items is null || item.Items.Length == 0))
                {
                    found = item.CheckOnClick ? item with { Checked = !item.Checked } : item;
                    return found;
                }
                return item.Items is null ? item : item with { Items = Visit(item.Items, enabled) };
            }).ToArray();
        }
        var updated = Visit(_items, true);
        if (found is null) return;
        if (found.CheckOnClick) SetProperty(ref _items, updated, nameof(Items));
        ItemClicked?.Invoke(this, new(found.Id, found.Text, found.Checked));
    }

    private static CommandItem[] Copy(CommandItem[] items) => items.Select(item =>
        item with { Items = item.Items is null ? null : Copy(item.Items) }).ToArray();
}
