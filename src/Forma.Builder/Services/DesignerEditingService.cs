using System.Text.Json;
using FControl = Forma.Core.Controls.Control;

namespace Forma.Builder;

/// <summary>Editing rules for the designer, independent of native and browser views.</summary>
public sealed class DesignerEditingService(BuilderViewModel viewModel)
{
    private readonly BuilderViewModel _viewModel = viewModel;

    public DesignerEditResult Execute(string action, string? id, JsonElement payload)
    {
        var form = _viewModel.Form;
        if (form is null) return new("Ready");
        var control = Walk(form).FirstOrDefault(item => item.Id == id);
        if (action == "select" && control is not null)
        {
            _viewModel.SelectedControl = control;
            return new("Ready");
        }
        if (action == "preview")
        {
            _viewModel.PreviewMode = Boolean(payload, "enabled", false);
            foreach (var timer in Walk(form).OfType<Forma.Core.Controls.Timer>())
                timer.Enabled = _viewModel.PreviewMode && _viewModel.Appearance[timer.Id].Enabled;
            if (!_viewModel.PreviewMode) foreach (var dialog in Walk(form).OfType<Forma.Core.Controls.Dialog>()) dialog.Close("Cancel");
            return new("Ready");
        }
        // Editing guards belong here as well as in the browser UI.
        if (_viewModel.PreviewMode) return new("Ready");
        if (action == "drop" && control is not null
            && (control == form || control is Forma.Core.Controls.LayoutContainer or Forma.Core.Controls.Panel)
            && Number(payload, "x") is int x && Number(payload, "y") is int y)
        {
            var added = AddControl(String(payload, "control"), x, y, control);
            return new(added is null ? "Ready" : $"Added {added.Name}", added);
        }
        if (action == "command")
        {
            var selected = _viewModel.SelectedControl;
            if (selected is null || selected == form || !_viewModel.Appearance.TryGetValue(selected.Id, out var appearance)
                || appearance.Locked || !Walk(form).Contains(selected)) return new("Ready");
            switch (String(payload, "command"))
            {
                case "delete":
                    var removed = Walk(selected).ToArray();
                    selected.Parent?.Remove(selected);
                    foreach (var child in removed)
                    {
                        _viewModel.Appearance.Remove(child.Id);
                        (child as IDisposable)?.Dispose();
                    }
                    foreach (var menu in Walk(form).OfType<Forma.Core.Controls.ContextMenu>())
                        if (removed.Any(item => item.Id == menu.TargetId)) menu.TargetId = "";
                    _viewModel.SelectedControl = form;
                    return new("Control deleted");
                case "bring-front":
                case "send-back":
                    var siblings = selected.Parent!.Children.OrderBy(item => _viewModel.Appearance[item.Id].ZIndex).ToList();
                    siblings.Remove(selected);
                    if (String(payload, "command") == "bring-front") siblings.Add(selected);
                    else siblings.Insert(0, selected);
                    for (var index = 0; index < siblings.Count; index++) _viewModel.Appearance[siblings[index].Id].ZIndex = index;
                    return new("Layer order updated");
            }
        }
        if (control is null || !_viewModel.Appearance.TryGetValue(control.Id, out var bounds)) return new("Ready");
        switch (action)
        {
            case "move" when control != form && !bounds.Locked
                && Number(payload, "x") is int left && Number(payload, "y") is int top:
                if (control is Forma.Core.Controls.INonvisualControl) return new("Ready");
                var parentId = String(payload, "parentId");
                if (parentId is not null && parentId != control.Parent?.Id)
                {
                    var target = Walk(form).FirstOrDefault(item => item.Id == parentId);
                    if (target is null || !(target == form || target is Forma.Core.Controls.Panel or Forma.Core.Controls.LayoutContainer)
                        || _viewModel.Appearance[target.Id].Locked || Walk(control).Contains(target))
                        return new("Ready");
                    // Reparent the same model instance; descendants and identities stay intact.
                    control.Parent?.Remove(control);
                    control.LayoutSlot = target is Forma.Core.Controls.TabControl tabs ? tabs.SelectedTab + 1
                        : target is Forma.Core.Controls.SplitContainer ? Math.Min(2, target.Children.Count + 1)
                        : target is Forma.Core.Controls.TableLayoutPanel ? target.Children.Count + 1 : 1;
                    control.X = left;
                    control.Y = top;
                    target.Add(control);
                    ResizeControl(control, bounds.Width, bounds.Height);
                }
                Position(control, left, top);
                _viewModel.SelectedControl = control;
                return new("Control moved");
            case "resize" when !bounds.Locked && control is not Forma.Core.Controls.INonvisualControl
                && Number(payload, "width") is int width && Number(payload, "height") is int height:
                ResizeControl(control, width, height);
                if (control != form && Number(payload, "x") is int rx && Number(payload, "y") is int ry)
                    Position(control, rx, ry);
                _viewModel.SelectedControl = control;
                return new("Control resized");
            case "property" when control == _viewModel.SelectedControl:
                ApplyProperty(control, payload);
                return new("Property updated");
        }
        return new("Ready");
    }
    private FControl? AddControl(string? kind, int x, int y, FControl parent)
    {
        if (
            _viewModel.Form is null
            || _viewModel.Appearance[parent.Id].Locked
            || x < 0
            || y < 0
            || x >= _viewModel.Appearance[parent.Id].Width
            || y >= _viewModel.Appearance[parent.Id].Height
        )
            return null;
        FControl? control = kind switch
        {
            "contextmenu" => new Forma.Core.Controls.ContextMenu(),
            "contextmenustrip" => new Forma.Core.Controls.ContextMenuStrip(),
            "dialog" => new Forma.Core.Controls.Dialog(),
            "confirmationdialog" => new Forma.Core.Controls.ConfirmationDialog(),
            "menustrip" => new Forma.Core.Controls.MenuStrip(),
            "toolbar" => new Forma.Core.Controls.Toolbar(),
            "toolstrip" => new Forma.Core.Controls.ToolStrip(),
            "statusbar" => new Forma.Core.Controls.StatusBar(),
            "filepicker" => new Forma.Core.Controls.FilePicker(),
            "folderpicker" => new Forma.Core.Controls.FolderPicker(),
            "propertygrid" => new Forma.Core.Controls.PropertyGrid(),
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
            "listview" => new Forma.Core.Controls.ListView(),
            "treeview" => new Forma.Core.Controls.TreeView(),
            "pagination" => new Forma.Core.Controls.Pagination(),
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
            return null;
        do { control.Name = $"{kind}{++_viewModel.ControlSequence}"; } while (Walk(_viewModel.Form).Any(c => c.Name == control.Name));
        _viewModel.Appearance[control.Id] = kind switch
        {
            "menustrip" or "toolbar" or "toolstrip" or "statusbar" => new Appearance { Width = 500, Height = 36, ForeColor = "#1f2937", BackColor = "#f8fafc", PaddingTop = 2, PaddingBottom = 2, PaddingLeft = 4, PaddingRight = 4 },
            "filepicker" or "folderpicker" => new Appearance { Width = 320, Height = 40, ForeColor = "#1f2937", BackColor = "#ffffff", PaddingTop = 0, PaddingBottom = 0, PaddingLeft = 0, PaddingRight = 0 },
            "propertygrid" => new Appearance { Width = 300, Height = 200, ForeColor = "#1f2937", BackColor = "#ffffff", PaddingTop = 0, PaddingBottom = 0, PaddingLeft = 0, PaddingRight = 0 },
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
            "listbox" or "listview" or "treeview" or "checkedlistbox" or "richtextbox" or "textarea" or "circularprogress" => new Appearance
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
        _viewModel.Appearance[control.Id].ZIndex = control
            is Forma.Core.Controls.Panel
                or Forma.Core.Controls.LayoutContainer
            ? 0
            : 10;
        if (control is Forma.Core.Controls.CircularProgress) { _viewModel.Appearance[control.Id].Width = 120; _viewModel.Appearance[control.Id].Height = 120; _viewModel.Appearance[control.Id].BorderRadius = 100; }
        if (control is Forma.Core.Controls.LinkLabel) { _viewModel.Appearance[control.Id].Width = 180; _viewModel.Appearance[control.Id].ForeColor = "#2563eb"; _viewModel.Appearance[control.Id].BackColor = "#ffffff"; _viewModel.Appearance[control.Id].BorderWidth = 0; _viewModel.Appearance[control.Id].Cursor = "pointer"; }
        if (control is Forma.Core.Controls.RichTextBox) { _viewModel.Appearance[control.Id].Width = 320; _viewModel.Appearance[control.Id].Height = 180; }
        if (control is Forma.Core.Controls.TreeView) { _viewModel.Appearance[control.Id].Width = 260; _viewModel.Appearance[control.Id].Height = 200; }
        if (control is Forma.Core.Controls.Pagination) { _viewModel.Appearance[control.Id].Width = 300; _viewModel.Appearance[control.Id].Height = 40; _viewModel.Appearance[control.Id].ForeColor = "#1f2937"; _viewModel.Appearance[control.Id].BackColor = "#ffffff"; }
        if (control is Forma.Core.Controls.MaskedTextBox) _viewModel.Appearance[control.Id].Placeholder = "000-0000";
        if (control is Forma.Core.Controls.SearchBox) _viewModel.Appearance[control.Id].Placeholder = "Search...";
        if (control is Forma.Core.Controls.PasswordBox) _viewModel.Appearance[control.Id].Placeholder = "Password";
        if (control is Forma.Core.Controls.PasswordBox) _viewModel.Appearance[control.Id].Password = true;
        if (control is Forma.Core.Controls.INonvisualControl)
            parent = _viewModel.Form;
        control.X = x;
        control.Y = y;
        if (parent is Forma.Core.Controls.TabControl tabParent)
            control.LayoutSlot = tabParent.SelectedTab + 1;
        else if (parent is Forma.Core.Controls.SplitContainer)
            control.LayoutSlot = Math.Min(2, parent.Children.Count + 1);
        else if (parent is Forma.Core.Controls.TableLayoutPanel)
            control.LayoutSlot = parent.Children.Count + 1;
        parent.Add(control);
        if (control is not Forma.Core.Controls.INonvisualControl)
            ResizeControl(control, _viewModel.Appearance[control.Id].Width, _viewModel.Appearance[control.Id].Height);
        _viewModel.SelectedControl = control;
        return control;
    }

    private (int Width, int Height) AvailableBounds(FControl parent)
    {
        var a = _viewModel.Appearance[parent.Id];
        if (parent == _viewModel.Form)
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
        var bounds = _viewModel.Appearance[control.Id];
        var parentBounds = AvailableBounds(control.Parent ?? _viewModel.Form!);
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
        var a = _viewModel.Appearance[control.Id];
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
        if (control is Forma.Core.Controls.CommandControl commands && property == "commandItems" && text is not null)
            commands.Items = JsonSerializer.Deserialize<Forma.Core.Controls.CommandItem[]>(text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        if (control is Forma.Core.Controls.ContextMenu menu && property == "targetId" && text is not null)
        {
            if (text != "" && !Walk(_viewModel.Form!).Any(item => item.Id == text && item is not Forma.Core.Controls.INonvisualControl))
                throw new ArgumentException("Choose a control in the current form.");
            menu.TargetId = text;
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
        if (control is Forma.Core.Controls.CheckedListBox checkedList) {
            if (property == "items" && text is not null) checkedList.Items = Lines(text);
            if (property == "checkedIndices" && text is not null) checkedList.CheckedIndices = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(int.Parse).ToArray();
        }
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
            case "name" when !string.IsNullOrWhiteSpace(text):
                var name = text.Trim();
                if (
                    !(char.IsLetter(name[0]) || name[0] == '_')
                    || name.Any(c => !char.IsLetterOrDigit(c) && c != '_')
                    || Walk(_viewModel.Form!).Any(c => c != control && c.Name == name)
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
                Position(control, x, control.Y ?? 0);
                break;
            case "y" when control != _viewModel.Form && number is int y:
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
            ResizeControl(control, a.Width, a.Height);
    }

    private void ResizeControl(FControl control, int width, int height)
    {
        var a = _viewModel.Appearance[control.Id];
        if (control == _viewModel.Form)
        {
            var minWidth = Math.Max(
                240,
                _viewModel.Form
                    .Children.Where(c => c is not Forma.Core.Controls.INonvisualControl)
                    .Select(c =>
                        _viewModel.Appearance[c.Id].Width
                        + _viewModel.Appearance[c.Id].MarginLeft
                        + _viewModel.Appearance[c.Id].MarginRight
                        + (_viewModel.Appearance[c.Id].Locked ? c.X ?? 0 : 0)
                    )
                    .DefaultIfEmpty(0)
                    .Max()
            );
            var minHeight = Math.Max(
                160,
                _viewModel.Form
                    .Children.Where(c => c is not Forma.Core.Controls.INonvisualControl)
                    .Select(c =>
                        _viewModel.Appearance[c.Id].Height
                        + _viewModel.Appearance[c.Id].MarginTop
                        + _viewModel.Appearance[c.Id].MarginBottom
                        + (_viewModel.Appearance[c.Id].Locked ? c.Y ?? 0 : 0)
                    )
                    .DefaultIfEmpty(0)
                    .Max()
            );
            _viewModel.Form.Width = a.Width = ConstrainedSize(
                width,
                minWidth,
                1600,
                a.MinimumWidth,
                a.MaximumWidth
            );
            _viewModel.Form.Height = a.Height = ConstrainedSize(
                height,
                minHeight,
                1200,
                a.MinimumHeight,
                a.MaximumHeight
            );
            foreach (var child in _viewModel.Form.Children.Where(c => c is not Forma.Core.Controls.INonvisualControl))
                Position(child, child.X ?? 0, child.Y ?? 0);
        }
        else
        {
            var parentBounds = AvailableBounds(control.Parent ?? _viewModel.Form!);
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
                ResizeControl(child, _viewModel.Appearance[child.Id].Width, _viewModel.Appearance[child.Id].Height);
        }
    }

    private static int ConstrainedSize(int value, int floor, int limit, int minimum, int maximum)
    {
        var low = Math.Clamp(minimum, floor, limit);
        var high = maximum == 0 ? limit : Math.Clamp(maximum, low, limit);
        return Math.Clamp(value, low, high);
    }

    private static string[] Lines(string text) =>
        text.Split('\n').Select(s => s.TrimEnd('\r')).Where(s => s.Length > 0).ToArray();

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

    private static IEnumerable<FControl> Walk(FControl root)
    {
        yield return root;
        foreach (var child in root.Children)
        foreach (var item in Walk(child)) yield return item;
    }
}
