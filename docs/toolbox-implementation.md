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

## File and folder pickers, PropertyGrid

The More controls group now includes FilePicker, FolderPicker, and PropertyGrid.
They support drag insertion into containers, sizing, contextual properties,
Save/Open, and design Undo/Redo.

FilePicker and FolderPicker display Selected path with a Browse button. In
Preview, Browse opens the Builder's native file or folder dialog. In Design,
use the inspector's Choose file / Choose folder action, or edit Selected path.
Cancellation leaves the selection unchanged. FilePicker selects one existing
file and supports label/pattern filter pairs, for example
`Images|*.png;*.jpg|All files|*.*`. Both expose Dialog title and use Text for the
Browse button caption. Disabled pickers do not open dialogs in Preview.

Application hosts subscribe to `BrowseRequested`, open their preferred dialog,
and assign `SelectedPath`; `SelectedPathChanged` reports changes. Core does not
depend on native dialogs. Project files store the path and picker configuration;
they do not copy selected file contents or directory contents into the project.
Multiple file selection and file upload are future additions.

PropertyGrid displays categorized name/value rows. Configure Properties (JSON),
for example:

```json
[
  { "Name": "Title", "Value": "My application", "Category": "General", "ReadOnly": false },
  { "Name": "Version", "Value": "1.0", "Category": "General", "ReadOnly": true }
]
```

Preview edits commit on change and raise `PropertyValueChanged` with Index,
Name, and Value. The whole-grid Read only property and individual entry flags
prevent editing. In Design, change the JSON property in the inspector. Entries
are copied and validated, with a maximum of 200 rows and 32,767 characters per
value. Values are strings; automatic object reflection, typed value editors,
and binding to another control are future enhancements.

For your next manual check: choose and cancel a file/folder in Preview, then
use the inspector Choose action in Design and undo it. Edit a PropertyGrid
value in Preview and try its per-row ReadOnly flag.

## Menus, toolbars, and status bars

More controls includes MenuStrip, Toolbar, ToolStrip, and StatusBar. ToolStrip
shares Toolbar behavior under the familiar desktop name. These are controls
inside the designed form, separate from the Builder's own menus and toolbar.
They support normal placement, resizing, styling, Save/Open and design Undo/Redo.
Automatic edge docking is not implemented yet.

MenuStrip and toolbars use Commands (JSON). Items require a unique ID throughout
the tree and support Text, Enabled, Checked, CheckOnClick, Separator, and nested
Items. For example:

```json
[
  { "Id": "file", "Text": "File", "Items": [
    { "Id": "open", "Text": "Open" },
    { "Id": "separator", "Text": "", "Separator": true },
    { "Id": "show-grid", "Text": "Show grid", "CheckOnClick": true }
  ] }
]
```

Disabled parent items also disable their descendants. Clicking a checkable leaf
toggles Checked and raises ItemClicked; application code can call InvokeItem.
The command tree is copied and validated (up to 300 items and eight levels).
In Preview, menus open submenus, Escape closes them, and clicking a leaf closes
the menus. Browser buttons/summaries support native keyboard focus. A visual
item editor, application keyboard shortcuts, item icons and overflow menus are
future enhancements.

## Context menus and dialogs

More controls includes ContextMenu, ContextMenuStrip, Dialog, and
ConfirmationDialog. They appear in the component tray. Nonvisual behavior is
identified by `INonvisualControl`; context menus share `CommandControl` behavior
without requiring inheritance from `Component`.

ContextMenu and ContextMenuStrip use the same Commands (JSON) format as
MenuStrip. Select their Target from the named controls in the inspector, or
leave it as Form (default). Right-click that target in Preview to open its menu.
An explicitly assigned menu on a child takes precedence over its parent's menu.
Escape, outside clicks, and command selection close the popup. Disabled menus
do not open. Deleting a target resets its menu to the form; Undo restores the
assignment. Save/Open preserves targets through stable control IDs.

Dialogs expose Dialog title, Message, Buttons (`OK`, `OKCancel`, `YesNo`, or
`YesNoCancel`), and Allow Escape. Select a dialog in the component tray, enter
Preview, and use Show dialog (Preview) in its inspector. ConfirmationDialog
defaults to Yes/No. Last result and the Builder status line show the selected
button. Returning to Design closes active dialogs.

In application code, call `Dialog.Show()` and subscribe to `Closed` for its
Result. Open state and results are transient and are not written to `.forma`
files; dialog configuration is persisted and design edits are undoable. Core
does not depend on browser DOM: the WebView2 adapter renders native HTML modal
dialogs, which provide browser focus handling. Allow Escape controls user
dismissal; application code can always cancel a dialog with `Close("Cancel")`.

This batch implements message/confirmation dialogs. Arbitrary content trees,
dialog windows, task dialogs, and the system dialog catalog remain future work.

The Builder displays clicked item text/ID in its status line during Preview.
It does not bind a designed form's Open/Save commands to Builder operations.
Application code subscribes to ItemClicked to implement those actions. Toolbar
and ToolStrip also expose horizontal/vertical Orientation.

StatusBar uses Text for its left message and Right text for its right message.
Application code can update both properties at runtime; the browser announces
updates as status text. Multiple status panels and automatic docking remain
future enhancements.
