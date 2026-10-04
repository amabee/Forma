namespace Forma.Core.Controls;

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
