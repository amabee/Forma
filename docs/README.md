# Forma documentation

Start with the [workspace walkthrough](builder-first-interaction.md), then
[using components and scripts](components-and-scripting.md).

| Guide | What it covers |
| --- | --- |
| [Component reference](component-reference.md) | Each component, inspector properties, ranges and options |
| [Runtime properties](runtime-properties.md) | Supported `forma.get`, `forma.set` and `forma.bind` keys, types and restrictions |
| [Custom Properties](component-customization.md) | CSS, `script.js`, custom JSON, editor tools, events and reactive state |
| [Global scripts](global-scripts.md) | `global-script.js`, `forma.provide`, `forma.use`, shared state and scope lifetime |
| [Examples](examples) | Copyable JavaScript and C# recipes |
| [Project files](project-files.md) | Save/Open, `.forma`, embedded sources/images and Undo/Redo |
| [Inspector properties](properties-implementation.md) | Current inspector behavior and remaining property work |
| [Toolbox implementation](toolbox-implementation.md) | Implemented controls and layout behavior |
| [Architecture](architecture.md) | MVVM, folder structure, runtime and editor boundaries |
| [Roadmap](roadmap-stages.md) | Implemented batches and pending work |

## Choose the right script scope

Put a component's event handlers in its **Custom Properties → Script** tab
(`script.js`). Open **Project → Global script** for shared application setup
(`global-script.js`), which runs before component scripts.

For example, put this in the global script:

```js
forma.provide("app", { count: forma.ref(0) });
```

Then put this in a button's script:

```js
const app = forma.use("app");
forma.on("click", () => {
  app.count.value++;
  forma.set("countLabel", "text", `Count: ${app.count.value}`);
});
```

Create a Label named `countLabel` for that example. Save the editor and start a
fresh Preview. Shared values persist throughout that Preview session; a new
Preview starts them again from the saved source. Saving the project stores code
and design values, not the current Preview's counter.

A plain `forma.get` returns a snapshot. For a live value outside an event, use
`const name = forma.bind("nameInput", "value")` and read `name.value` inside the
handler. See the reactive examples in the Custom Properties guide.

The original [toolbox catalog](Forma_Toolbox_Components_Layouts_Controls.md) and
[property proposal](Forma_Properties_System.md) include future ideas. Use the
guides above and roadmap status to identify what currently works.
