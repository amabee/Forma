# Events, payloads and component handles

Give the interface something to do. Forma's Events inspector creates JavaScript
handlers, connects them to the component, and opens their code in the workspace.
Scripts run in Preview; editor changes take effect after Save and a fresh Preview.

## Add a handler from the inspector

1. Select a component, including a component in the tray.
2. Open **Events** beside Properties and search for an event.
3. Click the event. An existing handler opens at its code; a missing handler is
   generated in **event.js**, with an import and subscription in **script.js**.
4. Replace the generated comment with your code. Save applies both source files.
5. Save the project, then start a fresh Preview.

The inspector lists handlers in the **saved script**. Unsaved editor drafts are
preserved when navigating to an event, but the inspector's list updates after Save.
Locked components cannot open editable handlers. Fix syntax errors before adding
another handler; generation preserves your source rather than replacing it.

### event.js holds methods; script.js connects them

For a Button named `btnContinue`, the generated structure is:

```js
// event.js — this component's handler module
export class ButtonEventHandler {
  /** @param {FormaEvent<"DoubleClick">} event */
  static btnContinue_DoubleClick(event) {
    forma.set("resultLabel", "text", `Double clicked ${event.source.name}`);
  }
}
```

```js
// script.js — this component's setup and subscriptions
import { ButtonEventHandler } from "./event.js";

forma.on("DoubleClick", ButtonEventHandler.btnContinue_DoubleClick);
```

Use standard JavaScript `import`. `use ButtonEventHandler` is not supported
syntax; `forma.use("name")` retrieves a shared provider and is a different API.
Class methods use `static methodName(event) { ... }`, without `function`.

Each component owns its event.js file. Imported handler methods receive that
component's script-local `forma` and `component` context. Project providers remain
shared within Preview. Adding another handler reuses the imported class, avoids
method-name collisions, and preserves the current drafts of both files. Existing
inline handlers remain supported and open in script.js. Automatic generation
expects the exported class named for the component kind, such as
`ButtonEventHandler` or `TimerEventHandler`; retain that class for inspector additions.

Right-click a component or its Solution Explorer entry and choose **View Events**
to edit the module directly. Files are embedded in .forma projects and also
written to the component's editing folder for external editors. Old projects
without event.js load with an empty event module. Imported project JS/JSON files
can still provide reusable helpers; see [imports and providers](global-scripts.md).

## Choose the subscription target

`forma.on(name, handler)` listens on the component whose script is running.
`forma.root.on(name, handler)` listens on the active Preview form from any script.
Both return an unsubscribe function and remove the listener when its subscribing
script is replaced, removed, or Preview ends.

```js
// main.js, or any component's script.js
forma.root.on("Load", () => {
  forma.set("welcomeLabel", "text", "Ready when you are.");
});

const stop = forma.root.on("Click", ({ source, subscriber }) => {
  console.log(`${source.name} clicked; ${subscriber.name} is listening.`);
});

// stop(); // Unsubscribe early if needed.
```

Root lifecycle events work even when the form has no component script. Native
events follow browser propagation: Click and Input can bubble from children,
while lifecycle events such as Load are raised on their own component and do not
bubble. Focus/Blur subscriptions capture focus transitions on inner inputs.
PropertyChanged and native bridge component events can reach the root from tray
components, whose DOM elements are outside the form's subtree.

Names are case insensitive. `DoubleClick` aliases `dblclick`; `MouseWheel` aliases
`wheel`. A registered event still needs a source: an ordinary Label does not
produce keyboard input without focus, and a Button does not produce text Input.

## Event payload reference

Handlers receive a browser-compatible event with Forma metadata. Destructuring
works directly:

```js
forma.root.on("Input", ({ id, property, value, previousValue, source }) => {
  console.log({ id, property, value, previousValue });
  forma.set("resultLabel", "text", `${source.name}: ${String(value)}`);
});
```

| Field | Meaning |
| --- | --- |
| `eventId` | Client event-object identifier, shared by listeners receiving that event |
| `timestamp` | Epoch milliseconds when Forma first records the event |
| `type` | Original browser event name, such as `click`, `dblclick` or `propertychanged` |
| `eventName` | Forma display name, such as `Click`, `DoubleClick` or `PropertyChanged`; custom names retain their name |
| `id` | Originating component ID, also available as `source.id` |
| `source` | Handle for the originating component |
| `subscriber` | Handle for the component owning the subscription; the form for root subscriptions |
| `target` | Original **DOM target**, which may be an inner input, span or icon |
| `currentTarget` | **DOM element** receiving this listener; captured for later use |
| `path` | Component handles from the source upward through its model parents |
| `phase` | `capture`, `target` or `bubble`, relative to the component subscription |
| `origin` | `user`, `programmatic`, `binding`, `system` or `native`; details below |
| `property` | Affected runtime property for supported Input/Change and PropertyChanged events |
| `value`, `previousValue` | Current and last observed values when the event supplies property data |
| `detail` | Original event-specific detail; its shape depends on the event |
| `handled` | Writable boolean shared across listeners; does not automatically cancel or stop propagation |
| `defaultPrevented`, `cancelable`, `bubbles`, `isTrusted` | Original browser event flags |
| `propagationStopped` | Whether propagation was stopped through the wrapper or current browser flag |
| `originalEvent` | Underlying browser Event/CustomEvent; no raw .NET control is exposed |
| `mouse`, `pointer`, `keyboard` | Optional structured input data; absent when the event lacks that input type |
| `modifiers` | `{ ctrl, shift, alt, meta }` booleans |

