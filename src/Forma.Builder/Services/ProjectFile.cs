using System.Reflection;
using System.Text.Json;
using Forma.Core.Controls;
using C = Forma.Core.Controls;
using FControl = Forma.Core.Controls.Control;

namespace Forma.Builder;

public sealed class ProjectDocument
{
    public string Format { get; set; } = "forma-project";
    public int Version { get; set; } = 1;
    public ProjectNode Root { get; set; } = new();
}

public sealed class ProjectNode
{
    public string Kind { get; set; } = "";
    public string Id { get; set; } = "";
    public Dictionary<string, JsonElement> Properties { get; set; } = [];
    public JsonElement Appearance { get; set; }
    public List<ProjectNode> Children { get; set; } = [];
}

/// <summary>Versioned JSON design files. Only registered controls can be instantiated.</summary>
public static class ProjectFile
{
    public const long MaximumBytes = 50 * 1024 * 1024;
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        MaxDepth = 128,
    };
    private static readonly Dictionary<string, Type> Types = new Type[]
    {
        typeof(Forma.Core.Form),
        typeof(C.Button),
        typeof(C.Label),
        typeof(C.TextBox),
        typeof(C.Panel),
        typeof(C.GroupBox),
        typeof(C.SplitContainer),
        typeof(C.TabControl),
        typeof(C.FlowLayoutPanel),
        typeof(C.TableLayoutPanel),
        typeof(C.CheckBox),
        typeof(C.RadioButton),
        typeof(C.ComboBox),
        typeof(C.ListBox),
        typeof(C.Image),
        typeof(C.DataGridView),
        typeof(Forma.Core.Controls.Timer),
        typeof(C.BackgroundWorker),
        typeof(C.NumericUpDown),
        typeof(C.Slider),
        typeof(C.ProgressBar),
        typeof(C.CircularProgress),
        typeof(C.ToggleSwitch),
        typeof(C.ToggleButton),
        typeof(C.DatePicker),
        typeof(C.TimePicker),
        typeof(C.DateTimePicker),
        typeof(C.ColorPicker),
        typeof(C.SearchBox),
        typeof(C.PasswordBox),
        typeof(C.TextArea),
        typeof(C.LinkLabel),
        typeof(C.MaskedTextBox),
        typeof(C.CheckedListBox),
        typeof(C.RichTextBox),
        typeof(C.PictureBox),
        typeof(C.ListView),
        typeof(C.TreeView),
        typeof(C.Pagination),
        typeof(C.FilePicker),
        typeof(C.FolderPicker),
        typeof(C.PropertyGrid),
        typeof(C.MenuStrip), typeof(C.Toolbar), typeof(C.ToolStrip), typeof(C.StatusBar),
        typeof(C.ContextMenu), typeof(C.ContextMenuStrip), typeof(C.Dialog), typeof(C.ConfirmationDialog),
        typeof(C.Tooltip),
        typeof(C.Icon), typeof(C.EmptyState), typeof(C.Skeleton), typeof(C.Card), typeof(C.Badge), typeof(C.Avatar), typeof(C.Divider), typeof(C.Toast), typeof(C.Spinner), typeof(C.LoadingOverlay),
    }.ToDictionary(t => t.Name.ToLowerInvariant());
    private static readonly HashSet<string> PropertyNames = new(StringComparer.Ordinal)
    {
        "Name",
        "Text",
        "Value",
        "Placeholder",
        "Label",
        "X",
        "Y",
        "LayoutSlot",
        "Title",
        "Width",
        "Height",
        "Checked",
        "Items",
        "SelectedIndex",
        "Source",
        "SizeMode",
        "Orientation",
        "Gap",
        "Columns",
        "Tabs",
        "SelectedTab",
        "Rows",
        "ReadOnly",
        "Password",
        "Multiline",
        "SpellCheck",
        "MaxLength",
        "MinLength",
        "Interval",
        "Nodes",
        "SelectedNode",
        "ExpandedNodes",
        "TotalItems",
        "PageSize",
        "Page",
        "Document",
        "SelectedPath", "DialogTitle", "Filter", "Entries",
        "RightText",
        "RowCount",
        "TargetId", "Message", "Buttons", "CanCancel",
        "InitialDelay", "ShowDuration", "Placement", "SortingEnabled", "FilteringEnabled", "FilterText", "SortColumn", "SortDirection", "SelectedRow",
        "IconName", "StrokeWidth", "Lines", "Description", "HeaderVisible", "Variant", "Initials", "Shape", "Thickness", "LineStyle", "Duration", "Position", "Dismissible", "IsActive", "Speed",
        "Url",
        "Visited",
        "Mask",
        "CheckedIndices",
        "WorkerReportsProgress",
        "WorkerSupportsCancellation",
        "Minimum",
        "Maximum",
        "Increment",
        "DateValue",
        "Color",
    };

    private static IEnumerable<PropertyInfo> Properties(Type type) =>
        type.GetProperties()
            .Where(p =>
                p.CanRead && p.SetMethod?.IsPublic == true && PropertyNames.Contains(p.Name)
            )
            .GroupBy(p => p.Name)
            .Select(g => g.OrderBy(p => Distance(type, p.DeclaringType!)).First());

    private static int Distance(Type type, Type declaring)
    {
        var n = 0;
        while (type != declaring && type.BaseType is { } parent)
        {
            type = parent;
            n++;
        }
        return n;
    }

    public static ProjectDocument Capture(
        FControl root,
        Func<FControl, JsonElement> appearance,
        bool embedImages = false
    )
    {
        ProjectNode Node(FControl control)
        {
            var properties = Properties(control.GetType())
                .ToDictionary(
                    p => p.Name,
                    p => JsonSerializer.SerializeToElement(p.GetValue(control), p.PropertyType)
                );
            if (
                embedImages
                && control is C.Image image
                && Uri.TryCreate(image.Source, UriKind.Absolute, out var uri)
                && uri.IsFile
            )
            {
                var info = new FileInfo(uri.LocalPath);
                if (info.Length > 10 * 1024 * 1024)
                    throw new InvalidDataException("An image exceeds the 10 MB image limit.");
                var mime = Path.GetExtension(info.Name).ToLowerInvariant() switch
                {
                    ".svg" => "image/svg+xml",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    ".bmp" => "image/bmp",
                    ".ico" => "image/x-icon",
                    _ => "image/png",
                };
                properties["Source"] = JsonSerializer.SerializeToElement(
                    $"data:{mime};base64,{Convert.ToBase64String(File.ReadAllBytes(info.FullName))}"
                );
            }
            return new()
            {
                Kind = control.ControlType,
                Id = control.Id,
                Properties = properties,
                Appearance = appearance(control),
                Children = control.Children.Select(Node).ToList(),
            };
        }
        return new() { Root = Node(root) };
    }

    public static string Serialize(ProjectDocument document) =>
        JsonSerializer.Serialize(document, Options);

    public static ProjectDocument Parse(string json)
    {
        if (System.Text.Encoding.UTF8.GetByteCount(json) > MaximumBytes)
            throw new InvalidDataException("The project exceeds the 50 MB limit.");
        var document =
            JsonSerializer.Deserialize<ProjectDocument>(json, Options)
            ?? throw new InvalidDataException("Empty project file.");
        if (document.Format != "forma-project" || document.Version != 1)
            throw new InvalidDataException("This is not a supported Forma project version.");
        var ids = new HashSet<string>();
        var count = 0;
        void Validate(ProjectNode node, int depth)
        {
            if (node is null || depth > 32 || ++count > 5000)
                throw new InvalidDataException("The project tree is too large or invalid.");
            if (
                node.Kind is null
                || !Types.ContainsKey(node.Kind)
                || (depth == 0) != (node.Kind == "form")
            )
                throw new InvalidDataException("Unknown control kind or invalid root form.");
            if (
                string.IsNullOrEmpty(node.Id)
                || node.Id.Length > 128
                || node.Id.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_')
                || !ids.Add(node.Id)
            )
                throw new InvalidDataException("Invalid or duplicate control ID.");
            if (
                node.Properties is null
                || node.Children is null
                || node.Appearance.ValueKind != JsonValueKind.Object
            )
                throw new InvalidDataException("Missing control properties or appearance.");
            foreach (var dimension in new[] { "Width", "Height" })
                if (
                    !node.Appearance.TryGetProperty(dimension, out var value)
                    || !value.TryGetInt32(out var size)
                    || size < 20
                    || size > 1600
                )
                    throw new InvalidDataException("Invalid control dimensions.");
            if (
                node.Children.Count > 0
                && node.Kind
                    is not (
                        "form"
                        or "panel"
                        or "groupbox"
                        or "card"
                        or "splitcontainer"
                        or "tabcontrol"
                        or "flowlayoutpanel"
                        or "tablelayoutpanel"
                    )
            )
                throw new InvalidDataException("This control cannot contain children.");
            foreach (var child in node.Children)
            {
                if (depth > 0 && child.Kind is "timer" or "backgroundworker")
                    throw new InvalidDataException("Nonvisual components must belong to the form.");
                Validate(child, depth + 1);
            }
        }
        Validate(document.Root, 0);
        return document;
    }

    public static (Forma.Core.Form Form, Dictionary<string, JsonElement> Appearance) Restore(
        ProjectDocument document
    )
    {
        // Validate even documents passed directly by callers before building any tree.
        document = Parse(Serialize(document));
        var appearance = new Dictionary<string, JsonElement>();
        FControl Build(ProjectNode node)
        {
            var control = (FControl)Activator.CreateInstance(Types[node.Kind])!;
            typeof(FControl).GetProperty(nameof(FControl.Id))!.SetValue(control, node.Id);
            if (control is C.NumericControl numeric)
            {
                var minimum = node.Properties.TryGetValue("Minimum", out var min)
                    ? min.GetDouble()
                    : 0;
                var maximum = node.Properties.TryGetValue("Maximum", out var max)
                    ? max.GetDouble()
                    : 100;
                if (!double.IsFinite(minimum) || !double.IsFinite(maximum) || minimum > maximum)
                    throw new InvalidDataException("Invalid numeric bounds.");
                numeric.Minimum = Math.Min(0, minimum);
                numeric.Maximum = maximum;
                numeric.Minimum = minimum;
            }
            // Bounds and item collections must be restored before their selected values.
            var ordered = Properties(control.GetType())
                .Where(p =>
                    control is not C.NumericControl || p.Name is not ("Minimum" or "Maximum")
                )
                .OrderBy(p =>
                    p.Name
                        is "Value"
                            or "SelectedIndex"
                            or "SelectedTab"
                            or "CheckedIndices"
                            or "Document"
                            or "SelectedNode"
                            or "ExpandedNodes"
                            or "Page"
                            or "SortColumn" or "SelectedRow"
                        ? 1
                        : 0
                );
            foreach (var property in ordered)
                if (node.Properties.TryGetValue(property.Name, out var value))
                {
                    try
                    {
                        property.SetValue(control, value.Deserialize(property.PropertyType));
                    }
                    catch (Exception error)
                        when (error
                                is JsonException
                                    or TargetInvocationException
                                    or ArgumentException
                        )
                    {
                        throw new InvalidDataException(
                            $"Invalid {node.Kind}.{property.Name}.",
                            error
                        );
                    }
                }
            appearance.Add(control.Id, node.Appearance.Clone());
            foreach (var child in node.Children)
                control.Add(Build(child));
            return control;
        }
        return ((Forma.Core.Form)Build(document.Root), appearance);
    }

    public static ProjectDocument Read(string path)
    {
        if (new FileInfo(path).Length > MaximumBytes)
            throw new InvalidDataException("The project exceeds the 50 MB limit.");
        return Parse(File.ReadAllText(path));
    }

    public static void Write(string path, ProjectDocument document)
    {
        var json = Serialize(document);
        Parse(json);
        var fullPath = Path.GetFullPath(path);
        var temporary = Path.Combine(
            Path.GetDirectoryName(fullPath)!,
            $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp"
        );
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write))
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                stream.Write(bytes);
                stream.Flush(true);
            }
            File.Move(temporary, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }
}
