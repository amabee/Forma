# Properties implemented in Builder

The inspector is generated from C# descriptors in `InspectorCatalog.cs`, with
categories, editor types, bounds, choices, and control-specific availability.
Changing a selected control's values does not rebuild its editors, preserving
focus while typing. Advanced starts collapsed; category expansion is preserved
when switching between kinds.

| Category | Supported properties |
| --- | --- |
| General | Name, Text/Title, read-only ID, Tag, Locked |
| Appearance | Background, foreground, border style/color/width/radius, opacity, shadow, cursor; Button style presets |
| Typography | Family, size, weight (including 100–900), style, alignment, line height, letter spacing |
| Layout | X/Y, size, minimum/maximum size, independent top/right/bottom/left margin and padding |
| Behavior | Enabled, Visible, Focusable, TabIndex, ToolTip; TextBox placeholder, ReadOnly, Password, MaxLength |
| Advanced | CSS Class, read-only CSS ID, visual Custom CSS, Z-index |

Properties are contextual. Root position and child borders/spacing are omitted
for Forms. TextBox options appear only on TextBox, and style presets only on
Button. Form font settings affect its preview title. Name is separate from
render identity and title; names must be unique identifiers. ID remains read-only
because Core IDs are set once and renderers use them as stable keys.

Locked controls remain selectable but cannot be moved, resized, deleted, or
edited. Unlocking remains available. Locking the form prevents adding controls
and resizing it; it does not lock its children recursively.

Style presets change button background/foreground/border colors. Individual
color edits switch the preset to Custom. Cursor, tooltips, focusability, and
keyboard tab order are best checked in Preview; design mode preserves selection
and dragging. Tag is metadata and is also exposed as `data-tag` on the element.

MaximumWidth/Height of zero means the parent or canvas limit. Minimums are
subject to available space and hard minimum dimensions. Size constraints apply
to both numeric property edits and resize handles. Margin consumes available
parent space; larger margin may shrink/reposition the control to keep it inside.
Form shrink keeps children inside and preserves the position of locked children.

Custom CSS accepts visual declarations (color, background-color, fonts, text,
borders, line height, letter spacing, and shadows). These override ordinary
appearance fields while geometry stays controlled by Layout. Removing Custom
CSS restores descriptor values. CSS classes cannot replace reserved designer
classes; changing CSS Class removes previously assigned user classes.

State remains owned by the C# Builder. Appearance metadata is not yet a shared
Core styling API or a saveable project format.

## Remaining proposal items

- Events tab, handler generation, and binding to user code.
- Dock/Anchor and horizontal/vertical alignment: these need a layout system.
- TextBox Multiline, CharacterCasing, ScrollBars, and WordWrap.
- Custom attributes, RenderMode, data binding, and AllowDrop behavior in apps.
- Form BackgroundImage and native window StartPosition, Resizable, MinimizeBox,
  MaximizeBox, ShowIcon, and native BorderStyle.
- Save/load and moving the descriptor/property model into the shared Core API.

These are not presented as working editors. The proposal recommends incremental
implementation rather than adding every property immediately.

## Checks

Run `dotnet test tests/Forma.Tests` and
`node --test tests/WebRuntime/designer.test.cjs`. Checks cover contextual metadata,
identity restrictions, descriptor editor generation, locking, size limits,
spacing/typography rendering, and preview tab order. Manual desktop inspection
is still needed for the resulting layout.
