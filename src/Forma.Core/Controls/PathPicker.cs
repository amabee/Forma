using Forma.Core.Rendering;

namespace Forma.Core.Controls;

/// <summary>Path selection state. The host handles BrowseRequested with a native dialog.</summary>
public abstract class PathPicker : Control
{
    private string _selectedPath = "";
    private string _dialogTitle = "Choose a path";
    public string SelectedPath
    {
        get => _selectedPath;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.Length > 32767 || value.Contains('\0')) throw new ArgumentException("Invalid path.");
            if (_selectedPath == value) return;
            SetProperty(ref _selectedPath, value);
            SelectedPathChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public string DialogTitle
    {
        get => _dialogTitle;
        set { ArgumentNullException.ThrowIfNull(value); SetProperty(ref _dialogTitle, value); }
    }
    public event EventHandler? SelectedPathChanged;
    public event EventHandler? BrowseRequested;
    public void RequestBrowse() => BrowseRequested?.Invoke(this, EventArgs.Empty);

    protected override void WriteRenderState(IDictionary<string, object?> state)
    {
        base.WriteRenderState(state);
        state["selectedPath"] = SelectedPath; state["dialogTitle"] = DialogTitle;
    }

    protected override void OnRuntimeEvent(RuntimeEvent e)
    {
        base.OnRuntimeEvent(e);
        if (e.Name == "browse") RequestBrowse();
    }
}
