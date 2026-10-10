# Properties implemented in Builder

The inspector is generated from C# descriptors in `InspectorCatalog.cs`, with
categories, editor types, bounds, choices and component-specific availability.
Updates preserve editor focus; category expansion is remembered. Search filters
properties, and **Custom Properties…** appears immediately below the search.

| Category | Supported properties |
| --- | --- |
| General | Name, Text/Title, read-only ID, Tag, Locked |
| Appearance | Background, foreground, borders, opacity, shadow, cursor; component style/variant options |
| Typography | Family, size, weight, style, alignment, line height, letter spacing |
| Layout | X/Y, size, minimum/maximum size, per-side margin/padding, Dock/Anchor and container-specific slots/settings |
| Behavior | Enabled, Visible, Focusable, TabIndex, ToolTip; component-specific input and runtime settings |
| Advanced | CSS Class and read-only CSS ID |

Properties are contextual. Root position is omitted, input settings appear on
applicable inputs, and layout providers control the fields they own. Name is
separate from render identity; names must be unique identifiers. IDs are stable
keys used by renderers, persistence and scripts.

Locked controls remain selectable but cannot be moved, resized, deleted or edited.
Unlocking remains available. Locking the form does not lock children recursively.
Enabled/Visible control runtime interaction; Design preserves selection.

## Layout and styling

MaximumWidth/Height of zero uses the available parent/canvas limit. Size
constraints apply to property edits and resize handles. Margin consumes parent
space. Free-position containers support Dock/Anchor; managed layouts own child
placement. See [Dock and Anchor](components-and-scripting.md#dock-and-anchor).

Custom CSS and Z-index live in **Custom Properties → Styles**, rather than
separate inspector fields. Scoped `:host` styles override ordinary appearance
settings. Keep position and sizing in Layout to preserve designer geometry.
Nonvisual popup components style their runtime popup, not their designer tray.
The editor also contains `script.js` and custom JSON values.

Design appearance and applied sources are saved in `.forma` projects and
participate in Undo/Redo. Appearance metadata currently belongs to Builder;
moving the styling contract into the shared Core API is still separate work.

## Properties from JavaScript

Use `forma.get`, `forma.set` and `forma.bind` with exact camelCase keys and
types in [runtime properties](runtime-properties.md). Code suggestions reflect
supported component keys. Inspector labels do not automatically become script
properties, and read-only status fields cannot be set.

```js
const enabled = forma.bind("timer", "enabled");
forma.on("click", () => {
  forma.set("timer", "interval", 1000);
  enabled.value = !enabled.value;
});
```

Attach that example to a Button, with a Timer named `timer`. Numeric setters
take numbers rather than quoted strings. Runtime writes affect the Preview
copy; they do not edit the saved design. Bridge updates are asynchronous.

## Remaining proposal items

- Visual Events-tab authoring and generated C# handlers; JavaScript event
  handlers already work in `script.js`.
- AutoSize, explicit AutoScroll controls and further layout/alignment settings.
- Additional TextBox options such as CharacterCasing and WordWrap.
- Custom attributes, RenderMode and application AllowDrop configuration.
- A visual binding editor beyond the current reactive scripting API.
- Form BackgroundImage and further native window configuration properties.
- A shared Core styling/property contract.

The [original property proposal](Forma_Properties_System.md) includes future
ideas. Use the current [component reference](component-reference.md) for available
fields and exact ranges, and the [roadmap](roadmap-stages.md) for priorities.