**Click identifies the component; it does not automatically supply a property
change.** Its property/value fields are normally undefined. Use Input, Change or
PropertyChanged when you need values. Input/Change previousValue describes the
last observed value, not an entire edit session or a value from persistent storage.

### Browser compatibility

Existing code continues to work: `event.target.closest(...)`, `event.key`,
`event.clientX`, `event.deltaY`, `event.clipboardData`, `event.dataTransfer`,
`event.composedPath()` and native event `instanceof` checks are preserved.
Use `source` and `subscriber` for Forma handles; target/currentTarget retain
their DOM meaning. `path` follows model parents; `composedPath()` follows DOM nodes.

For a click on a button's inner icon, target can be the icon while source is the
Button and subscriber is the Form. Forwarded tray events may target the root DOM
element; source still identifies the tray component. Do not infer the component
ID solely from `event.target.id`.

### Structured input data

| Object | Fields |
| --- | --- |
| `mouse` | `x`, `y` in viewport/client coordinates; `screenX`, `screenY`, `button`, `buttons`; wheel events also provide `deltaX`, `deltaY`, `deltaZ`, `deltaMode` |
| `pointer` | `id`, `type`, `pressure`, `isPrimary`, `width`, `height` when pointer data exists |
| `keyboard` | `key`, `code`, `repeat`, `location`, `isComposing` |
| `modifiers` | `ctrl`, `shift`, `alt`, `meta` |

The structured objects are read-only. Native fields remain available too:

```js
forma.on("KeyDown", event => {
  if (event.keyboard?.key === "Enter" && !event.keyboard.isComposing) {
    console.log(event.source.name, event.modifiers.shift);
  }
});
```

## Component handles and forma.root

`forma.root`, event.source, event.subscriber, and path entries expose:

| Member | Use |
| --- | --- |
| `id`, `name`, `kind` | Component identity; kind is the lowercase runtime kind |
| `element` | DOM element, or null if it no longer exists |
| `get(property)` | Read a supported runtime property |
| `set(property, value)` | Send a typed property change through the native bridge |
| `bind(property)` | Obtain a live reference; read/write its `.value` as permitted |
| `on(event, handler)` | Subscribe to that component; returns unsubscribe |

```js
const formTitle = forma.root.bind("text");

forma.root.on("Click", ({ source }) => {
  console.log(source.id, source.kind, source.get("text"));
  formTitle.value = `Last clicked: ${source.name}`;
});
```

These handles use the same supported keys and value types as forma.get/set/bind.
Writes remain asynchronous. A handle's subscriptions and bindings belong to
the script that obtained it; storing the handle in shared state does not extend
that lifetime. After that script is destroyed, handle operations reject further
use. Identity remains readable for diagnostics. Handles have no dispose method
for deleting a component. See [runtime properties](runtime-properties.md).

## PropertyChanged and change origins

PropertyChanged reports observed runtime property changes. It is noncancelable,
bubbles to the root, and runs even for a disabled or hidden source component.
Unchanged snapshots and equivalent array values do not produce repeat notifications.
There is no initial PropertyChanged event for the starting values.

```js
forma.root.on("PropertyChanged", ({ source, property, value, previousValue, origin }) => {
  console.log(`${source.name}.${property}`, previousValue, "→", value, origin);
});
```

| Input/control family | Typical property |
| --- | --- |
| TextBox, SearchBox, PasswordBox, TextArea, MaskedTextBox | `value` (text input; model text is normalized to value for notifications) |
| NumericUpDown, Slider, Rating, date/time inputs, ColorPicker | `value`; numbers stay numbers, date/time/color inputs use strings |
| CheckBox, RadioButton, Switch, ToggleButton, Chip | `checked` boolean |
| ComboBox, ListBox, ListView and single-selection groups/navigation | `selectedIndex` |
| CheckBoxGroup, ChipGroup, CheckedListBox | `checkedIndices` array |
| FilePicker, FolderPicker | `selectedPath` string |
| TabControl, Accordion | `selectedTab` |
| DataGridView row selection | `selectedRow` |

Other projected runtime properties can notify too: for example, Timer.interval,
enabled, visible, grid rows and geometry. Payload arrays/objects are copies;
mutating event.value does not update the component. Empty/nonfinite numeric
DOM input is represented as null in the input payload.

