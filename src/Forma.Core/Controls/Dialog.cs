namespace Forma.Core.Controls;

/// <summary>Modal message/confirmation state. Open state and results are transient.</summary>
public class Dialog : Component
{
    private string _dialogTitle = "Message";
    private string _message = "Your message here.";
    private string _buttons = "OKCancel";
    private bool _canCancel = true;
    private bool _isOpen;
    private string _result = "";
    public string DialogTitle { get => _dialogTitle; set { ArgumentNullException.ThrowIfNull(value); SetProperty(ref _dialogTitle, value); } }
    public string Message { get => _message; set { ArgumentNullException.ThrowIfNull(value); SetProperty(ref _message, value); } }
    public string Buttons
    {
        get => _buttons;
        set
        {
            if (value is not ("OK" or "OKCancel" or "YesNo" or "YesNoCancel")) throw new ArgumentException("Unsupported dialog buttons.");
            SetProperty(ref _buttons, value);
        }
    }
    public bool CanCancel { get => _canCancel; set => SetProperty(ref _canCancel, value); }
    public bool IsOpen => _isOpen;
    public string Result => _result;
    public event EventHandler<DialogClosedEventArgs>? Closed;
    public void Show()
    {
        if (_isOpen) return;
        SetProperty(ref _result, "", nameof(Result));
        SetProperty(ref _isOpen, true, nameof(IsOpen));
    }
    public void Close(string result)
    {
        if (!_isOpen) return;
        var allowed = Buttons switch { "OK" => new[] { "OK" }, "OKCancel" => ["OK", "Cancel"], "YesNo" => ["Yes", "No"], _ => ["Yes", "No", "Cancel"] };
        // Application code can always cancel; CanCancel controls user dismissal.
        if (result != "Cancel" && !allowed.Contains(result)) return;
        SetProperty(ref _isOpen, false, nameof(IsOpen));
        SetProperty(ref _result, result, nameof(Result));
        Closed?.Invoke(this, new(result));
    }
}
