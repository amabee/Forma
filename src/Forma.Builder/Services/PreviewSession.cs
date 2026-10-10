using System.Text.Json;
using Forma.Core.Controls;
using Control = Forma.Core.Controls.Control;
using TabControl = Forma.Core.Controls.TabControl;
using RichTextBox = Forma.Core.Controls.RichTextBox;
using PropertyGrid = Forma.Core.Controls.PropertyGrid;
using DataGridView = Forma.Core.Controls.DataGridView;
using CheckBox = Forma.Core.Controls.CheckBox;

namespace Forma.Builder;

/// <summary>A separate runtime tree, so trying the app never edits its design.</summary>
public sealed class PreviewSession : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    public Forma.Core.Form Form { get; }
    public Dictionary<string, Appearance> Appearance { get; }
    public IEnumerable<Control> Controls => Walk(Form);

    public PreviewSession(BuilderViewModel design)
    {
        if (design.Form is null) throw new InvalidOperationException("Create a form before previewing.");
        var copy = ProjectFile.Restore(ProjectFile.Capture(design.Form,
            control => JsonSerializer.SerializeToElement(design.Appearance[control.Id])));
        Form = copy.Form;
        Appearance = copy.Appearance.ToDictionary(pair => pair.Key, pair => pair.Value.Deserialize<Appearance>()!);
    }

    public object State() => new
    {
        type = "designer", action = "runtime-preview", id = Form.Id,
        controls = Controls.Select(control =>
        {
            var item = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                JsonSerializer.Serialize(Appearance[control.Id], JsonOptions))!;
            item["id"] = control.Id;
            item["name"] = control.Name ?? control.ControlType;
            item["text"] = control.Text;
            item["kind"] = control.ControlType;
            item["parentId"] = control.Parent?.Id;
            item["layoutSlot"] = control.LayoutSlot;
            item["component"] = control is INonvisualControl;
            item["selectedTab"] = (control as TabControl)?.SelectedTab ?? 0;
            item["readOnly"] = control switch
            {
                RichTextBox rich => rich.ReadOnly,
                PropertyGrid grid => grid.ReadOnly,
                DataGridView grid => grid.ReadOnly,
                Rating rating => rating.ReadOnly,
                _ => Appearance[control.Id].ReadOnly
            };
            if (control is DataGridView dataGrid)
            {
                item["columns"] = dataGrid.Columns; item["rows"] = dataGrid.Rows;
                item["sortingEnabled"] = dataGrid.SortingEnabled; item["filteringEnabled"] = dataGrid.FilteringEnabled;
                item["selectedRow"] = dataGrid.SelectedRow; item["filterText"] = dataGrid.FilterText;
                item["sortColumn"] = dataGrid.SortColumn; item["sortDirection"] = dataGrid.SortDirection;
            }
            if (control is Forma.Core.Controls.Image image) item["source"] = image.Source;
            if (control is ChoiceControl choice) { item["items"] = choice.Items; item["selectedIndex"] = choice.SelectedIndex; }
            if (control is MultiChoiceControl multi) { item["items"] = multi.Items; item["checkedIndices"] = multi.CheckedIndices; }
            if (control is Rating ratingValue) { item["number"] = ratingValue.Value; item["stars"] = ratingValue.Stars; }
            if (control is CheckBox check) item["checked"] = check.Checked;
            if (control is Chip chip) { item["variant"] = chip.Variant; item["removable"] = chip.Removable; item["isRemoved"] = chip.IsRemoved; }
            if (control is IconButton iconButton) { item["iconName"] = iconButton.IconName; item["showText"] = iconButton.ShowText; }
            if (control is CommandButton commandButton) item["description"] = commandButton.Description;
            if (control is SplitButton split) item["primaryEnabled"] = split.PrimaryEnabled;
            if (control is CommandControl commands) item["commandItems"] = commands.Items;
            if (control is LayoutContainer layout) { item["orientation"] = layout.Orientation; item["gap"] = layout.Gap; }
            if (control is SelectionGroup group) item["orientation"] = group.Orientation;
            if (control is ScrollablePanel scroll) item["scrollDirection"] = scroll.ScrollDirection;
            if (control is PathPicker picker) item["selectedPath"] = picker.SelectedPath;
            if (control is Spinner spinner) item["isActive"] = spinner.IsActive;
            if (control is LoadingOverlay overlay) item["isActive"] = overlay.IsActive;
            if (control is Toast toast) {
                item["variant"] = toast.Variant; item["position"] = toast.Position;
                item["duration"] = toast.Duration; item["dismissible"] = toast.Dismissible; item["isOpen"] = toast.IsOpen;
            }
            return item;
        }).ToArray()
    };

    public void Start()
    {
        foreach (var timer in Controls.OfType<Forma.Core.Controls.Timer>())
            timer.Enabled = Appearance[timer.Id].Enabled;
    }

    public void SetValue(string id, string property, JsonElement value)
    {
        var control = Controls.FirstOrDefault(control => control.Id == id)
            ?? throw new ArgumentException("The target control no longer exists.");
        switch (property)
        {
            case "orientation" when control is LayoutContainer layout && value.ValueKind == JsonValueKind.String && value.GetString() is "horizontal" or "vertical": layout.Orientation = value.GetString()!; return;
            case "orientation" when control is SelectionGroup group && value.ValueKind == JsonValueKind.String && value.GetString() is "horizontal" or "vertical": group.Orientation = value.GetString()!; return;
            case "gap" when control is LayoutContainer layout && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var gap): layout.Gap = gap; return;
            case "scrollDirection" when control is ScrollablePanel scroll && value.ValueKind == JsonValueKind.String && value.GetString() is "both" or "horizontal" or "vertical": scroll.ScrollDirection = value.GetString()!; return;
            case "commandItems" when control is CommandControl commands:
                if (value.ValueKind != JsonValueKind.Array) throw new ArgumentException("Commands must be an array.");
                commands.Items = value.Deserialize<CommandItem[]>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? []; return;
            case "primaryEnabled" when control is SplitButton split && value.ValueKind is JsonValueKind.True or JsonValueKind.False: split.PrimaryEnabled = value.GetBoolean(); return;
            case "description" when control is CommandButton commandButton && value.ValueKind == JsonValueKind.String: commandButton.Description = value.GetString()!; return;
            case "iconName" when control is IconButton iconButton && value.ValueKind == JsonValueKind.String && Forma.Core.Controls.Icon.Names.Contains(value.GetString()!): iconButton.IconName = value.GetString()!; return;
            case "showText" when control is IconButton iconButton && value.ValueKind is JsonValueKind.True or JsonValueKind.False: iconButton.ShowText = value.GetBoolean(); return;
            case "variant" when control is Chip chip && value.ValueKind == JsonValueKind.String && value.GetString() is "neutral" or "info" or "success" or "warning" or "danger": chip.Variant = value.GetString()!; return;
            case "removable" when control is Chip chip && value.ValueKind is JsonValueKind.True or JsonValueKind.False: chip.Removable = value.GetBoolean(); return;
            case "isRemoved" when control is Chip chip && value.ValueKind is JsonValueKind.True or JsonValueKind.False:
                if (value.GetBoolean()) chip.Remove(); else chip.Restore(); return;
            case "items" when control is ChoiceControl choice: choice.Items = StringArray(value); return;
            case "items" when control is MultiChoiceControl multi: multi.Items = StringArray(value); return;
            case "checkedIndices" when control is MultiChoiceControl multi && value.ValueKind == JsonValueKind.Array:
                if (value.EnumerateArray().Any(index => index.ValueKind != JsonValueKind.Number || !index.TryGetInt32(out _))) throw new ArgumentException("Checked indices must be integers.");
                multi.CheckedIndices = value.EnumerateArray().Select(index => index.GetInt32()).ToArray(); return;
            case "readOnly" when control is Rating rating && value.ValueKind is JsonValueKind.True or JsonValueKind.False: rating.ReadOnly = value.GetBoolean(); return;
            case "variant" or "position" or "duration" or "dismissible" when control is Toast toast:
                ValidateToastOption(property, value);
                switch (property) {
                    case "variant": toast.Variant = value.GetString()!; break;
                    case "position": toast.Position = value.GetString()!; break;
                    case "duration": toast.Duration = value.GetInt32(); break;
                    case "dismissible": toast.Dismissible = value.GetBoolean(); break;
                }
                return;
            case "columns" when control is DataGridView grid:
                grid.Columns = StringArray(value); return;
            case "rows" when control is DataGridView grid && value.ValueKind == JsonValueKind.Array:
                var rows = value.EnumerateArray().Select(StringArray).ToArray();
                grid.Rows = rows; return;
            case "readOnly" when control is DataGridView grid && value.ValueKind is JsonValueKind.True or JsonValueKind.False:
                grid.ReadOnly = value.GetBoolean(); return;
            case "sortingEnabled" when control is DataGridView grid && value.ValueKind is JsonValueKind.True or JsonValueKind.False:
                grid.SortingEnabled = value.GetBoolean(); return;
            case "filteringEnabled" when control is DataGridView grid && value.ValueKind is JsonValueKind.True or JsonValueKind.False:
                grid.FilteringEnabled = value.GetBoolean(); return;
            case "source" when control is Forma.Core.Controls.Image image && value.ValueKind == JsonValueKind.String:
                var source = value.GetString()!;
                if (source.Length > 14 * 1024 * 1024 || source.Contains('\0')) throw new ArgumentException("Invalid image source.");
                if (source != "") {
                    if (Path.IsPathFullyQualified(source)) source = new Uri(source).AbsoluteUri;
                    if (!Uri.TryCreate(source, UriKind.Absolute, out var uri) || uri.Scheme is not ("file" or "http" or "https" or "data")
                        || uri.Scheme == "data" && !source.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                        throw new ArgumentException("Use a file path, file/http/https URI, or image data URI.");
                }
                image.Source = source; return;

            case "isActive" when control is Spinner spinner && value.ValueKind is JsonValueKind.True or JsonValueKind.False:
                spinner.IsActive = value.GetBoolean(); return;
            case "isActive" when control is LoadingOverlay overlay && value.ValueKind is JsonValueKind.True or JsonValueKind.False:
                overlay.IsActive = value.GetBoolean(); return;
            case "text" when value.ValueKind == JsonValueKind.String:
                var text = value.GetString()!;
                if (text.Length > 32767) throw new ArgumentException("Text is too long.");
                control.Text = text; return;
            case "enabled" when value.ValueKind is JsonValueKind.True or JsonValueKind.False:
                Appearance[id].Enabled = value.GetBoolean();
                if (control is Forma.Core.Controls.Timer timer) timer.Enabled = value.GetBoolean();
                return;
            case "visible" when value.ValueKind is JsonValueKind.True or JsonValueKind.False:
                Appearance[id].Visible = value.GetBoolean(); return;
            case "checked" when control is CheckBox check && value.ValueKind is JsonValueKind.True or JsonValueKind.False:
                check.Checked = value.GetBoolean(); return;
            case "value" when control is NumericControl number && value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var numeric) && double.IsFinite(numeric):
                number.Value = numeric; return;
            case "value" when control is DateTimeInput date && value.ValueKind == JsonValueKind.String:
                date.DateValue = value.GetString()!; return;
            case "value" when control is ColorPicker color && value.ValueKind == JsonValueKind.String:
                color.Color = value.GetString()!; return;
            case "selectedIndex" when control is ChoiceControl choice && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var index):
                choice.SelectedIndex = index; return;
            case "selectedTab" when control is TabControl tabs && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var tab):
                tabs.SelectedTab = tab; return;
            case "selectedRow" when control is DataGridView grid && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var row):
                grid.SelectedRow = row; return;
            case "sortColumn" when control is DataGridView grid && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var column):
                grid.SortColumn = column; return;
            case "sortDirection" when control is DataGridView grid && value.ValueKind == JsonValueKind.String:
                grid.SortDirection = value.GetString()!; return;
            case "filterText" when control is DataGridView grid && value.ValueKind == JsonValueKind.String:
                grid.FilterText = value.GetString()!; return;
        }
        throw new ArgumentException($"Property '{property}' is unsupported or has the wrong value type for {control.ControlType}.");
    }

    private static string[] StringArray(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array || value.EnumerateArray().Any(cell => cell.ValueKind != JsonValueKind.String))
            throw new ArgumentException("Grid columns and cells must be arrays of strings.");
        return value.EnumerateArray().Select(cell => cell.GetString()!).ToArray();
    }

    private static void ValidateToastOption(string property, JsonElement value)
    {
        var valid = property switch {
            "text" => value.ValueKind == JsonValueKind.String && value.GetString()!.Length <= 32767,
            "variant" => value.ValueKind == JsonValueKind.String && Toast.Variants.Contains(value.GetString()!),
            "position" => value.ValueKind == JsonValueKind.String && value.GetString() is "top-right" or "top-left" or "bottom-right" or "bottom-left",
            "duration" => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out _),
            "dismissible" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
            _ => false
        };
        if (!valid) throw new ArgumentException($"Invalid toast option '{property}'.");
    }

    public void ShowToast(string id, JsonElement options)
    {
        var toast = Controls.FirstOrDefault(control => control.Id == id) as Toast
            ?? throw new ArgumentException("The target must be a Toast.");
        if (options.ValueKind is not (JsonValueKind.Object or JsonValueKind.Undefined)) throw new ArgumentException("Toast options must be an object.");
        if (options.ValueKind == JsonValueKind.Object) {
            var properties = options.EnumerateObject().ToArray();
            foreach (var property in properties) ValidateToastOption(property.Name, property.Value);
            foreach (var property in properties) SetValue(id, property.Name, property.Value);
        }
        toast.Show();
    }

    /// <summary>Apply each script command to the latest runtime rows, avoiding stale browser read/modify/write races.</summary>
    public void EditGrid(string id, string operation, JsonElement payload)
    {
        var grid = Controls.FirstOrDefault(control => control.Id == id) as DataGridView
            ?? throw new ArgumentException("The target must be a DataGridView.");
        var rows = grid.Rows.ToList();
        var selection = grid.SelectedRow;
        int Index(string name, int count)
        {
            if (!payload.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var index) || index < 0 || index >= count)
                throw new ArgumentException($"Grid {name} index is out of range.");
            return index;
        }
        switch (operation)
        {
            case "addRow": rows.Add(StringArray(payload.GetProperty("row"))); break;
            case "updateRow": rows[Index("index", rows.Count)] = StringArray(payload.GetProperty("row")); break;
            case "removeRow":
                var removed = Index("index", rows.Count);
                rows.RemoveAt(removed);
                selection = selection == removed ? -1 : selection > removed ? selection - 1 : selection;
                break;
            case "clearRows": rows.Clear(); selection = -1; break;
            case "setCell":
                var row = Index("rowIndex", rows.Count);
                var column = Index("columnIndex", grid.Columns.Length);
                var cell = payload.GetProperty("value");
                if (cell.ValueKind != JsonValueKind.String) throw new ArgumentException("Grid cells must be strings.");
                var cells = rows[row];
                if (cells.Length <= column) {
                    var padded = Enumerable.Repeat("", grid.Columns.Length).ToArray();
                    cells.CopyTo(padded, 0); cells = padded;
                }
                cells[column] = cell.GetString()!; rows[row] = cells;
                break;
            default: throw new ArgumentException($"Unknown grid operation '{operation}'.");
        }
        // ReadOnly governs interactive editing; scripts may update data in a read-only grid.
        grid.Rows = rows.ToArray();
        grid.SelectedRow = selection;
    }

    public void Dispose()
    {
        foreach (var component in Controls.OfType<IDisposable>()) component.Dispose();
    }

    private static IEnumerable<Control> Walk(Control root)
    {
        yield return root;
        foreach (var child in root.Children)
            foreach (var descendant in Walk(child)) yield return descendant;
    }
}
