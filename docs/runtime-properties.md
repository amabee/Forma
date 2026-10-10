# Properties in Preview scripts

Use `forma.get(name, property)`, `forma.set(name, property, value)`, or
`forma.bind(name, property)`. Names and stable IDs are accepted. Keys are
camelCase. Code suggestions list supported keys for the chosen component.
Read-only properties can be read/bound but cannot be set.

Setters update the separate Preview tree, never the saved design. Updates cross
the native bridge; an immediate get after set can still return the old value.
Use the value you just assigned locally, or observe a binding for confirmation.
Array getters return defensive copies. Structured objects use camelCase keys.

## Shared appearance and layout

Visual controls support these fields where the inspector exposes them:

| Group | Properties | Value type |
| --- | --- | --- |
| Geometry | x, y, width, height, minimumWidth, minimumHeight, maximumWidth, maximumHeight | Integer pixels; max size 0 means unconstrained |
| Child assignment | layoutSlot | One-based integer pane/tab/cell |
| Free-position layout | dock, anchor | Inspector option strings; layout providers retain control of child placement |
| Colors | backColor, foreColor, borderColor | Six-digit hex string or transparent |
| Border | borderStyle, borderWidth, borderRadius | Inspector option string; integer pixels |
| Typography | fontFamily, fontSize, fontWeight, fontStyle, textAlign, lineHeight, letterSpacing | Inspector option strings; fontSize integer, lineHeight/letterSpacing finite numbers |
| Spacing | marginTop/Right/Bottom/Left, paddingTop/Right/Bottom/Left | Integers, 0–64 |
| Appearance | opacity, shadow, cursor, cssClass | Percent integer; inspector option strings; CSS class string |
| Interaction | focusable, tabIndex, toolTip | Boolean, integer, string |
| Metadata | tag | String; also available on tray components |

Numbers must be numbers, not quoted numeric strings. Numeric fields clamp to
inspector ranges. Invalid types, colors and option strings are rejected before
changing the property. The component reference lists exact ranges and choices.
Form has the applicable shared fields; child-only fields such as x/y are absent.
Dock and Anchor remain supported as described in the usage guide.

Parent layouts retain authority over geometry. X/Y writes are rejected for
stack/table children and docked controls. AppShell, responsive layouts and Dock
can override rendered dimensions. Geometry getters report configured model
bounds, rather than measurements of the rendered DOM. Custom CSS can override
inspector appearance.

```js
forma.set("greeting", "fontSize", 24);
forma.set("greeting", "foreColor", "#16a34a");
forma.set("nameInput", "placeholder", "Full name");
forma.set("nameInput", "maxLength", 80);
forma.set("nameInput", "readOnly", false);
const size = forma.bind("greeting", "fontSize");
size.subscribe(value => console.log("Font size:", value));
```

## Component fields added to scripting

All fields below support get/set/bind unless marked read-only. Existing text,
value, selection, source, Timer, numeric-range and notification APIs continue
to work.

| Component | Writable properties | Read-only properties |
| --- | --- | --- |
| TextBox, SearchBox, PasswordBox, TextArea | placeholder, readOnly, password, maxLength | — |
| MaskedTextBox | Same fields, mask | maskCompleted |
| RichTextBox | readOnly, document | — |
| PropertyGrid | readOnly, entries | — |
| TreeView | nodes, selectedNode, expandedNodes | — |
| Button | style (inspector preset; Custom preserves colors) | — |
| Image, PictureBox | sizeMode | — |
| Avatar | initials, shape, sizeMode | — |
| Card | description, headerVisible | — |
| Badge | variant | — |
| Icon | iconName, strokeWidth | — |
| EmptyState | description, iconName | — |
| Divider | orientation, thickness, lineStyle | — |
| Rating | stars | — |
| TableLayoutPanel | columns, rowCount | — |
| CheckBoxGroup, ChipGroup, Toolbar, ToolStrip | orientation | — |
| Tooltip | targetId, initialDelay, showDuration, placement | — |
| LoadingOverlay, ContextMenu, ContextMenuStrip | targetId | — |
| Dialog, ConfirmationDialog | dialogTitle, message, buttons, canCancel | result, isOpen |
| FilePicker | dialogTitle, filter | selectedPath (existing picker API) |
| FolderPicker | dialogTitle | selectedPath (existing picker API) |
| Pagination | totalItems, pageSize, page | pageCount |
| StatusBar | rightText | — |
| LinkLabel | url (absolute http/https) | — |
| BackgroundWorker | workerReportsProgress, workerSupportsCancellation | isBusy |

TextBox live input is still read using value and written using text. Changing
worker flags does not launch background work. Use showDialog to open a Dialog;
isOpen is a status, not a setter. Target references accept a visual control's
name or ID; get(targetId) returns its stable ID. Empty target uses the component's
existing parent/default targeting behavior.

```js
forma.set("userAvatar", "shape", "square");
forma.set("statusBadge", "variant", "success");
forma.set("helpTip", "targetId", "nameInput");
forma.set("helpTip", "placement", "bottom");
forma.set("pages", "totalItems", 250);
forma.set("pages", "pageSize", 25);
const count = forma.get("pages", "pageCount");
```

## Structured data

Pass arrays directly, not JSON strings. Core model validation still enforces
limits and structure. Invalid arrays leave the previous value intact. Use a
setter after modifying a getter's copy.

```js
forma.set("tree", "nodes", [
  { id: "people", text: "People", children: [
    { id: "angel", text: "Angel", children: [] }
  ] }
]);
forma.set("tree", "expandedNodes", ["people"]);
forma.set("tree", "selectedNode", "angel");

forma.set("settingsGrid", "entries", [
  { name: "Theme", value: "Dark", category: "Appearance", readOnly: false }
]);

forma.set("notes", "document", [
  { kind: "paragraph", runs: [
    { text: "Welcome!", bold: true, italic: false, underline: false }
  ] }
]);
```

Tree node IDs must be unique (2000 nodes, maximum depth 24). Expanded node IDs
are filtered to existing nodes. Rich documents accept paragraph/bullet/number
blocks, up to 500 blocks and 5000 runs, with a 1 MiB total text limit. PropertyGrid
accepts up to 200 string-valued entries. These programmatic setters can update
data on a read-only editor; readOnly controls user editing.

## Design-only fields and aliases

Identity and authoring settings (name, id, locked, custom source editing) are
not runtime setters. Read the current component's identity using component.name
and component.id. Set control-specific values using the API key: value for
numeric/date/color values, columns/rows for DataGridView, and arrays for items,
tabs, document, nodes and entries. Inspector text formats such as gridRows JSON
or one-item-per-line fields do not change the API's typed values.
