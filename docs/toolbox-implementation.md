# Toolbox implementation

## More basic controls

The More controls palette adds LinkLabel, MaskedTextBox, and CheckedListBox.
Their properties and state are retained by Save/Open and design Undo/Redo.

LinkLabel exposes URL and raises LinkClicked. The Builder opens HTTP/HTTPS links
in the default browser when clicked during Preview. Design clicks select it.
Application code can subscribe to LinkClicked to decide what happens on a click.

MaskedTextBox exposes Mask and a read-only Complete indicator. Supported mask
tokens are `0` (ASCII digit), `L` (ASCII letter), `A` (ASCII letter or digit),
and `*` (any character except underscore). Other characters are literals.
Underscores mark unfilled slots. `000-0000` formats a seven-digit number;
`LL-00` formats a letter/digit code. An empty mask allows unrestricted text.
In application code, call SetMaskedText to format input; TextChanged reports edits.
Optional slots, escaping mask tokens, and culture-specific masks are future work.

CheckedListBox exposes Items (one per line) and Checked indices (comma-separated,
zero-based, e.g. `0, 2`). Removing items prunes out-of-range indices.
Preview check changes raise ItemCheck with Index and Checked. Application code
can call SetItemChecked or set CheckedIndices; only SetItemChecked raises ItemCheck.

## Practical inputs

The Inputs and progress palette adds NumericUpDown, Slider, ProgressBar,
CircularProgress, ToggleSwitch, ToggleButton, DatePicker, TimePicker,
DateTimePicker, ColorPicker, SearchBox, PasswordBox, and TextArea.
All support drag insertion, selection, sizing, and contextual properties.

Numeric controls expose Minimum, Maximum, Increment, and a numeric Value.
Value is clamped to the bounds; nonfinite values are ignored. Slider and
NumericUpDown preview input raises ValueChanged. Progress indicators display
the configured value; they are not user-editable inputs.
Toggle controls use Checked and CheckedChanged.
Date/time controls use DateValue and ValueChanged: `yyyy-MM-dd`, `HH:mm`, and
`yyyy-MM-ddTHH:mm` respectively. Empty values clear the field; invalid dates
are ignored. Date/time values are local strings with no timezone conversion.
ColorPicker exposes Color as `#RRGGBB` and ColorChanged.
SearchBox, PasswordBox, and TextArea inherit TextBox's TextChanged event.
SearchBox currently provides text input rather than application filtering logic.

For your next Demo contribution, subscribe to Slider.ValueChanged and copy
its Value into a ProgressBar.Value, or use a ToggleSwitch to change the greeting.

The current Builder palette is functional: Panel, GroupBox, SplitContainer,
TabControl, FlowLayoutPanel, TableLayoutPanel, Button, Label, TextBox, CheckBox,
RadioButton, ComboBox, ListBox, Image, Timer, BackgroundWorker, and DataGridView.
The larger catalog in `Forma_Toolbox_Components_Layouts_Controls.md` remains a roadmap.

Drag an item into the form or a container. Double-click inserts into the form.
Panel and GroupBox support free positioning. FlowLayoutPanel wraps children;
TableLayoutPanel arranges them in columns. SplitContainer has two grid panes:
set a child's Layout slot to 1 or 2. TabControl uses newline-separated tab names;
new children belong to the active tab, and Layout slot chooses their page.
Managed layouts determine child positions, so moving children with the mouse
is available in free-position containers. Corner resizing remains available.
Splitter dragging and drag-to-reorder are future additions.

CheckBox/RadioButton expose Checked. RadioButton browser interactions group by
parent container. ComboBox/ListBox expose newline-separated Items and Selected
index. Image offers a Choose image file picker, also accepts a URL or absolute
file path, and supports contain/cover/fill sizing.
DataGridView exposes newline-separated Columns, Rows as JSON arrays, and Read only;
editing cells in Preview updates C# rows. Sorting, data binding and virtualization
are future work.

Timer and BackgroundWorker appear in a nonvisual tray. Enable a Timer in its
inspector and enter Preview to see ticks in the Builder window title. Returning
to Design stops timers. In application code, subscribe to Timer.Tick and call
Start/Stop. Tick runs on a thread-pool thread; dispatch UI work to the UI thread.
BackgroundWorker.RunAsync accepts a task delegate with cancellation and progress;
subscribe to ProgressChanged/RunWorkerCompleted. The Builder exposes its settings;
task/event binding is still application code.

## Styling and icons

The Builder uses locally compiled [Tailwind CSS](https://tailwindcss.com/docs/installation/tailwind-cli).
Edit `src/Forma.Builder/Frontend/input.css`, which composes utilities with `@apply`.
Small rules for dynamic geometry and runtime property values remain necessary.
`DesignerWeb/designer.css` is generated; do not edit it directly.

From `src/Forma.Builder/Frontend`, run `npm ci` then `npm run build`.
On Windows, .NET builds also regenerate assets when these dependencies are installed.
Generated CSS and SVGs are included so running the app needs no Node or internet.
Run JavaScript checks from the repository root with
`node --test tests/WebRuntime/*.test.cjs` after installing frontend dependencies.

Toolbox and toolbar SVGs are downloaded from [Lucide](https://lucide.dev/) through
the pinned `lucide-static` package and copied by `Frontend/build.cjs`.
The local icon directory includes Lucide's license. No runtime icon CDN is used.

For your next contribution: add a small Demo using a CheckBox and ComboBox, wire
their C# events, and make the greeting respond to the selected option.

## Drag guides and layers

Free-position dragging snaps to parent/sibling edges and centers within six
screen pixels, with measurement guides for 8 px and 16 px gaps. Hold Alt to
bypass snapping. Release, Escape, or cancellation clears the guides.
Containers start behind regular controls regardless of insertion order.
Use Bring to front / Send to back in Properties for sibling layer order;
Advanced Z-index can also adjust the order. Layer actions work within a parent.

## Data widgets

ListView is currently a single-selection list using Items and Selected index;
columns, icons, and alternate views are future enhancements.
TreeView uses Nodes JSON with Id, Text, and Children. IDs must be unique;
Selected node ID and comma-separated Expanded node IDs are editable properties.
Preview exposes native buttons for selection and expand/collapse. Application
code uses SelectedNodeChanged and SetExpanded. Node trees are copied and validated
(up to 2,000 nodes and 24 nesting levels).
Pagination exposes Total items, Page size, and a one-based Page. Previous/Next
buttons honor page boundaries. Application code uses PageChanged to update its
own data display; Pagination does not automatically filter another control.
These widgets retain configuration in Save/Open and design Undo/Redo.
