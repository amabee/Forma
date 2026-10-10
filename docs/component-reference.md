# Component and property reference

This reference covers the 72 implemented toolbox entries plus Form. It was checked against the compiled Core models and InspectorCatalog. The larger roadmap is separate. Start with [the usage guide](components-and-scripting.md) for setup, JavaScript recipes, and C# integration.

## Reading the tables

- Inspector IDs are the field names stored in Builder state; C# model properties use PascalCase.
- A property in the inspector is **not automatically supported by `forma.get` or `forma.set`**. See the JavaScript API table in the usage guide.
- Shared visual properties are in Builder `Appearance`, not automatically properties on every Core control.
- C# defaults below are model constructor defaults. Builder may supply different initial geometry, colors, or text when you drop a control.
- Image source is a read-only field filled by picker actions. File/folder selected paths can be edited or chosen. IDs are immutable.
- C# properties without notifications should be configured before rendering, or followed by `renderer.UpdateAsync(control)` when your host changes them.

## Shared inspector properties

Visual controls expose geometry and appearance. Tray components expose the applicable General/Behavior fields and Custom Properties action. The inspector filters these by kind; do not apply a visual field to a tray component.

### Advanced

| ID | Label | Editor | Values / range | Notes |
| --- | --- | --- | --- | --- |
| `cssClass` | CSS class | text | — | Editable |
| `id` | CSS ID | text | — | Read-only |

### Appearance

| ID | Label | Editor | Values / range | Notes |
| --- | --- | --- | --- | --- |
| `backColor` | Background | color | — | Editable |
| `foreColor` | Foreground | color | — | Editable |
| `borderStyle` | Border style | select | `none`, `solid`, `dashed`, `dotted` | Editable; children only |
| `borderColor` | Border color | color | — | Editable; children only |
| `borderWidth` | Border width | number | 0 to 10 | Editable; children only |
| `borderRadius` | Radius | number | 0 to 100 | Editable; children only |
| `opacity` | Opacity (%) | number | 10 to 100 | Editable; children only |
| `shadow` | Shadow | select | `None`, `Small`, `Medium`, `Large` | Editable; children only |
| `cursor` | Cursor | select | `default`, `pointer`, `text`, `crosshair`, `help`, `not-allowed` | Editable; children only |

### Behavior

| ID | Label | Editor | Values / range | Notes |
| --- | --- | --- | --- | --- |
| `enabled` | Enabled | checkbox | — | Editable; children only |
| `visible` | Visible | checkbox | — | Editable; children only |
| `focusable` | Focusable | checkbox | — | Editable; children only |
| `tabIndex` | Tab index | number | 0 to 32767 | Editable; children only |
| `toolTip` | Tooltip | text | — | Editable; children only |

### General

| ID | Label | Editor | Values / range | Notes |
| --- | --- | --- | --- | --- |
| `name` | Name | text | — | Editable |
| `id` | ID | text | — | Read-only |
| `text` | Text / Title | text | — | Editable |
| `tag` | Tag | text | — | Editable |
| `locked` | Locked | checkbox | — | Editable |

### Layout

| ID | Label | Editor | Values / range | Notes |
| --- | --- | --- | --- | --- |
| `x` | X | number | 0 to 1600 | Editable; children only |
| `y` | Y | number | 0 to 1200 | Editable; children only |
| `width` | Width | number | 24 to 1600 | Editable |
| `height` | Height | number | 20 to 1200 | Editable |
| `minimumWidth` | Min width | number | 0 to 1600 | Editable |
| `minimumHeight` | Min height | number | 0 to 1200 | Editable |
| `maximumWidth` | Max width (0=auto) | number | 0 to 1600 | Editable |
| `maximumHeight` | Max height (0=auto) | number | 0 to 1200 | Editable |
| `marginTop` | Margin top | number | 0 to 64 | Editable; children only |
| `marginRight` | Margin right | number | 0 to 64 | Editable; children only |
| `marginBottom` | Margin bottom | number | 0 to 64 | Editable; children only |
| `marginLeft` | Margin left | number | 0 to 64 | Editable; children only |
| `paddingTop` | Padding top | number | 0 to 64 | Editable; children only |
| `paddingRight` | Padding right | number | 0 to 64 | Editable; children only |
| `paddingBottom` | Padding bottom | number | 0 to 64 | Editable; children only |
| `paddingLeft` | Padding left | number | 0 to 64 | Editable; children only |
| `layoutSlot` | Pane / tab / cell | number | 1 to 100 | Editable; children only |

### Typography

| ID | Label | Editor | Values / range | Notes |
| --- | --- | --- | --- | --- |
| `fontFamily` | Font family | select | `Segoe UI`, `Arial`, `Consolas`, `Georgia` | Editable |
| `fontSize` | Font size | number | 8 to 48 | Editable |
| `fontWeight` | Weight | select | `normal`, `bold`, `100`, `200`, `300`, `400`, `500`, `600`, `700`, `800`, `900` | Editable |
| `fontStyle` | Font style | select | `normal`, `italic` | Editable |
| `textAlign` | Alignment | select | `left`, `center`, `right` | Editable |
| `lineHeight` | Line height | number | 0.5 to 4 | Editable |
| `letterSpacing` | Letter spacing | number | -5 to 20 | Editable |

Geometry is in pixels relative to the parent content area. Flow/Table children use layout order/cell placement instead of free X/Y. Font size is pixels. Opacity is percent. Per-side margin/padding fields are the actual appearance spacing. Locked prevents designer edits; Enabled and Visible affect Preview. Custom Properties contains CSS, behavior JavaScript, and custom JSON; its CSS can override inspector appearance.

## Implemented controls

