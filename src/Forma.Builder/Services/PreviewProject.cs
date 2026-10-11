using System.Text.Json;

namespace Forma.Builder;

/// <summary>
/// The whole project as it was when Preview started. Every preview window builds its form from
/// this snapshot, so scripts can open other forms (forma.showForm) while designer edits made in
/// the meantime do not leak into the running app.
/// </summary>
public sealed class PreviewProject
{
    private readonly List<ProjectNode> _forms;
    private readonly string _globalScript;
    private readonly ProjectEntryService.ModuleSource[] _files;
    private readonly Dictionary<string, JsonElement> _shared = new(StringComparer.Ordinal);
    private const int MaxSharedKeys = 1000, MaxSharedValueLength = 1_000_000;

    /// <summary>forma.shared for the whole run: every window reads and writes the same values.</summary>
    public IReadOnlyDictionary<string, JsonElement> SharedState => _shared;

    /// <summary>Raised when a window changes a shared value; other windows pass it to their page.</summary>
    public event EventHandler<SharedChange>? SharedChanged;

    public sealed record SharedChange(string Key, JsonElement? Value, object Source);

    /// <summary>Stores (or with a null value, deletes) one shared value and tells the other windows.</summary>
    public void SetShared(string key, JsonElement? value, object source)
    {
        if (string.IsNullOrEmpty(key) || key.Length > 128) throw new ArgumentException("Shared value names must be 1–128 characters.");
        if (value is { } json)
        {
            if (json.GetRawText().Length > MaxSharedValueLength) throw new ArgumentException($"Shared value '{key}' is too large.");
            if (!_shared.ContainsKey(key) && _shared.Count >= MaxSharedKeys) throw new ArgumentException("Too many shared values.");
            _shared[key] = json.Clone();
        }
        else if (!_shared.Remove(key)) return;
        SharedChanged?.Invoke(this, new(key, value?.Clone(), source));
    }

    private PreviewProject(List<ProjectNode> forms, string globalScript, ProjectEntryService.ModuleSource[] files, string startFormId)
    {
        _forms = forms;
        _globalScript = globalScript;
        _files = files;
        StartFormId = startFormId;
    }

    /// <summary>The form Preview opens first: the one active in the designer.</summary>
    public string StartFormId { get; }

    public IEnumerable<(string Id, string Name)> Forms =>
        _forms.Select(form => (form.Id, form.Properties.TryGetValue("Name", out var name) && name.ValueKind == JsonValueKind.String ? name.GetString()! : form.Id));

    public static PreviewProject Start(BuilderViewModel design)
    {
        if (design.Form is null) throw new InvalidOperationException("Create a form before previewing.");
        // Validate once through the project-file rules, exactly like saving would.
        var document = ProjectFile.Parse(ProjectFile.Serialize(design.CaptureProject()));
        var forms = document.Version == 1 ? [document.Root] : document.Forms.ToList();
        return new(forms, design.GlobalScript, ProjectEntryService.ModuleSources(design.Files), design.Form.Id);
    }

    /// <summary>Builds a fresh runtime copy of one form, found by ID or (case-insensitive) name.</summary>
    public PreviewSession Open(string nameOrId)
    {
        var node = _forms.FirstOrDefault(form => form.Id == nameOrId)
            ?? _forms.FirstOrDefault(form => Forms.First(entry => entry.Id == form.Id).Name.Equals(nameOrId, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"There is no form named '{nameOrId}'.");
        var (form, appearance) = ProjectFile.Restore(new ProjectDocument { Root = node });
        var appearances = appearance.ToDictionary(pair => pair.Key, pair => pair.Value.Deserialize<Appearance>()!);
        return new PreviewSession(form, appearances, _globalScript, _files, _shared);
    }
}
