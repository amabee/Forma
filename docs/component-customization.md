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
Warnings catch unknown literal component names, unsupported `api.get`/`api.set`
properties and unknown API methods. Dynamic expressions are checked at runtime.
These checks do not execute code or replace Preview testing.

JavaScript suggestions open while typing, or with **Ctrl+Space**. Type `api.` for
methods, `api.set("` for component names, and the second argument for supported
properties. For example, `api.set("userAvatar", "` offers `source`, while
`api.get("nameInput", "` offers `value`. `component.properties.` suggests keys
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
api.on("click", () => {
  component.properties.clicks += 1;
  api.set("label1", "text", `Clicked ${component.properties.clicks} times`);
});
```

| API | Purpose |
| --- | --- |
| `api.on(event, handler)` | Listen on this component; handles callback errors and cleanup |
| `api.get(nameOrId, property)` | Read supported control values |
| `api.set(nameOrId, property, value)` | Send a typed change to the runtime model |
| `api.find(nameOrId)` | Get another control's DOM element |
| `api.showDialog(nameOrId)` | Open an enabled Dialog or ConfirmationDialog |
| `api.cleanup(callback)` | Register cleanup for custom listeners/resources |
| `api.validate()` | Raise Validating, check this component's HTML input constraints, then raise Validated on success |
| `api.submit()` | Validate this component and raise cancellable Submit; returns true when accepted |
| `api.reset()` | Raise cancellable Reset; your handler decides which values to restore |

Supported model setters: `text`, `enabled`, `visible`, `checked`, `value`,
`selectedIndex`, and `selectedTab`. Value/index/checked setters require an
appropriate control type. Changes cross the WebView2 bridge asynchronously;
an immediate `get()` after `set()` may still read the earlier model state.
DOM input values can be read directly from `component.element` inside input
events. Custom values are useful for immediate local state such as counters.

DataGridView also supports `selectedRow`, `filterText`, `sortColumn`, and
`sortDirection` through get/set. Use `api.on("row-selection", event => { ... })`
to react after selection changes; `event.detail.row` is the original Rows index.

Timer components support `api.on("tick", handler)` and their Enabled setting.
Listeners registered through the API survive runtime state updates without
being installed twice, and are cleaned up when the session ends. Native
file/folder pickers, tabs, and the existing control events continue to work.

## JavaScript events

Register events in a component's JavaScript tab under **Custom Properties**.
Names are case insensitive: `Click` and `click` register the same event.
`DoubleClick` maps to browser `dblclick`, and `MouseWheel` maps to `wheel`;
the browser names also work. Event suggestions are available inside `api.on`.

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
api.on("Load", () => {
  api.set("welcomeLabel", "text", "Welcome to Forma!");
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
when the behavior is removed, replaced, or Preview ends. Use `api.cleanup` for
resource disposal even when your component has no Destroyed handler.

### Input and native browser events

Native handlers receive the original browser event: `event.key`, `event.target`,
`event.clientX`, `event.deltaY`, `event.clipboardData`, and `event.dataTransfer`
are available for their relevant event types. Focus and Blur also catch focus
changes on inner inputs of composite controls. Interactive events are ignored
for a disabled or hidden component; lifecycle events still run.

```javascript
api.on("KeyDown", event => {
  if (event.key === "Enter") {
    api.set("resultLabel", "text", api.get("nameInput", "value"));
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
api.on("DragOver", event => event.preventDefault());
api.on("Drop", event => {
  event.preventDefault();
  api.set("resultLabel", "text", event.dataTransfer.getData("text/plain"));
});
```

This is separate from dragging controls around the designer canvas.

### Validate, submit, and reset

Validating runs when an inner input loses focus, or when `api.validate()` or
`api.submit()` is called. Cancel synchronously with `event.preventDefault()`.
Validated runs only if validation was not cancelled and HTML input constraints
(such as `required`, `pattern`, or numeric limits) pass. Constraints can be set
on the actual input obtained from `component.element` or `api.find`.

```javascript
api.on("Validating", event => {
  if (!api.get("nameInput", "value").trim()) {
    event.preventDefault();
    api.set("resultLabel", "text", "Please enter your name.");
  }
});

api.on("Submit", () => {
  api.set("resultLabel", "text", "Submitted!");
});

api.on("Click", () => api.submit());

api.on("Reset", () => {
  api.set("nameInput", "text", "");
});
```

Put this example on your submit button. `api.submit()` validates the current
component; the custom Validating handler above checks the named input. For a
container, its HTML constraints cover descendant inputs. `api.reset()` raises
Reset without silently changing model values; define the values to restore in
your Reset handler. Both Submit and Reset can be cancelled with preventDefault.
Asynchronous handlers are supported and errors are reported, but cancellation
must occur before the handler awaits anything. No event sends data to a server
unless your script explicitly does so.

This customizes existing controls. Defining new toolbox control types, replacing
their complete templates, compiling C# scripts, and executable export remain
separate future features. Runtime changes do not edit the source design.


## Toast and loading indicators

Give a Toast the Name `savedToast` and a LoadingOverlay the Name `busyOverlay`.
Choose the overlay's Target (blank covers its parent). In a Button's Custom
Properties behavior, this example starts a visual loading state and ends it with
a notification; replace the delay with your application operation:

```js
let pending;
api.on("click", () => {
  clearTimeout(pending);
  api.set("busyOverlay", "isActive", true);
  pending = setTimeout(() => {
    api.set("busyOverlay", "isActive", false);
    api.showToast("savedToast");
  }, 1000);
});
api.cleanup(() => clearTimeout(pending));
```

`api.closeToast(nameOrId)` closes a notification explicitly. `api.get(nameOrId,
"isActive")` reads Spinner/LoadingOverlay state. Use the actual Name displayed in
the inspector (generated names include the design's sequence number). Notification
and overlay portal CSS also supports `:host` in the source component's custom CSS.


For the complete current API, component catalog, events and recipes, see
[Using components and scripts](components-and-scripting.md) and
[the property reference](component-reference.md).