Jump to: [Avatar](#avatar) · [BackgroundWorker](#backgroundworker) · [Badge](#badge) · [Button](#button) · [Card](#card) · [CheckBox](#checkbox) · [CheckedListBox](#checkedlistbox) · [CircularProgress](#circularprogress) · [ColorPicker](#colorpicker) · [ComboBox](#combobox) · [ConfirmationDialog](#confirmationdialog) · [ContextMenu](#contextmenu) · [ContextMenuStrip](#contextmenustrip) · [DataGridView](#datagridview) · [DatePicker](#datepicker) · [DateTimePicker](#datetimepicker) · [Dialog](#dialog) · [Divider](#divider) · [EmptyState](#emptystate) · [FilePicker](#filepicker) · [FlowLayoutPanel](#flowlayoutpanel) · [FolderPicker](#folderpicker) · [Form](#form) · [GroupBox](#groupbox) · [Icon](#icon) · [Image](#image) · [Label](#label) · [LinkLabel](#linklabel) · [ListBox](#listbox) · [ListView](#listview) · [LoadingOverlay](#loadingoverlay) · [MaskedTextBox](#maskedtextbox) · [MenuStrip](#menustrip) · [NumericUpDown](#numericupdown) · [Pagination](#pagination) · [Panel](#panel) · [PasswordBox](#passwordbox) · [PictureBox](#picturebox) · [ProgressBar](#progressbar) · [PropertyGrid](#propertygrid) · [RadioButton](#radiobutton) · [RichTextBox](#richtextbox) · [SearchBox](#searchbox) · [Skeleton](#skeleton) · [Slider](#slider) · [Spinner](#spinner) · [SplitContainer](#splitcontainer) · [StatusBar](#statusbar) · [TabControl](#tabcontrol) · [TableLayoutPanel](#tablelayoutpanel) · [TextArea](#textarea) · [TextBox](#textbox) · [TimePicker](#timepicker) · [Timer](#timer) · [Toast](#toast) · [ToggleButton](#togglebutton) · [ToggleSwitch](#toggleswitch) · [Toolbar](#toolbar) · [ToolStrip](#toolstrip) · [Tooltip](#tooltip) · [TreeView](#treeview)

### Avatar

**Kind:** `avatar` · **Base model:** `Image` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `initials` | Initials (blank=automatic) | text | — | No |
| `shape` | Shape | select | `circle`, `rounded`, `square` | No |
| `source` | Image source | text | — | Yes |
| `sizeMode` | Size mode | select | `contain`, `cover`, `fill` | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Initials` | `String` | `""` | Yes |
| `Shape` | `String` | `"circle"` | Yes |
| `Source` | `String` | `""` | Yes |
| `SizeMode` | `String` | `"cover"` | Yes |

**Declared/inherited C# events:** `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### BackgroundWorker

**Kind:** `backgroundworker` · **Base model:** `Component` · **Designer:** component tray.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `workerReportsProgress` | Report progress | checkbox | — | No |
| `workerSupportsCancellation` | Cancellation | checkbox | — | No |
| `isBusy` | Busy | checkbox | — | Yes |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `IsBusy` | `Boolean` | `false` | No |
| `WorkerReportsProgress` | `Boolean` | `true` | Yes |
| `WorkerSupportsCancellation` | `Boolean` | `true` | Yes |

**Declared C# methods:** `RunAsync(Func`3 work)`; `CancelAsync()`; `Dispose()`.

**Declared/inherited C# events:** `ProgressChanged`, `RunWorkerCompleted`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### Badge

**Kind:** `badge` · **Base model:** `Control` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `variant` | Variant | select | `neutral`, `info`, `success`, `warning`, `danger` | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Variant` | `String` | `"info"` | Yes |

**Declared/inherited C# events:** `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### Button

**Kind:** `button` · **Base model:** `Control` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `style` | Style | select | `Default`, `Primary`, `Secondary`, `Success`, `Warning`, `Danger`, `Custom` | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Enabled` | `Boolean` | `true` | Yes |

**Declared C# methods:** `OnClick()`.

**Declared/inherited C# events:** `Click`, `MouseDown`, `MouseUp`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### Card

**Kind:** `card` · **Base model:** `LayoutContainer` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `description` | Description | textarea | — | No |
| `headerVisible` | Show header | checkbox | — | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Description` | `String` | `"Add content to this card."` | Yes |
| `HeaderVisible` | `Boolean` | `true` | Yes |
| `Orientation` | `String` | `"horizontal"` | Yes |
| `Gap` | `Int32` | `8` | Yes |
| `Columns` | `Int32` | `2` | Yes |
| `Tabs` | `String[]` | `["Tab 1","Tab 2"]` | Yes |
| `RowCount` | `Int32` | `2` | Yes |
| `SelectedTab` | `Int32` | `0` | Yes |

**Declared/inherited C# events:** `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### CheckBox

**Kind:** `checkbox` · **Base model:** `Control` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `checked` | Checked | checkbox | — | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Checked` | `Boolean` | `false` | Yes |

**Declared/inherited C# events:** `CheckedChanged`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### CheckedListBox

**Kind:** `checkedlistbox` · **Base model:** `Control` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `checkedIndices` | Checked indices | text | — | No |
| `items` | Items (one per line) | textarea | — | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Items` | `String[]` | `["Item 1","Item 2","Item 3"]` | Yes |
| `CheckedIndices` | `Int32[]` | `[]` | Yes |

**Declared C# methods:** `SetItemChecked(Int32 index, Boolean value)`.

**Declared/inherited C# events:** `ItemCheck`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### CircularProgress

**Kind:** `circularprogress` · **Base model:** `NumericControl` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `minimum` | Minimum | number | — | No |
| `maximum` | Maximum | number | — | No |
| `number` | Value | number | — | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Minimum` | `Double` | `0` | Yes |
| `Maximum` | `Double` | `100` | Yes |
| `Increment` | `Double` | `1` | Yes |
| `Value` | `Double` | `0` | Yes |

**Declared/inherited C# events:** `ValueChanged`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### ColorPicker

**Kind:** `colorpicker` · **Base model:** `Control` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `color` | Selected color | color | — | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Color` | `String` | `"#2878ff"` | Yes |

**Declared/inherited C# events:** `ColorChanged`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### ComboBox

**Kind:** `combobox` · **Base model:** `ChoiceControl` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `items` | Items (one per line) | textarea | — | No |
| `selectedIndex` | Selected index | number | -1 to 10000 | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Items` | `String[]` | `["Item 1","Item 2","Item 3"]` | Yes |
| `SelectedIndex` | `Int32` | `0` | Yes |

**Declared/inherited C# events:** `SelectedIndexChanged`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### ConfirmationDialog

**Kind:** `confirmationdialog` · **Base model:** `Dialog` · **Designer:** component tray.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `dialogTitle` | Dialog title | text | — | No |
| `message` | Message | textarea | — | No |
| `buttons` | Buttons | select | `OK`, `OKCancel`, `YesNo`, `YesNoCancel` | No |
| `canCancel` | Allow Escape | checkbox | — | No |
| `result` | Last result | text | — | Yes |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `DialogTitle` | `String` | `"Confirm"` | Yes |
| `Message` | `String` | `"Do you want to continue?"` | Yes |
| `Buttons` | `String` | `"YesNo"` | Yes |
| `CanCancel` | `Boolean` | `true` | Yes |
| `IsOpen` | `Boolean` | `false` | No |
| `Result` | `String` | `""` | No |

**Declared/inherited C# events:** `Closed`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### ContextMenu

**Kind:** `contextmenu` · **Base model:** `CommandControl` · **Designer:** component tray.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `commandItems` | Commands (JSON) | textarea | — | No |
| `targetId` | Target | target | — | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `TargetId` | `String` | `""` | Yes |
| `Items` | `CommandItem[]` | `[{"Id":"action","Text":"Action","Enabled":true,"Checked":false,"CheckOnClick":false,"Separator":false,"Item…` | Yes |

**Declared/inherited C# events:** `ItemClicked`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### ContextMenuStrip

**Kind:** `contextmenustrip` · **Base model:** `ContextMenu` · **Designer:** component tray.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `commandItems` | Commands (JSON) | textarea | — | No |
| `targetId` | Target | target | — | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `TargetId` | `String` | `""` | Yes |
| `Items` | `CommandItem[]` | `[{"Id":"action","Text":"Action","Enabled":true,"Checked":false,"CheckOnClick":false,"Separator":false,"Item…` | Yes |

**Declared/inherited C# events:** `ItemClicked`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### DataGridView

**Kind:** `datagridview` · **Base model:** `Control` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `gridColumns` | Columns (one per line) | textarea | — | No |
| `gridRows` | Rows (JSON) | textarea | — | No |
| `readOnly` | Read only | checkbox | — | No |
| `sortingEnabled` | Allow sorting | checkbox | — | No |
| `filteringEnabled` | Allow filtering | checkbox | — | No |
| `filterText` | Filter text | text | — | No |
| `sortColumn` | Sort column (-1=none) | number | -1 to 99 | No |
| `sortDirection` | Sort direction | select | `ascending`, `descending` | No |
| `selectedRow` | Selected row (-1=none) | number | -1 to 100000 | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `SortingEnabled` | `Boolean` | `true` | Yes |
| `FilteringEnabled` | `Boolean` | `true` | Yes |
| `FilterText` | `String` | `""` | Yes |
| `SortColumn` | `Int32` | `-1` | Yes |
| `SortDirection` | `String` | `"ascending"` | Yes |
| `SelectedRow` | `Int32` | `-1` | Yes |
| `VisibleRowIndices` | `Int32[]` | `[0,1]` | No |
| `Columns` | `String[]` | `["Name","Value"]` | Yes |
| `Rows` | `String[][]` | `[["First row","1"],["Second row","2"]]` | Yes |
| `ReadOnly` | `Boolean` | `true` | Yes |

**Declared C# methods:** `SetCell(Int32 row, Int32 column, String value)`.

**Declared/inherited C# events:** `RowSelectionChanged`, `CellChanged`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### DatePicker

**Kind:** `datepicker` · **Base model:** `DateTimeInput` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `dateValue` | Value | date | — | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `DateValue` | `String` | `""` | Yes |

**Declared/inherited C# events:** `ValueChanged`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### DateTimePicker

**Kind:** `datetimepicker` · **Base model:** `DateTimeInput` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `dateValue` | Value | datetime-local | — | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `DateValue` | `String` | `""` | Yes |

**Declared/inherited C# events:** `ValueChanged`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### Dialog

**Kind:** `dialog` · **Base model:** `Component` · **Designer:** component tray.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `dialogTitle` | Dialog title | text | — | No |
| `message` | Message | textarea | — | No |
| `buttons` | Buttons | select | `OK`, `OKCancel`, `YesNo`, `YesNoCancel` | No |
| `canCancel` | Allow Escape | checkbox | — | No |
| `result` | Last result | text | — | Yes |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `DialogTitle` | `String` | `"Message"` | Yes |
| `Message` | `String` | `"Your message here."` | Yes |
| `Buttons` | `String` | `"OKCancel"` | Yes |
| `CanCancel` | `Boolean` | `true` | Yes |
| `IsOpen` | `Boolean` | `false` | No |
| `Result` | `String` | `""` | No |

**Declared C# methods:** `Show()`; `Close(String result)`.

**Declared/inherited C# events:** `Closed`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### Divider

**Kind:** `divider` · **Base model:** `Control` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `thickness` | Line thickness | number | 1 to 12 | No |
| `lineStyle` | Line style | select | `solid`, `dashed`, `dotted` | No |
| `orientation` | Orientation | select | `horizontal`, `vertical` | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Orientation` | `String` | `"horizontal"` | Yes |
| `Thickness` | `Int32` | `1` | Yes |
| `LineStyle` | `String` | `"solid"` | Yes |

**Declared/inherited C# events:** `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### EmptyState

**Kind:** `emptystate` · **Base model:** `Control` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `description` | Description | textarea | — | No |
| `iconName` | Icon | select | `image`, `search`, `folder-open`, `square-check`, `circle`, `house`, `settings`, `lock`, `calendar`, `file-plus`, `list`, `x` | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Description` | `String` | `"Add items to see them here."` | Yes |
| `IconName` | `String` | `"folder-open"` | Yes |

**Declared/inherited C# events:** `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### FilePicker

See [FilePicker/FolderPicker usage and JavaScript/C# examples](components-and-scripting.md#filepicker-and-folderpicker).

**Kind:** `filepicker` · **Base model:** `PathPicker` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `selectedPath` | Selected path | text | — | No |
| `dialogTitle` | Dialog title | text | — | No |
| `filter` | File filter | text | — | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Filter` | `String` | `"All files\|*.*"` | Yes |
| `SelectedPath` | `String` | `""` | Yes |
| `DialogTitle` | `String` | `"Choose file"` | Yes |

**Declared/inherited C# events:** `SelectedPathChanged`, `BrowseRequested`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### FlowLayoutPanel

**Kind:** `flowlayoutpanel` · **Base model:** `LayoutContainer` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `orientation` | Orientation | select | `horizontal`, `vertical` | No |
| `gap` | Gap | number | 0 to 64 | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Orientation` | `String` | `"horizontal"` | Yes |
| `Gap` | `Int32` | `8` | Yes |
| `Columns` | `Int32` | `2` | Yes |
| `Tabs` | `String[]` | `["Tab 1","Tab 2"]` | Yes |
| `RowCount` | `Int32` | `2` | Yes |
| `SelectedTab` | `Int32` | `0` | Yes |

**Declared/inherited C# events:** `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### FolderPicker

See [FilePicker/FolderPicker usage and JavaScript/C# examples](components-and-scripting.md#filepicker-and-folderpicker).

**Kind:** `folderpicker` · **Base model:** `PathPicker` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `selectedPath` | Selected path | text | — | No |
| `dialogTitle` | Dialog title | text | — | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `SelectedPath` | `String` | `""` | Yes |
| `DialogTitle` | `String` | `"Choose folder"` | Yes |

**Declared/inherited C# events:** `SelectedPathChanged`, `BrowseRequested`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### Form

**Kind:** `form` · **Base model:** `Control` · **Designer:** visual control.

Uses the shared inspector fields and contextual actions.

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Title` | `String` | `""` | Yes |
| `Width` | `Int32` | `1366` | Yes |
| `Height` | `Int32` | `768` | Yes |

**Declared/inherited C# events:** `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### GroupBox

**Kind:** `groupbox` · **Base model:** `LayoutContainer` · **Designer:** visual control.

Uses the shared inspector fields and contextual actions.

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Orientation` | `String` | `"horizontal"` | Yes |
| `Gap` | `Int32` | `8` | Yes |
| `Columns` | `Int32` | `2` | Yes |
| `Tabs` | `String[]` | `["Tab 1","Tab 2"]` | Yes |
| `RowCount` | `Int32` | `2` | Yes |
| `SelectedTab` | `Int32` | `0` | Yes |

**Declared/inherited C# events:** `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### Icon

**Kind:** `icon` · **Base model:** `Control` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `iconName` | Icon | select | `image`, `search`, `folder-open`, `square-check`, `circle`, `house`, `settings`, `lock`, `calendar`, `file-plus`, `list`, `x` | No |
| `strokeWidth` | Stroke width | number | 1 to 4 | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Names` | `IReadOnlyList`1` | `["image","search","folder-open","square-check","circle","house","settings","lock","calendar","file-plus","l…` | No |
| `IconName` | `String` | `"image"` | Yes |
| `StrokeWidth` | `Int32` | `2` | Yes |

**Declared/inherited C# events:** `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### Image

**Kind:** `image` · **Base model:** `Control` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `source` | Image source | text | — | Yes |
| `sizeMode` | Size mode | select | `contain`, `cover`, `fill` | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Source` | `String` | `""` | Yes |
| `SizeMode` | `String` | `"contain"` | Yes |

**Declared/inherited C# events:** `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### Label

**Kind:** `label` · **Base model:** `Control` · **Designer:** visual control.

Uses the shared inspector fields and contextual actions.

**Declared/inherited C# events:** `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### LinkLabel

**Kind:** `linklabel` · **Base model:** `Control` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `url` | URL | text | — | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Url` | `String` | `"https://example.com/"` | Yes |
| `Visited` | `Boolean` | `false` | Yes |

**Declared C# methods:** `OnLinkClicked()`.

**Declared/inherited C# events:** `LinkClicked`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### ListBox

**Kind:** `listbox` · **Base model:** `ChoiceControl` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `items` | Items (one per line) | textarea | — | No |
| `selectedIndex` | Selected index | number | -1 to 10000 | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Items` | `String[]` | `["Item 1","Item 2","Item 3"]` | Yes |
| `SelectedIndex` | `Int32` | `0` | Yes |

**Declared/inherited C# events:** `SelectedIndexChanged`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### ListView

**Kind:** `listview` · **Base model:** `ChoiceControl` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `items` | Items (one per line) | textarea | — | No |
| `selectedIndex` | Selected index | number | -1 to 10000 | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Items` | `String[]` | `["Item 1","Item 2","Item 3"]` | Yes |
| `SelectedIndex` | `Int32` | `0` | Yes |

**Declared/inherited C# events:** `SelectedIndexChanged`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### LoadingOverlay

**Kind:** `loadingoverlay` · **Base model:** `Component` · **Designer:** component tray.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `isActive` | Active | checkbox | — | No |
| `targetId` | Target | target | — | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `TargetId` | `String` | `""` | Yes |
| `IsActive` | `Boolean` | `false` | Yes |

**Declared/inherited C# events:** `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### MaskedTextBox

**Kind:** `maskedtextbox` · **Base model:** `TextBox` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `placeholder` | Placeholder | text | — | No |
| `readOnly` | Read only | checkbox | — | No |
| `password` | Password | checkbox | — | No |
| `maxLength` | Max length | number | 1 to 32767 | No |
| `mask` | Mask | text | — | No |
| `maskCompleted` | Complete | checkbox | — | Yes |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Mask` | `String` | `"000-0000"` | Yes |
| `MaskCompleted` | `Boolean` | `false` | No |
| `ReadOnly` | `Boolean` | `false` | Yes |
| `Multiline` | `Boolean` | `false` | Yes |
| `Password` | `Boolean` | `false` | Yes |
| `SpellCheck` | `Boolean` | `true` | Yes |
| `MaxLength` | `Int32` | `0` | Yes |
| `MinLength` | `Int32` | `0` | Yes |
| `SelectionStart` | `Int32` | `0` | Yes |
| `SelectionEnd` | `Int32` | `0` | Yes |
| `SelectionLength` | `Int32` | `0` | Yes |

**Declared C# methods:** `SetMaskedText(String text)`; `Format(String text)`.

**Declared/inherited C# events:** `TextChanged`, `EnterPressed`, `EscapePressed`, `GotFocus`, `LostFocus`, `KeyDown`, `KeyUp`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### MenuStrip

**Kind:** `menustrip` · **Base model:** `CommandControl` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `commandItems` | Commands (JSON) | textarea | — | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Items` | `CommandItem[]` | `[{"Id":"file","Text":"File","Enabled":true,"Checked":false,"CheckOnClick":false,"Separator":false,"Items":[…` | Yes |

**Declared/inherited C# events:** `ItemClicked`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### NumericUpDown

**Kind:** `numericupdown` · **Base model:** `NumericControl` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `minimum` | Minimum | number | — | No |
| `maximum` | Maximum | number | — | No |
| `increment` | Step | number | 0.001 to 1000000 | No |
| `number` | Value | number | — | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Minimum` | `Double` | `0` | Yes |
| `Maximum` | `Double` | `100` | Yes |
| `Increment` | `Double` | `1` | Yes |
| `Value` | `Double` | `0` | Yes |

**Declared/inherited C# events:** `ValueChanged`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### Pagination

**Kind:** `pagination` · **Base model:** `Control` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `totalItems` | Total items | number | 0 to 2147483647 | No |
| `pageSize` | Page size | number | 1 to 10000 | No |
| `page` | Page | number | 1 to 2147483647 | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `TotalItems` | `Int32` | `100` | Yes |
| `PageSize` | `Int32` | `10` | Yes |
| `PageCount` | `Int32` | `10` | No |
| `Page` | `Int32` | `1` | Yes |

**Declared/inherited C# events:** `PageChanged`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### Panel

**Kind:** `panel` · **Base model:** `Control` · **Designer:** visual control.

Uses the shared inspector fields and contextual actions.

**Declared/inherited C# events:** `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### PasswordBox

**Kind:** `passwordbox` · **Base model:** `TextBox` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `placeholder` | Placeholder | text | — | No |
| `readOnly` | Read only | checkbox | — | No |
| `password` | Password | checkbox | — | No |
| `maxLength` | Max length | number | 1 to 32767 | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `ReadOnly` | `Boolean` | `false` | Yes |
| `Multiline` | `Boolean` | `false` | Yes |
| `Password` | `Boolean` | `true` | Yes |
| `SpellCheck` | `Boolean` | `true` | Yes |
| `MaxLength` | `Int32` | `0` | Yes |
| `MinLength` | `Int32` | `0` | Yes |
| `SelectionStart` | `Int32` | `0` | Yes |
| `SelectionEnd` | `Int32` | `0` | Yes |
| `SelectionLength` | `Int32` | `0` | Yes |

**Declared/inherited C# events:** `TextChanged`, `EnterPressed`, `EscapePressed`, `GotFocus`, `LostFocus`, `KeyDown`, `KeyUp`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### PictureBox

**Kind:** `picturebox` · **Base model:** `Image` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `source` | Image source | text | — | Yes |
| `sizeMode` | Size mode | select | `contain`, `cover`, `fill` | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Source` | `String` | `""` | Yes |
| `SizeMode` | `String` | `"contain"` | Yes |

**Declared/inherited C# events:** `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### ProgressBar

**Kind:** `progressbar` · **Base model:** `NumericControl` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `minimum` | Minimum | number | — | No |
| `maximum` | Maximum | number | — | No |
| `number` | Value | number | — | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Minimum` | `Double` | `0` | Yes |
| `Maximum` | `Double` | `100` | Yes |
| `Increment` | `Double` | `1` | Yes |
| `Value` | `Double` | `0` | Yes |

**Declared/inherited C# events:** `ValueChanged`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### PropertyGrid

**Kind:** `propertygrid` · **Base model:** `Control` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `entries` | Properties (JSON) | textarea | — | No |
| `readOnly` | Read only | checkbox | — | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Entries` | `PropertyEntry[]` | `[{"Name":"Name","Value":"Example","Category":"General","ReadOnly":false},{"Name":"Enabled","Value":"True","…` | Yes |
| `ReadOnly` | `Boolean` | `false` | Yes |

**Declared C# methods:** `SetEntryValue(Int32 index, String value)`.

**Declared/inherited C# events:** `PropertyValueChanged`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### RadioButton

**Kind:** `radiobutton` · **Base model:** `CheckBox` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `checked` | Checked | checkbox | — | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Checked` | `Boolean` | `false` | Yes |

**Declared/inherited C# events:** `CheckedChanged`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### RichTextBox

**Kind:** `richtextbox` · **Base model:** `Control` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `document` | Rich document (JSON) | textarea | — | No |
| `readOnly` | Read only | checkbox | — | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Text` | `String` | `"Write something..."` | Yes |
| `Document` | `RichBlock[]` | `[{"Kind":"paragraph","Runs":[{"Text":"Write something...","Bold":false,"Italic":false,"Underline":false}]}]` | Yes |
| `ReadOnly` | `Boolean` | `false` | Yes |

**Declared C# methods:** `SetPlainText(String text)`.

**Declared/inherited C# events:** `TextChanged`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### SearchBox

**Kind:** `searchbox` · **Base model:** `TextBox` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `placeholder` | Placeholder | text | — | No |
| `readOnly` | Read only | checkbox | — | No |
| `password` | Password | checkbox | — | No |
| `maxLength` | Max length | number | 1 to 32767 | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `ReadOnly` | `Boolean` | `false` | Yes |
| `Multiline` | `Boolean` | `false` | Yes |
| `Password` | `Boolean` | `false` | Yes |
| `SpellCheck` | `Boolean` | `true` | Yes |
| `MaxLength` | `Int32` | `0` | Yes |
| `MinLength` | `Int32` | `0` | Yes |
| `SelectionStart` | `Int32` | `0` | Yes |
| `SelectionEnd` | `Int32` | `0` | Yes |
| `SelectionLength` | `Int32` | `0` | Yes |

**Declared/inherited C# events:** `TextChanged`, `EnterPressed`, `EscapePressed`, `GotFocus`, `LostFocus`, `KeyDown`, `KeyUp`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### Skeleton

**Kind:** `skeleton` · **Base model:** `Control` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `shape` | Shape | select | `text`, `rectangle`, `circle` | No |
| `lines` | Lines | number | 1 to 10 | No |
| `isActive` | Active | checkbox | — | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Shape` | `String` | `"text"` | Yes |
| `Lines` | `Int32` | `3` | Yes |
| `IsActive` | `Boolean` | `true` | Yes |

**Declared/inherited C# events:** `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### Slider

**Kind:** `slider` · **Base model:** `NumericControl` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `minimum` | Minimum | number | — | No |
| `maximum` | Maximum | number | — | No |
| `increment` | Step | number | 0.001 to 1000000 | No |
| `number` | Value | number | — | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Minimum` | `Double` | `0` | Yes |
| `Maximum` | `Double` | `100` | Yes |
| `Increment` | `Double` | `1` | Yes |
| `Value` | `Double` | `0` | Yes |

**Declared/inherited C# events:** `ValueChanged`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### Spinner

**Kind:** `spinner` · **Base model:** `Control` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `isActive` | Active | checkbox | — | No |
| `speed` | Animation speed (ms) | number | 100 to 5000 | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `IsActive` | `Boolean` | `true` | Yes |
| `Speed` | `Int32` | `800` | Yes |

**Declared/inherited C# events:** `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### SplitContainer

**Kind:** `splitcontainer` · **Base model:** `LayoutContainer` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `orientation` | Orientation | select | `horizontal`, `vertical` | No |
| `gap` | Gap | number | 0 to 64 | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Orientation` | `String` | `"horizontal"` | Yes |
| `Gap` | `Int32` | `8` | Yes |
| `Columns` | `Int32` | `2` | Yes |
| `Tabs` | `String[]` | `["Tab 1","Tab 2"]` | Yes |
| `RowCount` | `Int32` | `2` | Yes |
| `SelectedTab` | `Int32` | `0` | Yes |

**Declared/inherited C# events:** `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### StatusBar

**Kind:** `statusbar` · **Base model:** `Control` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `rightText` | Right text | text | — | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `RightText` | `String` | `""` | Yes |

**Declared/inherited C# events:** `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### TabControl

**Kind:** `tabcontrol` · **Base model:** `LayoutContainer` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `orientation` | Orientation | select | `horizontal`, `vertical` | No |
| `tabs` | Tabs (one per line) | textarea | — | No |
| `selectedTab` | Selected tab | number | 0 to 99 | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Orientation` | `String` | `"horizontal"` | Yes |
| `Gap` | `Int32` | `8` | Yes |
| `Columns` | `Int32` | `2` | Yes |
| `Tabs` | `String[]` | `["Tab 1","Tab 2"]` | Yes |
| `RowCount` | `Int32` | `2` | Yes |
| `SelectedTab` | `Int32` | `0` | Yes |

**Declared/inherited C# events:** `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### TableLayoutPanel

**Kind:** `tablelayoutpanel` · **Base model:** `LayoutContainer` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `gap` | Gap | number | 0 to 64 | No |
| `columns` | Columns | number | 1 to 12 | No |
| `rowCount` | Rows | number | 1 to 100 | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Orientation` | `String` | `"horizontal"` | Yes |
| `Gap` | `Int32` | `8` | Yes |
| `Columns` | `Int32` | `2` | Yes |
| `Tabs` | `String[]` | `["Tab 1","Tab 2"]` | Yes |
| `RowCount` | `Int32` | `2` | Yes |
| `SelectedTab` | `Int32` | `0` | Yes |

**Declared/inherited C# events:** `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### TextArea

**Kind:** `textarea` · **Base model:** `TextBox` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `placeholder` | Placeholder | text | — | No |
| `readOnly` | Read only | checkbox | — | No |
| `password` | Password | checkbox | — | No |
| `maxLength` | Max length | number | 1 to 32767 | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `ReadOnly` | `Boolean` | `false` | Yes |
| `Multiline` | `Boolean` | `true` | Yes |
| `Password` | `Boolean` | `false` | Yes |
| `SpellCheck` | `Boolean` | `true` | Yes |
| `MaxLength` | `Int32` | `0` | Yes |
| `MinLength` | `Int32` | `0` | Yes |
| `SelectionStart` | `Int32` | `0` | Yes |
| `SelectionEnd` | `Int32` | `0` | Yes |
| `SelectionLength` | `Int32` | `0` | Yes |

**Declared/inherited C# events:** `TextChanged`, `EnterPressed`, `EscapePressed`, `GotFocus`, `LostFocus`, `KeyDown`, `KeyUp`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### TextBox

**Kind:** `textbox` · **Base model:** `Control` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `placeholder` | Placeholder | text | — | No |
| `readOnly` | Read only | checkbox | — | No |
| `password` | Password | checkbox | — | No |
| `maxLength` | Max length | number | 1 to 32767 | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `ReadOnly` | `Boolean` | `false` | Yes |
| `Multiline` | `Boolean` | `false` | Yes |
| `Password` | `Boolean` | `false` | Yes |
| `SpellCheck` | `Boolean` | `true` | Yes |
| `MaxLength` | `Int32` | `0` | Yes |
| `MinLength` | `Int32` | `0` | Yes |
| `SelectionStart` | `Int32` | `0` | Yes |
| `SelectionEnd` | `Int32` | `0` | Yes |
| `SelectionLength` | `Int32` | `0` | Yes |

**Declared C# methods:** `OnTextChanged()`; `SetText(String text)`; `SetSelection(Int32 start, Int32 end)`; `SetFocus()`; `OnEnterPressed()`; `OnEscapePressed()`; `OnGotFocus()`; `OnLostFocus()`; `OnKeyDown(KeyEventArgs e)`; `OnKeyUp(KeyEventArgs e)`.

**Declared/inherited C# events:** `TextChanged`, `EnterPressed`, `EscapePressed`, `GotFocus`, `LostFocus`, `KeyDown`, `KeyUp`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### TimePicker

**Kind:** `timepicker` · **Base model:** `DateTimeInput` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `dateValue` | Value | time | — | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `DateValue` | `String` | `""` | Yes |

**Declared/inherited C# events:** `ValueChanged`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### Timer

**Kind:** `timer` · **Base model:** `Component` · **Designer:** component tray.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `interval` | Interval (ms) | number | 10 to 3600000 | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Interval` | `Int32` | `1000` | Yes |
| `Enabled` | `Boolean` | `false` | Yes |

**Declared C# methods:** `Start()`; `Stop()`; `Dispose()`.

**Declared/inherited C# events:** `Tick`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### Toast

**Kind:** `toast` · **Base model:** `Component` · **Designer:** component tray.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `variant` | Variant | select | `neutral`, `info`, `success`, `warning`, `danger` | No |
| `position` | Position | select | `top-right`, `top-left`, `bottom-right`, `bottom-left` | No |
| `duration` | Duration (ms) | number | 500 to 60000 | No |
| `dismissible` | Allow dismissal | checkbox | — | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Variant` | `String` | `"info"` | Yes |
| `Position` | `String` | `"bottom-right"` | Yes |
| `Duration` | `Int32` | `4000` | Yes |
| `Dismissible` | `Boolean` | `true` | Yes |
| `IsOpen` | `Boolean` | `false` | No |

**Declared C# methods:** `Show()`; `Close()`.

**Declared/inherited C# events:** `Closed`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### ToggleButton

**Kind:** `togglebutton` · **Base model:** `CheckBox` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `checked` | Checked | checkbox | — | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Checked` | `Boolean` | `false` | Yes |

**Declared/inherited C# events:** `CheckedChanged`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### ToggleSwitch

**Kind:** `toggleswitch` · **Base model:** `CheckBox` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `checked` | Checked | checkbox | — | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Checked` | `Boolean` | `false` | Yes |

**Declared/inherited C# events:** `CheckedChanged`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### Toolbar

**Kind:** `toolbar` · **Base model:** `CommandControl` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `commandItems` | Commands (JSON) | textarea | — | No |
| `orientation` | Orientation | select | `horizontal`, `vertical` | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Orientation` | `String` | `"horizontal"` | Yes |
| `Items` | `CommandItem[]` | `[{"Id":"new","Text":"New","Enabled":true,"Checked":false,"CheckOnClick":false,"Separator":false,"Items":nul…` | Yes |

**Declared/inherited C# events:** `ItemClicked`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### ToolStrip

**Kind:** `toolstrip` · **Base model:** `Toolbar` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `commandItems` | Commands (JSON) | textarea | — | No |
| `orientation` | Orientation | select | `horizontal`, `vertical` | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Orientation` | `String` | `"horizontal"` | Yes |
| `Items` | `CommandItem[]` | `[{"Id":"new","Text":"New","Enabled":true,"Checked":false,"CheckOnClick":false,"Separator":false,"Items":nul…` | Yes |

**Declared/inherited C# events:** `ItemClicked`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### Tooltip

**Kind:** `tooltip` · **Base model:** `Component` · **Designer:** component tray.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `targetId` | Target | target | — | No |
| `initialDelay` | Initial delay (ms) | number | 0 to 10000 | No |
| `showDuration` | Duration (ms) | number | 500 to 60000 | No |
| `placement` | Placement | select | `top`, `bottom`, `left`, `right` | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `TargetId` | `String` | `""` | Yes |
| `InitialDelay` | `Int32` | `500` | Yes |
| `ShowDuration` | `Int32` | `5000` | Yes |
| `Placement` | `String` | `"top"` | Yes |

**Declared/inherited C# events:** `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

### TreeView

**Kind:** `treeview` · **Base model:** `Control` · **Designer:** visual control.

| Inspector ID | Label | Editor | Values / range | Read-only |
| --- | --- | --- | --- | --- |
| `nodes` | Nodes (JSON) | textarea | — | No |
| `selectedNode` | Selected node ID | text | — | No |
| `expandedNodes` | Expanded node IDs | text | — | No |

**C# model properties** (includes properties inherited from the listed base, excluding common Control fields):

| Property | Type | Constructor default | Writable |
| --- | --- | --- | --- |
| `Nodes` | `TreeNode[]` | `[{"Id":"root","Text":"Root","Children":[{"Id":"child","Text":"Child","Children":[]}]}]` | Yes |
| `SelectedNode` | `String` | `""` | Yes |
| `ExpandedNodes` | `String[]` | `["root"]` | Yes |

**Declared C# methods:** `SetExpanded(String id, Boolean expanded)`.

**Declared/inherited C# events:** `SelectedNodeChanged`, `ChildAdded`, `ChildRemoved`. Declared events are not all forwarded by WebView2; see the event support table in the usage guide.

## Common Core model fields

`Id` is an init-only stable identifier; `Name` is the script lookup name; `Text` is display text; `X`/`Y` are nullable positions; `LayoutSlot` is a one-based pane/tab/cell. `Children` and `Parent` describe ownership. `Add`, `Insert`, `Remove` and `MoveChild` manage the tree. Every control has `PropertyChanged`. The base `Value` is a string field; NumericControl shadows it with a double. Input JavaScript should use the guide’s typed runtime API.

## Selection controls and Rating

### RadioGroup

Inherits ChoiceControl through SelectionGroup. `Items` defaults to three items,
`SelectedIndex` defaults to zero (-1 clears), and `Orientation` defaults to
horizontal. Inspector: Items, Selected index, Orientation, and common properties.
JS: items and selectedIndex get/set/bind. C# event: SelectedIndexChanged.

### SegmentedControl

Uses the same model and properties as RadioGroup, rendered as buttons. Supports
arrow keys/Home/End and a single keyboard tab stop for the selected segment.

### CheckBoxGroup

Inherits MultiChoiceControl. `Items` defaults to three items; `CheckedIndices`
defaults to empty and normalizes to unique valid ascending indices. Orientation
defaults to horizontal. Inspector: Items, Checked indices, Orientation, and common
properties. JS: items and checkedIndices get/set/bind. C# method/event:
SetItemChecked(index, checked), ItemCheck. CheckedListBox shares this model base.

### Rating

Inherits NumericControl. Stars defaults to five (1–10); Value defaults to zero
and rounds to whole stars within bounds. ReadOnly defaults to false.
Inspector: Stars, Value, Read only, and common properties. JS: value and readOnly
get/set/bind. C# event: ValueChanged. Delete/Backspace clears to zero in Preview.

## Chips and icon action buttons

### Chip

Inherits CheckBox. Checked defaults false, Removable false, Variant neutral.
Supports neutral/info/success/warning/danger. IsRemoved is transient and omitted
from saved projects. Remove() requires Removable and raises Removed once;
Restore() clears removal. JS get/set/bind supports checked, variant, removable,
isRemoved. Inspector exposes Checked, Variant, and Removable.

### ChipGroup and ButtonGroup

ChipGroup inherits MultiSelectionGroup/MultiChoiceControl: Items, CheckedIndices,
Orientation, ItemCheck and SetItemChecked. ButtonGroup inherits SelectionGroup/
ChoiceControl: Items, SelectedIndex, Orientation and SelectedIndexChanged.
The inspector exposes those choices and orientation; JS uses items plus
checkedIndices or selectedIndex. Defaults match their shared model bases.

### IconButton and FloatingActionButton

IconButton inherits Button. IconName defaults search; ShowText defaults false;
Text defaults Search. FloatingActionButton inherits IconButton, defaults to
file-plus/Add, and has a circular Builder appearance. Icons use Icon.Names.
Inspector: Icon, Show text, and common properties. JS get/set/bind: iconName,
showText, and common properties. Both retain Button.Click/OnClick behavior.

## Command menu buttons

### DropdownButton and SplitButton

DropdownButton inherits CommandControl, with Text=Actions and the default Action
command. SplitButton inherits DropdownButton, defaults Text=Run, and adds
PrimaryEnabled=true, PrimaryClick, and InvokePrimary(). Both inherit Items,
ItemClicked, and InvokeItem(id). Inspector: Commands JSON and common properties;
SplitButton also exposes Primary enabled. JS get/set/bind: commandItems and
primaryEnabled (SplitButton). Events: command-item and primary-click (SplitButton).

### CommandButton

Inherits IconButton/Button. Defaults: Text=Command, IconName=file-plus,
ShowText=true, Description=Perform an action. Inspector includes Description,
Icon, Show text, and common properties. JS get/set/bind supports description,
iconName, showText, and common properties. Click/OnClick retain Button behavior.

## Inspector/model aliases

| Inspector ID | C# model property | JavaScript API when supported |
| --- | --- | --- |
| `number` | `NumericControl.Value` | `value` |
| `dateValue` | `DateTimeInput.DateValue` | `value` |
| `color` | `ColorPicker.Color` | `value` |
| `commandItems` | `CommandControl.Items` | Configure in inspector/C# |
| `gridColumns`, `gridRows` | `DataGridView.Columns`, `Rows` | Inspector/C#; JS `forma.get/set` uses array properties `columns`, `rows` |
| `source` | `Image.Source` | Choose image / configure in C# |
| `rowCount` | `TableLayoutPanel.RowCount` | Configure in inspector/C# |
| `checkedIndices` | `MultiChoiceControl.CheckedIndices` | JS get/set uses an integer array |
| `targetId` | `TargetId` | Inspector resolves names to stable IDs; C# uses the target’s Id |

The current Core LayoutContainer base also exposes shared fields such as Tabs/Columns on its descendants. Only the contextual fields listed above have meaning for that particular rendered layout; for example Card is not a tab control.
