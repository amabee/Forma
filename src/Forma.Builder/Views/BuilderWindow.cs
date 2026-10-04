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
    private WebView2Bridge? _bridge;
    private WebView2Renderer? _renderer;
    private bool _ready;
    private readonly CoalescedRefresh _stateRefresh;
    private bool _documentStateDirty;
    private PreviewWindow? _runtimePreview;
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
        if (_viewModel.Form is not null)
            await _renderer.RemoveAsync(_viewModel.Form);
        DisposeComponents();
        _viewModel.CreateNew();
        await _renderer.RenderAsync(_viewModel.Form);
        await _bridge.SendAsync(
            new
            {
                type = "designer",
                action = "initialize",
                id = _viewModel.Form.Id,
                title = _viewModel.Form.Title,
            }
        );
        await StateAsync("Ready — drag a control onto the form");
        _ready = true;
    }

    private void OnDesignerMessage(object? sender, BridgeMessage message)
    {
        if (IsDisposed || Disposing) return;
        // WebView2 forbids nested modal message loops in its callbacks. Post the
        // command to WinForms so dialogs open after the WebView2 callback returns.
        BeginInvoke(new Action(async () => {
            if (!IsDisposed && !Disposing) await HandleDesignerMessageAsync(message);
        }));
    }

    private async Task HandleDesignerMessageAsync(BridgeMessage message)
    {
        if (message.Type == "custom" && message.Event == "error" && _ready
            && message.Payload is JsonElement errorPayload && errorPayload.TryGetProperty("message", out var errorMessage))
        {
            await StateAsync($"Component style error: {errorMessage.GetString()}");
            return;
        }
        if (message.Type != "designer" || !_ready || _viewModel.Form is null || _bridge is null)
            return;
        try
        {
            var payload = message.Payload is JsonElement json && json.ValueKind == JsonValueKind.Object
                ? json : default;
            if (message.Event == "preview")
            {
                await _bridge.SendAsync(new { type = "designer", action = "return-to-design" });
                if (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("enabled", out var enabled) && enabled.ValueKind == JsonValueKind.True)
                {
                    _runtimePreview?.Close();
                    _runtimePreview = new PreviewWindow(new PreviewSession(_viewModel));
                    _runtimePreview.Show(this);
                }
                await StateAsync("Preview opened in its own window");
                return;
            }
            if (message.Event == "command")
            {
                switch (String(payload, "command"))
                {
                    case "undo":
                    case "redo":
                        var undo = String(payload, "command") == "undo";
                        var snapshot = undo ? _viewModel.Undo() : _viewModel.Redo();
                        if (snapshot is not null) await RestoreHistoryAsync(snapshot, undo ? "Undone" : "Redone");
                        return;
                    case "save": SaveProject(); return;
                    case "save-as": SaveProject(true); return;
                    case "open": await OpenProjectAsync(); return;
                    case "new": if (ConfirmDiscard()) await NewFormAsync(); return;
                    case "about": ShowAbout(); return;
                    case "edit-characteristics":
                    case "edit-custom-properties":
                        if (_viewModel.SelectedControl is FControl selected && !_viewModel.PreviewMode && !_viewModel.Appearance[selected.Id].Locked)
                        {
                            using var editor = new ComponentEditorWindow(
                                selected.Name ?? selected.ControlType,
                                ComponentEditorService.Folder(_viewModel, selected),
                                ComponentEditorService.Template(selected, _viewModel.Appearance[selected.Id]),
                                async source =>
                                {
                                    var result = _viewModel.ExecuteEdit("customize", selected.Id, JsonSerializer.SerializeToElement(source));
                                    await StateAsync(result.Status);
                                });
                            editor.ShowDialog(this);
                        }
                        return;
                    case "exit": Close(); return;
                    case "show-dialog":
                        if (_viewModel.ShowDialog(_viewModel.SelectedControl?.Id)) await StateAsync("Dialog opened");
                        return;
                    case "choose-path":
                        if (_viewModel.SelectedControl is Forma.Core.Controls.PathPicker pathPicker)
                            await ChoosePathAsync(pathPicker, design: true);
                        return;
                    case "choose-image":
                        if (_viewModel.SelectedControl is Forma.Core.Controls.Image image
                            && !_viewModel.PreviewMode && !_viewModel.Appearance[image.Id].Locked)
                        {
                            using var picker = new OpenFileDialog
                            {
                                Title = "Choose image",
                                Filter = "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp;*.svg;*.ico|All files|*.*",
                                CheckFileExists = true
                            };
                            if (picker.ShowDialog(this) == DialogResult.OK)
                            {
                                var result = _viewModel.ExecuteEdit("image-source", image.Id,
                                    JsonSerializer.SerializeToElement(new { value = new Uri(picker.FileName).AbsoluteUri }));
                                await StateAsync(result.Status);
                            }
                        }
                        return;
                }
            }
            var edit = _viewModel.ExecuteEdit(message.Event ?? "", message.Id, payload);
            if (edit.AddedControl is not null) SubscribeControl(edit.AddedControl);
            await StateAsync(edit.Status);
        }
        catch (Exception error)
        {
            if (!IsDisposed) await StateAsync($"Designer error: {error.Message}");
        }
    }

    private void SubscribeControl(FControl control)
    {
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

    private Task StateAsync(string status) => _stateRefresh.Request(status);

    private Task SendStateAsync(string status)
    {
        if (IsDisposed || _bridge is null || _viewModel.Form is null || _viewModel.SelectedControl is null)
            return Task.CompletedTask;
        if (_documentStateDirty) { UpdateProjectTitle(); _documentStateDirty = false; }
        return _bridge.SendAsync(
            new
            {
                type = "designer",
                action = "state",
                id = _viewModel.Form.Id,
                title = _viewModel.Form.Title,
                selectedId = _viewModel.SelectedControl.Id,
                canUndo = _viewModel.CanUndo, canRedo = _viewModel.CanRedo,
                status,
                propertySchema = InspectorCatalog
                    .ForKind(_viewModel.SelectedControl.ControlType)
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
                controls = Walk(_viewModel.Form)
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
                        component = c is Forma.Core.Controls.INonvisualControl,
                        @checked = (c as Forma.Core.Controls.CheckBox)?.Checked ?? false,
                        nodes = c is Forma.Core.Controls.TreeView tree ? JsonSerializer.Serialize(tree.Nodes) : "[]",
                        selectedNode = (c as Forma.Core.Controls.TreeView)?.SelectedNode ?? "",
                        expandedNodes = c is Forma.Core.Controls.TreeView branches ? string.Join(", ", branches.ExpandedNodes) : "",
                        totalItems = (c as Forma.Core.Controls.Pagination)?.TotalItems ?? 0,
                        pageSize = (c as Forma.Core.Controls.Pagination)?.PageSize ?? 10,
                        page = (c as Forma.Core.Controls.Pagination)?.Page ?? 1,
                        selectedPath = (c as Forma.Core.Controls.PathPicker)?.SelectedPath ?? "",
                        dialogTitle = (c as Forma.Core.Controls.Dialog)?.DialogTitle ?? (c as Forma.Core.Controls.PathPicker)?.DialogTitle ?? "",
                        filter = (c as Forma.Core.Controls.FilePicker)?.Filter ?? "",
                        entries = c is Forma.Core.Controls.PropertyGrid propertyGrid ? JsonSerializer.Serialize(propertyGrid.Entries) : "[]",
                        commandItems = c is Forma.Core.Controls.CommandControl commands ? JsonSerializer.Serialize(commands.Items) : "[]",
                        rightText = (c as Forma.Core.Controls.StatusBar)?.RightText ?? "",
                        targetId = (c as Forma.Core.Controls.ContextMenu)?.TargetId ?? (c as Forma.Core.Controls.Tooltip)?.TargetId ?? (c as Forma.Core.Controls.LoadingOverlay)?.TargetId ?? "",
                        message = (c as Forma.Core.Controls.Dialog)?.Message ?? "",
                        buttons = (c as Forma.Core.Controls.Dialog)?.Buttons ?? "OK",
                        canCancel = (c as Forma.Core.Controls.Dialog)?.CanCancel ?? true,
                        result = (c as Forma.Core.Controls.Dialog)?.Result ?? "",
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
                        orientation = (c as Forma.Core.Controls.Divider)?.Orientation ?? (c as Forma.Core.Controls.Toolbar)?.Orientation ?? (c as Forma.Core.Controls.LayoutContainer)?.Orientation
                            ?? "horizontal",
                        gap = (c as Forma.Core.Controls.LayoutContainer)?.Gap ?? 8,
                        columns = (c as Forma.Core.Controls.LayoutContainer)?.Columns ?? 2,
                        rowCount = (c as Forma.Core.Controls.LayoutContainer)?.RowCount ?? 2,
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
                        width = _viewModel.Appearance[c.Id].Width,
                        height = _viewModel.Appearance[c.Id].Height,
                        fontSize = _viewModel.Appearance[c.Id].FontSize,
                        foreColor = _viewModel.Appearance[c.Id].ForeColor,
                        backColor = _viewModel.Appearance[c.Id].BackColor,
                        enabled = _viewModel.Appearance[c.Id].Enabled,
                        visible = _viewModel.Appearance[c.Id].Visible,
                        fontFamily = _viewModel.Appearance[c.Id].FontFamily,
                        fontWeight = _viewModel.Appearance[c.Id].FontWeight,
                        fontStyle = _viewModel.Appearance[c.Id].FontStyle,
                        textAlign = _viewModel.Appearance[c.Id].TextAlign,
                        borderStyle = _viewModel.Appearance[c.Id].BorderStyle,
                        borderColor = _viewModel.Appearance[c.Id].BorderColor,
                        borderWidth = _viewModel.Appearance[c.Id].BorderWidth,
                        borderRadius = _viewModel.Appearance[c.Id].BorderRadius,
                        padding = _viewModel.Appearance[c.Id].Padding,
                        opacity = _viewModel.Appearance[c.Id].Opacity,
                        placeholder = _viewModel.Appearance[c.Id].Placeholder,
                        readOnly = c is Forma.Core.Controls.PropertyGrid propertyInput ? propertyInput.ReadOnly : c is Forma.Core.Controls.RichTextBox richInput ? richInput.ReadOnly : c is Forma.Core.Controls.DataGridView dgv
                            ? dgv.ReadOnly
                            : _viewModel.Appearance[c.Id].ReadOnly,
                        password = _viewModel.Appearance[c.Id].Password,
                        maxLength = _viewModel.Appearance[c.Id].MaxLength,
                        tag = _viewModel.Appearance[c.Id].Tag,
                        locked = _viewModel.Appearance[c.Id].Locked,
                        style = _viewModel.Appearance[c.Id].Style,
                        shadow = _viewModel.Appearance[c.Id].Shadow,
                        cursor = _viewModel.Appearance[c.Id].Cursor,
                        lineHeight = _viewModel.Appearance[c.Id].LineHeight,
                        letterSpacing = _viewModel.Appearance[c.Id].LetterSpacing,
                        focusable = _viewModel.Appearance[c.Id].Focusable,
                        tabIndex = _viewModel.Appearance[c.Id].TabIndex,
                        toolTip = _viewModel.Appearance[c.Id].ToolTip,
                        cssClass = _viewModel.Appearance[c.Id].CssClass,
                        customCss = _viewModel.Appearance[c.Id].CustomCss,
                        description = (c as Forma.Core.Controls.Card)?.Description ?? (c as Forma.Core.Controls.EmptyState)?.Description ?? "",
                        iconName = (c as Forma.Core.Controls.Icon)?.IconName ?? (c as Forma.Core.Controls.EmptyState)?.IconName ?? "image",
                        strokeWidth = (c as Forma.Core.Controls.Icon)?.StrokeWidth ?? 2,
                        lines = (c as Forma.Core.Controls.Skeleton)?.Lines ?? 3,
                        headerVisible = (c as Forma.Core.Controls.Card)?.HeaderVisible ?? true,
                        variant = (c as Forma.Core.Controls.Badge)?.Variant ?? (c as Forma.Core.Controls.Toast)?.Variant ?? "info",
                        initials = (c as Forma.Core.Controls.Avatar)?.Initials ?? "",
                        shape = (c as Forma.Core.Controls.Avatar)?.Shape ?? (c as Forma.Core.Controls.Skeleton)?.Shape ?? "circle",
                        thickness = (c as Forma.Core.Controls.Divider)?.Thickness ?? 1,
                        lineStyle = (c as Forma.Core.Controls.Divider)?.LineStyle ?? "solid",
                        position = (c as Forma.Core.Controls.Toast)?.Position ?? "bottom-right",
                        duration = (c as Forma.Core.Controls.Toast)?.Duration ?? 4000,
                        dismissible = (c as Forma.Core.Controls.Toast)?.Dismissible ?? true,
                        isActive = (c as Forma.Core.Controls.Spinner)?.IsActive ?? (c as Forma.Core.Controls.LoadingOverlay)?.IsActive ?? (c as Forma.Core.Controls.Skeleton)?.IsActive ?? false,
                        speed = (c as Forma.Core.Controls.Spinner)?.Speed ?? 800,
                        initialDelay = (c as Forma.Core.Controls.Tooltip)?.InitialDelay ?? 500,
                        showDuration = (c as Forma.Core.Controls.Tooltip)?.ShowDuration ?? 5000,
                        placement = (c as Forma.Core.Controls.Tooltip)?.Placement ?? "top",
                        sortingEnabled = (c as Forma.Core.Controls.DataGridView)?.SortingEnabled ?? false,
                        filteringEnabled = (c as Forma.Core.Controls.DataGridView)?.FilteringEnabled ?? false,
                        filterText = (c as Forma.Core.Controls.DataGridView)?.FilterText ?? "",
                        sortColumn = (c as Forma.Core.Controls.DataGridView)?.SortColumn ?? -1,
                        sortDirection = (c as Forma.Core.Controls.DataGridView)?.SortDirection ?? "ascending",
                        selectedRow = (c as Forma.Core.Controls.DataGridView)?.SelectedRow ?? -1,
                        customization = _viewModel.Appearance[c.Id].Customization,
                        zIndex = _viewModel.Appearance[c.Id].ZIndex,
                        marginTop = _viewModel.Appearance[c.Id].MarginTop,
                        marginRight = _viewModel.Appearance[c.Id].MarginRight,
                        marginBottom = _viewModel.Appearance[c.Id].MarginBottom,
                        marginLeft = _viewModel.Appearance[c.Id].MarginLeft,
                        paddingTop = _viewModel.Appearance[c.Id].PaddingTop,
                        paddingRight = _viewModel.Appearance[c.Id].PaddingRight,
                        paddingBottom = _viewModel.Appearance[c.Id].PaddingBottom,
                        paddingLeft = _viewModel.Appearance[c.Id].PaddingLeft,
                        minimumWidth = _viewModel.Appearance[c.Id].MinimumWidth,
                        minimumHeight = _viewModel.Appearance[c.Id].MinimumHeight,
                        maximumWidth = _viewModel.Appearance[c.Id].MaximumWidth,
                        maximumHeight = _viewModel.Appearance[c.Id].MaximumHeight,
                    })
                    .ToArray(),
            }
        );
    }

    private async Task ChoosePathAsync(Forma.Core.Controls.PathPicker picker, bool design)
    {
        if (!_ready || IsDisposed || _viewModel.Form is null || !Walk(_viewModel.Form).Contains(picker)
            || !_viewModel.Appearance.TryGetValue(picker.Id, out var appearance)
            || (design ? _viewModel.PreviewMode || appearance.Locked : !_viewModel.PreviewMode || !appearance.Enabled)) return;
        try
        {
            string? selected = null;
            if (picker is Forma.Core.Controls.FilePicker file)
            {
                using var dialog = new OpenFileDialog { Title = picker.DialogTitle, Filter = file.Filter, CheckFileExists = true };
                if (File.Exists(picker.SelectedPath)) dialog.FileName = picker.SelectedPath;
                if (dialog.ShowDialog(this) == DialogResult.OK) selected = dialog.FileName;
            }
            else
            {
                using var dialog = new FolderBrowserDialog { Description = picker.DialogTitle, UseDescriptionForTitle = true };
                if (Directory.Exists(picker.SelectedPath)) dialog.SelectedPath = picker.SelectedPath;
                if (dialog.ShowDialog(this) == DialogResult.OK) selected = dialog.SelectedPath;
            }
            if (selected is null) return;
            if (design) _viewModel.ExecuteEdit("path-source", picker.Id,
                JsonSerializer.SerializeToElement(new { property = "selectedPath", value = selected }));
            else picker.SelectedPath = selected;
            await StateAsync("Path selected");
        }
        catch (Exception error)
        {
            if (!IsDisposed) await StateAsync($"Could not choose path: {error.Message}");
        }
    }

    private static IEnumerable<FControl> Walk(FControl root)
    {
        yield return root;
        foreach (var child in root.Children)
        foreach (var item in Walk(child))
            yield return item;
    }

    private void DisposeComponents()
    {
        if (_viewModel.Form is not null)
            foreach (var disposable in Walk(_viewModel.Form).OfType<IDisposable>())
                disposable.Dispose();
    }

    private static string? String(JsonElement json, string name) =>
        json.ValueKind == JsonValueKind.Object
        && json.TryGetProperty(name, out var v)
        && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    private async Task RestoreHistoryAsync(DesignSnapshot snapshot, string status)
    {
        var restored = ProjectFile.Restore(ProjectFile.Parse(snapshot.Json));
        var appearances = restored.Appearance.ToDictionary(p => p.Key, p => p.Value.Deserialize<Appearance>()!);
        _ready = false;
        try {
            if (_viewModel.Form is not null) await _renderer!.RemoveAsync(_viewModel.Form);
            DisposeComponents();
            _viewModel.ApplyDocument(restored.Form, appearances, snapshot.SelectedId);
            await _renderer!.RenderAsync(_viewModel.Form);
            foreach (var control in Walk(_viewModel.Form)) SubscribeControl(control);
            await _bridge!.SendAsync(new { type = "designer", action = "initialize", id = _viewModel.Form.Id, title = _viewModel.Form.Title });
        } finally { _ready = true; }
        await StateAsync(status);
    }

    private ProjectDocument CaptureProject(bool embedImages = false) => _viewModel.CaptureProject(embedImages);
    private bool HasUnsavedChanges => _viewModel.HasUnsavedChanges;
    private void UpdateProjectTitle() => _viewModel.RefreshDocumentState();
    private bool ConfirmDiscard()
    {
        if (!HasUnsavedChanges) return true;
        return MessageBox.Show(this, "Save changes to this design before continuing?", "Forma Builder", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) switch {
            DialogResult.Yes => SaveProject(), DialogResult.No => true, _ => false
        };
    }
    private bool SaveProject(bool saveAs = false)
    {
        if (_viewModel.Form is null) return false;
        var path = _viewModel.ProjectPath;
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
                if (Walk(_viewModel.Form).FirstOrDefault(c => c.Id == node.Id) is Forma.Core.Controls.Image image && node.Properties.TryGetValue("Source", out var source)) image.Source = source.GetString() ?? "";
                foreach (var child in node.Children) UseEmbeddedImages(child);
            }
            UseEmbeddedImages(document.Root);
            _viewModel.ProjectPath = path; _viewModel.MarkSaved(); UpdateProjectTitle();
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
            if (_viewModel.Form is not null) await _renderer!.RemoveAsync(_viewModel.Form);
            DisposeComponents();
            _viewModel.ApplyDocument(restored.Form, appearances);
            _viewModel.ProjectPath = dialog.FileName;
            _viewModel.ClearHistory();
            await _renderer!.RenderAsync(_viewModel.Form);
            foreach (var control in Walk(_viewModel.Form)) SubscribeControl(control);
            await _bridge!.SendAsync(new { type = "designer", action = "initialize", id = _viewModel.Form.Id, title = _viewModel.Form.Title });
            _viewModel.MarkSaved(); _ready = true;
            await StateAsync($"Opened {Path.GetFileName(_viewModel.ProjectPath)}");
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
        _runtimePreview?.Close();
        DisposeComponents();
        if (_bridge is not null)
            _bridge.MessageReceived -= OnDesignerMessage;
        _bridge?.Dispose();
        base.OnFormClosed(e);
    }
}
