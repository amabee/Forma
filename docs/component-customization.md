# Custom Properties

Select a control or component and open **Advanced → Custom Properties…** in
Properties, or use Code in the activity rail. The editor docks below the canvas and has three source tabs:

- **Styles (CSS):** starts with the component's current appearance and a hover
  rule. `:host` selects the component; `:host input` selects its inputs. All
  selectors must start with `:host`; `@media` and `@supports` are supported.
  Source styles override the inspector's appearance. Keep sizing and position
  in Layout to preserve designer drag/resize geometry.
- **Behavior (JavaScript):** runs once when Preview starts. The boilerplate
  includes the API and a click example. Script and callback errors appear in
  the Preview window title.
- **Custom values (JSON):** an object containing your own values, such as a
  counter, caption, threshold or configuration. Access it with
  `component.properties`. Preview gets its own mutable copy.

For nonvisual components such as Toast and LoadingOverlay, CSS `:host` targets
the visible runtime popup. It does not style the component's designer tray item.
Dialogs, tooltips, and context menus use the same popup-only CSS scope.

Custom CSS and Z-index no longer have separate inspector fields. Set styling and
`z-index` in the Styles tab; the starter CSS includes the current layer value.

The built-in editor uses locally bundled CodeMirror with line numbers, syntax
colors, bracket matching, folding, indentation and search (Ctrl+F). Use **Format**
or **Ctrl+Shift+F** for the current tab. **Format on save** formats all three files
with Prettier before saving; uncheck it to save without formatting. Syntax errors
stop formatting and leave your text intact. The footer shows the cursor location
and unsaved changes. Drag the divider above the code panel, or focus it and use the up/down arrow keys, to resize the panel. The designer and property inspector remain available. The header's Light mode / Dark mode toggle changes both the workspace and code editor and remembers your choice; it does not change component appearance.

Syntax errors in JavaScript, CSS and JSON appear as underlines and gutter markers.
Hover them for the message, or click the Problems count for the problem list.
Warnings catch unknown literal component names, unsupported `forma.get`/`forma.set`
properties and unknown API methods. Dynamic expressions are checked at runtime.
These checks do not execute code or replace Preview testing.

JavaScript suggestions open while typing, or with **Ctrl+Space**. Type `forma.` for
methods, `forma.set("` for component names, and the second argument for supported
properties. For example, `forma.set("userAvatar", "` offers `source`, while
`forma.get("nameInput", "` offers `value`. `component.properties.` suggests keys
from the JSON tab. Arrow keys select suggestions; Enter accepts and Escape closes.
CSS also has the language package's property suggestions. Suggestions are local,
and use this form's component names. They do not provide full C# IntelliSense.

Use the built-in editor directly, or click **Open external editor**. Forma finds
VS Code when installed; **Choose editor…** selects another editor executable.
The chosen editor is remembered for the current Builder session. Sources are
written as `component.css`, `behavior.js`, and `custom-properties.json`.
**Save** or **Ctrl+S** in Forma saves the files and applies the properties in one
step. While the Custom Properties panel is open, saves from the external editor
are detected and applied automatically after a short debounce. There are no
separate Reload or Apply buttons. Each changed valid save records one undo step;
duplicate file notifications do not add edits. Invalid JSON or incomplete file
writes leave the last valid properties active and show an error in the editor.
Close the code panel when finished; unsaved text is not applied. Save the main project
normally to retain its latest properties in the `.forma` file.

Saved projects put editing files alongside the `.forma` file in
`<project-name>.components/<control-id>/`. Unsaved designs use
`%LOCALAPPDATA%/Forma/ComponentEditors/<form-id>/<control-id>/`. Applied sources
are embedded in the `.forma` file, including save/open and undo/redo; those
editing folders are not needed to open or preview the project. Applying again
does not change a currently running preview. Start a new Preview to test it.

## Behavior API

`component` exposes `id`, `name`, `element` (the DOM element), and
`properties`. Other controls are addressed by their unique Name or ID.
Existing scripts using `component.characteristics` remain compatible, and old
`characteristics.json` editing files can still be reloaded.

```javascript
forma.on("click", () => {
  component.properties.clicks += 1;
  forma.set("label1", "text", `Clicked ${component.properties.clicks} times`);
});
```

