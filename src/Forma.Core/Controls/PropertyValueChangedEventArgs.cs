namespace Forma.Core.Controls;

public sealed class PropertyValueChangedEventArgs(int index, string name, string value) : EventArgs
{
    public int Index { get; } = index;
    public string Name { get; } = name;
    public string Value { get; } = value;
}
