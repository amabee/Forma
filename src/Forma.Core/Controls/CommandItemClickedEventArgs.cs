namespace Forma.Core.Controls;

public sealed class CommandItemClickedEventArgs(string id, string text, bool isChecked) : EventArgs
{
    public string Id { get; } = id;
    public string Text { get; } = text;
    public bool Checked { get; } = isChecked;
}