| API | Purpose |
| --- | --- |
| `forma.on(event, handler)` | Listen on this component; handles callback errors and cleanup |
| `forma.get(nameOrId, property)` | Read supported control values |
| `forma.set(nameOrId, property, value)` | Send a typed change to the runtime model |
| `forma.find(nameOrId)` | Get another control's DOM element |
| `forma.showDialog(nameOrId)` | Open an enabled Dialog or ConfirmationDialog |
| `forma.cleanup(callback)` | Register cleanup for custom listeners/resources |
| `forma.validate()` | Raise Validating, check this component's HTML input constraints, then raise Validated on success |
| `forma.submit()` | Validate this component and raise cancellable Submit; returns true when accepted |
| `forma.reset()` | Raise cancellable Reset; your handler decides which values to restore |
| `forma.addRow(gridName, cells)` | Append a row of string cells |
| `forma.updateRow(gridName, rowIndex, cells)` | Replace one source row |
| `forma.removeRow(gridName, rowIndex)` | Remove a source row and keep selection aligned |
| `forma.clearRows(gridName)` | Remove every row |
| `forma.setCell(gridName, rowIndex, columnIndex, value)` | Update one string cell, including in a read-only grid |

`forma` is the preferred script API name. Existing scripts using `api` continue
to work: both names refer to the same API in the component's JavaScript scope.
The internal `window.forma` object is the WebView bridge; use the local `forma`
name in your scripts, not `window.forma`.

## Reactive state and bindings

`forma.get` still returns a snapshot. For a live value declared outside an event,
use `forma.bind` and read its `.value` when you need it:

```javascript
const firstName = forma.bind("firstName", "value");

forma.on("Click", () => {
  alert(firstName.value);
});
```

Put this on your button. The binding resolves the control's ID when created, so
renaming it in runtime state does not detach the binding. Reads use the current
DOM/model value; typing immediately changes the value you read. Writing
`firstName.value = "Angel"` updates the control through the native bridge.
Text input `value` bindings automatically write the model's `text` property.
Other bindings use the same supported properties as get/set. `selectedPath` is
read-only: change it through the picker. A removed target reads as `undefined`;
writes to a missing target are rejected. Model writes are asynchronous: an
immediate read can show the old value until the host applies the write. Array
getters return copies, just as `forma.get` does.

### Your own reactive values

```javascript
const count = forma.ref(0);
const state = forma.reactive({ prefix: "Clicks" });
const caption = forma.computed(() => `${state.prefix}: ${count.value}`);

forma.effect(() => {
  forma.set("countLabel", "text", caption.value);
});

forma.on("Click", () => {
  count.value++;
});
```

`ref(initial)` holds a local value. Read/write `.value`. `reactive(object)` tracks
assignments to top-level properties of a plain object. It is shallow: replace
nested objects or arrays to notify subscribers, rather than mutating them in
place. Assigning the same object back to a ref does not count as a change.
`computed(callback)` derives a read-only `.value` and updates its dependencies
when the callback takes a different branch. Keep computations synchronous and
free of side effects.

`effect(callback)` runs immediately and reruns when reactive values read during
its synchronous execution change. Reads after `await` are not tracked. Return
a cleanup function to release resources before the next run and on disposal.
The returned function stops the effect manually. Avoid writing unconditionally
to your own dependencies; non-stabilizing feedback loops are stopped and reported.

### Subscribe to changes

```javascript
const firstName = forma.bind("firstName", "value");

const stop = firstName.subscribe((value, previous) => {
  console.log("Name changed:", previous, "→", value);
});

forma.watch(() => firstName.value.trim(), value => {
  forma.set("greetingLabel", "text", value ? `Hello, ${value}!` : "");
}, { immediate: true });

// Call stop() if you want to unsubscribe early.
```

`subscribe` is available on refs, bindings, and computed values. `forma.watch`
accepts one of those values or a getter function. By default, callbacks run only
after a value changes. `{ immediate: true }` also invokes the callback with the
initial value and `undefined` as the previous value. `watch` returns a stop
function. Callbacks receive `(value, previous)`; reactive reads inside callbacks
do not add new dependencies to the watch.

Bindings notify from input/change events and runtime state refreshes. Equivalent
array data and unchanged refreshes do not trigger repeated callbacks. DOM changes
made directly by a script without an input/change event may require dispatching
that event to notify subscribers; live `.value` reads still see the current DOM.
State belongs to the component's behavior, rather than being shared automatically
across scripts. Effects, watchers, and binding subscriptions are disposed when
its behavior is replaced, removed, or Preview ends. No polling timers are used.
Reading `.value` into a normal variable produces a snapshot again, so keep the
ref/binding itself outside the event.

Supported model setters: `text`, `enabled`, `visible`, `checked`, `value`,
`selectedIndex`, and `selectedTab`. Value/index/checked setters require an
appropriate control type. Changes cross the WebView2 bridge asynchronously;
an immediate `get()` after `set()` may still read the earlier model state.
DOM input values can be read directly from `component.element` inside input
events. Custom values are useful for immediate local state such as counters.

DataGridView also supports `columns`, `rows`, `readOnly`, `sortingEnabled`,
`filteringEnabled`, `selectedRow`, `filterText`, `sortColumn`, and
`sortDirection` through get/set. Use `forma.on("row-selection", event => { ... })`
to react after selection changes; `event.detail.row` is the original Rows index.

