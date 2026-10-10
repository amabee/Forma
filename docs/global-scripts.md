# Component scripts and shared state

The component JavaScript file is now **script.js**. Existing projects still
load their embedded scripts, and old external behavior.js files are accepted
when script.js does not exist. Saving writes script.js; the old file is not
deleted. The legacy internal Behavior source field remains for file-format
compatibility. CSS and custom-properties.json continue to work as before.

Open **Project → Global script** to edit **global-script.js** in the docked code
editor. It uses the same formatting, diagnostics, Save/auto-apply and external
editor actions. Global source is embedded in the .forma project and participates
in Save/Open and Undo/Redo. After saving, start a fresh Preview to test changes.

## Provide a shared module

Put this in global-script.js:

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
use. These are normal JavaScript functions, not a custom language or an ES-module
import loader. They do not fetch packages or external URLs.

Module names must start with a letter and contain letters, digits, underscores,
dots or hyphens, up to 64 characters. A name is provided once, from the global
script. Duplicate names and missing modules produce clear Preview errors.
Literal names from saved provide calls are suggested while typing forma.use.

## Use a simple shared namespace

For small applications, modules are optional. Global script:

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
