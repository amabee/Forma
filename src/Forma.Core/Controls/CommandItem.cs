namespace Forma.Core.Controls;

public sealed record CommandItem(string Id, string Text, bool Enabled = true, bool Checked = false,
    bool CheckOnClick = false, bool Separator = false, CommandItem[]? Items = null);
