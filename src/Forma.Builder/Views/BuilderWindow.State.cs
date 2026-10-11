using System.Text.Json;
using FControl = Forma.Core.Controls.Control;

namespace Forma.Builder;

// Publishing the design, selection and inspector state to the designer page.
public sealed partial class BuilderWindow
{
    private Task StateAsync(string status) => _stateRefresh.Request(status);

    private Task SendStateAsync(string status)
    {
        if (IsDisposed || _bridge is null || _viewModel.Form is null || _viewModel.SelectedControl is null)
            return Task.CompletedTask;
        if (_documentStateDirty) { UpdateProjectTitle(); _documentStateDirty = false; }
        var ids = _viewModel.ProjectControls.Select(c => c.Id).ToHashSet();
        foreach (var (key, editor) in _editors.ToArray()) {
            if (key.StartsWith("file:", StringComparison.Ordinal)) {
                var entry = _viewModel.Files.FirstOrDefault(file => file.Id == key[5..] && !file.IsFolder);
                if (entry is null) editor.Close(); else editor.RefreshProjectSource(entry.Name, entry.Content);
                continue;
            }
            if (key == "global:" + _viewModel.ProjectRoot.Id || ids.Contains(key)) { _orphanedEditors.Remove(key); editor.RefreshCompletionContext(); }
            // Undo, redo or a canvas delete removed this component. Keep unsaved code open so it
            // isn't lost silently; redo restores the component, or the user can copy the code.
            else if (!editor.HasUnsavedChanges) editor.Close();
            else if (_orphanedEditors.Add(key)) status = $"{editor.DocumentName}: its component was removed; unsaved code kept open";
        }
        return _bridge.SendAsync(
            new
            {
                type = "designer",
                action = "state",
                id = _viewModel.Form.Id,
                globalScript = _viewModel.GlobalScript,
                title = _viewModel.Form.Title,
                explorer = ProjectExplorerService.Build(_viewModel),
                forms = _viewModel.Forms.Select(form => new { id = form.Id, name = form.Name, title = form.Title }).ToArray(),
                selectedId = _viewModel.SelectedControl.Id,
                selectedIds = _viewModel.SelectedIds,
                // Properties every selected control has, for editing several controls at once.
                sharedProperties = SharedProperties(),
                canPaste = _viewModel.Clipboard.Count > 0,
                canUndo = _viewModel.CanUndo, canRedo = _viewModel.CanRedo,
                hasUnsavedChanges = _viewModel.IsMarkedUnsaved,
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
                        checkedIndices = c is Forma.Core.Controls.MultiChoiceControl checkedList ? string.Join(", ", checkedList.CheckedIndices) : "",
                        stars = (c as Forma.Core.Controls.Rating)?.Stars ?? 5,
                        items = c is Forma.Core.Controls.MultiChoiceControl list ? string.Join("\n", list.Items) : c is Forma.Core.Controls.ChoiceControl choice
                            ? string.Join("\n", choice.Items)
                            : "",
                        selectedIndex = (c as Forma.Core.Controls.ChoiceControl)?.SelectedIndex
                            ?? -1,
                        source = (c as Forma.Core.Controls.Image)?.Source ?? "",
                        sizeMode = (c as Forma.Core.Controls.Image)?.SizeMode ?? "contain",
                        orientation = (c as Forma.Core.Controls.SelectionGroup)?.Orientation ?? (c as Forma.Core.Controls.MultiSelectionGroup)?.Orientation ?? (c as Forma.Core.Controls.Divider)?.Orientation ?? (c as Forma.Core.Controls.Toolbar)?.Orientation ?? (c as Forma.Core.Controls.LayoutContainer)?.Orientation
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
                        readOnly = c is Forma.Core.Controls.Rating rated ? rated.ReadOnly : c is Forma.Core.Controls.PropertyGrid propertyInput ? propertyInput.ReadOnly : c is Forma.Core.Controls.RichTextBox richInput ? richInput.ReadOnly : c is Forma.Core.Controls.DataGridView dgv
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
                        expanded = (c as Forma.Core.Controls.Accordion)?.Expanded ?? true,
                        breakpoint = (c as Forma.Core.Controls.ResponsiveLayout)?.Breakpoint ?? (c as Forma.Core.Controls.BreakpointContainer)?.Breakpoint ?? 600,
                        narrowColumns = (c as Forma.Core.Controls.ResponsiveGrid)?.NarrowColumns ?? 1,
                        narrowOrientation = (c as Forma.Core.Controls.ResponsiveStack)?.NarrowOrientation ?? "vertical",
                        aspectRatio = (c as Forma.Core.Controls.AspectRatioContainer)?.AspectRatio ?? 16d / 9d,
                        scrollDirection = (c as Forma.Core.Controls.ScrollablePanel)?.ScrollDirection ?? "both",
                        primaryEnabled = (c as Forma.Core.Controls.SplitButton)?.PrimaryEnabled ?? true,
                        description = (c as Forma.Core.Controls.CommandButton)?.Description ?? (c as Forma.Core.Controls.Card)?.Description ?? (c as Forma.Core.Controls.EmptyState)?.Description ?? "",
                        showText = (c as Forma.Core.Controls.IconButton)?.ShowText ?? false,
                        removable = (c as Forma.Core.Controls.Chip)?.Removable ?? false,
                        isRemoved = (c as Forma.Core.Controls.Chip)?.IsRemoved ?? false,
                        iconName = (c as Forma.Core.Controls.IconButton)?.IconName ?? (c as Forma.Core.Controls.Icon)?.IconName ?? (c as Forma.Core.Controls.EmptyState)?.IconName ?? "image",
                        strokeWidth = (c as Forma.Core.Controls.Icon)?.StrokeWidth ?? 2,
                        lines = (c as Forma.Core.Controls.Skeleton)?.Lines ?? 3,
                        headerVisible = (c as Forma.Core.Controls.Card)?.HeaderVisible ?? true,
                        variant = (c as Forma.Core.Controls.Chip)?.Variant ?? (c as Forma.Core.Controls.Badge)?.Variant ?? (c as Forma.Core.Controls.Toast)?.Variant ?? "info",
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
                        dock = _viewModel.Appearance[c.Id].Dock,
                        anchor = _viewModel.Appearance[c.Id].Anchor,
                        minimumHeight = _viewModel.Appearance[c.Id].MinimumHeight,
                        maximumWidth = _viewModel.Appearance[c.Id].MaximumWidth,
                        maximumHeight = _viewModel.Appearance[c.Id].MaximumHeight,
                    })
                    .ToArray(),
            }
        );
    }

    private string[] SharedProperties()
    {
        var kinds = _viewModel.ProjectControls.Where(control => _viewModel.SelectedIds.Contains(control.Id))
            .Select(control => control.ControlType).Distinct().ToList();
        if (kinds.Count == 0) return [];
        return kinds.Select(kind => InspectorCatalog.ForKind(kind).Select(property => property.Id))
            .Aggregate((shared, next) => shared.Intersect(next))
            .Where(id => id is not ("name" or "id")).ToArray();
    }
}
