namespace Forma.Core.Controls;

/// <summary>
/// Carries the control a tree-structure event relates to, such as the child
/// passed to <see cref="Control.ChildAdded"/> or <see cref="Control.ChildRemoved"/>.
/// </summary>
public sealed class ControlEventArgs(Control control) : EventArgs
{
    public Control Control { get; } = control;
}
