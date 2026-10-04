namespace Forma.Core.Controls;

public sealed class DialogClosedEventArgs(string result) : EventArgs
{
    public string Result { get; } = result;
}
