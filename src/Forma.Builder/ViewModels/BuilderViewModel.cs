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
    private readonly List<Forma.Core.Form> _forms = [];
    private readonly Dictionary<string, string?> _formSelections = [];
    public List<ProjectEntry> Files { get; } = [];
    public IReadOnlyList<Forma.Core.Form> Forms => _forms;
    public IEnumerable<Control> ProjectControls => _forms.SelectMany(Walk);
    public Forma.Core.Form ProjectRoot => _forms.First();
    public string GlobalScript { get => Appearance[ProjectRoot.Id].GlobalScript; set => Appearance[ProjectRoot.Id].GlobalScript = value; }
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
        set { if (value is not null && _forms.Count == 0) _forms.Add(value); Set(ref _form, value); }
    }
    public Control? SelectedControl
    {
        get => _selectedControl;
        set { Set(ref _selectedControl, value); if (Form is not null) _formSelections[Form.Id] = value?.Id; }
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
            && (action is "drop" or "move" or "resize" or "property" or "customize" or "global-script" or "image-source" or "path-source"
                || action == "command" && payload.ValueKind == JsonValueKind.Object
                    && payload.TryGetProperty("command", out var command)
                    && command.ValueKind == JsonValueKind.String
                    && command.GetString() is "delete" or "bring-front" or "send-back" or "add-tab");
        var before = mutates ? CaptureHistory() : null;
        try { return _editing.Execute(action, id, payload); }
        finally
        {
            if (before is not null)
            {
                _editing.ReflowLayouts();
                var property = action == "property" && payload.ValueKind == JsonValueKind.Object
                    && payload.TryGetProperty("property", out var field) && field.ValueKind == JsonValueKind.String
                    ? field.GetString() : null;
                var group = property is "text" or "document" ? $"text:{id}"
                    : property is "gridColumns" or "gridRows" ? $"grid:{id}:{property}" : null;
                RecordEdit(before, CaptureHistory(), group);
            }
            else if (action != "select") RefreshDocumentState();
        }
    }

    [MemberNotNull(nameof(Form))]
    public void CreateNew()
    {
        PreviewMode = false;
        Appearance.Clear();
        _forms.Clear(); _formSelections.Clear(); Files.Clear();
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
    public void AddForm()
    {
        if (Form is null) { CreateNew(); return; }
        if (Forms.Count >= 100) throw new InvalidOperationException("A project supports up to 100 forms.");
        var before = CaptureHistory();
        var number = 1;
        while (_forms.Any(form => form.Name == $"form{number}")) number++;
        var next = new Forma.Core.Form { Name = $"form{number}", Title = $"Form{number}", Width = 640, Height = 440 };
        _forms.Add(next); Appearance[next.Id] = new Appearance { Width = 640, Height = 440, BackColor = "#ffffff", ForeColor = "#1f2937" };
        Form = next; SelectedControl = next; PreviewMode = false;
        RecordEdit(before, CaptureHistory());
    }

    public bool SelectForm(string id)
    {
        var form = _forms.FirstOrDefault(form => form.Id == id);
        if (form is null || PreviewMode) return false;
        Form = form;
        SelectedControl = Walk(form).FirstOrDefault(control => control.Id == _formSelections.GetValueOrDefault(id)) ?? form;
        return true;
    }

    [MemberNotNull(nameof(Form))]
    public void ApplyDocument(Forma.Core.Form form, Dictionary<string, Appearance> appearance, string? selectedId = null)
        => ApplyProject([form], appearance, form.Id, selectedId);

    [MemberNotNull(nameof(Form))]
    public void ApplyProject(IEnumerable<Forma.Core.Form> forms, Dictionary<string, Appearance> appearance, string? activeFormId = null, string? selectedId = null, IEnumerable<ProjectEntry>? files = null)
    {
        var next = forms.ToList();
        if (next.Count == 0) throw new InvalidOperationException("A project needs a form.");
        PreviewMode = false;
        _forms.Clear(); _forms.AddRange(next); _formSelections.Clear();
        Files.Clear(); if (files is not null) Files.AddRange(files);
        Form = next.FirstOrDefault(form => form.Id == activeFormId) ?? next[0];
        Appearance.Clear();
        foreach (var pair in appearance) Appearance.Add(pair.Key, pair.Value);
        SelectedControl = Walk(Form).FirstOrDefault(control => control.Id == selectedId) ?? Form;
        ControlSequence = ProjectControls.Count();
        RefreshDocumentState();
    }

    private static IEnumerable<Control> Walk(Control root)
    {
        yield return root;
        foreach (var child in root.Children)
        foreach (var descendant in Walk(child)) yield return descendant;
    }

    public ProjectDocument CaptureProject(bool embedImages = false)
    {
        var document = ProjectFile.CaptureProject(Forms, control => JsonSerializer.SerializeToElement(Appearance[control.Id]), embedImages);
        if (Files.Count > 0) {
            document.Forms = document.Version == 1 ? [document.Root] : document.Forms;
            document.Version = 3;
            document.Files = Files.Select(file => new ProjectEntry { Id = file.Id, Name = file.Name, ParentId = file.ParentId, IsFolder = file.IsFolder, Content = file.Content }).ToList();
        }
        return document;
    }

    public T EditProjectFiles<T>(Func<List<ProjectEntry>, T> edit)
    {
        var before = CaptureHistory();
        var result = edit(Files);
        RecordEdit(before, CaptureHistory());
        return result;
    }

    public void RemoveForm(string id)
    {
        if (_forms.Count < 2) throw new InvalidOperationException("A project must keep at least one form.");
        var form = _forms.FirstOrDefault(form => form.Id == id) ?? throw new ArgumentException("Form not found.");
        if (Walk(form).Any(control => Appearance[control.Id].Locked)) throw new InvalidOperationException("Unlock the form and its components first.");
        var before = CaptureHistory(); var global = GlobalScript;
        _forms.Remove(form);
        foreach (var control in Walk(form)) { Appearance.Remove(control.Id); (control as IDisposable)?.Dispose(); }
        GlobalScript = global;
        if (Form == form) { Form = _forms[0]; SelectedControl = Form; }
        _formSelections.Remove(id);
        RecordEdit(before, CaptureHistory());
    }

    private string Snapshot() => Form is null ? "" : ProjectFile.Serialize(CaptureProject());

    public DesignSnapshot CaptureHistory()
    {
        // Preserve image data in undo entries even after a source file is moved.
        try { return new(ProjectFile.Serialize(CaptureProject(embedImages: true)), SelectedControl?.Id, Form?.Id); }
        catch (IOException) { return new(Snapshot(), SelectedControl?.Id, Form?.Id); }
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
