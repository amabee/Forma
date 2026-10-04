namespace Forma.Core.Controls;

public sealed class ItemCheckEventArgs(int index, bool isChecked) : EventArgs
{
    public int Index { get; } = index;
    public bool Checked { get; } = isChecked;
}
