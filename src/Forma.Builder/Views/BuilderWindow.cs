using System.Text.Json;
using Forma.Core.Rendering;
using Forma.WebView2;
using Microsoft.Web.WebView2.Core;
using FControl = Forma.Core.Controls.Control;

namespace Forma.Builder;

public sealed partial class BuilderWindow : System.Windows.Forms.Form
{
    private readonly Microsoft.Web.WebView2.WinForms.WebView2 _preview = new()
    {
        Dock = DockStyle.Fill,
        AllowExternalDrop = true,
    };
    private WebView2Bridge? _bridge;
    private WebView2Renderer? _renderer;
    private bool _ready;
    private readonly CoalescedRefresh _stateRefresh;
    private bool _documentStateDirty;
    private PreviewWindow? _runtimePreview;
    private ComponentEditorView? _componentEditor;
    private string? _editorTargetId;
    private readonly Dictionary<string, ComponentEditorView> _editors = new();
    private bool _codeVisible;
    private bool _closingEditors;
    private readonly HashSet<FControl> _subscribedControls = [];
    private readonly HashSet<string> _orphanedEditors = [];
    private string _workspaceTheme = "dark";
    private readonly BuilderViewModel _viewModel = new();

    public BuilderWindow()
    {
        _stateRefresh = new CoalescedRefresh(SendStateAsync);
        Text = _viewModel.WindowTitle;
        _viewModel.PropertyChanged += (_, change) =>
        {
            if (change.PropertyName == nameof(BuilderViewModel.WindowTitle))
                Text = _viewModel.WindowTitle;
        };
        ClientSize = new Size(1280, 840);
        MinimumSize = new Size(1000, 650);
        StartPosition = FormStartPosition.CenterScreen;
        Controls.Add(_preview);
        Load += async (_, _) => await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            await _preview.EnsureCoreWebView2Async();
            // The designer has its own canvas zoom; page zoom would scale the whole UI instead.
            _preview.CoreWebView2.Settings.IsZoomControlEnabled = false;
            var completion = new TaskCompletionSource<bool>();
            void Navigated(object? sender, CoreWebView2NavigationCompletedEventArgs e) =>
                completion.TrySetResult(e.IsSuccess);
            _preview.CoreWebView2.NavigationCompleted += Navigated;
            try
            {
                _preview.CoreWebView2.Navigate(
                    new Uri(
                        Path.Combine(AppContext.BaseDirectory, "DesignerWeb", "index.html")
                    ).AbsoluteUri
                );
                if (!await completion.Task.WaitAsync(TimeSpan.FromSeconds(30)))
                    throw new InvalidOperationException("The designer page could not load.");
            }
            finally
            {
                _preview.CoreWebView2.NavigationCompleted -= Navigated;
            }
            _bridge = new WebView2Bridge(_preview.CoreWebView2);
            _renderer = new WebView2Renderer(_bridge);
            _bridge.MessageReceived += OnDesignerMessage;
            await _renderer.InitializeAsync();
            await NewProjectAsync();
            _ready = true;
            await StartRecoveryAsync();
        }
        catch (Exception error)
        {
            MessageBox.Show(
                this,
                error.Message,
                "Forma Builder",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    private void SubscribeControl(FControl control)
    {
        if (!_subscribedControls.Add(control)) return;
        if (control is Forma.Core.Controls.Dialog dialog)
            dialog.Closed += async (_, result) =>
            {
                if (_ready && !IsDisposed && _viewModel.PreviewMode) await StateAsync($"{control.Name}: {result.Result}");
            };
        if (control is Forma.Core.Controls.CommandControl commands)
            commands.ItemClicked += async (_, item) =>
            {
                if (_ready && !IsDisposed && _viewModel.PreviewMode && _viewModel.Appearance[control.Id].Enabled)
                    await StateAsync($"{control.Name}: {item.Text} ({item.Id})");
            };
        if (control is Forma.Core.Controls.PathPicker picker)
            picker.BrowseRequested += async (_, _) => await ChoosePathAsync(picker, design: false);
        control.PropertyChanged += async (_, _) =>
        {
            if (_ready && !IsDisposed && _viewModel.Appearance.ContainsKey(control.Id))
            {
                _documentStateDirty = true;
                await StateAsync("Ready");
            }
        };
        if (control is Forma.Core.Controls.LinkLabel link)
            link.LinkClicked += (_, _) => {
                if (_viewModel.PreviewMode && _viewModel.Appearance[link.Id].Enabled) {
                    try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(link.Url) { UseShellExecute = true }); }
                    catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException) { MessageBox.Show(this, error.Message, "Could not open link"); }
                }
            };
        if (control is Forma.Core.Controls.Timer timer)
            timer.Tick += (_, _) =>
            {
                if (!_ready || IsDisposed || !IsHandleCreated)
                    return;
                try
                {
                    BeginInvoke(() =>
                    {
                        if (_ready && !IsDisposed) {
                            UpdateProjectTitle();
                            Text += $" - {timer.Name}: tick {DateTime.Now:HH:mm:ss}";
                        }
                    });
                }
                catch (InvalidOperationException)
                { /* Window closed while a tick was queued. */
                }
            };
    }

    private static IEnumerable<FControl> Walk(FControl root)
    {
        yield return root;
        foreach (var child in root.Children)
        foreach (var item in Walk(child))
            yield return item;
    }

    private static string? String(JsonElement json, string name) =>
        json.ValueKind == JsonValueKind.Object
        && json.TryGetProperty(name, out var v)
        && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    private bool HasUnsavedChanges => _viewModel.HasUnsavedChanges;
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Ask even mid-render (_ready is false then); only skip before a design exists.
        if (_viewModel.Form is not null && !ConfirmDiscard()) e.Cancel = true;
        base.OnFormClosing(e);
    }

    private void ShowAbout()
    {
        MessageBox.Show(
            this,
            "Forma is a modern C# UI framework for Windows desktop applications.\n\n"
                + "Built with .NET and WebView2, Forma brings modern web-based rendering and styling "
                + "to a code-first C# development experience—without traditional XML layouts or "
                + "designer-generated code.\n\n"
                + "Code. Design. Build. Forma.",
            "About Forma"
        );
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _ready = false;
        StopRecovery();
        _runtimePreview?.Close();
        CloseDockedEditor();
        DisposeComponents();
        if (_bridge is not null)
            _bridge.MessageReceived -= OnDesignerMessage;
        _bridge?.Dispose();
        base.OnFormClosed(e);
    }
}
