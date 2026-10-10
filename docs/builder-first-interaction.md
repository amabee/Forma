# Builder workspace walkthrough

Run `dotnet run --project src/Forma.Builder` from the repository root. Close the
old Builder before rebuilding its executable. The native Windows host contains
the web workspace: menus, toolbox, canvas, inspector and docked code editor.

## Build and test a small form

1. Drag a Panel onto the form, then drop a TextBox, Label and Button inside it.
   The children belong to the panel and move with it. Double-clicking a toolbox
   entry also inserts a control.
2. Name them `nameInput`, `greetingLabel` and `greetButton` in Properties. Names
   are unique identifiers; the read-only ID stays stable across renames.
3. Move and resize them using selection handles or Layout values. Change zoom
   and confirm movement still uses form coordinates. Hold Alt to bypass snapping
   guides; Escape cancels an active drag.
4. Select the Button and click **Custom Properties…** below the property search.
   In the **Script** tab, replace the starter source with:

   ```js
   forma.on("click", () => {
     const name = String(forma.get("nameInput", "value") ?? "").trim();
     forma.set("greetingLabel", "text", name ? `Hello, ${name}!` : "Enter a name.");
   });
   ```

5. Save the editor, then click Preview. Type a name and click the button. Preview
   runs a separate design copy in a resizable window with maximize support.
   Close it to end that runtime session. Saving script edits requires a fresh
   Preview to test the changed source.
6. Save the project with Ctrl+S. Open the `.forma` file with Ctrl+O to confirm the
   design and script reopen. Ctrl+Z/Ctrl+Y undo and redo design edits; focused
   text editors keep their own text undo behavior.

## Layout and properties

Panel and GroupBox allow free positioning. Flow/stack/table layouts own child
positions; X/Y edits do not override their arrangement. Use layout slots for
split panes, tabs and table cells. TabControl has an Add tab action and horizontal
or vertical tabs. Dock/Anchor apply to free-position children; Dock controls
position and can control dimensions. This is intentional layout behavior.

Click empty form space to edit the form title, dimensions and appearance. Use
property search to find component-specific settings. Enabled/Visible affect
Preview; Design keeps components selectable. Locked prevents design edits.
The workspace theme toggle changes the editor, not your form's appearance.

For live values outside events, use `forma.bind` and read `.value` when needed.
For shared values, open **Project → Global script** and use
`forma.provide`/`forma.use` or `forma.shared`. See the
[script guide](components-and-scripting.md), [runtime property reference](runtime-properties.md)
and [global script examples](global-scripts.md).

Preview runs inside Builder. A saved design is not an exported executable;
executable export and generated C# event handlers remain roadmap work.

## Architecture and contributing

`BuilderViewModel` owns design state; services validate editing, persistence,
properties and Preview operations. Views host native windows and dispatch
browser messages. The browser handles immediate pointer feedback, while C#
validates committed edits. Preview owns its own model and reactive script scope.
See [the MVVM architecture and folder structure](architecture.md).

Before adding a control, check the [roadmap](roadmap-stages.md) and
[toolbox implementation](toolbox-implementation.md). A complete addition needs
a model, renderer, factory/toolbox registration, contextual inspector properties,
persistence, supported script operations and meaningful verification.

## Automated checks

```powershell
dotnet build Forma.slnx
dotnet test Forma.slnx
node --test tests/WebRuntime/*.test.cjs
```

Desktop testing complements these checks for pointer interactions, rendering
and native window integration.
