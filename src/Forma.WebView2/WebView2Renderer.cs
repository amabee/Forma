using System.ComponentModel;
using System.Text.Json;
using Forma.Core.Controls;
using Forma.Core.Rendering;

namespace Forma.WebView2;

/// <summary>
/// Projects a Forma control tree onto the web runtime and keeps it in sync as
/// the tree changes.
/// </summary>
public sealed class WebView2Renderer : IRenderer
{
    private readonly IBridge _bridge;

    /// <summary>
    /// Controls currently projected into the web runtime, keyed by
    /// <see cref="Control.Id"/>. Holds the handler delegates too, so every
    /// subscription made at render time can be undone at removal time.
    /// </summary>
    private readonly Dictionary<string, Registration> _controls = [];

    public WebView2Renderer(IBridge bridge)
    {
        _bridge = bridge;

        _bridge.MessageReceived += OnMessageReceived;
    }

    private sealed class Registration
    {
        public required Control Control { get; init; }
        public required PropertyChangedEventHandler PropertyChanged { get; init; }
        public required EventHandler<ControlEventArgs> ChildAdded { get; init; }
        public required EventHandler<ControlEventArgs> ChildRemoved { get; init; }
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Renders <paramref name="control"/> and its subtree, parents before
    /// children so the runtime can attach each element to an existing node.
    /// </summary>
    public async Task RenderAsync(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);

        // Rendering an already-rendered control must not subscribe a second
        // time, or every later change would be sent once per render.
        if (_controls.ContainsKey(control.Id))
        {
            return;
        }

        Register(control);

        await _bridge.SendAsync(
            new
            {
                type = "create",
                id = control.Id,
                control = control.ControlType,
                parentId = control.Parent?.Id,
                index = control.Parent?.Children.ToList().IndexOf(control) ?? 0,
                properties = Properties(control),
            }
        );

        foreach (var child in control.Children)
        {
            await RenderAsync(child);
        }
    }

    public Task UpdateAsync(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);

