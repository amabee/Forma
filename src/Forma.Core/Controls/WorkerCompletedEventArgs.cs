namespace Forma.Core.Controls;

public sealed class WorkerCompletedEventArgs(bool cancelled, Exception? error) : EventArgs
{
    public bool Cancelled { get; } = cancelled;
    public Exception? Error { get; } = error;
}
