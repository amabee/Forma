namespace Forma.Core.Controls;

public sealed class Timer : Component, IDisposable
{
    private System.Threading.Timer? _timer;
    private int _interval = 1000;
    private volatile int _generation;
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
        // Like the WinForms Timer, raise Tick on the thread that started the timer
        // (the UI thread), so handlers can update controls safely.
        var context = SynchronizationContext.Current;
        var generation = ++_generation;
        void Raise()
        {
            // A callback queued before Stop() must not tick afterwards.
            if (generation == _generation && _timer is not null)
                Tick?.Invoke(this, EventArgs.Empty);
        }
        _timer = new System.Threading.Timer(
            _ =>
            {
                if (context is null) Raise();
                else context.Post(_ => Raise(), null);
            },
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
        _generation++;
        _timer.Dispose();
        _timer = null;
        OnPropertyChanged(nameof(Enabled));
    }

    public void Dispose() => Stop();
}
