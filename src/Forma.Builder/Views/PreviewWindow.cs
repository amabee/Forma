using Forma.Core.Controls;
using Forma.WebView2;
using Microsoft.Web.WebView2.Core;
using LinkLabel = Forma.Core.Controls.LinkLabel;
using System.Text.Json;
using Forma.Core.Rendering;

namespace Forma.Builder;

/// <summary>Hosts the current design as a running Forma window.</summary>
public sealed class PreviewWindow : System.Windows.Forms.Form
{
    private readonly PreviewSession _session;
    private readonly Microsoft.Web.WebView2.WinForms.WebView2 _web = new() { Dock = DockStyle.Fill };
    private WebView2Bridge? _bridge;
    private WebView2Renderer? _renderer;
    private bool _ready;
    private readonly CoalescedRefresh _stateRefresh;

    public PreviewWindow(PreviewSession session)
    {
        _session = session;
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
            if (message.Event == "set")
                _session.SetValue(message.Id, payload.GetProperty("property").GetString()!, payload.GetProperty("value"));
            else if (message.Event == "grid")
                _session.EditGrid(message.Id, payload.GetProperty("operation").GetString()!, payload);
            else if (message.Event == "show-dialog" && _session.Appearance.TryGetValue(message.Id, out var appearance) && appearance.Enabled
                && _session.Controls.FirstOrDefault(control => control.Id == message.Id) is Forma.Core.Controls.Dialog dialog)
                dialog.Show();
            if (_session.Appearance.TryGetValue(message.Id, out var toastAppearance) && toastAppearance.Enabled
                && _session.Controls.FirstOrDefault(control => control.Id == message.Id) is Toast toast)
            {
                if (message.Event == "show-toast") toast.Show();
                if (message.Event == "close-toast") toast.Close();
            }
            await _stateRefresh.Request("Ready");
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException)
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

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _ready = false;
        foreach (var control in _session.Controls)
        {
            control.PropertyChanged -= ControlChanged;
            if (control is PathPicker picker) picker.BrowseRequested -= BrowseRequested;
            if (control is LinkLabel link) link.LinkClicked -= LinkClicked;
            if (control is Forma.Core.Controls.Timer timer) timer.Tick -= TimerTick;
        }
        if (_bridge is not null) _bridge.MessageReceived -= CustomMessage;
        _bridge?.Dispose();
        _session.Dispose();
        base.OnFormClosed(e);
    }
}
