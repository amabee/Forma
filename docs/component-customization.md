# Custom Properties

Select a control or component and open **Advanced → Custom Properties…** in
Properties. The separate Forma editor has three source tabs:

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
and unsaved changes. The window can be maximized.

Use the built-in editor directly, or click **Open external editor**. Forma finds
VS Code when installed; **Choose editor…** selects another editor executable.
The chosen editor is remembered for the current Builder session. Sources are
written as `component.css`, `behavior.js`, and `custom-properties.json`.
**Save** or **Ctrl+S** in Forma saves the files and applies the properties in one
step. While the Custom Properties window is open, saves from the external editor
are detected and applied automatically after a short debounce. There are no
separate Reload or Apply buttons. Each changed valid save records one undo step;
duplicate file notifications do not add edits. Invalid JSON or incomplete file
writes leave the last valid properties active and show an error in the editor.
Close the editor when finished; unsaved text is not applied. Save the main project
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
