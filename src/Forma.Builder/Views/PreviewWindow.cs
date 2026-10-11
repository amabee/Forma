using Forma.Core.Controls;
using Forma.WebView2;
using Microsoft.Web.WebView2.Core;
using LinkLabel = Forma.Core.Controls.LinkLabel;
using System.Text.Json;
using Forma.Core.Rendering;

namespace Forma.Builder;

/// <summary>
/// Hosts one form of the design as a running Forma window. Scripts can open other forms of
/// the project in their own windows (forma.showForm), modally like ShowDialog or not.
/// </summary>
public sealed class PreviewWindow : System.Windows.Forms.Form
{
    private readonly PreviewProject _project;
    private readonly PreviewSession _session;
    // The window that opened this one, and the script request waiting for it to close.
    private readonly PreviewWindow? _opener;
    private readonly string? _requestId;
    private JsonElement? _result;
    // Windows opened from this preview; closing the first window closes them all, like an app.
    private readonly List<PreviewWindow> _opened = [];
    private readonly Microsoft.Web.WebView2.WinForms.WebView2 _web = new() { Dock = DockStyle.Fill };
    private WebView2Bridge? _bridge;
    private WebView2Renderer? _renderer;
    private bool _ready;
    private readonly CoalescedRefresh _stateRefresh;

    public PreviewWindow(PreviewProject project) : this(project, project.Open(project.StartFormId)) { }

    private PreviewWindow(PreviewProject project, PreviewSession session, PreviewWindow? opener = null, string? requestId = null)
    {
        _project = project;
        _session = session;
        _opener = opener;
        _requestId = requestId;
        _stateRefresh = new CoalescedRefresh(_ => !_ready || IsDisposed || _bridge is null
            ? Task.CompletedTask : _bridge.SendAsync(_session.State()));
        var bounds = session.Appearance[session.Form.Id];
        Text = session.Form.Title;
        ClientSize = new Size(bounds.Width, bounds.Height);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        StartPosition = FormStartPosition.CenterParent;
        Controls.Add(_web);
        Load += async (_, _) => await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            await _web.EnsureCoreWebView2Async();
            if (IsDisposed) return;
            var loaded = new TaskCompletionSource<bool>();
            void Navigated(object? sender, CoreWebView2NavigationCompletedEventArgs e) => loaded.TrySetResult(e.IsSuccess);
            _web.CoreWebView2.NavigationCompleted += Navigated;
            try
            {
                _web.CoreWebView2.Navigate(new Uri(Path.Combine(AppContext.BaseDirectory, "DesignerWeb", "preview.html")).AbsoluteUri);
                if (!await loaded.Task.WaitAsync(TimeSpan.FromSeconds(30)))
                    throw new InvalidOperationException("The preview page could not load.");
            }
            finally { if (!IsDisposed) _web.CoreWebView2.NavigationCompleted -= Navigated; }
            if (IsDisposed) return;
            _bridge = new WebView2Bridge(_web.CoreWebView2);
            _renderer = new WebView2Renderer(_bridge);
            _bridge.MessageReceived += CustomMessage;
            await _renderer.InitializeAsync();
            await _renderer.RenderAsync(_session.Form);
            foreach (var control in _session.Controls)
            {
                control.PropertyChanged += ControlChanged;
                if (control is PathPicker picker) picker.BrowseRequested += BrowseRequested;
                if (control is LinkLabel link) link.LinkClicked += LinkClicked;
                if (control is Forma.Core.Controls.Timer timer) timer.Tick += TimerTick;
                if (control is CommandControl commands) commands.ItemClicked += CommandInvoked;
            }
            _ready = true;
            await _bridge.SendAsync(_session.State());
            _session.Start();
        }
        catch (Exception error)
        {
            if (IsDisposed) return;
            MessageBox.Show(this, error.Message, "Could not start preview", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
        }
    }

