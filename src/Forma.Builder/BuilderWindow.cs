using System.Text.Json;
using Forma.Core.Rendering;
using Forma.WebView2;
using Microsoft.Web.WebView2.Core;
using FControl = Forma.Core.Controls.Control;

namespace Forma.Builder;

public sealed class BuilderWindow : System.Windows.Forms.Form
{
    private readonly Microsoft.Web.WebView2.WinForms.WebView2 _preview = new()
    { Dock = DockStyle.Fill, AllowExternalDrop = true };
    private readonly Dictionary<string, Appearance> _appearance = [];
    private WebView2Bridge? _bridge;
    private WebView2Renderer? _renderer;
    private Forma.Core.Form? _form;
    private FControl? _selected;
    private int _count;
    private bool _ready;

    private sealed class Appearance
    {
        public int Width { get; set; } = 120;
        public int Height { get; set; } = 36;
        public string ForeColor { get; set; } = "#ffffff";
        public string BackColor { get; set; } = "#2878ff";
        public int FontSize { get; set; } = 14;
        public bool Enabled { get; set; } = true;
        public bool Visible { get; set; } = true;
        public string FontFamily { get; set; } = "Segoe UI";
        public string FontWeight { get; set; } = "normal";
        public string FontStyle { get; set; } = "normal";
        public string TextAlign { get; set; } = "center";
        public string BorderStyle { get; set; } = "solid";
        public string BorderColor { get; set; } = "#d3deee";
        public int BorderWidth { get; set; } = 1;
        public int BorderRadius { get; set; } = 4;
        public int Padding { get; set; } = 8;
        public int Opacity { get; set; } = 100;
        public string Placeholder { get; set; } = "Enter your name…";
        public bool ReadOnly { get; set; }
        public bool Password { get; set; }
        public int MaxLength { get; set; } = 32767;
    }

