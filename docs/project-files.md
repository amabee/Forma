# Forma design files

The Builder saves designs as `.forma`. Open also accepts `.frma`.
The extension belongs to Forma; internally the file is readable, versioned JSON
with `format: "forma-project"` and `version: 1`.

- Save: Ctrl+S or the File menu/toolbar. The first save asks for a location.
- Save As: Ctrl+Shift+S or File > Save As.
- Open: Ctrl+O or the File menu/toolbar.
- An asterisk in the window title marks unsaved changes.
- New, Open, and closing the window offer Save, Discard, or Cancel when needed.

Files retain the form title and size, stable control IDs and names, nesting and
sibling order, control values, layout slots, appearance, spacing, layer order,
and component configuration. Components reopen stopped, in Design mode.
Local images are embedded as data URLs on save so they survive moving the design
or deleting the source image. Remote image URLs remain references.

Files describe the design and its values; they do not contain C# event handlers,
background tasks, or a compiled application. JSON is plain text, not encrypted.
Zoom, current selection, and transient preview state are not saved.

Open validates the format version, control types, IDs, tree structure, and
properties before replacing the active design. Unsupported versions and malformed
files report an error. Saves write a temporary sibling file and replace the target
only after writing successfully. Current limits are 50 MB per project, 10 MB per
newly embedded image, 5,000 controls, and 32 nesting levels.

Windows file association and opening a design by double-clicking Explorer are
future installer work. Use Open inside Builder for now.

## Undo and redo

Use Edit or the toolbar, Ctrl+Z for Undo, and Ctrl+Y or Ctrl+Shift+Z for Redo.
Design history includes insertion, deletion (including descendants), movement,
resizing, property changes, image selection, and layer actions. Consecutive edits
to the same Text field within 800 ms form one history entry. A new edit after Undo
clears the redo branch. History retains up to 100 edits and trims older large entries.

Undo restores control IDs, properties, nesting, and selection. Save keeps history;
New and Open start fresh histories. Returning to the saved design clears the unsaved
marker. History is for the current session and is not stored in the `.forma` file.
Preview interactions are not recorded, and design Undo/Redo is disabled in Preview.
When a text field has focus, Ctrl+Z uses its native text undo; click the canvas or
the Edit/toolbar action to undo a design edit.
