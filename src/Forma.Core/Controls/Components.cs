namespace Forma.Core.Controls;

/// <summary>A nonvisual component represented in the Builder's component tray.</summary>
public abstract class Component : Control { }

public sealed class Timer : Component, IDisposable
{
    private System.Threading.Timer? _timer;
    private int _interval = 1000;
    public int Interval
    {
        get => _interval;
        set
        {
            SetProperty(ref _interval, Math.Clamp(value, 10, 3600000));
            _timer?.Change(_interval, _interval);
        }
    }
    public bool Enabled
    {
        get => _timer is not null;
        set
        {
            if (value)
                Start();
            else
                Stop();
        }
    }
    public event EventHandler? Tick;

    public void Start()
    {
        if (_timer is not null)
            return;
        _timer = new System.Threading.Timer(
            _ => Tick?.Invoke(this, EventArgs.Empty),
            null,
            _interval,
            _interval
        );
        OnPropertyChanged(nameof(Enabled));
    }

    public void Stop()
    {
        if (_timer is null)
            return;
        _timer.Dispose();
        _timer = null;
        OnPropertyChanged(nameof(Enabled));
    }

    public void Dispose() => Stop();
}

public sealed class WorkerCompletedEventArgs(bool cancelled, Exception? error) : EventArgs
{
    public bool Cancelled { get; } = cancelled;
    public Exception? Error { get; } = error;
}

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
