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
                _ => Appearance[control.Id].ReadOnly
            };
            if (control is DataGridView dataGrid)
            {
                item["selectedRow"] = dataGrid.SelectedRow; item["filterText"] = dataGrid.FilterText;
                item["sortColumn"] = dataGrid.SortColumn; item["sortDirection"] = dataGrid.SortDirection;
            }
            if (control is Forma.Core.Controls.Image image) item["source"] = image.Source;
            if (control is PathPicker picker) item["selectedPath"] = picker.SelectedPath;
            if (control is Spinner spinner) item["isActive"] = spinner.IsActive;
            if (control is LoadingOverlay overlay) item["isActive"] = overlay.IsActive;
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
