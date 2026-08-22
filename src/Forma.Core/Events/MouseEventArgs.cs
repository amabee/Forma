namespace Forma.Core.Events;

public class MouseEventArgs : EventArgs
{
    public MouseEventArgs(int x, int y, bool leftButton, bool rightButton)
    {
        X = x;
        Y = y;
        LeftButton = leftButton;
        RightButton = rightButton;
    }

    public int X { get; }
    public int Y { get; }
    public bool LeftButton { get; }
    public bool RightButton { get; }
}
