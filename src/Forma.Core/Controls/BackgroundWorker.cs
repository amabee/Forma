namespace Forma.Core.Controls;

public sealed class BackgroundWorker : Component, IDisposable
{
    private CancellationTokenSource? _cancellation;
    private int _busy;
    public bool IsBusy => Volatile.Read(ref _busy) != 0;
    public bool WorkerReportsProgress { get; set; } = true;
    public bool WorkerSupportsCancellation { get; set; } = true;
    public event EventHandler<int>? ProgressChanged;
    public event EventHandler<WorkerCompletedEventArgs>? RunWorkerCompleted;

    public async Task RunAsync(Func<CancellationToken, IProgress<int>, Task> work)
    {
        ArgumentNullException.ThrowIfNull(work);
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
            throw new InvalidOperationException("The worker is already running.");
        _cancellation = new CancellationTokenSource();
        var token = _cancellation.Token;
        var progress = new Progress<int>(value =>
        {
            if (WorkerReportsProgress)
                ProgressChanged?.Invoke(this, Math.Clamp(value, 0, 100));
        });
        Exception? error = null;
        var cancelled = false;
        try
        {
            await Task.Run(() => work(token, progress), token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            cancelled = true;
        }
        catch (Exception exception)
        {
            error = exception;
        }
        finally
        {
            _cancellation.Dispose();
            _cancellation = null;
            Interlocked.Exchange(ref _busy, 0);
        }
        RunWorkerCompleted?.Invoke(this, new(cancelled, error));
    }

    public void CancelAsync()
    {
        if (WorkerSupportsCancellation)
            _cancellation?.Cancel();
    }

    public void Dispose() => _cancellation?.Cancel();
}
