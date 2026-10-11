using System.Text.Json;
using FControl = Forma.Core.Controls.Control;

namespace Forma.Builder;

// Applying one inspector property edit to a control or its appearance.
public sealed partial class DesignerEditingService
{
    private void ApplyProperty(FControl control, JsonElement payload)
    {
        var property = String(payload, "property");
        var a = _viewModel.Appearance[control.Id];
        var text = String(payload, "value");
        var number = Number(payload, "value");
        var descriptor = InspectorCatalog
            .ForKind(control.ControlType)
            .FirstOrDefault(p => p.Id == property);
        if (control is Forma.Core.Controls.AspectRatioContainer && property == "height") return;
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
        if (control is Forma.Core.Controls.CommandControl commands && property == "commandItems" && text is not null)
            commands.Items = JsonSerializer.Deserialize<Forma.Core.Controls.CommandItem[]>(text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        if (control is Forma.Core.Controls.ContextMenu menu && property == "targetId" && text is not null)
        {
            if (text != "" && !Walk(_viewModel.Form!).Any(item => item.Id == text && item is not Forma.Core.Controls.INonvisualControl))
                throw new ArgumentException("Choose a control in the current form.");
            menu.TargetId = text;
        }
        if (control is Forma.Core.Controls.Tooltip tooltip)
        {
            if (property == "targetId" && text is not null)
            {
                if (text != "" && !Walk(_viewModel.Form!).Any(item => item.Id == text && item is not Forma.Core.Controls.INonvisualControl))
                    throw new ArgumentException("Choose a control in the current form.");
                tooltip.TargetId = text;
            }
            if (property == "initialDelay" && number is int delay) tooltip.InitialDelay = delay;
            if (property == "showDuration" && number is int duration) tooltip.ShowDuration = duration;
            if (property == "placement" && text is not null) tooltip.Placement = text;
        }
        if (control is Forma.Core.Controls.Icon icon)
        {
            if (property == "iconName" && text is not null) icon.IconName = text;
            if (property == "strokeWidth" && number is int stroke) icon.StrokeWidth = stroke;
        }
        if (control is Forma.Core.Controls.EmptyState empty)
        {
            if (property == "description" && text is not null) empty.Description = text;
            if (property == "iconName" && text is not null) empty.IconName = text;
        }
        if (control is Forma.Core.Controls.Skeleton skeleton)
        {
            if (property == "shape" && text is not null) skeleton.Shape = text;
            if (property == "lines" && number is int lines) skeleton.Lines = lines;
            if (property == "isActive") skeleton.IsActive = Boolean(payload, "value", skeleton.IsActive);
        }
        if (control is Forma.Core.Controls.Card card)
        {
            if (property == "description" && text is not null) card.Description = text;
            if (property == "headerVisible") card.HeaderVisible = Boolean(payload, "value", card.HeaderVisible);
        }
        if (control is Forma.Core.Controls.Badge badge && property == "variant" && text is not null) badge.Variant = text;
        if (control is Forma.Core.Controls.Avatar avatar)
        {
            if (property == "initials" && text is not null) avatar.Initials = text;
            if (property == "shape" && text is not null) avatar.Shape = text;
        }
        if (control is Forma.Core.Controls.Divider divider)
        {
            if (property == "orientation" && text is not null) divider.Orientation = text;
            if (property == "thickness" && number is int thickness) divider.Thickness = thickness;
            if (property == "lineStyle" && text is not null) divider.LineStyle = text;
        }
        if (control is Forma.Core.Controls.Spinner spinner)
        {
            if (property == "isActive") spinner.IsActive = Boolean(payload, "value", spinner.IsActive);
            if (property == "speed" && number is int speed) spinner.Speed = speed;
        }
        if (control is Forma.Core.Controls.LoadingOverlay overlay)
        {
            if (property == "isActive") overlay.IsActive = Boolean(payload, "value", overlay.IsActive);
            if (property == "targetId" && text is not null)
            {
                if (text != "" && !Walk(_viewModel.Form!).Any(item => item.Id == text && item != control && item is not Forma.Core.Controls.INonvisualControl))
                    throw new ArgumentException("Choose another visual control as the overlay target.");
                overlay.TargetId = text;
            }
        }
        if (control is Forma.Core.Controls.Toast toast)
        {
            if (property == "variant" && text is not null) toast.Variant = text;
            if (property == "position" && text is not null) toast.Position = text;
            if (property == "duration" && number is int duration) toast.Duration = duration;
            if (property == "dismissible") toast.Dismissible = Boolean(payload, "value", toast.Dismissible);
        }
        if (control is Forma.Core.Controls.DataGridView dataGrid)
        {
            if (property == "sortingEnabled") dataGrid.SortingEnabled = Boolean(payload, "value", dataGrid.SortingEnabled);
            if (property == "filteringEnabled") dataGrid.FilteringEnabled = Boolean(payload, "value", dataGrid.FilteringEnabled);
            if (property == "filterText" && text is not null) dataGrid.FilterText = text;
            if (property == "sortColumn" && number is int column) dataGrid.SortColumn = column;
            if (property == "sortDirection" && text is not null) dataGrid.SortDirection = text;
            if (property == "selectedRow" && number is int row) dataGrid.SelectedRow = row;
        }
        if (control is Forma.Core.Controls.Dialog dialog)
        {
            if (property == "dialogTitle" && text is not null) dialog.DialogTitle = text;
            if (property == "message" && text is not null) dialog.Message = text;
            if (property == "buttons" && text is not null) dialog.Buttons = text;
            if (property == "canCancel") dialog.CanCancel = Boolean(payload, "value", dialog.CanCancel);
        }
        if (control is Forma.Core.Controls.Toolbar toolbar && property == "orientation" && text is not null) toolbar.Orientation = text;
        if (control is Forma.Core.Controls.StatusBar status && property == "rightText" && text is not null) status.RightText = text;
        if (control is Forma.Core.Controls.PathPicker picker && text is not null) {
            if (property == "selectedPath") picker.SelectedPath = text;
            if (property == "dialogTitle") picker.DialogTitle = text;
            if (property == "filter" && picker is Forma.Core.Controls.FilePicker file) file.Filter = text;
        }
        if (control is Forma.Core.Controls.PropertyGrid propertyGrid) {
            if (property == "entries" && text is not null) propertyGrid.Entries = JsonSerializer.Deserialize<Forma.Core.Controls.PropertyEntry[]>(text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            if (property == "readOnly") propertyGrid.ReadOnly = Boolean(payload, "value", propertyGrid.ReadOnly);
        }
        if (control is Forma.Core.Controls.LinkLabel link && property == "url" && text is not null) link.Url = text;
        if (control is Forma.Core.Controls.MaskedTextBox masked && property == "mask" && text is not null) masked.Mask = text;
        if (control is Forma.Core.Controls.MultiChoiceControl checkedList) {
            if (property == "items" && text is not null) checkedList.Items = Lines(text);
            if (property == "checkedIndices" && text is not null) checkedList.CheckedIndices = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(int.Parse).ToArray();
        }
        if (control is Forma.Core.Controls.SelectionGroup group && property == "orientation" && text is not null) group.Orientation = text;
        if (control is Forma.Core.Controls.MultiSelectionGroup checkGroup && property == "orientation" && text is not null) checkGroup.Orientation = text;
        if (control is Forma.Core.Controls.IconButton iconButton) {
            if (property == "iconName" && text is not null) iconButton.IconName = text;
            if (property == "showText") iconButton.ShowText = Boolean(payload, "value", iconButton.ShowText);
        }
        if (control is Forma.Core.Controls.SplitButton split && property == "primaryEnabled") split.PrimaryEnabled = Boolean(payload, "value", split.PrimaryEnabled);
        if (control is Forma.Core.Controls.CommandButton commandButton && property == "description" && text is not null) commandButton.Description = text;
        if (control is Forma.Core.Controls.Chip chip) {
            if (property == "variant" && text is not null) chip.Variant = text;
            if (property == "removable") chip.Removable = Boolean(payload, "value", chip.Removable);
        }
        if (control is Forma.Core.Controls.Rating rating) {
            if (property == "stars" && number is int stars) rating.Stars = stars;
            if (property == "readOnly") rating.ReadOnly = Boolean(payload, "value", rating.ReadOnly);
        }
        if (control is Forma.Core.Controls.Accordion accordion && property == "expanded" && payload.TryGetProperty("value", out var expanded) && expanded.ValueKind is JsonValueKind.True or JsonValueKind.False) accordion.Expanded = expanded.GetBoolean();
        if (control is Forma.Core.Controls.BreakpointContainer breakpointControl && property == "breakpoint" && number is int switchWidth) breakpointControl.Breakpoint = switchWidth;
        if (control is Forma.Core.Controls.ResponsiveGrid responsiveGrid && property == "columns" && number is int gridColumns) responsiveGrid.Columns = gridColumns;
        if (control is Forma.Core.Controls.ResponsiveGrid narrowGrid && property == "narrowColumns" && number is int narrowColumns) narrowGrid.NarrowColumns = narrowColumns;
        if (control is Forma.Core.Controls.ResponsiveStack narrowStack && property == "narrowOrientation" && text is not null) narrowStack.NarrowOrientation = text;
        if (control is Forma.Core.Controls.AspectRatioContainer aspect && property == "aspectRatio" && payload.TryGetProperty("value", out var ratioValue) && ratioValue.ValueKind == JsonValueKind.Number && ratioValue.TryGetDouble(out var ratio) && double.IsFinite(ratio)) { aspect.AspectRatio = ratio; _layout.ResizeControl(control, a.Width, a.Height); }
        if (control is Forma.Core.Controls.ResponsiveLayout responsive && property == "breakpoint" && number is int breakpoint) responsive.Breakpoint = breakpoint;
        if (control is Forma.Core.Controls.ScrollablePanel scroll && property == "scrollDirection" && text is not null) scroll.ScrollDirection = text;
        if (control is Forma.Core.Controls.RichTextBox rich) {
            if (property == "text" && text is not null) rich.SetPlainText(text);
            if (property == "document" && text is not null) rich.Document = JsonSerializer.Deserialize<Forma.Core.Controls.RichBlock[]>(text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            if (property == "readOnly") rich.ReadOnly = Boolean(payload, "value", rich.ReadOnly);
        }
        if (control is Forma.Core.Controls.TreeView tree) {
            if (property == "nodes" && text is not null) tree.Nodes = JsonSerializer.Deserialize<Forma.Core.Controls.TreeNode[]>(text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            if (property == "selectedNode" && text is not null) tree.SelectedNode = text;
            if (property == "expandedNodes" && text is not null) tree.ExpandedNodes = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        }
        if (control is Forma.Core.Controls.Pagination pages && number is int pageNumber) {
            if (property == "totalItems") pages.TotalItems = pageNumber;
            if (property == "pageSize") pages.PageSize = pageNumber;
            if (property == "page") pages.Page = pageNumber;
        }
        switch (property)
        {
            case "dock" when text is not null && Appearance.DockValues.Contains(text):
                a.Dock = text;
                break;
            case "anchor" when text is not null && Appearance.AnchorValues.Contains(text):
                a.Anchor = text;
                break;
            case "name" when !string.IsNullOrWhiteSpace(text):
                var name = text.Trim();
                if (
                    !(char.IsLetter(name[0]) || name[0] == '_')
                    || name.Any(c => !char.IsLetterOrDigit(c) && c != '_')
                    || Walk(_viewModel.Form!).Any(c => c != control && c.Name == name)
                    || control == _viewModel.Form && _viewModel.Forms.Any(form => form != control && form.Name == name)
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
            case "x" when control != _viewModel.Form && number is int x:
                _layout.Position(control, x, control.Y ?? 0);
                break;
            case "y" when control != _viewModel.Form && number is int y:
                _layout.Position(control, control.X ?? 0, y);
                break;
            case "width" when number is int w:
                _layout.ResizeControl(control, w, a.Height);
                break;
            case "height" when number is int h:
                _layout.ResizeControl(control, a.Width, h);
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
            case "borderSides" when text is not null && Appearance.BorderSideValues.Contains(text):
                a.BorderSides = text;
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
            case "rowCount" when control is Forma.Core.Controls.TableLayoutPanel table && number is int rows:
                table.RowCount = rows;
                break;
            case "tabs" when control is Forma.Core.Controls.TabControl tabs && text is not null:
                tabs.Tabs = Lines(text);
                break;
            case "selectedTab"
                when control is Forma.Core.Controls.TabControl tabs && number is int tab:
                tabs.SelectedTab = tab;
                break;
            case "layoutSlot" when number is int slot:
                control.LayoutSlot = control.Parent is Forma.Core.Controls.BreakpointContainer ? Math.Clamp(slot, 1, 2) : slot;
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
            timerComponent.Enabled = _viewModel.PreviewMode && a.Enabled;
        if (
            control is not Forma.Core.Controls.INonvisualControl
            && (
                control != _viewModel.Form
                || property
                    is "minimumWidth"
                        or "minimumHeight"
                        or "maximumWidth"
                        or "maximumHeight"
            )
        )
            _layout.ResizeControl(control, a.Width, a.Height);
    }
}
