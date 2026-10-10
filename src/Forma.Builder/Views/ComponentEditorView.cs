using System.Diagnostics;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;

namespace Forma.Builder;

/// <summary>Source editing with automatic design updates on successful saves.</summary>
public sealed class ComponentEditorView : UserControl
{
    private readonly string _folder;
    private readonly bool _globalScript;
    private const string GlobalFile = "global-script.js";
    private readonly Microsoft.Web.WebView2.WinForms.WebView2 _web = new() { Dock = DockStyle.Fill };
    private ComponentCustomization _draft;
    private bool _editorReady;
    private readonly string _componentName;
    private readonly Func<object>? _completionContext;
    private Task? _initialization;
    private string _theme = "dark";
    private string _activeDocument = "css";
    public bool HasUnsavedChanges { get; private set; }
    public string DocumentName => _componentName;
    public event EventHandler? MetadataChanged;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 32, Padding = new Padding(8) };
    private static string? _preferredEditor;
    private readonly ComponentSaveSession _saveSession;
    private readonly System.Windows.Forms.Timer _reloadDelay = new() { Interval = 350 };
    private FileSystemWatcher? _watcher;
    private bool _saving;
    private bool _closed;
    private int _readRetries;
    private readonly TaskCompletionSource<bool> _editorLoaded = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public ComponentCustomization Result => _saveSession.Current;

    public ComponentEditorView(string name, string folder, ComponentCustomization source, Func<ComponentCustomization, Task> apply, Func<object>? completionContext = null, bool globalScript = false)
    {
        Text = $"{name} — Custom Properties";
        _componentName = name;
        _globalScript = globalScript;
        _completionContext = completionContext;
        _folder = folder; _saveSession = new ComponentSaveSession(source, apply); _draft = source;
        ClientSize = new Size(1040, 720); MinimumSize = new Size(760, 500);
        PaintStatus();
        Controls.Add(_web); Controls.Add(_status);
        if (!globalScript && File.Exists(Path.Combine(folder, "component.css")) && (File.Exists(Path.Combine(folder, ComponentEditorService.ScriptFile)) || File.Exists(Path.Combine(folder, "behavior.js")))
            && (File.Exists(Path.Combine(folder, ComponentEditorService.PropertiesFile)) || File.Exists(Path.Combine(folder, "characteristics.json"))))
            _draft = ComponentEditorService.Read(folder);
        _status.Text = folder;
        _reloadDelay.Tick += async (_, _) => await ReloadSavedFilesAsync();
    }

    private async Task InitializeEditorAsync()
    {
        try {
            SetStatus("Loading code editor…");
            var page = Path.Combine(AppContext.BaseDirectory, "DesignerWeb", "code-editor.html");
            foreach (var asset in new[] { page, Path.Combine(Path.GetDirectoryName(page)!, "code-editor.bundle.js"), Path.Combine(Path.GetDirectoryName(page)!, "code-editor.css") })
                if (!File.Exists(asset)) throw new FileNotFoundException("Code editor asset missing. Rebuild Forma.", asset);
            await _web.EnsureCoreWebView2Async().WaitAsync(TimeSpan.FromSeconds(30));
            if (_closed || IsDisposed) return;
            _web.CoreWebView2.WebMessageReceived += EditorMessage;
            _web.CoreWebView2.NavigationCompleted += EditorNavigated;
            _web.CoreWebView2.Navigate(new Uri(page).AbsoluteUri + "?embedded=1");
            await _editorLoaded.Task.WaitAsync(TimeSpan.FromSeconds(30));
        } catch (Exception error) {
            if (!_closed && !IsDisposed) SetStatus($"Could not load code editor: {error.Message}");
        }
    }

    private void EditorNavigated(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess) _editorLoaded.TrySetException(new InvalidOperationException($"Editor navigation failed: {e.WebErrorStatus}"));
    }

    private void SendEditor(object message)
    {
        if (_editorReady && !_closed && !IsDisposed) _web.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(message, JsonOptions));
    }
    public Task StartEditorAsync() => _initialization ??= InitializeEditorAsync();
    public void ActivateDocument(string document) {
        _activeDocument = document is "behavior" or "characteristics" ? document : "css";
        SendEditor(new { action = "activate", document = _activeDocument });
    }
    public void RefreshCompletionContext() => SendEditor(new { action = "context", context = _completionContext?.Invoke() });
    public void ApplyTheme(string theme) {
        var value = theme == "light" ? "light" : "dark";
        if (value == _theme) return;
        _theme = value; PaintStatus(); SendEditor(new { action = "theme", theme = _theme });
    }
    // Matches the editor's VS Code status bar (Dark Modern / Light Modern).
    private void PaintStatus() {
        var light = _theme == "light";
        _status.BackColor = light ? Color.FromArgb(248, 248, 248) : Color.FromArgb(24, 24, 24);
        _status.ForeColor = light ? Color.FromArgb(111, 111, 111) : Color.FromArgb(157, 157, 157);
    }

    private void EditorMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (_closed || IsDisposed || Disposing) return;
        var json = e.WebMessageAsJson;
        // Choosing an external editor also opens native modal UI.
        BeginInvoke(new Action(async () => {
            if (!_closed && !IsDisposed && !Disposing) await HandleEditorMessageAsync(json);
        }));
    }

    private void SetStatus(string text)
    {
        _status.Text = text;
        // Native status is a loading/error fallback until the browser is ready.
        _status.Visible = !_editorReady;
        if (_editorReady) SendEditor(new { action = "status", status = text });
    }

    private async Task HandleEditorMessageAsync(string json)
    {
        if (_closed || IsDisposed) return;
        try {
            var message = JsonSerializer.Deserialize<JsonElement>(json);
            if (message.ValueKind != JsonValueKind.Object || !message.TryGetProperty("type", out var type) || type.GetString() != "editor") return;
            var command = message.GetProperty("event").GetString();
            if (command == "ready") {
                _editorReady = true; _status.Visible = false; Populate(_draft); StartWatching(); _editorLoaded.TrySetResult(true); return;
            }
            if (command == "dirty") {
                HasUnsavedChanges = message.GetProperty("dirty").GetBoolean();
                Text = $"{(message.GetProperty("dirty").GetBoolean() ? "* " : "")}{_componentName} — Custom Properties";
                MetadataChanged?.Invoke(this, EventArgs.Empty); return;
            }
            if (command == "document") {
                var document = message.GetProperty("document").GetString();
                if (document is "css" or "behavior" or "characteristics") _activeDocument = document;
                return;
            }
            if (command is "save" or "external") {
                _draft = message.GetProperty("source").Deserialize<ComponentCustomization>(JsonOptions)
                    ?? throw new ArgumentException("Missing editor sources.");
                await RunAsync(command == "save" ? SaveAsync : OpenExternalEditor);
            }
            if (command == "choose-editor") ChooseEditor();
            if (command == "close") RequestClose();
        } catch (Exception error) when (error is JsonException or ArgumentException or InvalidOperationException or KeyNotFoundException) {
            SendEditor(new { action = "error", status = error.Message });
        }
    }

    private ComponentCustomization Source() => _draft;
    private void WriteSources(ComponentCustomization source)
    {
        if (!_globalScript) { ComponentEditorService.Write(_folder, source); return; }
        ComponentCustomization.Validate(source);
        Directory.CreateDirectory(_folder);
        File.WriteAllText(Path.Combine(_folder, GlobalFile), source.Behavior);
    }
    private ComponentCustomization ReadSources()
    {
        if (!_globalScript) return ComponentEditorService.Read(_folder);
        var path = Path.Combine(_folder, GlobalFile);
        if (new FileInfo(path).Length > 800_000) throw new ArgumentException("Global script is too large.");
        return ComponentCustomization.Validate(new() { Behavior = File.ReadAllText(path) });
    }
    private void Populate(ComponentCustomization source)
    {
        _draft = source;
        SendEditor(new { action = "source", source, document = _activeDocument, globalScript = _globalScript, theme = _theme, context = _completionContext?.Invoke(), name = _componentName, status = _globalScript ? "Global script runs before component scripts. Save applies automatically." : "Use :host for styles. Script runs in Preview. Save applies automatically." });
    }

    private async Task OpenExternalEditor()
    {
        if (_preferredEditor is null)
        {
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft VS Code", "Code.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Microsoft VS Code", "Code.exe")
            };
            _preferredEditor = candidates.FirstOrDefault(File.Exists);
        }
        if (_preferredEditor is null && !ChooseEditor()) {
            SendEditor(new { action = "error", status = "External editor selection cancelled. Your changes remain in the editor." });
            return;
        }
        await SaveAsync();
        var start = new ProcessStartInfo(_preferredEditor!) { UseShellExecute = false, WorkingDirectory = _folder };
        foreach (var file in _globalScript ? new[] { GlobalFile } : new[] { "component.css", ComponentEditorService.ScriptFile, ComponentEditorService.PropertiesFile }) start.ArgumentList.Add(Path.Combine(_folder, file));
        using var process = Process.Start(start);
        SetStatus("Save in your external editor; changes apply automatically here.");
    }

    private bool ChooseEditor()
    {
        using var picker = new OpenFileDialog { Title = "Choose your code editor", Filter = "Applications|*.exe", CheckFileExists = true };
        if (picker.ShowDialog(this) != DialogResult.OK) return false;
        _preferredEditor = picker.FileName; SetStatus($"Editor: {Path.GetFileNameWithoutExtension(_preferredEditor)}"); return true;
    }

    private async Task SaveAsync()
    {
        if (_saving) return;
        _saving = true;
        try
        {
            var source = ComponentCustomization.Validate(Source());
            WriteSources(source);
            await ApplyAsync(source);
            if (!IsDisposed) {
                SetStatus("Saved and applied. Start a new Preview to test script changes.");
                SendEditor(new { action = "saved", source, status = _status.Text });
            }
        }
        finally { _saving = false; }
    }

    private async Task ApplyAsync(ComponentCustomization source)
    {
        await _saveSession.ApplyAsync(source);
    }

    private void StartWatching()
    {
        if (_watcher is not null) return;
        try
        {
            Directory.CreateDirectory(_folder);
            if (_globalScript || !File.Exists(Path.Combine(_folder, "component.css"))) WriteSources(Source());
            _watcher = new FileSystemWatcher(_folder)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                SynchronizingObject = this
            };
            void Changed(object sender, FileSystemEventArgs e)
            {
                if (_closed || IsDisposed || Disposing || e.Name is not ("component.css" or "script.js" or "behavior.js" or "global-script.js" or "custom-properties.json" or "characteristics.json")) return;
                _readRetries = 0; _reloadDelay.Stop(); _reloadDelay.Start();
            }
            _watcher.Changed += Changed; _watcher.Created += Changed; _watcher.Renamed += Changed;
            _watcher.Deleted += Changed;
            _watcher.EnableRaisingEvents = true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
        { SetStatus($"Could not watch external saves: {error.Message}"); }
    }

    private async Task ReloadSavedFilesAsync()
    {
        _reloadDelay.Stop();
        if (_closed || IsDisposed || Disposing) return;
        if (_saving) { _reloadDelay.Start(); return; }
        _saving = true;
        try
        {
            var source = ReadSources();
            if (_saveSession.IsCurrent(source)) return;
            await ApplyAsync(source);
            if (_closed || IsDisposed) return;
            Populate(source);
            SetStatus("External save applied. Start a new Preview to test script changes.");
        }
        catch (Exception error) when (error is ArgumentException or System.Text.Json.JsonException or IOException or UnauthorizedAccessException)
        {
            SetStatus($"Save not applied: {error.Message}");
            if (!_closed && ++_readRetries < 3) _reloadDelay.Start();
        }
        finally { _saving = false; }
    }

    public event EventHandler? Closed;
    public void RequestClose() {
        if (!HasUnsavedChanges || MessageBox.Show(this, "Discard unsaved component code?", "Unsaved code", MessageBoxButtons.YesNo) == DialogResult.Yes) Close();
    }
    public void Close() { if (_closed) return; Closed?.Invoke(this, EventArgs.Empty); Dispose(); }

    protected override void Dispose(bool disposing)
    {
        if (!disposing || _closed) { base.Dispose(disposing); return; }
        _closed = true;
        _editorLoaded.TrySetCanceled();
        if (_web.CoreWebView2 is not null) {
            _web.CoreWebView2.WebMessageReceived -= EditorMessage;
            _web.CoreWebView2.NavigationCompleted -= EditorNavigated;
        }
        _watcher?.Dispose(); _reloadDelay.Stop(); _reloadDelay.Dispose();
        base.Dispose(disposing);
    }

    private async Task RunAsync(Func<Task> action)
    {
        if (_saving || _closed) return;
        try { await action(); }
        catch (Exception error) when (error is ArgumentException or System.Text.Json.JsonException or IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        { SendEditor(new { action = "error", status = error.Message }); SetStatus(error.Message); }
    }
}
