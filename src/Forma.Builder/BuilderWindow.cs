using System.Text.Json;
using Forma.Core.Rendering;
using Forma.WebView2;
using Microsoft.Web.WebView2.Core;
using FControl = Forma.Core.Controls.Control;

namespace Forma.Builder;

public sealed class BuilderWindow : System.Windows.Forms.Form
{
    private readonly Microsoft.Web.WebView2.WinForms.WebView2 _preview = new()
    {
        Dock = DockStyle.Fill,
        AllowExternalDrop = true,
    };
    private readonly Dictionary<string, Appearance> _appearance = [];
    private WebView2Bridge? _bridge;
    private WebView2Renderer? _renderer;
    private Forma.Core.Form? _form;
    private FControl? _selected;
    private int _count;
    private bool _ready;
    private bool _previewMode;
    private readonly DesignHistory _history = new();
    private string? _projectPath;
    private string _savedSnapshot = "";

    private sealed class Appearance
    {
        public Appearance() { }
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
        public string Tag { get; set; } = "";
        public bool Locked { get; set; }
        public string Style { get; set; } = "Custom";
        public string Shadow { get; set; } = "None";
        public string Cursor { get; set; } = "default";
        public double LineHeight { get; set; } = 1.5;
        public double LetterSpacing { get; set; }
        public bool Focusable { get; set; } = true;
        public int TabIndex { get; set; }
        public string ToolTip { get; set; } = "";
        public string CssClass { get; set; } = "";
        public string CustomCss { get; set; } = "";
        public int ZIndex { get; set; }
        public int MarginTop { get; set; }
        public int MarginRight { get; set; }
        public int MarginBottom { get; set; }
        public int MarginLeft { get; set; }
        public int PaddingTop { get; set; } = 8;
        public int PaddingRight { get; set; } = 8;
        public int PaddingBottom { get; set; } = 8;
        public int PaddingLeft { get; set; } = 8;
        public int MinimumWidth { get; set; }
        public int MinimumHeight { get; set; }
        public int MaximumWidth { get; set; }
        public int MaximumHeight { get; set; }
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
            await NewFormAsync();
            _ready = true;
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

    private async Task NewFormAsync()
    {
        if (_renderer is null || _bridge is null)
            return;
        _ready = false;
        if (_form is not null)
            await _renderer.RemoveAsync(_form);
        DisposeComponents();
        _previewMode = false;
        _appearance.Clear();
        _count = 0;
        _history.Clear();
        _form = new Forma.Core.Form
        {
            Name = "form1",
            Title = "Form1",
            Width = 640,
            Height = 440,
        };
        _appearance[_form.Id] = new Appearance
        {
            Width = 640,
            Height = 440,
            BackColor = "#ffffff",
            ForeColor = "#1f2937",
        };
        await _renderer.RenderAsync(_form);
        await _bridge.SendAsync(
            new
            {
                type = "designer",
                action = "initialize",
                id = _form.Id,
                title = _form.Title,
            }
        );
        _selected = _form;
        _projectPath = null;
        _savedSnapshot = Snapshot();
        UpdateProjectTitle();
        await StateAsync("Ready — drag a control onto the form");
        _ready = true;
    }