        return _bridge.SendAsync(
            new
            {
                type = "update",
                id = control.Id,
                properties = Properties(control),
            }
        );
    }

    private static Dictionary<string, object?> Properties(Control control)
    {
        var properties = new Dictionary<string, object?> { ["text"] = control.Text, ["x"] = control.X, ["y"] = control.Y, ["layoutSlot"] = control.LayoutSlot };
        if (control is CheckBox check) properties["checked"] = check.Checked;
        if (control is NumericControl numeric) { properties["minimum"] = numeric.Minimum; properties["maximum"] = numeric.Maximum; properties["increment"] = numeric.Increment; properties["number"] = numeric.Value; }
        if (control is DateTimeInput date) properties["dateValue"] = date.DateValue;
        if (control is ColorPicker color) properties["color"] = color.Color;
        if (control is TextBox input) { properties["password"] = input.Password; properties["placeholder"] = input.Placeholder; }
        if (control is ChoiceControl choice) { properties["items"] = choice.Items; properties["selectedIndex"] = choice.SelectedIndex; }
        if (control is LinkLabel link) { properties["url"] = link.Url; properties["visited"] = link.Visited; }
        if (control is MaskedTextBox masked) properties["mask"] = masked.Mask;
        if (control is MultiChoiceControl checkedList) { properties["items"] = checkedList.Items; properties["checkedIndices"] = checkedList.CheckedIndices; }
        if (control is SelectionGroup group) properties["orientation"] = group.Orientation;
        if (control is MultiSelectionGroup checkGroup) properties["orientation"] = checkGroup.Orientation;
        if (control is IconButton iconButton) { properties["iconName"] = iconButton.IconName; properties["showText"] = iconButton.ShowText; }
        if (control is SplitButton splitButton) properties["primaryEnabled"] = splitButton.PrimaryEnabled;
        if (control is CommandButton commandButton) properties["description"] = commandButton.Description;
        if (control is Chip chip) { properties["variant"] = chip.Variant; properties["removable"] = chip.Removable; properties["isRemoved"] = chip.IsRemoved; }
        if (control is Rating rating) { properties["stars"] = rating.Stars; properties["readOnly"] = rating.ReadOnly; }
        if (control is TreeView tree) { properties["nodes"] = tree.Nodes; properties["selectedNode"] = tree.SelectedNode; properties["expandedNodes"] = tree.ExpandedNodes; }
        if (control is Pagination pages) { properties["page"] = pages.Page; properties["pageCount"] = pages.PageCount; }
        if (control is PathPicker picker) { properties["selectedPath"] = picker.SelectedPath; properties["dialogTitle"] = picker.DialogTitle; }
        if (control is PropertyGrid propertyGrid) { properties["entries"] = propertyGrid.Entries; properties["readOnly"] = propertyGrid.ReadOnly; }
        if (control is CommandControl commands) properties["commandItems"] = commands.Items;
        if (control is Toolbar toolbar) properties["orientation"] = toolbar.Orientation;
        if (control is StatusBar status) properties["rightText"] = status.RightText;
        if (control is ContextMenu context) properties["targetId"] = context.TargetId == "" ? control.Parent?.Id : context.TargetId;
        if (control is Tooltip tooltip) {
            properties["targetId"] = tooltip.TargetId == "" ? control.Parent?.Id : tooltip.TargetId;
            properties["initialDelay"] = tooltip.InitialDelay; properties["showDuration"] = tooltip.ShowDuration; properties["placement"] = tooltip.Placement;
        }
        if (control is Card card) { properties["description"] = card.Description; properties["headerVisible"] = card.HeaderVisible; }
        if (control is Forma.Core.Controls.Icon icon) { properties["iconName"] = icon.IconName; properties["strokeWidth"] = icon.StrokeWidth; }
        if (control is EmptyState empty) { properties["description"] = empty.Description; properties["iconName"] = empty.IconName; }
        if (control is Skeleton skeleton) { properties["shape"] = skeleton.Shape; properties["lines"] = skeleton.Lines; properties["isActive"] = skeleton.IsActive; }
        if (control is Badge badge) properties["variant"] = badge.Variant;
        if (control is Avatar avatar) { properties["initials"] = avatar.Initials; properties["shape"] = avatar.Shape; }
        if (control is Divider divider) { properties["orientation"] = divider.Orientation; properties["thickness"] = divider.Thickness; properties["lineStyle"] = divider.LineStyle; }
        if (control is Spinner spinner) { properties["isActive"] = spinner.IsActive; properties["speed"] = spinner.Speed; }
        if (control is LoadingOverlay overlay) { properties["isActive"] = overlay.IsActive; properties["targetId"] = overlay.TargetId == "" ? control.Parent?.Id : overlay.TargetId; }
        if (control is Toast toast) { properties["variant"] = toast.Variant; properties["position"] = toast.Position; properties["duration"] = toast.Duration; properties["dismissible"] = toast.Dismissible; properties["isOpen"] = toast.IsOpen; }
        if (control is Dialog dialog) { properties["dialogTitle"] = dialog.DialogTitle; properties["message"] = dialog.Message; properties["buttons"] = dialog.Buttons; properties["canCancel"] = dialog.CanCancel; properties["isOpen"] = dialog.IsOpen; }
        if (control is RichTextBox rich) {
            properties["document"] = rich.Document.Select(b => new { kind = b.Kind, runs = b.Runs!.Select(r => new { text = r.Text, bold = r.Bold, italic = r.Italic, underline = r.Underline }).ToArray() }).ToArray();
            properties["readOnly"] = rich.ReadOnly;
        }
        if (control is Forma.Core.Controls.Image image) { properties["source"] = image.Source; properties["sizeMode"] = image.SizeMode; }
        if (control is LayoutContainer layout)
        {
            properties["orientation"] = layout.Orientation; properties["gap"] = layout.Gap;
            properties["columns"] = layout.Columns; properties["rowCount"] = layout.RowCount; properties["tabs"] = layout.Tabs; properties["selectedTab"] = layout.SelectedTab;
        }
        if (control is DataGridView grid) {
            properties["columns"] = grid.Columns; properties["rows"] = grid.Rows; properties["readOnly"] = grid.ReadOnly;
            properties["sortingEnabled"] = grid.SortingEnabled; properties["filteringEnabled"] = grid.FilteringEnabled;
            properties["filterText"] = grid.FilterText; properties["sortColumn"] = grid.SortColumn; properties["sortDirection"] = grid.SortDirection;
            properties["selectedRow"] = grid.SelectedRow; properties["visibleRowIndices"] = grid.VisibleRowIndices;
        }
        return properties;
    }

    /// <summary>
    /// Removes <paramref name="control"/> and its subtree.
    /// </summary>
    /// <remarks>
    /// One message is sent for the root: removing its element detaches the
    /// descendants with it. The descendants still have to be unregistered here
    /// so their subscriptions do not outlive the elements.
    /// </remarks>
    public Task RemoveAsync(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);

        if (!_controls.ContainsKey(control.Id))
        {
            return Task.CompletedTask;
        }

        Unregister(control);

        return _bridge.SendAsync(new { type = "remove", id = control.Id });
    }

    private void Register(Control control)
    {
        var registration = new Registration
        {
            Control = control,
            PropertyChanged = (_, _) => Observe(UpdateAsync(control)),
            ChildAdded = (_, e) => Observe(RenderAsync(e.Control)),
            ChildRemoved = (_, e) => Observe(RemoveAsync(e.Control)),
        };

        control.PropertyChanged += registration.PropertyChanged;
        control.ChildAdded += registration.ChildAdded;
        control.ChildRemoved += registration.ChildRemoved;

        _controls[control.Id] = registration;
    }

    private void Unregister(Control control)
    {
        foreach (var child in control.Children)
        {
            Unregister(child);
        }

        if (!_controls.Remove(control.Id, out var registration))
        {
            return;
        }

        control.PropertyChanged -= registration.PropertyChanged;
        control.ChildAdded -= registration.ChildAdded;
        control.ChildRemoved -= registration.ChildRemoved;
    }

    /// <summary>
    /// Surfaces failures from sends started by an event handler, which has no
    /// caller left to await it.
    /// </summary>
    private static void Observe(Task task)
    {
        if (task.IsCompletedSuccessfully)
        {
            return;
        }

        task.ContinueWith(
            completed => Console.WriteLine(
                $"Forma renderer error: {completed.Exception?.GetBaseException().Message}"
            ),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default
        );
    }

    private void OnMessageReceived(object? sender, BridgeMessage message)
    {
        if (message.Type != "event")
            return;

        if (message.Id is null)
            return;

        if (!_controls.TryGetValue(message.Id, out var registration))
        {
            return;
        }

        if (registration.Control is Button button && message.Event == "click")
        {
            button.OnClick();
        }

        if (registration.Control is TextBox textBox && message.Event == "input"
            && message.Payload is JsonElement payload
            && payload.ValueKind == JsonValueKind.Object
            && payload.TryGetProperty("text", out var text)
            && text.ValueKind == JsonValueKind.String)
        {
            if (textBox is MaskedTextBox masked) masked.SetMaskedText(text.GetString() ?? "");
            else textBox.SetText(text.GetString());
        }
        if (registration.Control is LinkLabel link && message.Event == "link") link.OnLinkClicked();
        if (registration.Control is PathPicker picker && message.Event == "browse") picker.RequestBrowse();
        if (message.Payload is not JsonElement data || data.ValueKind != JsonValueKind.Object) return;
        if (registration.Control is Toast toastControl && message.Event == "toast-close"
            && (toastControl.Dismissible || data.TryGetProperty("reason", out var reason) && reason.ValueKind == JsonValueKind.String && reason.GetString() == "timeout")) toastControl.Close();
        if (registration.Control is DataGridView dataGrid)
        {
            if (message.Event == "grid-sort" && dataGrid.SortingEnabled && data.TryGetProperty("column", out var sortValue) && sortValue.ValueKind == JsonValueKind.Number && sortValue.TryGetInt32(out var sortColumn)
                && sortColumn >= 0 && sortColumn < dataGrid.Columns.Length)
            {
                dataGrid.SortDirection = dataGrid.SortColumn == sortColumn && dataGrid.SortDirection == "ascending" ? "descending" : "ascending";
                dataGrid.SortColumn = sortColumn;
            }
            if (message.Event == "grid-filter" && dataGrid.FilteringEnabled && data.TryGetProperty("text", out var filter) && filter.ValueKind == JsonValueKind.String)
                dataGrid.FilterText = filter.GetString()!;
            if (message.Event == "grid-select" && data.TryGetProperty("row", out var selectedValue) && selectedValue.ValueKind == JsonValueKind.Number && selectedValue.TryGetInt32(out var selectedRow)
                && dataGrid.VisibleRowIndices.Contains(selectedRow)) dataGrid.SelectedRow = selectedRow;
        }
        if (registration.Control is CommandControl commands && message.Event == "command-item"
            && data.TryGetProperty("itemId", out var itemId) && itemId.ValueKind == JsonValueKind.String)
            commands.InvokeItem(itemId.GetString()!);
        if (registration.Control is SplitButton split && message.Event == "primary-click") split.InvokePrimary();
        if (registration.Control is Dialog dialog && message.Event == "dialog-result"
            && data.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.String)
        {
            var selectedResult = result.GetString()!;
            if (selectedResult != "Cancel" || dialog.CanCancel || dialog.Buttons.Contains("Cancel")) dialog.Close(selectedResult);
        }
        if (registration.Control is TreeView tree) {
            if (message.Event == "tree-select" && data.TryGetProperty("node", out var node) && node.ValueKind == JsonValueKind.String) tree.SelectedNode = node.GetString()!;
            if (message.Event == "tree-expand" && data.TryGetProperty("node", out var branch) && branch.ValueKind == JsonValueKind.String && data.TryGetProperty("expanded", out var expanded) && expanded.ValueKind is JsonValueKind.True or JsonValueKind.False) tree.SetExpanded(branch.GetString()!, expanded.GetBoolean());
        }
        if (registration.Control is Pagination pages && message.Event == "page" && data.TryGetProperty("page", out var page) && page.ValueKind == JsonValueKind.Number && page.TryGetInt32(out var numberPage)) pages.Page = numberPage;
        if (registration.Control is PropertyGrid propertyGrid && message.Event == "property-value"
            && data.TryGetProperty("index", out var entryIndex) && entryIndex.ValueKind == JsonValueKind.Number && entryIndex.TryGetInt32(out var entry)
            && data.TryGetProperty("value", out var entryValue) && entryValue.ValueKind == JsonValueKind.String)
            propertyGrid.SetEntryValue(entry, entryValue.GetString()!);
        if (registration.Control is RichTextBox rich && !rich.ReadOnly && message.Event == "rich-input" && data.TryGetProperty("document", out var document)) {
            try { rich.Document = document.Deserialize<RichBlock[]>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? []; }
            catch (Exception error) when (error is JsonException or ArgumentException) { /* Reject malformed editor messages. */ }
        }
        if (registration.Control is Chip removedChip && message.Event == "chip-remove") removedChip.Remove();
        if (registration.Control is MultiChoiceControl checkedList && message.Event == "item-check"
            && data.TryGetProperty("index", out var itemIndex) && itemIndex.ValueKind == JsonValueKind.Number && itemIndex.TryGetInt32(out var checkedIndex)
            && data.TryGetProperty("checked", out var itemChecked) && itemChecked.ValueKind is JsonValueKind.True or JsonValueKind.False)
            checkedList.SetItemChecked(checkedIndex, itemChecked.GetBoolean());
        if (message.Event == "value" && data.TryGetProperty("value", out var inputValue))
        {
            if (registration.Control is NumericUpDown or Slider or Rating && inputValue.ValueKind == JsonValueKind.Number && inputValue.TryGetDouble(out var number)
                && (registration.Control is not Rating rated || !rated.ReadOnly))
                ((NumericControl)registration.Control).Value = number;
            if (registration.Control is DateTimeInput date && inputValue.ValueKind == JsonValueKind.String) date.DateValue = inputValue.GetString()!;
            if (registration.Control is ColorPicker color && inputValue.ValueKind == JsonValueKind.String) color.Color = inputValue.GetString()!;
        }
        if (registration.Control is CheckBox check && message.Event == "checked"
            && data.TryGetProperty("checked", out var checkedValue) && checkedValue.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            if (check is RadioButton && checkedValue.GetBoolean())
                foreach (var other in _controls.Values.Select(r => r.Control).OfType<RadioButton>())
                    if (other != check && other.Parent == check.Parent) other.Checked = false;
            check.Checked = checkedValue.GetBoolean();
        }
        if (registration.Control is ChoiceControl choice && message.Event == "selection"
            && data.TryGetProperty("selectedIndex", out var index) && index.ValueKind == JsonValueKind.Number && index.TryGetInt32(out var selected))
            choice.SelectedIndex = selected;
        if (registration.Control is TabControl tabs && message.Event == "tab"
            && data.TryGetProperty("selectedTab", out var tab) && tab.ValueKind == JsonValueKind.Number && tab.TryGetInt32(out var active))
            tabs.SelectedTab = active;
        if (registration.Control is DataGridView grid && message.Event == "cell"
            && data.TryGetProperty("row", out var row) && row.ValueKind == JsonValueKind.Number && row.TryGetInt32(out var r)
            && data.TryGetProperty("column", out var column) && column.ValueKind == JsonValueKind.Number && column.TryGetInt32(out var c)
            && data.TryGetProperty("value", out var value) && value.ValueKind == JsonValueKind.String)
            grid.SetCell(r, c, value.GetString()!);
    }
}
