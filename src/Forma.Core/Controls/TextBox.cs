using Forma.Core.Events;

namespace Forma.Core.Controls;

public class TextBox : Control {

    public event EventHandler? TextChanged;
    public event EventHandler? EnterPressed;
    public event EventHandler? EscapePressed;
    public event EventHandler? GotFocus;
    public event EventHandler? LostFocus;

    public event EventHandler<KeyEventArgs>? KeyDown;
    public event EventHandler<KeyEventArgs>? KeyUp;

    public bool ReadOnly { get; set; }
    public bool Multiline { get; set; }
    public bool Password { get; set; }
    public bool SpellCheck { get; set; } = true;

    public int MaxLength { get; set; }
    public int MinLength { get; set; }

    public int SelectionStart { get; set; }
    public int SelectionEnd { get; set; }
    public int SelectionLength { get; set; }

    public virtual void OnTextChanged()
    {
        TextChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetText(string? text)
    {
        if (Text != text)
        {
            Text = text;
            OnTextChanged();
        }
    }


    public void SetSelection(int start, int end)
    {
        SelectionStart = start;
        SelectionEnd = end;
        SelectionLength = end - start;
    }

    public virtual void SetFocus()
    {
        GotFocus?.Invoke(this, EventArgs.Empty);
    }

    public virtual void OnEnterPressed()
    {
        EnterPressed?.Invoke(this, EventArgs.Empty);
    }

    public virtual void OnEscapePressed()
    {
        EscapePressed?.Invoke(this, EventArgs.Empty);
    }

    public virtual void OnGotFocus()
    {
        GotFocus?.Invoke(this, EventArgs.Empty);
    }

    public virtual void OnLostFocus()
    {
        LostFocus?.Invoke(this, EventArgs.Empty);
    }

    public virtual void OnKeyDown(KeyEventArgs e)
    {
        KeyDown?.Invoke(this, e);
    }

    public virtual void OnKeyUp(KeyEventArgs e)
    {
        KeyUp?.Invoke(this, e);
    }

 }
