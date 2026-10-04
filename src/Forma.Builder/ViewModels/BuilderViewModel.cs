using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Control = Forma.Core.Controls.Control;

namespace Forma.Builder;

/// <summary>Designer state and history commands, independent of the native window and browser.</summary>
public sealed class BuilderViewModel : INotifyPropertyChanged
{
    private readonly DesignHistory _history = new();
    private readonly DesignerEditingService _editing;
    private Forma.Core.Form? _form;
    private Control? _selectedControl;
    private bool _previewMode;
    private string? _projectPath;
    private string _savedSnapshot = "";
    private bool _hasUnsavedChanges;
    private string _windowTitle = "Untitled - Forma Builder";

    public BuilderViewModel() => _editing = new DesignerEditingService(this);

    public event PropertyChangedEventHandler? PropertyChanged;
    public Dictionary<string, Appearance> Appearance { get; } = [];
    public int ControlSequence { get; set; }
    public Forma.Core.Form? Form
    {
        get => _form;
        set => Set(ref _form, value);
    }
    public Control? SelectedControl
    {
        get => _selectedControl;
        set => Set(ref _selectedControl, value);
    }
    public bool PreviewMode
    {
        get => _previewMode;
        set
        {
            if (Set(ref _previewMode, value)) NotifyHistory();
        }
    }
    public string? ProjectPath
    {
        get => _projectPath;
        set
        {
            if (Set(ref _projectPath, value)) RefreshTitle();
        }
    }
    public bool HasUnsavedChanges => Form is not null && Snapshot() != _savedSnapshot;
    public string WindowTitle => _windowTitle;
    public bool CanUndo => !PreviewMode && _history.CanUndo;
    public bool CanRedo => !PreviewMode && _history.CanRedo;

    public bool ShowDialog(string? id)
    {
        if (!PreviewMode || Form is null || Walk(Form).FirstOrDefault(item => item.Id == id) is not Forma.Core.Controls.Dialog dialog
            || !Appearance.TryGetValue(dialog.Id, out var appearance) || !appearance.Enabled) return false;
        dialog.Show();
        return true;
    }

    public DesignerEditResult ExecuteEdit(string action, string? id, JsonElement payload)
    {
        var mutates = !PreviewMode && Form is not null
            && (action is "drop" or "move" or "resize" or "property"
                || action == "command" && payload.ValueKind == JsonValueKind.Object
                    && payload.TryGetProperty("command", out var command)
                    && command.ValueKind == JsonValueKind.String
                    && command.GetString() is "delete" or "bring-front" or "send-back");
        var before = mutates ? CaptureHistory() : null;
        try { return _editing.Execute(action, id, payload); }
        finally
        {
            if (before is not null)
            {
                var property = action == "property" && payload.ValueKind == JsonValueKind.Object
                    && payload.TryGetProperty("property", out var field) && field.ValueKind == JsonValueKind.String
                    ? field.GetString() : null;
                var group = property is "text" or "document" ? $"text:{id}" : null;
                RecordEdit(before, CaptureHistory(), group);
            }
            else RefreshDocumentState();
        }
    }

    [MemberNotNull(nameof(Form))]
    public void CreateNew()
    {
        PreviewMode = false;
        Appearance.Clear();
        ControlSequence = 0;
        ClearHistory();
        Form = new Forma.Core.Form { Name = "form1", Title = "Form1", Width = 640, Height = 440 };
        Appearance[Form.Id] = new Appearance
        {
            Width = 640, Height = 440, BackColor = "#ffffff", ForeColor = "#1f2937"
        };
        SelectedControl = Form;
        ProjectPath = null;
        MarkSaved();
    }

    [MemberNotNull(nameof(Form))]
    public void ApplyDocument(Forma.Core.Form form, Dictionary<string, Appearance> appearance, string? selectedId = null)
    {
        PreviewMode = false;
        Form = form;
        Appearance.Clear();
        foreach (var pair in appearance) Appearance.Add(pair.Key, pair.Value);
        var controls = Walk(form).ToArray();
        SelectedControl = controls.FirstOrDefault(control => control.Id == selectedId) ?? form;
        ControlSequence = controls.Length;
        RefreshDocumentState();
    }

    private static IEnumerable<Control> Walk(Control root)
    {
        yield return root;
        foreach (var child in root.Children)
        foreach (var descendant in Walk(child)) yield return descendant;
    }

    public ProjectDocument CaptureProject(bool embedImages = false) => ProjectFile.Capture(
        Form ?? throw new InvalidOperationException("No design is open."),
        control => JsonSerializer.SerializeToElement(Appearance[control.Id]), embedImages);

    private string Snapshot() => Form is null ? "" : ProjectFile.Serialize(CaptureProject());

    public DesignSnapshot CaptureHistory()
    {
        // Preserve image data in undo entries even after a source file is moved.
        try { return new(ProjectFile.Serialize(CaptureProject(embedImages: true)), SelectedControl?.Id); }
        catch (IOException) { return new(Snapshot(), SelectedControl?.Id); }
    }

    public void MarkSaved()
    {
        _savedSnapshot = Snapshot();
        RefreshDocumentState();
    }

    // The bridge explicitly refreshes after a mutation batch, including appearance edits.
    public void RefreshDocumentState()
    {
        Set(ref _hasUnsavedChanges, HasUnsavedChanges, nameof(HasUnsavedChanges));
        RefreshTitle();
    }

    private void RefreshTitle() => Set(ref _windowTitle,
        $"{(_hasUnsavedChanges ? "* " : "")}{(ProjectPath is null ? "Untitled" : Path.GetFileName(ProjectPath))} - Forma Builder",
        nameof(WindowTitle));

    public bool RecordEdit(DesignSnapshot before, DesignSnapshot after, string? group = null)
    {
        var recorded = _history.Record(before, after, group);
        NotifyHistory();
        RefreshDocumentState();
        return recorded;
    }

    // The view applies the returned document to its renderer; the VM never owns WebView2.
    public DesignSnapshot? Undo()
    {
        if (!CanUndo) return null;
        var snapshot = _history.Undo();
        NotifyHistory();
        return snapshot;
    }
    public DesignSnapshot? Redo()
    {
        if (!CanRedo) return null;
        var snapshot = _history.Redo();
        NotifyHistory();
        return snapshot;
    }
    public void ClearHistory()
    {
        _history.Clear();
        NotifyHistory();
    }
    private void NotifyHistory()
    {
        PropertyChanged?.Invoke(this, new(nameof(CanUndo)));
        PropertyChanged?.Invoke(this, new(nameof(CanRedo)));
    }
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new(name));
        return true;
    }
}