    private async void ControlChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (!_ready || IsDisposed || _bridge is null) return;
        if (ReferenceEquals(sender, _session.Form)) Text = _session.Form.Title;
        try { await _stateRefresh.Request("Ready"); }
        catch (Exception error) when (IsDisposed || error is ObjectDisposedException) { }
    }

    private async void CustomMessage(object? sender, BridgeMessage message)
    {
        if (message.Type != "custom" || _bridge is null || IsDisposed) return;
        try
        {
            if (message.Payload is not JsonElement payload || payload.ValueKind != JsonValueKind.Object) return;
            if (message.Event == "error")
            {
                Text = $"{_session.Form.Title} — {payload.GetProperty("message").GetString()}";
                return;
            }
            if (!_ready || message.Id is null) return;
            var sourceId = payload.GetProperty("sourceId").GetString();
            if (sourceId is null || !_session.Appearance.TryGetValue(sourceId, out var source) || !source.Enabled) return;
            if (message.Event == "show-form")
            {
                var name = payload.GetProperty("form").GetString() ?? "";
                var modal = payload.TryGetProperty("modal", out var modalValue) && modalValue.ValueKind == JsonValueKind.True;
                var requestId = payload.TryGetProperty("requestId", out var request) && request.ValueKind == JsonValueKind.String ? request.GetString() : null;
                // A dialog needs its own modal loop, which WebView2 forbids inside its callbacks.
                BeginInvoke(() => OpenForm(name, modal, requestId));
                return;
            }
            if (message.Event == "close-form")
            {
                _result = payload.TryGetProperty("result", out var result) ? result.Clone() : null;
                BeginInvoke(Close);
                return;
            }
            if (message.Event == "set")
                _session.SetValue(message.Id, payload.GetProperty("property").GetString()!, payload.GetProperty("value"),
                    payload.TryGetProperty("origin", out var origin) && origin.GetString() == "binding" ? "binding" : "programmatic");
            else if (message.Event == "grid")
                _session.EditGrid(message.Id, payload.GetProperty("operation").GetString()!, payload);
            else if (message.Event == "show-dialog" && _session.Appearance.TryGetValue(message.Id, out var appearance) && appearance.Enabled
                && _session.Controls.FirstOrDefault(control => control.Id == message.Id) is Forma.Core.Controls.Dialog dialog)
                _session.ShowDialog(dialog.Id);
            if (_session.Appearance.TryGetValue(message.Id, out var toastAppearance) && toastAppearance.Enabled
                && _session.Controls.FirstOrDefault(control => control.Id == message.Id) is Toast toast)
            {
                if (message.Event == "show-toast") _session.ShowToast(toast.Id, payload.TryGetProperty("options", out var options) ? options : default);
                if (message.Event == "close-toast") _session.CloseToast(toast.Id);
            }
            await _stateRefresh.Request("Ready");
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException or JsonException)
        {
            if (!IsDisposed) Text = $"{_session.Form.Title} — {error.Message}";
        }
    }

    private async void TimerTick(object? sender, EventArgs e)
    {
        if (!_ready || _bridge is null || IsDisposed || sender is not Forma.Core.Controls.Timer timer) return;
        try { await _bridge.SendAsync(new { type = "designer", action = "component-event", id = timer.Id, @event = "tick" }); }
        catch (Exception error) when (IsDisposed || error is ObjectDisposedException) { }
    }

    private async void CommandInvoked(object? sender, CommandItemClickedEventArgs e)
    {
        if (!_ready || _bridge is null || IsDisposed || sender is not CommandControl commands) return;
        try { await _bridge.SendAsync(new { type = "designer", action = "component-event", id = commands.Id, @event = "command-item", detail = new { itemId = e.Id, text = e.Text, @checked = e.Checked } }); }
        catch (Exception error) when (IsDisposed || error is ObjectDisposedException) { }
    }

    private void BrowseRequested(object? sender, EventArgs e)
    {
        if (!_ready || sender is not PathPicker picker || !_session.Appearance[picker.Id].Enabled) return;
        if (picker is FilePicker file)
        {
            using var dialog = new OpenFileDialog { Title = picker.DialogTitle, Filter = file.Filter, CheckFileExists = true };
            if (dialog.ShowDialog(this) == DialogResult.OK) picker.SelectedPath = dialog.FileName;
        }
        else
        {
            using var dialog = new FolderBrowserDialog { Description = picker.DialogTitle, UseDescriptionForTitle = true };
            if (dialog.ShowDialog(this) == DialogResult.OK) picker.SelectedPath = dialog.SelectedPath;
        }
    }

    private void LinkClicked(object? sender, EventArgs e)
    {
        if (!_ready || sender is not LinkLabel link || !_session.Appearance[link.Id].Enabled) return;
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(link.Url) { UseShellExecute = true }); }
        catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException)
        { MessageBox.Show(this, error.Message, "Could not open link"); }
    }

    private PreviewWindow Root => _opener?.Root ?? this;

    private void OpenForm(string name, bool modal, string? requestId)
    {
        if (IsDisposed) return;
        PreviewSession session;
        try { session = _project.Open(name); }
        catch (Exception error) when (error is ArgumentException or InvalidDataException)
        {
            _ = SendFormClosedAsync(requestId, null, error.Message);
            return;
        }
        var window = new PreviewWindow(_project, session, this, requestId);
        Root._opened.Add(window);
        if (modal)
        {
            window.StartPosition = FormStartPosition.CenterParent;
            window.ShowDialog(this);
            // Closing a ShowDialog window only hides it; release its WebView2 now.
            window.Dispose();
        }
        else
        {
            window.StartPosition = FormStartPosition.Manual;
            window.Location = new Point(Location.X + 32, Location.Y + 32);
            window.Show();
        }
    }

    /// <summary>Resolves the opener's forma.showForm promise.</summary>
    private Task SendFormClosedAsync(string? requestId, JsonElement? result, string? error = null)
    {
        if (requestId is null || IsDisposed || _bridge is null) return Task.CompletedTask;
        try { return _bridge.SendAsync(new { type = "designer", action = "form-closed", requestId, result, error }); }
        catch (ObjectDisposedException) { return Task.CompletedTask; }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _ready = false;
        if (_opener is not null)
        {
            _opener.Root._opened.Remove(this);
            if (!_opener.IsDisposed) _ = _opener.SendFormClosedAsync(_requestId, _result);
        }
        else
            foreach (var window in _opened.ToArray()) if (!window.IsDisposed) window.Close();
        foreach (var control in _session.Controls)
        {
            control.PropertyChanged -= ControlChanged;
            if (control is PathPicker picker) picker.BrowseRequested -= BrowseRequested;
            if (control is LinkLabel link) link.LinkClicked -= LinkClicked;
            if (control is Forma.Core.Controls.Timer timer) timer.Tick -= TimerTick;
            if (control is CommandControl commands) commands.ItemClicked -= CommandInvoked;
        }
        if (_bridge is not null) _bridge.MessageReceived -= CustomMessage;
        _bridge?.Dispose();
        _session.Dispose();
        base.OnFormClosed(e);
    }
}
