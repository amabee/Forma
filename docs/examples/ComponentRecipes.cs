using Forma.Core.Controls;
using FormaForm = Forma.Core.Form;

namespace Forma.Documentation;

// Model recipes for a C# host; they do not start a window or replace Builder Preview.
public static class ComponentRecipes
{
    public static FormaForm Pickers()
    {
        var form = new FormaForm { Title = "Choose paths" };
        var label = new Label { Name = "pathLabel", Text = "Choose a file or folder" };
        var file = new FilePicker { Name = "inputFile", Text = "Choose image", DialogTitle = "Select an image", Filter = "Images|*.png;*.jpg;*.jpeg|All files|*.*" };
        var folder = new FolderPicker { Name = "outputFolder", DialogTitle = "Select output folder" };
        file.SelectedPathChanged += (_, _) => label.Text = file.SelectedPath;
        folder.SelectedPathChanged += (_, _) => label.Text = folder.SelectedPath;
        form.Add(file); form.Add(folder); form.Add(label);
        // Builder supplies native chooser handlers. A custom host handles BrowseRequested.
        return form;
    }

    public static FormaForm Greeting()
    {
        var form = new FormaForm { Name = "form1", Title = "Greeting", Width = 640, Height = 440 };
        var input = new TextBox { Name = "nameInput", Text = "", X = 20, Y = 20 };
        var label = new Label { Name = "greetingLabel", Text = "Welcome", X = 20, Y = 70 };
        var button = new Button { Name = "greetButton", Text = "Say hello", X = 20, Y = 120 };
        button.Click += (_, _) => label.Text = $"Hello, {input.Text ?? "friend"}!";
        input.TextChanged += (_, _) => label.Text = input.Text;
        form.Add(input); form.Add(label); form.Add(button);
        return form;
    }

    public static FormaForm Layouts()
    {
        var form = new FormaForm { Title = "Layouts" };
        var card = new Card { Name = "profileCard", Text = "Account", Description = "Edit your profile" };
        card.Add(new TextBox { Text = "", X = 10, Y = 10 });
        var split = new SplitContainer { Orientation = "horizontal", Gap = 8 };
        split.Add(new Label { Text = "Left pane", LayoutSlot = 1 });
        split.Add(new Label { Text = "Right pane", LayoutSlot = 2 });
        var tabs = new TabControl { Tabs = ["Profile", "Settings"], Orientation = "vertical" };
        tabs.Add(new Label { Text = "First page", LayoutSlot = 1 });
        tabs.Add(new CheckBox { Text = "Notifications", LayoutSlot = 2 });
        var flow = new FlowLayoutPanel { Orientation = "horizontal", Gap = 8 };
        flow.Add(new Button { Text = "One" }); flow.Add(new Button { Text = "Two" });
        var table = new TableLayoutPanel { Columns = 2, RowCount = 2, Gap = 8 };
        table.Add(new Label { Text = "Name", LayoutSlot = 1 });
        table.Add(new TextBox { Text = "", LayoutSlot = 2 });
        form.Add(card); form.Add(split); form.Add(tabs); form.Add(flow); form.Add(table);
        return form;
    }

    public static FormaForm InputsAndData()
    {
        var form = new FormaForm { Title = "Inputs and data" };
        var numeric = new NumericUpDown { Minimum = 0, Maximum = 20, Increment = 1, Value = 5 };
        var progress = new ProgressBar { Maximum = 20, Value = 5 };
        numeric.ValueChanged += (_, _) => progress.Value = numeric.Value;
        var choice = new ComboBox { Items = ["Small", "Medium", "Large"], SelectedIndex = 1 };
        var checkedList = new CheckedListBox { Items = ["Email", "SMS"], CheckedIndices = [0] };
        checkedList.SetItemChecked(1, true);
        var grid = new DataGridView { Columns = ["Name", "Count"], Rows = [["Alpha", "10"], ["Beta", "2"]], ReadOnly = false, SortColumn = 1 };
        grid.RowSelectionChanged += (_, _) => form.Title = $"Selected source row {grid.SelectedRow}";
        grid.SetCell(0, 1, "11");
        var tree = new TreeView { Nodes = [new TreeNode("root", "Projects", [new TreeNode("forma", "Forma")])], ExpandedNodes = ["root"] };
        var properties = new PropertyGrid { Entries = [new PropertyEntry("Name", "Forma", "General"), new PropertyEntry("Version", "0.1", "General", true)] };
        properties.SetEntryValue(0, "Forma Builder");
        var rich = new RichTextBox { Document = [new RichBlock("paragraph", [new RichRun("Welcome", Bold: true)])] };
        foreach (var control in new Control[] { numeric, progress, choice, checkedList, grid, tree, properties, rich,
            new DatePicker { DateValue = "2026-10-04" }, new TimePicker { DateValue = "14:30" },
            new DateTimePicker { DateValue = "2026-10-04T14:30" }, new ColorPicker { Color = "#2878ff" } }) form.Add(control);
        return form;
    }

    public static FormaForm FeedbackAndCommands()
    {
        var form = new FormaForm { Title = "Feedback" };
        var toast = new Toast { Name = "savedToast", Text = "Saved", Variant = "success", Position = "top-right", Duration = 3000 };
        var overlay = new LoadingOverlay { Name = "busyOverlay", TargetId = form.Id };
        var button = new Button { Text = "Show notification" };
        button.Click += (_, _) => { overlay.IsActive = false; toast.Show(); };
        var tooltip = new Tooltip { TargetId = button.Id, Text = "Click to show a notification", Placement = "bottom" };
        var dialog = new ConfirmationDialog { DialogTitle = "Delete?", Message = "Remove this item?", Buttons = "YesNo" };
        var menu = new MenuStrip { Items = [new CommandItem("file", "File", Items: [new CommandItem("delete", "Delete")])] };
        menu.ItemClicked += (_, args) => { if (args.Id == "delete") dialog.Show(); };
        foreach (var control in new Control[] { toast, overlay, button, tooltip, dialog, menu,
            new Badge { Text = "Ready", Variant = "success" }, new Avatar { Text = "Ada Lovelace", Initials = "AL", Shape = "circle" },
            new Icon { IconName = "search", StrokeWidth = 2 }, new EmptyState { Text = "No files", Description = "Add your first file", IconName = "folder-open" },
            new Skeleton { Shape = "text", Lines = 3 }, new Divider { Orientation = "horizontal", Thickness = 1 },
            new Spinner { Text = "Loading", IsActive = true, Speed = 800 } }) form.Add(control);
        return form;
    }
}
