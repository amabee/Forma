# Component scripts and shared state

The component JavaScript file is now **script.js**. Existing projects still
load their embedded scripts, and old external behavior.js files are accepted
when script.js does not exist. Saving writes script.js; the old file is not
deleted. The legacy internal Behavior source field remains for file-format
compatibility. CSS and custom-properties.json continue to work as before.

Open **Project → Main script** to edit **main.js** in the docked code
editor. It uses the same formatting, diagnostics, Save/auto-apply and external
editor actions. The same global source is used when previewing each form in a multi-form project.
Global source is embedded in the .forma project and participates
in Save/Open and Undo/Redo. After saving, start a fresh Preview to test changes.

## Split providers into project files

Right-click **Files** in Solution Explorer, add a folder named `providers`, then
add `counter.js` inside it. Put this in that file:

```js
/** @param {FormaApi} forma */
export function createCounter(forma) {
  const count = forma.ref(0);
  return { count, increment() { count.value++; } };
}
```

Open **main.js** and register the imported provider:

```js
import { createCounter } from "./providers/counter.js";
forma.provide("counter", createCounter(forma));
```

A button's `script.js` can use the shared instance:

```js
const counter = forma.use("counter");
forma.on("click", () => counter.increment());
```

Named/default exports, re-exports and side-effect imports are supported.
Component scripts can import helpers directly too. Paths are relative to the
importing project file; `main.js` and component scripts resolve from the **Files**
root. Include `.js` explicitly; extensionless imports also resolve `.js` or
`index.js`. JSON files may be imported as data. CSS is still edited/applied through
component CSS, not JavaScript imports. Top-level await is unsupported.

Each imported module executes once per Preview and shares its exported objects
with all importers. Provider factories receive `forma` explicitly, keeping their
runtime dependencies clear. Imported files also have the startup `forma` API
available; a side-effect file imported by main.js can register a provider there.
Keep component-specific event handlers in the component script. Save all changed
files and start a fresh Preview to reload them. Completion follows imported
exports and providers registered with literal names in main.js.

The entry source is still embedded using the legacy project field so older
projects reopen unchanged. New external editing copies use `main.js`; the reader
accepts `global-script.js` when `main.js` is absent.

## Provide a shared module

Put this in main.js:

```js
const count = forma.ref(0);
const user = forma.reactive({ firstName: "", lastName: "" });

forma.provide("app", {
  count,
  user,
  increment() { count.value++; },
  fullName() { return `${user.firstName} ${user.lastName}`.trim(); }
});
```

Then in a button's script.js:

```js
const app = forma.use("app");

forma.on("click", () => {
  app.user.firstName = forma.get("firstName", "value");
  app.increment();
});
```

And in a label's script.js:

```js
const app = forma.use("app");

forma.watch(app.count, value => {
  forma.set(component.id, "text", `Count: ${value}`);
}, { immediate: true });
```

Both component scripts receive the same app object and ref. Local variables
remain local: declaring const count in the global file does not inject a bare
count identifier into other scripts. Export it with provide and access it with
use. Provide/use names are separate from JavaScript import/export names.
Imports load project files only; packages and external URLs are unsupported.

Module names must start with a letter and contain letters, digits, underscores,
dots or hyphens, up to 64 characters. A name is provided once, from the global
script. Duplicate names and missing modules produce clear Preview errors.
Literal names from saved provide calls are suggested while typing forma.use.

## Use a simple shared namespace

For small applications, modules are optional. main.js:

```js
forma.shared.userName = "";
forma.shared.loggedIn = false;
```

Any component script:

```js
const shared = forma.shared;

forma.on("click", () => {
  shared.userName = forma.get("nameInput", "value");
  shared.loggedIn = true;
});
```

Another component can watch it:

```js
forma.watch(() => forma.shared.userName, name => {
  forma.set(component.id, "text", `Hello, ${name}`);
}, { immediate: true });
```

The shared namespace is shallow reactive state. Assign its top-level fields, or
store a ref/reactive object for nested data. Do not replace forma.shared itself.
Reading a primitive into const name = shared.userName still takes a snapshot;
retain the shared object or use a getter/watch/ref to read the current value.

## Execution and lifetime

Global code initializes once before component scripts. All component scripts
are installed before global and component Load/Ready callbacks run. You can use
forma.on("load", ...) in the global file for startup work that needs controls.
Normal global DOM event handlers attach to the root form; component-specific
handlers still belong in the component's script.

Shared modules and state survive runtime state refreshes and individual component
script reloads. Replacing the global source resets the module registry and shared
scope before reinstalling component scripts. A global initialization error is
reported once and component scripts wait until it is fixed, avoiding partially
initialized shared modules.

Scope lifetime is **one Preview session**. A fresh Preview, New/Open, or closing
the Preview clears state, effects and registered handlers. Global code persists
in the project; runtime values do not persist across app restarts. Disk/database
persistence is a separate feature.

Prefer forma.watch/effect in consuming component scripts so their subscriptions
are disposed with that component. If subscribing directly to a shared ref, keep
and clean up the returned stop function:

```js
const app = forma.use("app");
const stop = app.count.subscribe(value => console.log(value));
forma.cleanup(stop);
```

Register cleanup for application resources created by the global script too:

```js
const timer = setInterval(() => console.log("Running"), 1000);
forma.cleanup(() => clearInterval(timer));
```