### Populate and edit a DataGridView from JavaScript

Name your grid `employeesGrid`. In the form's JavaScript tab, initialize it:

```javascript
forma.on("Load", () => {
  forma.set("employeesGrid", "columns", ["Name", "Department"]);
  forma.set("employeesGrid", "rows", [
    ["Angel", "Engineering"],
    ["Jane", "Design"]
  ]);
});
```

In an Add button's JavaScript tab, read two textboxes and append a row:

```javascript
forma.on("Click", () => {
  const name = forma.get("nameInput", "value").trim();
  const department = forma.get("departmentInput", "value").trim();
  if (!name) return;
  forma.addRow("employeesGrid", [name, department]);
});
```

Other operations:

```javascript
forma.updateRow("employeesGrid", 0, ["Angel", "HR"]);
forma.setCell("employeesGrid", 0, 1, "Engineering");
forma.removeRow("employeesGrid", 0);
forma.clearRows("employeesGrid");

const rows = forma.get("employeesGrid", "rows");
const columns = forma.get("employeesGrid", "columns");
```

Columns and cells must be strings; convert numbers explicitly with `String(value)`.
Rows use arrays of string arrays. Getters return copies: modifying them does not
change the grid until you call `forma.set`. Row and column indices start at zero;
row indices refer to the original data order, even when sorting/filtering changes
the displayed order. Invalid indices or cell types are rejected. Removing the
selected row clears selection; removing an earlier row keeps the same logical
row selected. ReadOnly prevents user cell editing but permits script updates.

Commands cross the native bridge asynchronously. An immediate `forma.get` after
a write may return the previous state. Use addRow/updateRow/removeRow/clearRows/
setCell for sequential edits: each command is applied to the latest runtime
data, so consecutive appends do not lose rows. `forma.set(..., "rows", rows)`
deliberately replaces the entire data set. Preview edits do not modify the saved
design; use inspector Columns/Rows for starting data that should be saved.

Timer components support `forma.on("tick", handler)` and their Enabled setting.
Listeners registered through the API survive runtime state updates without
being installed twice, and are cleaned up when the session ends. Native
file/folder pickers, tabs, and the existing control events continue to work.

## JavaScript events

Register events in a component's JavaScript tab under **Custom Properties**.
Names are case insensitive: `Click` and `click` register the same event.
`DoubleClick` maps to browser `dblclick`, and `MouseWheel` maps to `wheel`;
the browser names also work. Event suggestions are available inside `forma.on`.

| Group | Events |
| --- | --- |
| Mouse | Click, DoubleClick, MouseDown, MouseUp, MouseMove, MouseEnter, MouseLeave, MouseOver, MouseOut, MouseWheel |
| Keyboard and input | KeyDown, KeyUp, KeyPress, Input, Change, BeforeInput |
| Focus | Focus, Blur, FocusIn, FocusOut |
| Lifecycle | Created, Mounted, Ready, Load, Updated, Destroyed |
| Geometry | Resize, Move, Layout |
| Drag and drop | DragStart, Drag, DragEnd, DragEnter, DragOver, DragLeave, Drop |
| Clipboard | Copy, Cut, Paste |
| Validation and forms | Validating, Validated, Submit, Reset |
| Control specific | tick, row-selection |

### Initialize the form

Select the **form**, open Custom Properties, and add:

```javascript
forma.on("Load", () => {
  forma.set("welcomeLabel", "text", "Welcome to Forma!");
});
```

In Preview, startup events run in this order: Created, Mounted, Ready, Load.
All component scripts have been installed before these callbacks run. They run
once per behavior initialization, including a fresh Preview or replacement of
the behavior/DOM element. Ready and Load mean the component DOM is available;
they do not wait for image downloads or asynchronous work inside other handlers.
Top-level JavaScript still runs first to register handlers and initialize local values.

Updated runs when this component's projected model changes, with
`event.detail.previous` and `event.detail.current`. Unchanged state refreshes do
not raise it. Avoid unconditionally changing a value from its Updated handler,
which can create a feedback loop. Move runs on model X/Y changes. Resize observes
actual rendered dimensions (the initial measurement is ignored). Layout follows
size, position, or parent changes. These describe runtime updates, rather than
designer gestures. Destroyed runs before handlers and resources are cleaned up
when the behavior is removed, replaced, or Preview ends. Use `forma.cleanup` for
resource disposal even when your component has no Destroyed handler.

### Input and native browser events

Native handlers receive the original browser event: `event.key`, `event.target`,
`event.clientX`, `event.deltaY`, `event.clipboardData`, and `event.dataTransfer`
are available for their relevant event types. Focus and Blur also catch focus
changes on inner inputs of composite controls. Interactive events are ignored
for a disabled or hidden component; lifecycle events still run.