    private async void OnDesignerMessage(object? sender, BridgeMessage message)
    {
        if (message.Type != "designer" || !_ready || _form is null || _bridge is null)
            return;
        try
        {
            var control = Walk(_form).FirstOrDefault(c => c.Id == message.Id);
            var payload =
                message.Payload is JsonElement json && json.ValueKind == JsonValueKind.Object
                    ? json
                    : default;
            var commandName = String(payload, "command");
            var changesDesign = !_previewMode && (message.Event is "drop" or "move" or "resize" or "property"
                || message.Event == "command" && commandName is "delete" or "choose-image" or "bring-front" or "send-back");
            var before = changesDesign ? HistorySnapshot() : null;
            switch (message.Event)
            {
                case "command":
                    var command = commandName;
                    if (command is "undo" or "redo") {
                        if (!_previewMode) {
                            var snapshot = command == "undo" ? _history.Undo() : _history.Redo();
                            if (snapshot is not null) await RestoreHistoryAsync(snapshot, command == "undo" ? "Undone" : "Redone");
                        }
                        break;
                    }
                    if (command is "save" or "save-as") { SaveProject(command == "save-as"); break; }
                    if (command == "open") { await OpenProjectAsync(); break; }
                    if (
                        command == "choose-image"
                        && _selected is Forma.Core.Controls.Image image
                        && !_previewMode
                        && !_appearance[image.Id].Locked
                    )
                    {
                        using var picker = new OpenFileDialog
                        {
                            Title = "Choose image",
                            Filter =
                                "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp;*.svg;*.ico|All files|*.*",
                            CheckFileExists = true,
                        };
                        if (picker.ShowDialog(this) == DialogResult.OK)
                            image.Source = new Uri(picker.FileName).AbsoluteUri;
                        await StateAsync("Ready");
                        break;
                    }
                    if (
                        command is "bring-front" or "send-back"
                        && _selected is not null
                        && _selected != _form
                        && !_previewMode
                        && !_appearance[_selected.Id].Locked
                    )
                    {
                        var siblings = _selected
                            .Parent!.Children.OrderBy(c => _appearance[c.Id].ZIndex)
                            .ToList();
                        siblings.Remove(_selected);
                        if (command == "bring-front")
                            siblings.Add(_selected);
                        else
                            siblings.Insert(0, _selected);
                        for (var i = 0; i < siblings.Count; i++)
                            _appearance[siblings[i].Id].ZIndex = i;
                        await StateAsync("Layer order updated");
                        break;
                    }
                    if (command == "new" && ConfirmDiscard())
                        await NewFormAsync();
                    else if (command == "about")
                        ShowAbout();
                    else if (command == "exit")
                        Close();
                    else if (
                        command == "delete"
                        && _selected is not null
                        && _selected != _form
                        && !_appearance[_selected.Id].Locked
                    )
                    {
                        var removed = Walk(_selected).ToArray();
                        _selected.Parent?.Remove(_selected);
                        foreach (var child in removed)
                        {
                            _appearance.Remove(child.Id);
                            (child as IDisposable)?.Dispose();
                        }
                        _selected = _form;
                        await StateAsync("Control deleted");
                    }
                    break;
                case "select" when control is not null:
                    _selected = control;
                    await StateAsync("Ready");
                    break;
                case "drop"
                    when control == _form
                        || control
                            is Forma.Core.Controls.LayoutContainer
                                or Forma.Core.Controls.Panel:
                    if (Number(payload, "x") is int x && Number(payload, "y") is int y)
                        await AddControlAsync(String(payload, "control"), x, y, control!);
                    break;
                case "preview":
                    _previewMode = Boolean(payload, "enabled", false);
                    foreach (var timer in Walk(_form).OfType<Forma.Core.Controls.Timer>())
                        timer.Enabled = _previewMode && _appearance[timer.Id].Enabled;
                    await StateAsync("Ready");
                    break;
                case "move"
                    when control is not null && control != _form && !_appearance[control.Id].Locked:
                    if (Number(payload, "x") is int left && Number(payload, "y") is int top)
                    {
                        Position(control, left, top);
                        _selected = control;
                        await StateAsync("Control moved");
                    }
                    break;
                case "resize" when control is not null && !_appearance[control.Id].Locked:
                    if (
                        Number(payload, "width") is int width
                        && Number(payload, "height") is int height
                    )
                    {
                        ResizeControl(control, width, height);
                        if (
                            control != _form
                            && Number(payload, "x") is int rx
                            && Number(payload, "y") is int ry
                        )
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
            if (before is not null) {
                var group = message.Event == "property" && String(payload, "property") is "text" or "document" ? $"text:{message.Id}" : null;
                if (_history.Record(before, HistorySnapshot(), group)) await StateAsync("Ready");
            }
        }
        catch (Exception error)
        {
            if (!IsDisposed)
                await StateAsync($"Designer error: {error.Message}");
        }
    }

    private async Task AddControlAsync(string? kind, int x, int y, FControl parent)
    {
        if (
            _form is null
            || _appearance[parent.Id].Locked
            || x < 0
            || y < 0
            || x >= _appearance[parent.Id].Width
            || y >= _appearance[parent.Id].Height
        )
            return;
        FControl? control = kind switch
        {
            "numericupdown" => new Forma.Core.Controls.NumericUpDown(),
            "slider" => new Forma.Core.Controls.Slider { Value = 25 },
            "progressbar" => new Forma.Core.Controls.ProgressBar { Value = 50 },
            "circularprogress" => new Forma.Core.Controls.CircularProgress { Value = 50 },
            "toggleswitch" => new Forma.Core.Controls.ToggleSwitch { Text = "Toggle" },
            "togglebutton" => new Forma.Core.Controls.ToggleButton { Text = "Toggle" },
            "datepicker" => new Forma.Core.Controls.DatePicker(),
            "timepicker" => new Forma.Core.Controls.TimePicker(),
            "datetimepicker" => new Forma.Core.Controls.DateTimePicker(),
            "colorpicker" => new Forma.Core.Controls.ColorPicker(),
            "searchbox" => new Forma.Core.Controls.SearchBox(),
            "passwordbox" => new Forma.Core.Controls.PasswordBox(),
            "textarea" => new Forma.Core.Controls.TextArea(),
            "button" => new Forma.Core.Controls.Button { Text = "Continue" },
            "label" => new Forma.Core.Controls.Label { Text = "Welcome to Forma" },
            "linklabel" => new Forma.Core.Controls.LinkLabel { Text = "Visit website" },
            "maskedtextbox" => new Forma.Core.Controls.MaskedTextBox(),
            "checkedlistbox" => new Forma.Core.Controls.CheckedListBox(),
            "textbox" => new Forma.Core.Controls.TextBox { Text = "" },
            "panel" => new Forma.Core.Controls.Panel(),
            "groupbox" => new Forma.Core.Controls.GroupBox { Text = "Group" },
            "splitcontainer" => new Forma.Core.Controls.SplitContainer(),
            "tabcontrol" => new Forma.Core.Controls.TabControl(),
            "flowlayoutpanel" => new Forma.Core.Controls.FlowLayoutPanel(),
            "tablelayoutpanel" => new Forma.Core.Controls.TableLayoutPanel(),
            "checkbox" => new Forma.Core.Controls.CheckBox { Text = "CheckBox" },
            "radiobutton" => new Forma.Core.Controls.RadioButton { Text = "RadioButton" },
            "combobox" => new Forma.Core.Controls.ComboBox(),
            "listbox" => new Forma.Core.Controls.ListBox(),
            "richtextbox" => new Forma.Core.Controls.RichTextBox(),
            "picturebox" => new Forma.Core.Controls.PictureBox { Text = "Picture" },
            "image" => new Forma.Core.Controls.Image { Text = "Image" },
            "datagridview" => new Forma.Core.Controls.DataGridView(),
            "timer" => new Forma.Core.Controls.Timer { Text = "Timer" },
            "backgroundworker" => new Forma.Core.Controls.BackgroundWorker
            {
                Text = "BackgroundWorker",
            },
            _ => null,
        };
        if (control is null)
            return;
        do { control.Name = $"{kind}{++_count}"; } while (Walk(_form).Any(c => c.Name == control.Name));
        _appearance[control.Id] = kind switch
        {
            "label" => new Appearance
            {
                Width = 260,
                Height = 40,
                ForeColor = "#1f2937",
                BackColor = "#ffffff",
                FontSize = 22,
            },
            "textbox" or "maskedtextbox" or "searchbox" or "passwordbox" or "datepicker" or "timepicker" or "datetimepicker" or "numericupdown" or "colorpicker" or "slider" or "progressbar" => new Appearance
            {
                Width = 240,
                Height = 36,
                ForeColor = "#1f2937",
                BackColor = "#ffffff",
            },
            "panel" => new Appearance
            {
                Width = 220,
                Height = 140,
                ForeColor = "#1f2937",
                BackColor = "#f3f6fb",
            },
            "groupbox"
            or "splitcontainer"
            or "tabcontrol"
            or "flowlayoutpanel"
            or "tablelayoutpanel" => new Appearance
            {
                Width = 300,
                Height = 200,
                ForeColor = "#1f2937",
                BackColor = "#ffffff",
                PaddingTop = 0,
                PaddingBottom = 0,
                PaddingLeft = 0,
                PaddingRight = 0,
            },
            "checkbox" or "radiobutton" or "toggleswitch" => new Appearance
            {
                Width = 160,
                Height = 36,
                ForeColor = "#1f2937",
                BackColor = "#ffffff",
                TextAlign = "left",
                BorderWidth = 0,
            },
            "combobox" => new Appearance
            {
                Width = 180,
                Height = 36,
                ForeColor = "#1f2937",
                BackColor = "#ffffff",
            },
            "listbox" or "checkedlistbox" or "richtextbox" or "textarea" or "circularprogress" => new Appearance
            {
                Width = 180,
                Height = 140,
                ForeColor = "#1f2937",
                BackColor = "#ffffff",
            },
            "image" or "picturebox" => new Appearance
            {
                Width = 180,
                Height = 120,
                ForeColor = "#1f2937",
                BackColor = "#f3f6fb",
                PaddingTop = 0,
                PaddingBottom = 0,
                PaddingLeft = 0,
                PaddingRight = 0,
            },
            "datagridview" => new Appearance
            {
                Width = 320,
                Height = 180,
                ForeColor = "#1f2937",
                BackColor = "#ffffff",
                PaddingTop = 0,
                PaddingBottom = 0,
                PaddingLeft = 0,
                PaddingRight = 0,
            },
            "timer" or "backgroundworker" => new Appearance { Enabled = false },
            _ => new Appearance(),
        };
        _appearance[control.Id].ZIndex = control
            is Forma.Core.Controls.Panel
                or Forma.Core.Controls.LayoutContainer
            ? 0
            : 10;
        if (control is Forma.Core.Controls.CircularProgress) { _appearance[control.Id].Width = 120; _appearance[control.Id].Height = 120; _appearance[control.Id].BorderRadius = 100; }
        if (control is Forma.Core.Controls.LinkLabel) { _appearance[control.Id].Width = 180; _appearance[control.Id].ForeColor = "#2563eb"; _appearance[control.Id].BackColor = "#ffffff"; _appearance[control.Id].BorderWidth = 0; _appearance[control.Id].Cursor = "pointer"; }
        if (control is Forma.Core.Controls.MaskedTextBox) _appearance[control.Id].Placeholder = "000-0000";
        if (control is Forma.Core.Controls.SearchBox) _appearance[control.Id].Placeholder = "Search...";
        if (control is Forma.Core.Controls.PasswordBox) _appearance[control.Id].Placeholder = "Password";
        if (control is Forma.Core.Controls.PasswordBox) _appearance[control.Id].Password = true;
        if (control is Forma.Core.Controls.Component)
            parent = _form;
        control.X = x;
        control.Y = y;
        if (parent is Forma.Core.Controls.TabControl tabParent)
            control.LayoutSlot = tabParent.SelectedTab + 1;
        else if (parent is Forma.Core.Controls.SplitContainer)
            control.LayoutSlot = Math.Min(2, parent.Children.Count + 1);
        else if (parent is Forma.Core.Controls.TableLayoutPanel)
            control.LayoutSlot = parent.Children.Count + 1;
        parent.Add(control);
        if (control is not Forma.Core.Controls.Component)
            ResizeControl(control, _appearance[control.Id].Width, _appearance[control.Id].Height);
        SubscribeControl(control);
        _selected = control;
        await StateAsync($"Added {control.Name}");
    }

    private void SubscribeControl(FControl control)
    {
        control.PropertyChanged += async (_, _) =>
        {
            if (_ready && !IsDisposed && _appearance.ContainsKey(control.Id))
                await StateAsync("Ready");
        };
        if (control is Forma.Core.Controls.LinkLabel link)
            link.LinkClicked += (_, _) => {
                if (_previewMode && _appearance[link.Id].Enabled) {
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

    private (int Width, int Height) AvailableBounds(FControl parent)
    {
        var a = _appearance[parent.Id];
        if (parent == _form)
            return (a.Width, a.Height);
        var header =
            parent is Forma.Core.Controls.GroupBox ? 28
            : parent is Forma.Core.Controls.TabControl ? 36
            : 0;
        return (
            Math.Max(24, a.Width - a.PaddingLeft - a.PaddingRight - 2 * a.BorderWidth),
            Math.Max(20, a.Height - a.PaddingTop - a.PaddingBottom - 2 * a.BorderWidth - header)
        );
    }

    private void Position(FControl control, int x, int y)
    {
        var bounds = _appearance[control.Id];
        var parentBounds = AvailableBounds(control.Parent ?? _form!);
        control.X = Math.Clamp(
            x,
            0,
            Math.Max(0, parentBounds.Width - bounds.Width - bounds.MarginLeft - bounds.MarginRight)
        );
        control.Y = Math.Clamp(
            y,
            0,
            Math.Max(
                0,
                parentBounds.Height - bounds.Height - bounds.MarginTop - bounds.MarginBottom
            )
        );
    }

    private void ApplyProperty(FControl control, JsonElement payload)
    {
        var property = String(payload, "property");
        var a = _appearance[control.Id];
        var text = String(payload, "value");
        var number = Number(payload, "value");
        var descriptor = InspectorCatalog
            .ForKind(control.ControlType)
            .FirstOrDefault(p => p.Id == property);
        if (descriptor is null || descriptor.ReadOnly || (a.Locked && property != "locked"))
            return;
        if (descriptor.Options is not null && (text is null || !descriptor.Options.Contains(text)))
            return;
        if (control is Forma.Core.Controls.NumericControl numberControl && payload.TryGetProperty("value", out var numericValue) && numericValue.ValueKind == JsonValueKind.Number && numericValue.TryGetDouble(out var n))
        {
            if (property == "minimum") numberControl.Minimum = n;
            if (property == "maximum") numberControl.Maximum = n;
            if (property == "increment") numberControl.Increment = n;
            if (property == "number") numberControl.Value = n;
        }
        if (control is Forma.Core.Controls.DateTimeInput date && property == "dateValue" && text is not null) date.DateValue = text;
        if (control is Forma.Core.Controls.ColorPicker color && property == "color" && text is not null) color.Color = text;
        if (control is Forma.Core.Controls.LinkLabel link && property == "url" && text is not null) link.Url = text;
        if (control is Forma.Core.Controls.MaskedTextBox masked && property == "mask" && text is not null) masked.Mask = text;
        if (control is Forma.Core.Controls.CheckedListBox checkedList) {
            if (property == "items" && text is not null) checkedList.Items = Lines(text);
            if (property == "checkedIndices" && text is not null) checkedList.CheckedIndices = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(int.Parse).ToArray();
        }
        if (control is Forma.Core.Controls.RichTextBox rich) {
            if (property == "text" && text is not null) rich.SetPlainText(text);
            if (property == "document" && text is not null) rich.Document = JsonSerializer.Deserialize<Forma.Core.Controls.RichBlock[]>(text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            if (property == "readOnly") rich.ReadOnly = Boolean(payload, "value", rich.ReadOnly);
        }
        switch (property)
        {
            case "name" when !string.IsNullOrWhiteSpace(text):
                var name = text.Trim();
                if (
                    !(char.IsLetter(name[0]) || name[0] == '_')
                    || name.Any(c => !char.IsLetterOrDigit(c) && c != '_')
                    || Walk(_form!).Any(c => c != control && c.Name == name)
                )
                    throw new ArgumentException(
                        "Name must be a unique C# identifier using letters, digits, or underscores."
                    );
                control.Name = name;
                break;
            case "text" when control is Forma.Core.Controls.MaskedTextBox maskedInput && text is not null: maskedInput.SetMaskedText(text); break;
            case "text" when text is not null:
                control.Text = text;
                break;
            case "x" when control != _form && number is int x:
                Position(control, x, control.Y ?? 0);
                break;
            case "y" when control != _form && number is int y:
                Position(control, control.X ?? 0, y);
                break;
            case "width" when number is int w:
                ResizeControl(control, w, a.Height);
                break;
            case "height" when number is int h:
                ResizeControl(control, a.Width, h);
                break;
            case "fontSize" when number is int size:
                a.FontSize = Math.Clamp(size, 8, 48);
                break;
            case "foreColor" when IsColor(text):
                a.ForeColor = text!;
                break;
            case "backColor" when IsColor(text):
                a.BackColor = text!;
                break;
            case "enabled":
                a.Enabled = Boolean(payload, "value", a.Enabled);
                break;
            case "visible":
                a.Visible = Boolean(payload, "value", a.Visible);
                break;
            case "fontFamily" when text is "Segoe UI" or "Arial" or "Consolas" or "Georgia":
                a.FontFamily = text;
                break;
            case "fontWeight" when text is not null:
                a.FontWeight = text;
                break;
            case "fontStyle" when text is "normal" or "italic":
                a.FontStyle = text;
                break;
            case "textAlign" when text is "left" or "center" or "right":
                a.TextAlign = text;
                break;
            case "borderStyle" when text is "none" or "solid" or "dashed" or "dotted":
                a.BorderStyle = text;
                break;
            case "borderColor" when IsColor(text):
                a.BorderColor = text!;
                break;
            case "borderWidth" when number is int bw:
                a.BorderWidth = Math.Clamp(bw, 0, 10);
                break;
            case "borderRadius" when number is int br:
                a.BorderRadius = Math.Clamp(br, 0, 100);
                break;
            case "padding" when number is int p:
                a.Padding = Math.Clamp(p, 0, 64);
                break;
            case "opacity" when number is int o:
                a.Opacity = Math.Clamp(o, 10, 100);
                break;
            case "placeholder" when control is Forma.Core.Controls.TextBox && text is not null:
                a.Placeholder = text;
                break;
            case "readOnly" when control is Forma.Core.Controls.TextBox:
                a.ReadOnly = Boolean(payload, "value", a.ReadOnly);
                break;
            case "password" when control is Forma.Core.Controls.TextBox:
                a.Password = Boolean(payload, "value", a.Password);
                break;
            case "maxLength" when control is Forma.Core.Controls.TextBox && number is int ml:
                a.MaxLength = Math.Clamp(ml, 1, 32767);
                break;
            case "checked" when control is Forma.Core.Controls.CheckBox check:
                var isChecked = Boolean(payload, "value", check.Checked);
                if (isChecked && check is Forma.Core.Controls.RadioButton)
                    foreach (
                        var sibling in control.Parent!.Children.OfType<Forma.Core.Controls.RadioButton>()
                    )
                        if (sibling != check)
                            sibling.Checked = false;
                check.Checked = isChecked;
                break;
            case "items"
                when control is Forma.Core.Controls.ChoiceControl choice && text is not null:
                choice.Items = Lines(text);
                break;
            case "selectedIndex"
                when control is Forma.Core.Controls.ChoiceControl choice && number is int i:
                choice.SelectedIndex = i;
                break;
            case "source" when control is Forma.Core.Controls.Image image && text is not null:
                image.Source = Path.IsPathFullyQualified(text) ? new Uri(text).AbsoluteUri : text;
                break;
            case "sizeMode" when control is Forma.Core.Controls.Image image && text is not null:
                image.SizeMode = text;
                break;
            case "orientation"
                when control is Forma.Core.Controls.LayoutContainer layout && text is not null:
                layout.Orientation = text;
                break;
            case "gap"
                when control is Forma.Core.Controls.LayoutContainer layout && number is int gap:
                layout.Gap = gap;
                break;
            case "columns"
                when control is Forma.Core.Controls.TableLayoutPanel grid && number is int cols:
                grid.Columns = cols;
                break;
            case "tabs" when control is Forma.Core.Controls.TabControl tabs && text is not null:
                tabs.Tabs = Lines(text);
                break;
            case "selectedTab"
                when control is Forma.Core.Controls.TabControl tabs && number is int tab:
                tabs.SelectedTab = tab;
                break;
            case "layoutSlot" when number is int slot:
                control.LayoutSlot = slot;
                break;
            case "gridColumns"
                when control is Forma.Core.Controls.DataGridView grid && text is not null:
                grid.Columns = Lines(text);
                break;
            case "gridRows"
                when control is Forma.Core.Controls.DataGridView grid && text is not null:
                grid.Rows = JsonSerializer.Deserialize<string[][]>(text) ?? [];
                break;
            case "readOnly" when control is Forma.Core.Controls.DataGridView grid:
                grid.ReadOnly = Boolean(payload, "value", grid.ReadOnly);
                break;
            case "interval"
                when control is Forma.Core.Controls.Timer timer && number is int interval:
                timer.Interval = interval;
                break;
            case "workerReportsProgress"
                when control is Forma.Core.Controls.BackgroundWorker worker:
                worker.WorkerReportsProgress = Boolean(
                    payload,
                    "value",
                    worker.WorkerReportsProgress
                );
                break;
            case "workerSupportsCancellation"
                when control is Forma.Core.Controls.BackgroundWorker worker:
                worker.WorkerSupportsCancellation = Boolean(
                    payload,
                    "value",
                    worker.WorkerSupportsCancellation
                );
                break;
            case "style" when text is not null:
                a.Style = text;
                if (text != "Custom")
                {
                    a.BackColor = text switch
                    {
                        "Primary" => "#2878ff",
                        "Success" => "#15803d",
                        "Warning" => "#f59e0b",
                        "Danger" => "#dc2626",
                        "Secondary" => "#64748b",
                        _ => "#f3f4f6",
                    };
                    a.ForeColor = text is "Default" or "Warning" ? "#1f2937" : "#ffffff";
                    a.BorderColor = a.BackColor;
                }
                break;
            default:
                var field = typeof(Appearance).GetProperty(
                    property!,
                    System.Reflection.BindingFlags.Public
                        | System.Reflection.BindingFlags.Instance
                        | System.Reflection.BindingFlags.IgnoreCase
                );
                if (field is null || !payload.TryGetProperty("value", out var value))
                    break;
                if (field.PropertyType == typeof(string) && text is not null)
                    field.SetValue(a, text);
                else if (
                    field.PropertyType == typeof(bool)
                    && value.ValueKind is JsonValueKind.True or JsonValueKind.False
                )
                    field.SetValue(a, value.GetBoolean());
                else if (
                    value.ValueKind == JsonValueKind.Number
                    && value.TryGetDouble(out var numeric)
                    && double.IsFinite(numeric)
                )
                {
                    numeric = Math.Clamp(
                        numeric,
                        descriptor.Min ?? double.MinValue,
                        descriptor.Max ?? double.MaxValue
                    );
                    if (field.PropertyType == typeof(int))
                        field.SetValue(a, (int)numeric);
                    else if (field.PropertyType == typeof(double))
                        field.SetValue(a, numeric);
                }
                break;
        }
        if (property is "backColor" or "foreColor" or "borderColor")
            a.Style = "Custom";
        if (control is Forma.Core.Controls.Timer timerComponent)
            timerComponent.Enabled = _previewMode && a.Enabled;
        if (
            control is not Forma.Core.Controls.Component
            && (
                control != _form
                || property
                    is "minimumWidth"
                        or "minimumHeight"
                        or "maximumWidth"
                        or "maximumHeight"
            )
        )
            ResizeControl(control, a.Width, a.Height);
        if (control == _form)
            Text = $"Forma Builder — {_form!.Title}";
    }

    private void ResizeControl(FControl control, int width, int height)
    {
        var a = _appearance[control.Id];
        if (control == _form)
        {
            var minWidth = Math.Max(
                240,
                _form
                    .Children.Where(c => c is not Forma.Core.Controls.Component)
                    .Select(c =>
                        _appearance[c.Id].Width
                        + _appearance[c.Id].MarginLeft
                        + _appearance[c.Id].MarginRight
                        + (_appearance[c.Id].Locked ? c.X ?? 0 : 0)
                    )
                    .DefaultIfEmpty(0)
                    .Max()
            );
            var minHeight = Math.Max(
                160,
                _form
                    .Children.Where(c => c is not Forma.Core.Controls.Component)
                    .Select(c =>
                        _appearance[c.Id].Height
                        + _appearance[c.Id].MarginTop
                        + _appearance[c.Id].MarginBottom
                        + (_appearance[c.Id].Locked ? c.Y ?? 0 : 0)
                    )
                    .DefaultIfEmpty(0)
                    .Max()
            );
            _form.Width = a.Width = ConstrainedSize(
                width,
                minWidth,
                1600,
                a.MinimumWidth,
                a.MaximumWidth
            );
            _form.Height = a.Height = ConstrainedSize(
                height,
                minHeight,
                1200,
                a.MinimumHeight,
                a.MaximumHeight
            );
            foreach (var child in _form.Children.Where(c => c is not Forma.Core.Controls.Component))
                Position(child, child.X ?? 0, child.Y ?? 0);
        }
        else
        {
            var parentBounds = AvailableBounds(control.Parent ?? _form!);
            a.Width = ConstrainedSize(
                width,
                24,
                Math.Max(24, parentBounds.Width - a.MarginLeft - a.MarginRight),
                a.MinimumWidth,
                a.MaximumWidth
            );
            a.Height = ConstrainedSize(
                height,
                20,
                Math.Max(20, parentBounds.Height - a.MarginTop - a.MarginBottom),
                a.MinimumHeight,
                a.MaximumHeight
            );
            Position(control, control.X ?? 0, control.Y ?? 0);
            foreach (var child in control.Children)
                ResizeControl(child, _appearance[child.Id].Width, _appearance[child.Id].Height);
        }
    }

    private static int ConstrainedSize(int value, int floor, int limit, int minimum, int maximum)
    {
        var low = Math.Clamp(minimum, floor, limit);
        var high = maximum == 0 ? limit : Math.Clamp(maximum, low, limit);
        return Math.Clamp(value, low, high);
    }

    private Task StateAsync(string status)
    {
        if (_bridge is null || _form is null || _selected is null)
            return Task.CompletedTask;
        UpdateProjectTitle();
        return _bridge.SendAsync(
            new
            {
                type = "designer",
                action = "state",
                id = _form.Id,
                title = _form.Title,
                selectedId = _selected.Id,
                canUndo = !_previewMode && _history.CanUndo, canRedo = !_previewMode && _history.CanRedo,
                status,
                propertySchema = InspectorCatalog
                    .ForKind(_selected.ControlType)
                    .Select(p => new
                    {
                        id = p.Id,
                        label = p.Label,
                        category = p.Category,
                        editor = p.Editor,
                        min = p.Min,
                        max = p.Max,
                        options = p.Options,
                        readOnly = p.ReadOnly,
                    })
                    .ToArray(),
                controls = Walk(_form)
                    .Select(c => new
                    {
                        id = c.Id,
                        name = c.Name ?? c.ControlType,
                        kind = c.ControlType,
                    minimum = (c as Forma.Core.Controls.NumericControl)?.Minimum ?? 0,
                    maximum = (c as Forma.Core.Controls.NumericControl)?.Maximum ?? 100,
                    increment = (c as Forma.Core.Controls.NumericControl)?.Increment ?? 1,
                    number = (c as Forma.Core.Controls.NumericControl)?.Value ?? 0,
                    dateValue = (c as Forma.Core.Controls.DateTimeInput)?.DateValue ?? "",
                    color = (c as Forma.Core.Controls.ColorPicker)?.Color ?? "#2878ff",
                        parentId = c.Parent?.Id,
                        layoutSlot = c.LayoutSlot,
                        component = c is Forma.Core.Controls.Component,
                        @checked = (c as Forma.Core.Controls.CheckBox)?.Checked ?? false,
                        document = c is Forma.Core.Controls.RichTextBox rich ? JsonSerializer.Serialize(rich.Document) : "[]",
                        url = (c as Forma.Core.Controls.LinkLabel)?.Url ?? "",
                        mask = (c as Forma.Core.Controls.MaskedTextBox)?.Mask ?? "",
                        maskCompleted = (c as Forma.Core.Controls.MaskedTextBox)?.MaskCompleted ?? false,
                        checkedIndices = c is Forma.Core.Controls.CheckedListBox checkedList ? string.Join(", ", checkedList.CheckedIndices) : "",
                        items = c is Forma.Core.Controls.CheckedListBox list ? string.Join("\n", list.Items) : c is Forma.Core.Controls.ChoiceControl choice
                            ? string.Join("\n", choice.Items)
                            : "",
                        selectedIndex = (c as Forma.Core.Controls.ChoiceControl)?.SelectedIndex
                            ?? -1,
                        source = (c as Forma.Core.Controls.Image)?.Source ?? "",
                        sizeMode = (c as Forma.Core.Controls.Image)?.SizeMode ?? "contain",
                        orientation = (c as Forma.Core.Controls.LayoutContainer)?.Orientation
                            ?? "horizontal",
                        gap = (c as Forma.Core.Controls.LayoutContainer)?.Gap ?? 8,
                        columns = (c as Forma.Core.Controls.LayoutContainer)?.Columns ?? 2,
                        tabs = c is Forma.Core.Controls.TabControl tabs
                            ? string.Join("\n", tabs.Tabs)
                            : "",
                        selectedTab = (c as Forma.Core.Controls.TabControl)?.SelectedTab ?? 0,
                        gridColumns = c is Forma.Core.Controls.DataGridView grid
                            ? string.Join("\n", grid.Columns)
                            : "",
                        gridRows = c is Forma.Core.Controls.DataGridView rows
                            ? JsonSerializer.Serialize(rows.Rows)
                            : "[]",
                        interval = (c as Forma.Core.Controls.Timer)?.Interval ?? 1000,
                        workerReportsProgress = (
                            c as Forma.Core.Controls.BackgroundWorker
                        )?.WorkerReportsProgress
                            ?? false,
                        workerSupportsCancellation = (
                            c as Forma.Core.Controls.BackgroundWorker
                        )?.WorkerSupportsCancellation
                            ?? false,
                        isBusy = (c as Forma.Core.Controls.BackgroundWorker)?.IsBusy ?? false,
                        text = c.Text ?? "",
                        x = c.X ?? 0,
                        y = c.Y ?? 0,
                        width = _appearance[c.Id].Width,
                        height = _appearance[c.Id].Height,
                        fontSize = _appearance[c.Id].FontSize,
                        foreColor = _appearance[c.Id].ForeColor,
                        backColor = _appearance[c.Id].BackColor,
                        enabled = _appearance[c.Id].Enabled,
                        visible = _appearance[c.Id].Visible,
                        fontFamily = _appearance[c.Id].FontFamily,
                        fontWeight = _appearance[c.Id].FontWeight,
                        fontStyle = _appearance[c.Id].FontStyle,
                        textAlign = _appearance[c.Id].TextAlign,
                        borderStyle = _appearance[c.Id].BorderStyle,
                        borderColor = _appearance[c.Id].BorderColor,
                        borderWidth = _appearance[c.Id].BorderWidth,
                        borderRadius = _appearance[c.Id].BorderRadius,
                        padding = _appearance[c.Id].Padding,
                        opacity = _appearance[c.Id].Opacity,
                        placeholder = _appearance[c.Id].Placeholder,
                        readOnly = c is Forma.Core.Controls.RichTextBox richInput ? richInput.ReadOnly : c is Forma.Core.Controls.DataGridView dgv
                            ? dgv.ReadOnly
                            : _appearance[c.Id].ReadOnly,
                        password = _appearance[c.Id].Password,
                        maxLength = _appearance[c.Id].MaxLength,
                        tag = _appearance[c.Id].Tag,
                        locked = _appearance[c.Id].Locked,
                        style = _appearance[c.Id].Style,
                        shadow = _appearance[c.Id].Shadow,
                        cursor = _appearance[c.Id].Cursor,
                        lineHeight = _appearance[c.Id].LineHeight,
                        letterSpacing = _appearance[c.Id].LetterSpacing,
                        focusable = _appearance[c.Id].Focusable,
                        tabIndex = _appearance[c.Id].TabIndex,
                        toolTip = _appearance[c.Id].ToolTip,
                        cssClass = _appearance[c.Id].CssClass,
                        customCss = _appearance[c.Id].CustomCss,
                        zIndex = _appearance[c.Id].ZIndex,
                        marginTop = _appearance[c.Id].MarginTop,
                        marginRight = _appearance[c.Id].MarginRight,
                        marginBottom = _appearance[c.Id].MarginBottom,
                        marginLeft = _appearance[c.Id].MarginLeft,
                        paddingTop = _appearance[c.Id].PaddingTop,
                        paddingRight = _appearance[c.Id].PaddingRight,
                        paddingBottom = _appearance[c.Id].PaddingBottom,
                        paddingLeft = _appearance[c.Id].PaddingLeft,
                        minimumWidth = _appearance[c.Id].MinimumWidth,
                        minimumHeight = _appearance[c.Id].MinimumHeight,
                        maximumWidth = _appearance[c.Id].MaximumWidth,
                        maximumHeight = _appearance[c.Id].MaximumHeight,
                    })
                    .ToArray(),
            }
        );
    }

    private static string[] Lines(string text) =>
        text.Split('\n').Select(s => s.TrimEnd('\r')).Where(s => s.Length > 0).ToArray();

    private static IEnumerable<FControl> Walk(FControl root)
    {
        yield return root;
        foreach (var child in root.Children)
        foreach (var item in Walk(child))
            yield return item;
    }

    private void DisposeComponents()
    {
        if (_form is not null)
            foreach (var disposable in Walk(_form).OfType<IDisposable>())
                disposable.Dispose();
    }

    private static string? String(JsonElement json, string name) =>
        json.ValueKind == JsonValueKind.Object
        && json.TryGetProperty(name, out var v)
        && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    private static int? Number(JsonElement json, string name) =>
        json.ValueKind == JsonValueKind.Object
        && json.TryGetProperty(name, out var v)
        && v.ValueKind == JsonValueKind.Number
        && v.TryGetInt32(out var n)
            ? n
            : null;

    private static bool Boolean(JsonElement json, string name, bool fallback) =>
        json.ValueKind == JsonValueKind.Object
        && json.TryGetProperty(name, out var v)
        && v.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? v.GetBoolean()
            : fallback;

    private static bool IsColor(string? value) =>
        value is { Length: 7 } && value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit);

    private DesignSnapshot HistorySnapshot()
    {
        // Keep images in the undo entry when possible, so deleting an image can be undone.
        try { return new(ProjectFile.Serialize(CaptureProject(embedImages: true)), _selected?.Id); }
        catch (IOException) { return new(Snapshot(), _selected?.Id); }
    }
    private async Task RestoreHistoryAsync(DesignSnapshot snapshot, string status)
    {
        var restored = ProjectFile.Restore(ProjectFile.Parse(snapshot.Json));
        var appearances = restored.Appearance.ToDictionary(p => p.Key, p => p.Value.Deserialize<Appearance>()!);
        _ready = false;
        try {
            if (_form is not null) await _renderer!.RemoveAsync(_form);
            DisposeComponents(); _previewMode = false; _form = restored.Form;
            _appearance.Clear(); foreach (var pair in appearances) _appearance.Add(pair.Key, pair.Value);
            _selected = Walk(_form).FirstOrDefault(c => c.Id == snapshot.SelectedId) ?? _form;
            _count = Walk(_form).Count();
            await _renderer!.RenderAsync(_form);
            foreach (var control in Walk(_form)) SubscribeControl(control);
            await _bridge!.SendAsync(new { type = "designer", action = "initialize", id = _form.Id, title = _form.Title });
        } finally { _ready = true; }
        await StateAsync(status);
    }

    private ProjectDocument CaptureProject(bool embedImages = false) => ProjectFile.Capture(_form!, c => JsonSerializer.SerializeToElement(_appearance[c.Id]), embedImages);
    private string Snapshot() => _form is null ? "" : ProjectFile.Serialize(CaptureProject());
    private bool HasUnsavedChanges => _form is not null && Snapshot() != _savedSnapshot;
    private void UpdateProjectTitle() => Text = $"{(HasUnsavedChanges ? "* " : "")}{(_projectPath is null ? "Untitled" : Path.GetFileName(_projectPath))} - Forma Builder";
    private bool ConfirmDiscard()
    {
        if (!HasUnsavedChanges) return true;
        return MessageBox.Show(this, "Save changes to this design before continuing?", "Forma Builder", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) switch {
            DialogResult.Yes => SaveProject(), DialogResult.No => true, _ => false
        };
    }
    private bool SaveProject(bool saveAs = false)
    {
        if (_form is null) return false;
        var path = _projectPath;
        if (path is null || saveAs) {
            using var dialog = new SaveFileDialog { Title = "Save Forma design", Filter = "Forma design (*.forma)|*.forma|Forma design (*.frma)|*.frma", DefaultExt = "forma", AddExtension = true, OverwritePrompt = true, FileName = path is null ? "Untitled.forma" : Path.GetFileName(path), InitialDirectory = path is null ? "" : Path.GetDirectoryName(path) };
            if (dialog.ShowDialog(this) != DialogResult.OK) return false;
            path = dialog.FileName;
        }
        try {
            var document = CaptureProject(embedImages: true);
            ProjectFile.Write(path, document);
            // Keep the opened image available even if its original file is moved later.
            void UseEmbeddedImages(ProjectNode node) {
                if (Walk(_form).FirstOrDefault(c => c.Id == node.Id) is Forma.Core.Controls.Image image && node.Properties.TryGetValue("Source", out var source)) image.Source = source.GetString() ?? "";
                foreach (var child in node.Children) UseEmbeddedImages(child);
            }
            UseEmbeddedImages(document.Root);
            _projectPath = path; _savedSnapshot = Snapshot(); UpdateProjectTitle();
            _ = StateAsync($"Saved {Path.GetFileName(path)}"); return true;
        } catch (Exception error) {
            MessageBox.Show(this, error.Message, "Could not save design", MessageBoxButtons.OK, MessageBoxIcon.Error); return false;
        }
    }
    private async Task OpenProjectAsync()
    {
        using var dialog = new OpenFileDialog { Title = "Open Forma design", Filter = "Forma designs (*.forma;*.frma)|*.forma;*.frma|All files|*.*", CheckFileExists = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try {
            // Fully decode before replacing the active design; bad files leave it intact.
            var restored = ProjectFile.Restore(ProjectFile.Read(dialog.FileName));
            var appearances = restored.Appearance.ToDictionary(p => p.Key, p => p.Value.Deserialize<Appearance>() ?? throw new InvalidDataException("Invalid appearance."));
            if (!ConfirmDiscard()) return;
            _ready = false;
            if (_form is not null) await _renderer!.RemoveAsync(_form);
            DisposeComponents(); _previewMode = false;
            _form = restored.Form; _appearance.Clear();
            foreach (var pair in appearances) _appearance.Add(pair.Key, pair.Value);
            _selected = _form; _count = Walk(_form).Count(); _projectPath = dialog.FileName; _history.Clear();
            await _renderer!.RenderAsync(_form);
            foreach (var control in Walk(_form)) SubscribeControl(control);
            await _bridge!.SendAsync(new { type = "designer", action = "initialize", id = _form.Id, title = _form.Title });
            _savedSnapshot = Snapshot(); _ready = true;
            await StateAsync($"Opened {Path.GetFileName(_projectPath)}");
        } catch (Exception error) {
            _ready = true;
            MessageBox.Show(this, error.Message, "Could not open design", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_ready && !ConfirmDiscard()) e.Cancel = true;
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
        DisposeComponents();
        if (_bridge is not null)
            _bridge.MessageReceived -= OnDesignerMessage;
        _bridge?.Dispose();
        base.OnFormClosed(e);
    }
}
