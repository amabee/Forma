# Solution Explorer

Use **Explorer** in the left sidebar to browse the current project, or **Toolbox**
to add controls. The sidebar remembers your choice. View also has a **Toggle
solution explorer** action. Explorer is the default for a fresh workspace.

The tree includes:

- **Project:** the saved project name, or Untitled project.
- **Forms:** every form and its nested components, including nonvisual services.
- **Source files:** `component.css`, `script.js` and `custom-properties.json`
  for each component and form. Containers group their own sources under Source
  files to separate them from nested components.
- **Files:** custom JavaScript, CSS and JSON files organized in folders.
- **main.js:** the shared project script.
- **Assets:** images currently referenced by Image/PictureBox/Avatar controls,
  including embedded images. Each entry identifies the component using it.

Click a form to open its designer. Click a component to select it in its owning
form and show its properties. Click a source file to open the correct code tab;
other open editors and their drafts stay intact. The global file opens the global
script editor. Locked components remain selectable, but their source files are
disabled until you unlock them in Properties.

Click an asset to select its image component, then use the inspector's image
picker or Source field to change it. Assets lists references in the design; it
is not an independent filesystem browser. Right-click Assets and choose **Add
Image to Current Form** to create an image component with a picked image;
right-click an asset and choose **Choose Image** to replace its source. The
project tree reflects embedded design sources even before external editing files
are written. It does not expose build folders or unrelated filesystem files.

The **Add form** button beside the heading adds a form to this project, just like
Ctrl+N. Rename a form or component using its Name field in Properties. Tree names
update after edits, Save/Open and Undo/Redo.

## Right-click actions

Menus depend on the selected item. They contain working actions rather than
future placeholders:

| Item | Actions |
| --- | --- |
| Project | Add Form, new JS/CSS/JSON file, New Folder, Add Existing File, Save Project, Open Global Script, Open Project Folder, expand/collapse |
| Forms folder | Add Form and expand/collapse |
| Custom folder / Files | New JS/CSS/JSON file, New Folder, Add Existing File; custom folders also support rename and removal |
| Custom file | Open, Rename, Remove from Project |
| Form / component | View Designer/Properties, Rename, View CSS/Script/Custom Properties, removal where allowed |
| Built-in source group/file | Open the existing sources; fixed component filenames cannot be renamed or removed independently |
| Assets / image reference | Add Image to Current Form; select the component, rename it, or choose its image |

The last form cannot be removed. Locked controls and descendants prevent their
removal. Deletion asks for confirmation and participates in project Undo/Redo.
**Open Project Folder** requires a saved project.

Use **Shift+F10** for the focused row's menu, arrows to choose an action, and
Escape to close it and restore focus. **F2** renames a custom file/folder or an
unlocked form/component. See [project file persistence](project-files.md#custom-project-files)
for custom-file limits and execution semantics.

## Search and keyboard navigation

Search filters names and keeps matching items' parent branches visible. Clear it
to return to the previous expansion state. Ordinary property updates preserve
expanded branches and focused rows.

- **Up/Down:** focus the previous/next visible row.
- **Right:** expand a branch, or focus its first child.
- **Left:** collapse a branch, or focus its parent.
- **Home/End:** focus the first/last visible row.
- **Enter/Space:** open or select the focused item.

Use Left/Right on the Explorer/Toolbox tabs to switch panels. Exploring the tree
does not create design history entries or mark the project dirty; adding a form
and editing its properties still do.