For DOM Input/Change, the live value is captured before Forma handlers, then
PropertyChanged is queued after the input event dispatch. A later C# confirmation
of the same value is deduplicated. For native model updates, reactive bindings
are refreshed before queued lifecycle/property callbacks run. PropertyChanged
contains the committed, possibly clamped model value, rather than the originally
requested value. A coalesced native refresh can summarize several writes as one
transition; this is not a notification for every intermediate assignment.

| Origin | Current meaning |
| --- | --- |
| `user` | Trusted browser input |
| `programmatic` | Untrusted browser dispatch or accepted script setters/commands |
| `binding` | Accepted writes through a live bound reference |
| `system` | Forma-generated lifecycle, layout and validation events |
| `native` | C# bridge events or observed backend changes without a more specific write origin |

Origin describes current dispatch/write provenance. Synthetic control events do
not always preserve the initiating user gesture. No transactionId or complete
causal chain is attached yet. An unsuccessful setter does not create a confirmed
property notification. Avoid writing unconditionally to the same property from
its PropertyChanged handler; that can cause a feedback loop across the bridge.

## Cancellation, propagation and cleanup

`preventDefault()` delegates to the browser. It only cancels an operation when
the event is cancelable. `stopPropagation()` stops further propagation;
`stopImmediatePropagation()` also stops later listeners on the same element.
Setting handled to true is an application flag, not a propagation instruction.

```js
forma.on("Validating", event => {
  if (!forma.get("nameInput", "value").trim()) {
    event.preventDefault(); // Synchronous: do this before any await.
  }
});

forma.on("Click", event => {
  event.handled = true;
});

forma.root.on("Click", event => {
  if (event.handled) return;
  console.log("No component marked this click handled.");
});
```

Validating is raised on blur or forma.validate()/forma.submit(). Validated follows
only when cancellation and HTML input constraints permit it. forma.submit()
then raises cancellable Submit; forma.reset() raises cancellable Reset and your
handler restores values. The Forma form surface does not automatically submit
when an ordinary Button is clicked. PropertyChanged describes an already applied
change; preventDefault cannot roll it back. Async cancellation after await is too
late for synchronous browser actions or validation.

Both on APIs return unsubscribe. Use forma.cleanup for your own resources.
Callback exceptions and promise rejections are reported by the dispatcher in
Preview; there is no errors array attached to every event.

## Lifecycle and event-specific details

Startup runs Created → Mounted → Ready → Load after all component scripts are
installed. These events run per script initialization, not after every state
refresh, and do not wait for image downloads or asynchronous handlers.
Destroyed runs before the script's listeners/resources are removed.

| Event | Detail / behavior |
| --- | --- |
| Updated | `detail.previous`, `detail.current` projected component snapshots; unchanged refreshes are ignored |
| Move | Previous/current projected snapshots when model X/Y changes |
| Resize | Actual rendered sizes from ResizeObserver; initial measurement is ignored; fallback uses model snapshots |
| Layout | Follows rendered/model size, position or parent changes |
| PropertyChanged | `detail.property`, `detail.value`, `detail.previousValue`, also exposed at the top level |
| row-selection | `detail.row`, the original Rows index, independent of sorting/filtering |
| command-item | Native forwarded commands can supply `detail.itemId`, `detail.text`, `detail.checked` |
| navigate | `detail.index`, `detail.text` |
| Tick | Native Timer event; no automatic tick counter is supplied |

Put Tick handlers on the Timer or listen through root. Core BackgroundWorker
events are not automatically forwarded as JS hooks. Drag/drop hooks are browser
events in Preview, separate from dragging designer controls. To accept a drop,
cancel DragOver's default action and read event.dataTransfer in Drop.

Available event groups include mouse, keyboard/input, focus, lifecycle, geometry,
drag/drop, clipboard, validation/forms, PropertyChanged, and the component-specific
hooks shown in the inspector. KeyPress is a legacy browser event; prefer KeyDown
or BeforeInput where appropriate.

## Log portable event metadata

```js
forma.root.on("PropertyChanged", event => {
  console.log(JSON.stringify(event, null, 2));
});
```

event.toJSON() supplies event IDs, identity descriptors, path IDs, property data,
detail, flags and structured input data. It omits DOM elements, originalEvent,
functions and circular object links. Live source/subscriber handles are replaced
by `{ id, name, kind }`. Serialization produces a diagnostic snapshot, not another
live component handle. Native browser type stays in type; use eventName for the
Forma display name.

## Current JS and C# boundary

Preview handlers and generated event.js modules currently execute JavaScript.
C# examples elsewhere document the Core models/events in a configured .NET host;
they do not mean C# scripts can be compiled in Builder's JS editor. C# handler
execution, transaction tracking and cancellable native operations across the
bridge remain future work. Current handlers use standard JS imports; there is no
new use keyword or arbitrary package loader.
