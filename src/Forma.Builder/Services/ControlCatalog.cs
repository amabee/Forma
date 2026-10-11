using C = Forma.Core.Controls;
using FControl = Forma.Core.Controls.Control;

namespace Forma.Builder;

/// <summary>
/// The single list of control kinds the Builder knows: the .NET type behind each kind,
/// how the toolbox creates it, its default appearance and whether it can hold children.
/// Adding a control to the designer means adding one entry here.
/// </summary>
public static class ControlCatalog
{
    public sealed record Entry(
        string Kind,
        Type Type,
        Func<FControl>? Create,
        Func<Appearance> DefaultAppearance,
        bool CanContainChildren,
        bool FormOnly = false
    );

    private const string Text = "#1f2937";
    private const string White = "#ffffff";
    private const string Tint = "#f3f6fb";

    private static readonly Dictionary<string, Entry> Entries = new Entry[]
    {
        // The form is a project root, never placed from the toolbox.
        new("form", typeof(Forma.Core.Form), null, () => new Appearance(), true),

        // Common controls
        Define<C.Button>(() => new C.Button { Text = "Continue" }),
        Define<C.Label>(() => new C.Label { Text = "Welcome to Forma" }, () => Sized(260, 40, a => a.FontSize = 22)),
        Define<C.LinkLabel>(() => new C.LinkLabel { Text = "Visit website" }, () => With(new Appearance(), a =>
        {
            a.Width = 180; a.ForeColor = "#2563eb"; a.BackColor = White; a.BorderWidth = 0; a.Cursor = "pointer";
        })),
        Define<C.TextBox>(() => new C.TextBox { Text = "" }, Field),
        Define<C.MaskedTextBox>(appearance: () => With(Field(), a => a.Placeholder = "000-0000")),
        Define<C.SearchBox>(appearance: () => With(Field(), a => a.Placeholder = "Search...")),
        Define<C.PasswordBox>(appearance: () => With(Field(), a => { a.Placeholder = "Password"; a.Password = true; })),
        Define<C.TextArea>(appearance: List),
        Define<C.RichTextBox>(appearance: () => Sized(320, 180)),
        Define<C.CheckBox>(() => new C.CheckBox { Text = "CheckBox" }, Toggle),
        Define<C.RadioButton>(() => new C.RadioButton { Text = "RadioButton" }, Toggle),
        Define<C.ToggleSwitch>(() => new C.ToggleSwitch { Text = "Toggle" }, Toggle),
        Define<C.ToggleButton>(() => new C.ToggleButton { Text = "Toggle" }),
        Define<C.ComboBox>(appearance: () => Sized(180, 36)),
        Define<C.ListBox>(appearance: List),
        Define<C.ListView>(appearance: List),
        Define<C.CheckedListBox>(appearance: List),
        Define<C.TreeView>(appearance: () => Sized(260, 200)),
        Define<C.DataGridView>(appearance: () => Unpadded(Sized(320, 180))),
        Define<C.Pagination>(appearance: () => Sized(300, 40)),
        Define<C.PropertyGrid>(appearance: () => Unpadded(Sized(300, 200))),
        Define<C.Image>(() => new C.Image { Text = "Image" }, Picture),
        Define<C.PictureBox>(() => new C.PictureBox { Text = "Picture" }, Picture),

        // Inputs and progress
        Define<C.NumericUpDown>(appearance: Field),
        Define<C.Slider>(() => new C.Slider { Value = 25 }, Field),
        Define<C.ProgressBar>(() => new C.ProgressBar { Value = 50 }, Field),
        Define<C.CircularProgress>(() => new C.CircularProgress { Value = 50 }, () => Sized(120, 120, a => a.BorderRadius = 100)),
        Define<C.DatePicker>(appearance: Field),
        Define<C.TimePicker>(appearance: Field),
        Define<C.DateTimePicker>(appearance: Field),
        Define<C.ColorPicker>(appearance: Field),
        Define<C.FilePicker>(appearance: Picker),
        Define<C.FolderPicker>(appearance: Picker),
        Define<C.RadioGroup>(appearance: () => Sized(280, 100)),
        Define<C.CheckBoxGroup>(appearance: () => Sized(280, 100)),
        Define<C.ChipGroup>(appearance: () => Sized(280, 100)),
        Define<C.SegmentedControl>(appearance: () => Sized(280, 44)),
        Define<C.Rating>(appearance: () => Sized(280, 44)),
        Define<C.ButtonGroup>(appearance: () => Sized(280, 44)),
        Define<C.Chip>(appearance: () => Sized(120, 36, a => a.BorderRadius = 18)),

        // Buttons and commands
        Define<C.IconButton>(appearance: () => With(new Appearance(), a => { a.Width = 44; a.Height = 44; })),
        Define<C.FloatingActionButton>(appearance: () => With(new Appearance(), a => { a.Width = 56; a.Height = 56; a.BorderRadius = 28; })),
        Define<C.DropdownButton>(appearance: CommandMenu),
        Define<C.SplitButton>(appearance: CommandMenu),
        Define<C.CommandButton>(appearance: () => With(new Appearance(), a => { a.Width = 240; a.Height = 64; })),
        Define<C.MenuStrip>(appearance: Bar),
        Define<C.Toolbar>(appearance: Bar),
        Define<C.ToolStrip>(appearance: Bar),
        Define<C.StatusBar>(appearance: Bar),
        Define<C.ContextMenu>(),
        Define<C.ContextMenuStrip>(),
        Define<C.Breadcrumb>(appearance: () => Sized(320, 40, a => { a.PaddingTop = 0; a.PaddingBottom = 0; })),
        Define<C.SideNavigation>(appearance: () => Sized(200, 220)),

        // Overlays
        Define<C.Dialog>(),
        Define<C.ConfirmationDialog>(),
        Define<C.Tooltip>(),
        Define<C.Toast>(),

        // Modern UI
        Define<C.Icon>(appearance: () => Ghost(32, 32)),
        Define<C.EmptyState>(appearance: () => Ghost(280, 180)),
        Define<C.Skeleton>(appearance: () => Ghost(280, 90)),
        Define<C.Badge>(appearance: () => Flat(a => { a.Width = 100; a.Height = 28; })),
        Define<C.Avatar>(appearance: () => Flat(a => { a.Width = a.Height = 64; a.FontSize = 22; })),
        Define<C.Divider>(appearance: () => Flat(a => { a.Width = 240; a.Height = 24; })),
        Define<C.Spinner>(appearance: () => Flat(a => { a.Width = 150; a.Height = 36; })),
        Define<C.LoadingOverlay>(appearance: () => Flat(_ => { })),

        // Containers
        Define<C.Panel>(appearance: () => Sized(220, 140, a => a.BackColor = Tint), container: true),
        Define<C.Card>(appearance: () => With(Container(), a => { a.BorderRadius = 10; a.Shadow = "Small"; }), container: true),
        Define<C.GroupBox>(() => new C.GroupBox { Text = "Group" }, Container, container: true),
        Define<C.SplitContainer>(appearance: Container, container: true),
        Define<C.TabControl>(appearance: Container, container: true),
        Define<C.FlowLayoutPanel>(appearance: Container, container: true),
        Define<C.TableLayoutPanel>(appearance: Container, container: true),
        Define<C.Accordion>(appearance: Container, container: true),
        Define<C.Sidebar>(appearance: Container, container: true),
        Define<C.AppShell>(appearance: Container, container: true),
        Define<C.ResponsivePanel>(appearance: Container, container: true),
        Define<C.ResponsiveGrid>(appearance: Container, container: true),
        Define<C.ResponsiveStack>(appearance: Container, container: true),
        Define<C.BreakpointContainer>(appearance: Container, container: true),
        Define<C.AspectRatioContainer>(appearance: Container, container: true),
        Define<C.StackPanel>(appearance: Container, container: true),
        Define<C.HStack>(appearance: Container, container: true),
        Define<C.VStack>(appearance: Container, container: true),
        Define<C.WrapPanel>(appearance: Container, container: true),
        Define<C.CenterPanel>(appearance: Container, container: true),
        Define<C.ScrollablePanel>(appearance: Container, container: true),

        // Tray components: saved projects may only place these directly on a form.
        Define<Forma.Core.Controls.Timer>(() => new Forma.Core.Controls.Timer { Text = "Timer" }, Disabled, formOnly: true),
        Define<C.BackgroundWorker>(() => new C.BackgroundWorker { Text = "BackgroundWorker" }, Disabled, formOnly: true),
    }.ToDictionary(entry => entry.Kind, StringComparer.Ordinal);