    public BuilderWindow()
    {
        Text = "Forma Builder";
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
            var completion = new TaskCompletionSource<bool>();
            void Navigated(object? sender, CoreWebView2NavigationCompletedEventArgs e)
                => completion.TrySetResult(e.IsSuccess);
            _preview.CoreWebView2.NavigationCompleted += Navigated;
            try
            {
                _preview.CoreWebView2.Navigate(new Uri(Path.Combine(AppContext.BaseDirectory,
                    "DesignerWeb", "index.html")).AbsoluteUri);
                if (!await completion.Task.WaitAsync(TimeSpan.FromSeconds(30)))
                    throw new InvalidOperationException("The designer page could not load.");
            }
            finally { _preview.CoreWebView2.NavigationCompleted -= Navigated; }
            _bridge = new WebView2Bridge(_preview.CoreWebView2);
            _renderer = new WebView2Renderer(_bridge);
            _bridge.MessageReceived += OnDesignerMessage;
            await _renderer.InitializeAsync();
            await NewFormAsync();
            _ready = true;
        }
        catch (Exception error)
        { MessageBox.Show(this, error.Message, "Forma Builder", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task NewFormAsync()
    {
        if (_renderer is null || _bridge is null) return;
        _ready = false;
        if (_form is not null) await _renderer.RemoveAsync(_form);
        _appearance.Clear();
        _count = 0;
        _form = new Forma.Core.Form { Title = "Form1", Width = 640, Height = 440 };
        _appearance[_form.Id] = new Appearance { Width = 640, Height = 440, BackColor = "#ffffff", ForeColor = "#1f2937" };
        await _renderer.RenderAsync(_form);
        await _bridge.SendAsync(new { type = "designer", action = "initialize", id = _form.Id, title = _form.Title });
        _selected = _form;
        await StateAsync("Ready — drag a control onto the form");
        _ready = true;
    }

    private async void OnDesignerMessage(object? sender, BridgeMessage message)
    {
        if (message.Type != "designer" || !_ready || _form is null || _bridge is null) return;
        try
        {
            var control = message.Id == _form.Id ? _form : _form.Children.FirstOrDefault(c => c.Id == message.Id);
            var payload = message.Payload is JsonElement json && json.ValueKind == JsonValueKind.Object ? json : default;
            switch (message.Event)
            {
                case "command":
                    var command = String(payload, "command");
                    if (command == "new") await NewFormAsync();
                    else if (command == "about") ShowAbout();
                    else if (command == "exit") Close();
                    else if (command == "delete" && _selected is not null && _selected != _form)
                    {
                        _form.Remove(_selected);
                        _appearance.Remove(_selected.Id);
                        _selected = _form;
                        await StateAsync("Control deleted");
                    }
                    break;
                case "select" when control is not null:
                    _selected = control;
                    await StateAsync("Ready");
                    break;
                case "drop" when message.Id == _form.Id:
                    if (Number(payload, "x") is int x && Number(payload, "y") is int y)
                        await AddControlAsync(String(payload, "control"), x, y);
                    break;
                case "move" when control is not null && control != _form:
                    if (Number(payload, "x") is int left && Number(payload, "y") is int top)
                    {
                        Position(control, left, top);
                        _selected = control;
                        await StateAsync("Control moved");
                    }
                    break;
                case "resize" when control is not null:
                    if (Number(payload, "width") is int width && Number(payload, "height") is int height)
                    {
                        ResizeControl(control, width, height);
                        if (control != _form && Number(payload, "x") is int rx && Number(payload, "y") is int ry)
                            Position(control, rx, ry);
                        _selected = control;
                        await StateAsync("Control resized");
                    }
                    break;
                case "property" when control is not null && control == _selected:
                    ApplyProperty(control, payload);
                    await StateAsync("Property updated");
                    break;
            }
        }
        catch (Exception error)
        {
            if (!IsDisposed) await StateAsync($"Designer error: {error.Message}");
        }
    }

    private async Task AddControlAsync(string? kind, int x, int y)
    {
        if (_form is null || x < 0 || y < 0 || x >= _form.Width || y >= _form.Height) return;
        FControl? control = kind switch
        {
            "button" => new Forma.Core.Controls.Button { Text = "Continue" },
            "label" => new Forma.Core.Controls.Label { Text = "Welcome to Forma" },
            "textbox" => new Forma.Core.Controls.TextBox { Text = "" },
            "panel" => new Forma.Core.Controls.Panel(),
            _ => null,
        };
        if (control is null) return;
        control.Name = $"{kind}{++_count}";
        _appearance[control.Id] = kind switch
        {
            "label" => new Appearance { Width = 260, Height = 40, ForeColor = "#1f2937", BackColor = "#ffffff", FontSize = 22 },
            "textbox" => new Appearance { Width = 240, Height = 36, ForeColor = "#1f2937", BackColor = "#ffffff" },
            "panel" => new Appearance { Width = 220, Height = 140, ForeColor = "#1f2937", BackColor = "#f3f6fb" },
            _ => new Appearance(),
        };
        Position(control, x, y);
        _form.Add(control);
        _selected = control;
        await StateAsync($"Added {control.Name}");
    }

    private void Position(FControl control, int x, int y)
    {
        var bounds = _appearance[control.Id];
        control.X = Math.Clamp(x, 0, Math.Max(0, _form!.Width - bounds.Width));
        control.Y = Math.Clamp(y, 0, Math.Max(0, _form.Height - bounds.Height));
    }

    private void ApplyProperty(FControl control, JsonElement payload)
    {
        var property = String(payload, "property");
        var a = _appearance[control.Id];
        var text = String(payload, "value");
        var number = Number(payload, "value");
        switch (property)
        {
            case "name" when control != _form && !string.IsNullOrWhiteSpace(text): control.Name = text.Trim(); break;
            case "text" when text is not null: control.Text = text; break;
            case "x" when control != _form && number is int x: Position(control, x, control.Y ?? 0); break;
            case "y" when control != _form && number is int y: Position(control, control.X ?? 0, y); break;
            case "width" when number is int w: ResizeControl(control, w, a.Height); break;
            case "height" when number is int h: ResizeControl(control, a.Width, h); break;
            case "fontSize" when number is int size: a.FontSize = Math.Clamp(size, 8, 48); break;
            case "foreColor" when IsColor(text): a.ForeColor = text!; break;
            case "backColor" when IsColor(text): a.BackColor = text!; break;
            case "enabled": a.Enabled = Boolean(payload, "value", a.Enabled); break;
            case "visible": a.Visible = Boolean(payload, "value", a.Visible); break;
            case "fontFamily" when text is "Segoe UI" or "Arial" or "Consolas" or "Georgia": a.FontFamily = text; break;
            case "fontWeight" when text is "normal" or "bold": a.FontWeight = text; break;
            case "fontStyle" when text is "normal" or "italic": a.FontStyle = text; break;
            case "textAlign" when text is "left" or "center" or "right": a.TextAlign = text; break;
            case "borderStyle" when text is "none" or "solid" or "dashed" or "dotted": a.BorderStyle = text; break;
            case "borderColor" when IsColor(text): a.BorderColor = text!; break;
            case "borderWidth" when number is int bw: a.BorderWidth = Math.Clamp(bw, 0, 10); break;
            case "borderRadius" when number is int br: a.BorderRadius = Math.Clamp(br, 0, 100); break;
            case "padding" when number is int p: a.Padding = Math.Clamp(p, 0, 64); break;
            case "opacity" when number is int o: a.Opacity = Math.Clamp(o, 10, 100); break;
            case "placeholder" when control is Forma.Core.Controls.TextBox && text is not null: a.Placeholder = text; break;
            case "readOnly" when control is Forma.Core.Controls.TextBox: a.ReadOnly = Boolean(payload, "value", a.ReadOnly); break;
            case "password" when control is Forma.Core.Controls.TextBox: a.Password = Boolean(payload, "value", a.Password); break;
            case "maxLength" when control is Forma.Core.Controls.TextBox && number is int ml: a.MaxLength = Math.Clamp(ml, 1, 32767); break;
        }
        if (control != _form) Position(control, control.X ?? 0, control.Y ?? 0);
        if (control == _form) Text = $"Forma Builder — {_form!.Title}";
    }

    private void ResizeControl(FControl control, int width, int height)
    {
        var a = _appearance[control.Id];
        if (control == _form)
        {
            var minWidth = Math.Max(240, _form.Children.Select(c => _appearance[c.Id].Width).DefaultIfEmpty(0).Max());
            var minHeight = Math.Max(160, _form.Children.Select(c => _appearance[c.Id].Height).DefaultIfEmpty(0).Max());
            _form.Width = a.Width = Math.Clamp(width, minWidth, 1600);
            _form.Height = a.Height = Math.Clamp(height, minHeight, 1200);
            foreach (var child in _form.Children) Position(child, child.X ?? 0, child.Y ?? 0);
        }
        else
        {
            a.Width = Math.Clamp(width, 24, _form!.Width);
            a.Height = Math.Clamp(height, 20, _form.Height);
            Position(control, control.X ?? 0, control.Y ?? 0);
        }
    }

    private Task StateAsync(string status)
    {
        if (_bridge is null || _form is null || _selected is null) return Task.CompletedTask;
        return _bridge.SendAsync(new
        {
            type = "designer", action = "state", id = _form.Id, title = _form.Title,
            selectedId = _selected.Id, status,
            controls = new[] { (FControl)_form }.Concat(_form.Children).Select(c => new
            {
                id = c.Id, name = c == _form ? _form.Title : c.Name, kind = c.ControlType,
                text = c.Text ?? "", x = c.X ?? 0, y = c.Y ?? 0,
                width = _appearance[c.Id].Width, height = _appearance[c.Id].Height,
                fontSize = _appearance[c.Id].FontSize, foreColor = _appearance[c.Id].ForeColor,
                backColor = _appearance[c.Id].BackColor, enabled = _appearance[c.Id].Enabled,
                visible = _appearance[c.Id].Visible,
                fontFamily = _appearance[c.Id].FontFamily, fontWeight = _appearance[c.Id].FontWeight,
                fontStyle = _appearance[c.Id].FontStyle, textAlign = _appearance[c.Id].TextAlign,
                borderStyle = _appearance[c.Id].BorderStyle, borderColor = _appearance[c.Id].BorderColor,
                borderWidth = _appearance[c.Id].BorderWidth, borderRadius = _appearance[c.Id].BorderRadius,
                padding = _appearance[c.Id].Padding, opacity = _appearance[c.Id].Opacity,
                placeholder = _appearance[c.Id].Placeholder, readOnly = _appearance[c.Id].ReadOnly,
                password = _appearance[c.Id].Password, maxLength = _appearance[c.Id].MaxLength,
            }).ToArray(),
        });
    }

    private static string? String(JsonElement json, string name) => json.ValueKind == JsonValueKind.Object
        && json.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    private static int? Number(JsonElement json, string name) => json.ValueKind == JsonValueKind.Object
        && json.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n) ? n : null;
    private static bool Boolean(JsonElement json, string name, bool fallback) => json.ValueKind == JsonValueKind.Object
        && json.TryGetProperty(name, out var v) && v.ValueKind is JsonValueKind.True or JsonValueKind.False ? v.GetBoolean() : fallback;
    private static bool IsColor(string? value) => value is { Length: 7 } && value[0] == '#'
        && value.Skip(1).All(Uri.IsHexDigit);

    private void ShowAbout()
    {
        MessageBox.Show(this,
            "Forma is a modern C# UI framework for Windows desktop applications.\n\n"
                + "Built with .NET and WebView2, Forma brings modern web-based rendering and styling "
                + "to a code-first C# development experience—without traditional XML layouts or "
                + "designer-generated code.\n\n"
                + "Code. Design. Build. Forma.", "About Forma");
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _ready = false;
        if (_bridge is not null) _bridge.MessageReceived -= OnDesignerMessage;
        _bridge?.Dispose();
        base.OnFormClosed(e);
    }
}
