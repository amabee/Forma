namespace Forma.Core.Controls;

public sealed class CellChangedEventArgs(int row, int column, string value) : EventArgs
{
    public int Row { get; } = row;
    public int Column { get; } = column;
    public string Value { get; } = value;
}