    public static IEnumerable<Entry> All => Entries.Values;

    public static bool TryGet(string? kind, out Entry entry)
    {
        entry = null!;
        return kind is not null && Entries.TryGetValue(kind, out entry!);
    }

    /// <summary>Creates a new toolbox control with its starting text and values, or null for unknown kinds.</summary>
    public static FControl? Create(string? kind) => TryGet(kind, out var entry) ? entry.Create?.Invoke() : null;

    private static Entry Define<T>(Func<T>? create = null, Func<Appearance>? appearance = null, bool container = false, bool formOnly = false)
        where T : FControl, new() =>
        new(typeof(T).Name.ToLowerInvariant(), typeof(T), create ?? (() => new T()), appearance ?? (() => new Appearance()), container, formOnly);

    private static Appearance With(Appearance appearance, Action<Appearance> change)
    {
        change(appearance);
        return appearance;
    }

    private static Appearance Sized(int width, int height, Action<Appearance>? change = null) =>
        With(new Appearance { Width = width, Height = height, ForeColor = Text, BackColor = White }, change ?? (_ => { }));

    private static Appearance Unpadded(Appearance appearance) =>
        With(appearance, a => a.PaddingTop = a.PaddingBottom = a.PaddingLeft = a.PaddingRight = 0);

    private static Appearance Field() => Sized(240, 36);
    private static Appearance List() => Sized(180, 140);
    private static Appearance Toggle() => Sized(160, 36, a => { a.TextAlign = "left"; a.BorderWidth = 0; });
    private static Appearance Picker() => Unpadded(Sized(320, 40));
    private static Appearance Picture() => Unpadded(Sized(180, 120, a => a.BackColor = Tint));
    private static Appearance Container() => Unpadded(Sized(300, 200));
    private static Appearance CommandMenu() => Unpadded(With(new Appearance(), a => { a.Width = 160; a.Height = 40; }));
    private static Appearance Disabled() => new() { Enabled = false };

    private static Appearance Bar() => new()
    {
        Width = 500, Height = 36, ForeColor = Text, BackColor = "#f8fafc",
        PaddingTop = 2, PaddingBottom = 2, PaddingLeft = 4, PaddingRight = 4,
    };

    // Borderless, unpadded presentation for decorative placeholders.
    private static Appearance Ghost(int width, int height) => Unpadded(new Appearance
    {
        Width = width, Height = height, BackColor = "transparent", ForeColor = "#64748b", BorderWidth = 0,
    });

    private static Appearance Flat(Action<Appearance> size) => With(Unpadded(new Appearance
    {
        BackColor = White, ForeColor = "#475569", BorderWidth = 0,
    }), size);
}
