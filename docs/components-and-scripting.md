# Using Forma components, properties, and code

This guide describes the current Builder and its 60 toolbox entries. The [component reference](component-reference.md) lists every implemented control, contextual inspector property, range, C# property, constructor default, and declared method/event. The [roadmap](roadmap-stages.md) tracks features still pending.

## Contents

- [Build a form](#build-a-form)
- [Properties and layout](#properties-and-layout)
- [Custom Properties editor](#custom-properties-editor)
- [JavaScript API](#javascript-api)
- [JavaScript recipes](#javascript-recipes)
- [Component families](#component-families)
- [FilePicker and FolderPicker](#filepicker-and-folderpicker)
- [Structured property formats](#structured-property-formats)
- [C# model code](#c-model-code)
- [Events available today](#events-available-today)
- [Troubleshooting and performance](#troubleshooting-and-performance)

## Build a form

1. Run `dotnet run --project src/Forma.Builder` from the repository root. The project uses Windows, .NET 10 and WebView2.
2. Drag from Toolbox to the form, or double-click a toolbox entry to insert it. Search filters the toolbox.
3. Select each control and set a useful **Name**, such as `nameInput` or `greetingLabel`. Generated names use the overall design sequence; a newly added TextBox is not necessarily `textbox1`.
4. Drag visual controls to move them; use selection corners/edges to resize. Nearby alignment/spacing guides help snapping. Canvas zoom changes display scale.
5. Edit Properties on the right. Containers own children dropped into their content area; moving a parent moves its children.
6. Preview opens the running design in a separate resizable/maximizable window. Its tree is a clone: typing and runtime scripts do not change the saved design. Close and reopen Preview to restart with your latest edits.
7. Save a `.forma` file. Open restores the tree, appearance and custom sources. Local images are embedded on save (10 MB per image limit). Undo/Redo history is session-only. Ctrl+Z/Ctrl+Y change the design; focused editable fields retain native text undo.

Timer, BackgroundWorker, Tooltip, Toast, LoadingOverlay, context menus and dialogs appear as named tray items below the form. They do not occupy canvas rectangles but can target visual controls or create runtime UI.

## Properties and layout

| Surface | Examples | Where to change |
| --- | --- | --- |
| Builder inspector / Appearance | Font size, Foreground, Width, Locked | Inspector; visual overrides in custom CSS |
| Runtime JavaScript API | `text`, `value`, `checked`, `selectedTab` | Supported `forma.get` / `forma.set` operations below |
| Core C# model | `Items`, `Rows`, `IconName`, `Description` | Object properties in a C# host project |

These overlap but are not interchangeable. NumericUpDown uses inspector `number`, C# `Value`, and JavaScript `value`. A field appearing in the inspector does not automatically make it a runtime API setter.

**Name** is a script lookup name; **ID** is immutable identity. Lookup accepts an exact Name or ID and requires one match. Names are case-sensitive. A target picker resolves a selected visual control to its ID; C# sets `TargetId = target.Id`.

X/Y are pixels in the parent content area, below headers. Width/Height and min/max dimensions are pixels. LayoutSlot is one-based for panes, tabs and cells. Font size is pixels; Opacity is percent. Per-side margin/padding fields control spacing. Enabled/Visible affect Preview; controls stay selectable in Design. Locked prevents designer edits. Contextual layer actions change stacking; custom Z-index is inside Custom Properties.

FlowLayoutPanel uses child order/orientation instead of free X/Y; drag to reorder. TableLayoutPanel uses Columns/Rows and cell placement. SplitContainer horizontal means left/right panes, vertical means top/bottom; splitter dragging is pending. TabControl supports horizontal/vertical headers; use Add tab, select a page, then drop its children. Tab selected index is zero-based; a child's LayoutSlot is one-based.

## Custom Properties editor

Select a component → **Advanced → Custom Properties**. Edit:

- **CSS:** scoped visual overrides using `:host`.
- **Behavior:** JavaScript executed once per Preview start.
- **Custom JSON:** an object available as `component.properties`.

The built-in editor docks beneath the canvas. Drag its top divider to resize. Use Ctrl+Space for Forma API, component-name, supported-property and custom-value suggestions. Syntax diagnostics show underlines, gutter markers and a Problems list. Unknown literal component names and unsupported runtime properties show warnings; dynamic values and behavior still need Preview testing.

The built-in editor has line numbers, syntax colors, bracket matching, folding, indentation and search (Ctrl+F). Format / Ctrl+Shift+F formats the current tab. Format on save (enabled by default) formats all three sources before saving; turn it off to skip formatting. Syntax errors stop formatting without replacing your text.

Save / Ctrl+S automatically applies sources. Open external editor uses detected VS Code or an executable chosen through Choose editor. External saves auto-apply while the component editor remains open. Invalid JSON keeps the last valid version. Reopen Preview after behavior changes because an existing Preview owns an independent copy.

Files are `component.css`, `behavior.js`, `custom-properties.json`. Saved designs use `<project-name>.components/<component-id>` beside the project. Unsaved designs use `%LOCALAPPDATA%/Forma/ComponentEditors`. Applied sources are also embedded in `.forma`.

Custom JSON must be an object:

```json
{ "greeting": "Hello", "clicks": 0 }
```

```js
forma.on("click", () => {
  component.properties.clicks += 1;
  forma.set("greetingLabel", "text", `${component.properties.greeting}: ${component.properties.clicks}`);
});
```

Runtime JSON changes reset when Preview restarts. `component.characteristics` remains a compatibility alias for the same object.

```css
:host { font-size: 20px; color: #1f2937; }
:host:hover { background-color: #eff6ff; }
:host input { border-color: #2878ff; }
```

Every selector must begin with `:host`. Regular rules, `@media` and `@supports` are supported; global selectors and standalone keyframes are not. Declarations take priority over inspector appearance. Boilerplate copies current styles: remove/edit a declaration if you want subsequent inspector changes to control it. Toast/LoadingOverlay popup CSS also matches its source component's `:host`. Keep geometry in Layout properties.

## JavaScript API

Behavior is JavaScript, not C#. It receives `forma` and `component`; `api` remains
a compatibility alias. It does not directly receive Core model objects or the WinForms window.

For live values declared outside events, use `const name = forma.bind("nameInput", "value")`
and read `name.value`. `forma.ref`, `forma.reactive`, `forma.computed`,
`forma.effect`, and `forma.watch` support local reactive state and automatic
updates. See [reactive state and bindings](component-customization.md#reactive-state-and-bindings)
for examples, subscriptions, cleanup, and write semantics.

| API | Purpose |
| --- | --- |
| `forma.on(event, handler)` | Listen on this component DOM element; automatic listener cleanup |
| `forma.get(nameOrId, property)` | Read one of the supported properties below |
| `forma.set(nameOrId, property, value)` | Send a supported update to the cloned Preview model |
| `forma.find(nameOrId)` | Get the matched DOM element for advanced local work |
| `forma.showDialog(nameOrId)` | Open Dialog/ConfirmationDialog |
| `forma.showToast(nameOrId)` / `forma.closeToast(nameOrId)` | Notification lifecycle |
| `forma.cleanup(callback)` | Register cleanup for timers, extra listeners or resources |

`component.id`, `component.name`, `component.element`, `component.properties` describe the current component. `forma.on` listeners run while the source is Enabled and Visible. Sync/async handler errors are reported in the Preview title. Behavior never runs in Design.

### Runtime property operations

Use exact camelCase keys. `forma.set` uses the bridge; it is not a synchronous DOM assignment. A getter immediately following a setter in the same callback may see the previous state. Keep the new value in a local variable if you need it immediately.

| Key | `forma.get` support | `forma.set` support | Value |
| --- | --- | --- | --- |
| `source` | Image/PictureBox/Avatar image source | Image/PictureBox/Avatar | Absolute local file path, file/http/https URL, image data URI; empty string clears |
| `selectedPath` | FilePicker/FolderPicker displayed selected path | Not supported | String; read after Browse finishes |
| `text` | Display/state text; use `value` for live input | All models | String, max 32767 characters |
| `enabled`, `visible` | All controls | All controls; enabling Timer also starts/stops it | Boolean |
| `checked` | CheckBox, RadioButton, ToggleSwitch, ToggleButton | Same types | Boolean |
| `value` | Live text inputs; NumericUpDown/Slider and native ProgressBar numbers; native date/time/color input values | NumericControl descendants, DateTimeInput descendants, ColorPicker | Finite number or correctly formatted string |
| `selectedIndex` | ComboBox/ListBox native selects; ListView has no equivalent DOM getter | ComboBox/ListBox/ListView | Zero-based integer; -1 clears |
| `selectedTab` | TabControl | TabControl | Zero-based integer |
| `selectedRow` | DataGridView | DataGridView | Original Rows index; -1 clears |
| `columns` | DataGridView | DataGridView | Array of header strings |
| `rows` | DataGridView | DataGridView | Array of string-cell arrays |
| `readOnly`, `sortingEnabled`, `filteringEnabled` | DataGridView | DataGridView | Boolean |
| `filterText` | DataGridView | DataGridView | String |
| `sortColumn` | DataGridView | DataGridView | Zero-based column; -1 unsorted |
| `sortDirection` | DataGridView | DataGridView | `ascending`, `descending` |
| `isActive` | Spinner, LoadingOverlay | Spinner, LoadingOverlay | Boolean |

For a textbox: **read `value`, write `text`**. `forma.set("nameInput", "value", "...")` is not supported. Numeric setters include ProgressBar/CircularProgress. Date/time formats are below.

DataGridView supports `columns` (string array), `rows` (array of string arrays),
`readOnly`, `sortingEnabled`, and `filteringEnabled` through get/set. Use its row
helpers to append, update, remove, or clear runtime data. Other collection fields,
font size, variant, targetId, description, iconName, Skeleton active state, page,
nodes and arbitrary model fields are not generic JS setters. Configure them in
the inspector or C#. `forma.get` is not a universal serializer:
CircularProgress/RichTextBox/CheckedListBox do not expose a generic input `.value`.
Unsupported operations should not be inferred from inspector labels.

## JavaScript recipes

### TextBox to Label on click

Name the TextBox `nameInput`, Label `greetingLabel`, and attach this to the **Button**:

```js
forma.on("click", () => {
  const name = String(forma.get("nameInput", "value") ?? "").trim();
  forma.set("greetingLabel", "text", name ? `Hello, ${name}!` : "Enter your name first.");
});
```

[Copyable source](examples/greet-button.js). To fill/reset the TextBox, use `forma.set("nameInput", "text", "")`.

### Live update

Attach this to the **TextBox**:

```js
forma.on("input", () => {
  forma.set("greetingLabel", "text", component.element.value);
});
```

[Copyable source](examples/live-input.js). Debounce expensive work that does not need to happen on each character.

### LoadingOverlay and Toast

Name tray components `busyOverlay` and `savedToast`. Configure overlay Target (blank covers the form), and Toast text/variant/position/duration. Attach to the **Button**:

```js
let pending;
forma.on("click", () => {
  clearTimeout(pending);
  forma.set("busyOverlay", "isActive", true);
  pending = setTimeout(() => {
    forma.set("busyOverlay", "isActive", false);
    forma.showToast("savedToast");
  }, 1000);
});
forma.cleanup(() => clearTimeout(pending));
```

[Copyable source](examples/busy-button.js). The delay demonstrates an indicator; replace it with your operation. Overlay blocks pointer interaction over its target; it is not a full keyboard-focus modal. Showing an already-open Toast does not restart its timeout; close then show for a fresh display.

### Selection, visibility, progress

```js
forma.on("click", () => {
  forma.set("greetingLabel", "visible", !forma.get("greetingLabel", "visible"));
  forma.set("notifications", "checked", true);
  forma.set("settingsTabs", "selectedTab", 1); // second page
  forma.set("workProgress", "value", 75);
});
```

Use your actual Names for label, checkbox/toggle, tabs and progress bar.

### DataGrid selection

Attach to **DataGridView**:

```js
forma.on("row-selection", event => {
  const row = event.detail.row;
  forma.set("rowLabel", "text", row < 0 ? "No row selected" : `Source row: ${row}`);
});
```

[Copyable source](examples/grid-selection.js). Rows remain identified by their original source indices after filtering/sorting. Another component can set the grid's filterText/sortColumn/sortDirection/selectedRow. Sorting/filtering does not reorder saved Rows.

### Timer and dialog

Configure a tray Timer Interval and Enabled; put this on the **Timer**:

```js
forma.on("tick", () => {
  forma.set("clockLabel", "text", new Date().toLocaleTimeString());
});
```

Put `forma.on("click", () => forma.showDialog("confirmDelete"));` on a Button. Configure the named Dialog's message/buttons. A JS promise returning the dialog result is not implemented; C# can subscribe to Closed. The planned event editor is separate future work.

## Component families

### Text, buttons, links and choices

- **Button:** Text, Style preset, common appearance; click behavior.
- **Label:** display Text; update through `text`.
- **TextBox/SearchBox/PasswordBox/TextArea:** starting Text, Placeholder, Read only, Password, Max length. Read live `value`. Password obscures presentation, not storage encryption.
- **MaskedTextBox:** `0`=digit, `L`=ASCII letter, `A`=letter/digit, `*`=non-underscore character; others are literals. Incomplete slots are `_`. Example `000-0000`; Complete is read-only. Optional-mask syntax is pending.
- **RichTextBox:** Edit content / Finish enables the formatting toolbar. Document supports paragraph/bullet/number blocks and bold/italic/underline runs. Paste imports plain text; plain Text replaces formatting. Arbitrary HTML/RTF import is pending.
- **LinkLabel:** HTTP/HTTPS URL; Preview hands clicks to the native host/default browser. C# exposes LinkClicked.
- **CheckBox/RadioButton/ToggleSwitch/ToggleButton:** Checked boolean. User radio interaction groups siblings with the same parent; explicitly set group states for programmatic changes.
- **ComboBox/ListBox/ListView:** Items, Selected index; zero-based, -1 none. ListView is a list-style widget, not a multi-column grid.
- **CheckedListBox:** Items and comma-separated zero-based Checked indices, e.g. `0, 2`; C# SetItemChecked raises ItemCheck. No generic JS checked-array setter.

### Numeric, dates and loading

NumericUpDown/Slider: Minimum, Maximum, Increment, Number. ProgressBar/CircularProgress: Minimum, Maximum, Number. Numeric values clamp to range; C# uses double Value. CircularProgress displays a percentage. JS `value` setters also update progress displays.

DatePicker uses `YYYY-MM-DD`, TimePicker `HH:mm`, DateTimePicker `YYYY-MM-DDTHH:mm` without timezone suffix. Empty string clears them. ColorPicker uses `#RRGGBB`. C# properties are DateValue and Color; JavaScript reads/sets formatted string `value`.

Spinner: Active and speed (100–5000 ms). Skeleton: Shape text/rectangle/circle, Lines (1–10 for text), Active; rectangle/circle show one placeholder. They animate only in Preview and respect reduced motion. Your code decides when an operation is finished.

### Containers and display

Panel is free-position; GroupBox adds a caption; Card adds Title/Description/Show header. SplitContainer has two panes; TabControl has names/selected tab/orientation/Add tab; FlowLayoutPanel packs/reorders with Gap; TableLayoutPanel uses Columns/Rows/Gap. C# `parent.Add(child)` owns a child; remove it from the old parent before reparenting. Parenting does not imply responsive docking.

Image/PictureBox/Avatar use **Choose image**; Source is read-only in the inspector. Clear image removes it. Contain fits; cover crops to fill; fill stretches. Local images embed on save. C# Source accepts file/data/loadable URL strings. In JavaScript, use `forma.set("userAvatar", "source", imagePathValue)` to load an image. `text` changes the name/fallback initials. Local paths are converted to file URLs in Preview; paths must exist and URLs must load. For a TextBox path, read `forma.get("imagePathInput", "value")`. For a FilePicker path, read `forma.get("inputFile", "selectedPath")` after browsing. Example on a separate Button:

```js
forma.on("click", () => {
  const imagePathValue = forma.get("inputFile", "selectedPath");
  forma.set("userAvatar", "source", imagePathValue);
});
```

Avatar: Text (accessible name/automatic initials), optional Initials (up to four characters), Shape circle/rounded/square, image fields. Failed/empty images show initials. Badge: neutral/info/success/warning/danger Variant; follows inspector font size/weight. Divider: Orientation, Thickness 1–12, solid/dashed/dotted line, optional Text caption.

Icon: image/search/folder-open/square-check/circle/house/settings/lock/calendar/file-plus/list/x; Foreground sets SVG color, Stroke width 1–4. EmptyState: Text title, Description, Icon. Add a separate action Button if needed.

### Tray feedback and workers

Toast: Text, Variant, corner Position, Duration 500–60000 ms, Allow dismissal. Show/close through JS or C#; open state is transient and notifications stack by corner.

LoadingOverlay: Text, Active, Target; blank Target covers parent (normally form). It lives in the tray and renders only in Preview. JS uses isActive, C# IsActive. It restores target aria-busy on cleanup.

Tooltip: Text, Target, Initial delay 0–10000 ms, Show duration 500–60000 ms, Placement. Hover/focus opens it; Escape/scroll/resize/removal/timeout closes it. Common ToolTip is a simpler native browser title.

Timer: Interval 10–3600000 ms and Enabled; starts in Preview and stops on disposal. BackgroundWorker: progress/cancellation flags, read-only Busy; C# runs work explicitly. Dropping a worker does not start a task, and JS has no RunAsync worker bridge.

### Data, commands and pickers

DataGridView: Columns/Rows, Read only, sorting/filtering flags, Filter text, Sort column/direction, Selected row. Selection/cell edits use original row indices. Virtualization/grouping/binding/column dragging are pending.

TreeView: JSON Nodes with unique IDs, Selected node, comma-separated Expanded nodes. Pagination: Total items/Page size/Page (one-based); your app fetches/changes data on PageChanged. PropertyGrid: categorized string Entries plus whole-grid/per-entry read-only; object binding/typed editors are pending.

MenuStrip/Toolbar/ToolStrip/ContextMenu/ContextMenuStrip: command JSON with nesting, separators, disabled/checkable leaves. Toolbar orientation is configurable; context menus need a Target. StatusBar: left Text/Right text. Implement command actions yourself in C#. Automatic docking/shortcuts are pending.

Dialog/ConfirmationDialog: title, message, Buttons (OK/OKCancel/YesNo/YesNoCancel), Allow cancel, read-only Result. They show messages, not arbitrary child form content. FilePicker/FolderPicker: Selected path, Dialog title; FilePicker also has a Windows Filter string. Builder supplies native dialogs; a custom C# host handles BrowseRequested and assigns SelectedPath.

## FilePicker and FolderPicker

These are visual controls with a path display and Browse button. FilePicker selects a single existing file; FolderPicker selects a directory. Builder supplies the native dialogs in Preview. Inspector Choose file / Choose folder actions set the starting design path and support Undo/Redo.

### Properties

| Inspector field | C# property | Applies to | Usage |
| --- | --- | --- | --- |
| Name | `Name` | Both | Unique script lookup name, e.g. `inputFile` / `outputFolder` |
| Text | `Text` | Both | Browse button caption, e.g. `Choose file…` |
| Selected path | `SelectedPath` | Both | Starting path in Design; selected path in Preview; empty means none |
| Dialog title | `DialogTitle` | Both | Title/description of the native chooser |
| File filter | `Filter` | FilePicker | Label/pattern pairs: `Images\|*.png;*.jpg\|All files\|*.*` |
| Enabled / Visible | Builder Appearance | Both | Whether the picker can be used / shown in Preview |

Filter entries alternate a label and a pattern separated by `|`. Separate multiple extensions with `;`. Examples: `Text files|*.txt`, `Images|*.png;*.jpg;*.jpeg|All files|*.*`. FolderPicker has no extension filter. A path is selection state; choosing it does not read file contents, write a file, or create a directory.

### JavaScript: read both selected paths

Name a FilePicker `inputFile`, a FolderPicker `outputFolder`, and a Label `pathLabel`. In Preview, use each Browse button first, then click a separate Button with this behavior:

```js
forma.on("click", () => {
  const file = forma.get("inputFile", "selectedPath");
  const folder = forma.get("outputFolder", "selectedPath");
  forma.set("pathLabel", "text", `File: ${file || "None"}\nFolder: ${folder || "None"}`);
});
```

[Copyable source](examples/read-picker-paths.js). `forma.get(name, "selectedPath")` reads the displayed path. The setter for selectedPath is not implemented; use inspector/C# to set a path. A native selection does not dispatch a supported JavaScript SelectedPathChanged event yet; read after selection with a separate button. Do not read on the picker's Browse click and expect the new path before the dialog finishes.

### C#: read, configure, and respond to selection

```csharp
var file = new Forma.Core.Controls.FilePicker {
    Name = "inputFile",
    Text = "Choose image",
    DialogTitle = "Select an image",
    Filter = "Images|*.png;*.jpg;*.jpeg|All files|*.*"
};
var folder = new Forma.Core.Controls.FolderPicker {
    Name = "outputFolder",
    DialogTitle = "Select output folder"
};
file.SelectedPathChanged += (_, _) => label.Text = file.SelectedPath;
folder.SelectedPathChanged += (_, _) => label.Text = folder.SelectedPath;
// Read: string selectedFile = file.SelectedPath;
// Clear: file.SelectedPath = "";
```

`label` is your Label model. Add the pickers to the form with `form.Add(file)` / `form.Add(folder)`. The compiled [C# recipes](examples/ComponentRecipes.cs) include this pattern.

In a custom host, `BrowseRequested` asks your code to show a native chooser. For example, in a WinForms host handler, create an OpenFileDialog using `file.DialogTitle` and `file.Filter`; after DialogResult.OK assign `file.SelectedPath = dialog.FileName`. For a folder chooser use FolderBrowserDialog and assign `folder.SelectedPath = dialog.SelectedPath`. `file.RequestBrowse()` / `folder.RequestBrowse()` raises the request; it does not independently create a window. Cancel leaves the previous selection unchanged. Builder already supplies these handlers.

Full field/default tables: [FilePicker reference](component-reference.md#filepicker), [FolderPicker reference](component-reference.md#folderpicker).

## Structured property formats

Items/Tabs/Grid Columns use one item per line. Grid Rows is JSON arrays of string cells:

```json
[["Ada", "10"], ["Grace", "2"]]
```

In JavaScript, DataGridView `columns` and `rows` are actual arrays rather than
inspector strings. Use `forma.set("gridName", "columns", ["Name", "Value"])`
and `forma.set("gridName", "rows", [["Ada", "10"]])` to replace data;
`forma.get` returns copies. Atomic row helpers are `forma.addRow`,
`forma.updateRow`, `forma.removeRow`, `forma.clearRows`, and `forma.setCell`.
See [grid scripting examples](component-customization.md#populate-and-edit-a-datagridview-from-javascript).
Existing scripts using `api` remain compatible with the preferred `forma` name.

Tree nodes use unique IDs (maximum depth 24, 2000 nodes):

```json
[{"Id":"root","Text":"Projects","Children":[{"Id":"forma","Text":"Forma"}]}]
```

PropertyGrid supports up to 200 entries; values are strings:

```json
[
  {"Name":"Name","Value":"Forma","Category":"General"},
  {"Name":"Version","Value":"0.1","Category":"General","ReadOnly":true}
]
```

Command items need stable unique IDs; a disabled parent disables descendants. A checkable leaf toggles then raises the item event:

```json
[
  {"Id":"file","Text":"File","Items":[
    {"Id":"open","Text":"Open"},
    {"Id":"separator","Text":"","Separator":true},
    {"Id":"autosave","Text":"Autosave","CheckOnClick":true,"Checked":false}
  ]}
]
```

Rich document blocks use paragraph/bullet/number and plain-text runs:

```json
[{"Kind":"paragraph","Runs":[{"Text":"Hello ","Bold":true},{"Text":"Forma","Italic":true}]}]
```

## C# model code

C# belongs in a host project, not Behavior. Builder does not generate a C# application or provide a C# script editor yet. [The Demo host](../src/Forma.Demo/Form1.cs) loads WebView2, constructs a bridge and renders Core controls.

```csharp
using Forma.Core.Controls;
var form = new Forma.Core.Form { Title = "Greeting" };
var input = new TextBox { Name = "nameInput", Text = "" };
var label = new Label { Name = "greetingLabel", Text = "Welcome" };
var button = new Button { Text = "Say hello" };
button.Click += (_, _) => label.Text = $"Hello, {input.Text ?? "friend"}!";
input.TextChanged += (_, _) => label.Text = input.Text;
form.Add(input);
form.Add(label);
form.Add(button);
// After your host creates WebView2Bridge:
// var renderer = new Forma.WebView2.WebView2Renderer(bridge);
// await renderer.InitializeAsync();
// await renderer.RenderAsync(form);
```

[Compiled C# recipes](examples/ComponentRecipes.cs) cover greeting, layouts, numeric/progress coupling, choices/checklists, grid/tree/property grid, rich text, dates/colors, feedback/commands and modern display. Validate with `dotnet build docs/examples/Forma.Documentation.Examples.csproj`. Recipes create model trees; the host supplies the window/rendering and styling/appearance.

Builder appearance is `Forma.Builder.Appearance` in `BuilderViewModel.Appearance[control.Id]`. Core models do not all have FontSize/Visible. In Builder code, ExecuteEdit makes undoable edits; its property action targets SelectedControl. Most model setters notify the renderer; auto-properties such as TextBox.ReadOnly do not all notify. Configure before RenderAsync or explicitly `await renderer.UpdateAsync(control)` after a host changes one. Declared fields do not promise every WinForms capability.

Many array getters return copies. Assign a new array instead of mutating returned Items/Rows/Nodes/Entries. Prefer meaningful operations: grid.SetCell (honors ReadOnly), checkedList.SetItemChecked, tree.SetExpanded, propertyGrid.SetEntryValue.

```csharp
using var worker = new Forma.Core.Controls.BackgroundWorker();
worker.ProgressChanged += (_, percent) => progress.Value = percent;
worker.RunWorkerCompleted += (_, result) => { /* result.Error / result.Cancelled */ };
await worker.RunAsync(async (cancellation, report) => {
    for (int i = 0; i <= 100; i += 10) {
        cancellation.ThrowIfCancellationRequested();
        report.Report(i);
        await Task.Delay(50, cancellation);
    }
});
```

Here `progress` is a host-created ProgressBar. Use the UI synchronization context for UI/bridge updates, and dispose workers/timers on shutdown. Core Timer Tick occurs on a worker thread; Builder Preview explicitly marshals bridge work.

## Events available today

The planned Events editor is future work. Scripts use DOM events through forma.on; C# subscribes to model events.

| Source | Current JS event | Current C# result |
| --- | --- | --- |
| Button | click | Click |
| Text inputs | input; live value | TextChanged via SetText |
| Checkbox/radio/toggle | native change; ToggleButton click | CheckedChanged |
| Choices | native select change; ListView clicks bubble | SelectedIndexChanged |
| Numeric/date/time/color | input | ValueChanged |
| Tabs | header click bubbles | SelectedTab notification |
| DataGrid selection | row-selection, event.detail.row | RowSelectionChanged |
| DataGrid cell edit | cell blur | CellChanged, source row/column |
| Tree | clicks | SelectedNodeChanged / expansion notification |
| Pagination | clicks | PageChanged |
| PropertyGrid | native input change | PropertyValueChanged |
| Commands | item clicks | ItemClicked |
| LinkLabel | click | LinkClicked; host opens URL |
| Picker browse | button click | BrowseRequested |
| Timer | tick | Tick |
| Dialog response | popup is outside tray source | Closed with Result |
| Toast dismissal/timeout | popup is outside tray source | Closed |

Popup events do not bubble to their tray source. `forma.on("Closed", ...)` is not a C# event subscription. Only tick and row-selection are currently dispatched as the additional component events above. Declarations such as Button.MouseDown/MouseUp and TextBox.EnterPressed/KeyDown are not all wired through WebView2 yet; use the support table, not declarations alone.

## Troubleshooting and performance

| Symptom | Check |
| --- | --- |
| Control not found | Exact Name/ID, case, uniqueness, generated suffix |
| Input read is stale | Read value / component.element.value, not snapshot text |
| Unsupported property | Runtime table; not every inspector field is a JS setter |
| Preview ignores a design edit | Close/reopen its cloned runtime |
| Font/color inspector ignored | Remove overriding Custom Properties CSS |
| Avatar shows initials | Re-select image; verify source/loadability/format |
| Flow/Table loses X/Y | Managed layout: reorder/change cell or use Panel/Card |
| Toast does not show on drop | Trigger showToast / C# Show; open state is not persisted |
| Overlay does nothing | Active, Enabled, valid Target; blank defaults to form |
| Picker has no dialog in own host | Handle BrowseRequested and assign SelectedPath |
| Array edit has no effect | Copied getter: assign new array / use model edit method |
| Script fails | Inspect Preview title for the reported error |

Optimization now merges repeated state requests, skips unchanged appearance, preserves modern-control DOM across unrelated edits, reuses inspector selection options, and avoids dirty-state serialization on simple selection. A regression test checks 100 controls over 20 unchanged passes without modern refreshes. That is a work-count check, not an end-to-end FPS measurement.

Large grids still render all visible rows; virtualization is pending. Design history still captures snapshots. Resize large source images appropriately, avoid unnecessarily short Timer intervals, debounce expensive input work, and use cleanup for timers/listeners. If lag remains, record control/row counts, image sizes and whether it occurs during dragging, property typing, save/open or Preview so the next profile targets the right path.
