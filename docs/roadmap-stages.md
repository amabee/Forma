# Toolbox roadmap stages

Scope: implement the entire catalog in `Forma_Toolbox_Components_Layouts_Controls.md`
in stages. That document is the full backlog; items below marked pending are not
implemented yet. Each stage needs real behavior, contextual properties, persistence,
Undo/Redo where appropriate, and tests before it is called complete.

| Stage | Scope | Status |
| --- | --- | --- |
| 1 | Basic controls, original six layout containers, practical inputs, image picker, Timer/BackgroundWorker, basic DataGridView | Implemented; advanced layout/grid behavior remains below |
| 2 | ListView, TreeView, DataGrid enhancements, Pagination, PropertyGrid, FilePicker, menus, toolbar, status bar, dialog, tooltip/context menu | In progress: ListView, TreeView and Pagination implemented; remaining items pending |
| 3 | Additional layout primitives, responsive containers, Sidebar/AppShell, breadcrumbs, accordion, tabs and command palette | Pending |
| 4 | Cards, badges, avatars, icons, dividers, empty states, toast/notifications, spinner/skeleton, richer selection/button controls | Pending |
| 5 | Form fields, validation, application components, themes and accessibility | Pending |
| 6 | Charts, dashboard widgets, advanced tables, sorting/filtering/grouping, data binding and virtualization | Pending |
| 7 | Calendar/scheduler, task/kanban/timeline/Gantt, productivity and drag/drop components | Pending |
| 8 | Media/document viewers, printing/export, desktop/system services and window features | Pending |
| 9 | Code/Markdown editors, developer tools, terminal/log/debug views, diagrams/canvas, maps and spreadsheets | Pending |
| 10 | Optional AI and collaboration controls, pluggable providers, experimental web/hybrid components | Pending |

Stage 1 includes Button, Label, LinkLabel, TextBox, RichTextBox, MaskedTextBox,
CheckBox, RadioButton, ComboBox, ListBox, CheckedListBox and PictureBox from the
document's Basic Controls section. It also includes the practical input batch
already listed in `toolbox-implementation.md`. PictureBox shares Image's source,
size mode, file picker, and embedded-image persistence.

RichTextBox supports styled runs with bold/italic/underline and paragraph/bullet/
number blocks. Use Properties > Edit content / Finish to edit in Design mode;
those edits are undoable. Preview permits runtime editing unless Read only is on.
Its Advanced rich-document JSON property stores structured text, not arbitrary
HTML or a WinForms RTF document. Plain Text edits replace existing formatting.
Paste imports plain text. Embedded images, hyperlinks inside rich documents,
font-size/color runs, tables, advanced list nesting and RTF import/export remain
future RichTextBox enhancements.

The editor uses the standard browser [Selection API](https://developer.mozilla.org/en-US/docs/Web/API/Selection)
to preserve selected text during formatting. Rich text is reconstructed from
text and formatting flags; document strings are never injected as HTML.

Existing gaps carried forward: splitter dragging, layout reordering,
cross-container drag, multi-select, copy/paste of controls, and C# generation.
Save/Open and session Undo/Redo are implemented. Windows file association is pending.
