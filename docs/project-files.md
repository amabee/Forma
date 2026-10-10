# Forma design files

The Builder saves designs as `.forma`. Open also accepts `.frma`.
The extension belongs to Forma; internally the file is readable, versioned JSON
with `format: "forma-project"`. Single-form projects use version 1; multi-form
projects use version 2 with a `forms` array. Projects containing custom files or
folders use version 3 with `forms` and `files` arrays. Versions 1 and 2 still open.

- Save: Ctrl+S or the File menu/toolbar. The first save asks for a location.
- Save As: Ctrl+Shift+S or File > Save As.
- Open: Ctrl+O or the File menu/toolbar.
- An asterisk in the window title marks unsaved changes.
- New Project, Open, and closing the window offer Save, Discard, or Cancel when needed.

Files retain the form title and size, stable control IDs and names, nesting and
sibling order, control values, layout slots, appearance, spacing, layer order,
and component configuration. Component CSS, `script.js`, custom JSON values,
and the form's `main.js` source are embedded too. Components reopen
stopped, in Design mode.
Local images are embedded as data URLs on save so they survive moving the design
or deleting the source image. Remote image URLs remain references.

Files describe the design and its values; they do not contain C# event handlers,
background tasks, or a compiled application. JSON is plain text, not encrypted.
Zoom, current selection, and transient Preview state are not saved. This includes
shared modules, reactive refs, timers and values changed by runtime scripts.
Every fresh Preview initializes these from the saved design and script source.

External editing files live in `<project-name>.components/<control-id>/`;
the global file lives in `<project-name>.components/<form-id>/global/`.
These folders are editing conveniences, not dependencies needed to reopen the
project. See [Custom Properties](component-customization.md) and
[global scripts](global-scripts.md) for save/auto-apply and legacy file support.

Open validates the format version, control types, IDs, tree structure, and
properties before replacing the active design. Unsupported versions and malformed
files report an error. Saves write a temporary sibling file and replace the target
only after writing successfully. Current limits are 50 MB per project, 10 MB per
newly embedded image, 5,000 controls, and 32 nesting levels.

Windows file association and opening a design by double-clicking Explorer are
future installer work. Use Open inside Builder for now.

## Forms within a project

**File → New form** / Ctrl+N adds another form to the current project and keeps
its path, existing forms and scripts. It is an undoable edit. Use the **Project
form** selector above the canvas to switch forms; switching is not a design edit
and does not mark a saved project dirty. Forms have unique names; their controls
use names scoped to that form. Save/Open retains every form, its control tree,
styles, component sources and images in one `.forma` file. Open selects the
first form. A project supports up to 100 forms, with the existing total project
limits applying across all forms.

**File → New project** / Ctrl+Shift+N starts a separate blank project and asks
about unsaved work. Preview runs only the selected form. Runtime navigation
between forms is separate work; adding a form does not automatically create
navigation or open another runtime window.

The global script is shared project source, stored on the first form for legacy
compatibility, and runs when previewing any form. Runtime state still belongs to
one Preview session. Component code tabs stay open when switching forms, and
selecting a component's code tab returns to its owning form.

## Custom project files

Explorer's **Files** branch supports folders and `.js`, `.css`, and `.json` files.
Right-click the project, Files, or a custom folder to create a file/folder or add
an existing file. Contents are embedded in `.forma`, so imported files remain
available when the original file is moved or removed. Each file opens in its own
editor tab with formatting, diagnostics and external editing support. JSON files
may contain arrays or primitives; component Custom Properties still requires a
JSON object.

Creating, renaming, removing and saving file contents participates in Undo/Redo.
Names are unique within each folder, ignoring case. Files keep their extension
when renamed. Folders and file IDs are independent of filesystem paths. Limits
are 1000 file/folder entries, 200,000 characters per file and 32 nesting levels,
within the existing 50 MB project limit. Removing a folder also removes its
children; confirmation mentions discarded unsaved editor drafts. Undo restores
the last applied project contents, not drafts that were never saved.

Custom files are stored project resources. They are not automatically executed,
applied as styles, or registered as `forma.use` modules. Component `script.js`
and `main.js` remain the execution entry points. Import `.js` or `.json`
resources from those scripts with relative paths from the Files root. Imported
JavaScript runs once per Preview; see [module imports](global-scripts.md).

External editing copies use
`<project-name>.components/<first-form-id>/project-files/<file-id>/<file-name>`.
Renaming changes the project's canonical editing filename; an older editing copy
may remain in that scratch folder. These folders are not required to reopen the
project.

## Undo and redo

Use Edit or the toolbar, Ctrl+Z for Undo, and Ctrl+Y or Ctrl+Shift+Z for Redo.
Design history includes insertion, deletion (including descendants), movement,
resizing, property changes, image selection, layer actions, and applied component
or global script edits. Consecutive edits
to the same Text field within 800 ms form one history entry. A new edit after Undo
clears the redo branch. History retains up to 100 edits and trims older large entries.

Undo restores control IDs, properties, nesting, and selection. Save keeps history;
New Project and Open start fresh histories; New Form is recorded in project history. Returning to the saved design clears the unsaved
marker. History is for the current session and is not stored in the `.forma` file.
Preview interactions are not recorded, and design Undo/Redo is disabled in Preview.
When a text field has focus, Ctrl+Z uses its native text undo; click the canvas or
the Edit/toolbar action to undo a design edit.