```javascript
forma.on("KeyDown", event => {
  if (event.key === "Enter") {
    forma.set("resultLabel", "text", forma.get("nameInput", "value"));
  }
});
```

Keyboard events require focus. Input/Change apply to controls that accept input.
KeyPress is a legacy browser event; prefer KeyDown or BeforeInput. Submit/Reset
from an actual HTML form also work, but Forma's form surface is a container, so
it does not automatically submit when a button is clicked. Clipboard events
depend on the browser's supported clipboard actions and focused control.

Drag events use browser drag and drop in Preview. To make a component a drag
source, set `component.element.draggable = true`. On the destination, allow a
drop with `event.preventDefault()` in DragOver:

```javascript
forma.on("DragOver", event => event.preventDefault());
forma.on("Drop", event => {
  event.preventDefault();
  forma.set("resultLabel", "text", event.dataTransfer.getData("text/plain"));
});
```

This is separate from dragging controls around the designer canvas.

### Validate, submit, and reset

Validating runs when an inner input loses focus, or when `forma.validate()` or
`forma.submit()` is called. Cancel synchronously with `event.preventDefault()`.
Validated runs only if validation was not cancelled and HTML input constraints
(such as `required`, `pattern`, or numeric limits) pass. Constraints can be set
on the actual input obtained from `component.element` or `forma.find`.

```javascript
forma.on("Validating", event => {
  if (!forma.get("nameInput", "value").trim()) {
    event.preventDefault();
    forma.set("resultLabel", "text", "Please enter your name.");
  }
});

forma.on("Submit", () => {
  forma.set("resultLabel", "text", "Submitted!");
});

forma.on("Click", () => forma.submit());

forma.on("Reset", () => {
  forma.set("nameInput", "text", "");
});
```

Put this example on your submit button. `forma.submit()` validates the current
component; the custom Validating handler above checks the named input. For a
container, its HTML constraints cover descendant inputs. `forma.reset()` raises
Reset without silently changing model values; define the values to restore in
your Reset handler. Both Submit and Reset can be cancelled with preventDefault.
Asynchronous handlers are supported and errors are reported, but cancellation
must occur before the handler awaits anything. No event sends data to a server
unless your script explicitly does so.

This customizes existing controls. Defining new toolbox control types, replacing
their complete templates, compiling C# scripts, and executable export remain
separate future features. Runtime changes do not edit the source design.


## Toast and loading indicators

Toast variants are `neutral`, `info`, `success`, `warning`, `caution`, `error`,
and `danger` (the existing red danger style remains supported). Select a Variant
in the inspector for a default, or choose it dynamically when showing a toast:

```javascript
forma.showToast("savedToast", {
  text: "Employee added successfully!",
  variant: "success",
  duration: 3000,
  position: "top-right",
  dismissible: true
});

forma.showToast("savedToast", {
  text: "Please enter a valid age.",
  variant: "warning"
});
```

Success has a solid green background, info blue, warning amber, caution orange,
and error/danger red, all with white text. Neutral uses the standard surface. Warning, caution, error, and
danger popups use the accessible alert role. Explicit custom CSS colors override
the variant styling. New toast starter CSS leaves palette colors to the Variant.
In older generated templates, unchanged generic color defaults matching the
inspector appearance are ignored for Toast, so a copied blue background does not
mask error/success styles. Typography and spacing still apply. To deliberately
override a variant, use `:host[data-variant="error"] { background-color: ...; }`.
`showToast(name)` still uses the component's current settings.
Options persist in the Preview instance; omitted options retain their current
values. Unknown options and invalid values are rejected before any changes apply.
Duration is clamped to 500–60000 milliseconds, matching the inspector.

Use `forma.get`/`forma.set` with `variant`, `position`, `duration`, and `dismissible`
for separate updates; `isOpen` is readable. Preview changes do not modify the
saved design. The message's `text` can come from a binding or fetched data:

```javascript
const name = forma.bind("firstName", "value");
forma.on("Click", () => {
  forma.showToast("savedToast", {
    text: `Welcome, ${name.value}!`,
    variant: "success"
  });
});
```

Give a Toast the Name `savedToast` and a LoadingOverlay the Name `busyOverlay`.
Choose the overlay's Target (blank covers its parent). In a Button's Custom
Properties behavior, this example starts a visual loading state and ends it with
a notification; replace the delay with your application operation:

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

`forma.closeToast(nameOrId)` closes a notification explicitly. `forma.get(nameOrId,
"isActive")` reads Spinner/LoadingOverlay state. Use the actual Name displayed in
the inspector (generated names include the design's sequence number). Notification
and overlay portal CSS also supports `:host` in the source component's custom CSS.


For the complete current API, component catalog, events and recipes, see
[Using components and scripts](components-and-scripting.md) and
[the property reference](component-reference.md).
