namespace Forma.Core.Events;

public class KeyEventArgs : EventArgs
{
    public KeyEventArgs(string key, bool ctrlKey, bool shiftKey, bool altKey)
    {
        Key = key;
        CtrlKey = ctrlKey;
        ShiftKey = shiftKey;
        AltKey = altKey;
    }

    public string Key { get; }
    public bool CtrlKey { get; }
    public bool ShiftKey { get; }
    public bool AltKey { get; }
    public bool Handled { get; set; }
}
