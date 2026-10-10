# Toolbox roadmap stages

Scope: implement the entire catalog in `Forma_Toolbox_Components_Layouts_Controls.md`
in stages. That document is the full backlog; items below marked pending are not
implemented yet. Each stage needs real behavior, contextual properties, persistence,
Undo/Redo where appropriate, and tests before it is called complete.

| Stage | Scope | Status |
| --- | --- | --- |
| 1 | Basic controls, original six layout containers, practical inputs, image picker, Timer/BackgroundWorker, basic DataGridView | Implemented; advanced layout/grid behavior remains below |
| 2 | ListView, TreeView, DataGrid enhancements, Pagination, PropertyGrid, FilePicker, menus, toolbar, status bar, dialog, tooltip/context menu | Initial batch implemented, including Tooltip and DataGrid sorting/filtering/row selection; advanced data features continue in stage 6 |
| 3 | Additional layout primitives, responsive containers, Sidebar/AppShell, breadcrumbs, accordion, tabs and command palette | Pending |
| 4 | Cards, badges, avatars, icons, dividers, empty states, toast/notifications, spinner/skeleton, richer selection/button controls | Card, Badge, Avatar, Divider, Toast, Spinner, LoadingOverlay, Icon, EmptyState, Skeleton, RadioGroup, CheckBoxGroup, SegmentedControl and Rating implemented; Chip, ChipGroup, ButtonGroup, IconButton and FloatingActionButton implemented; SplitButton, DropdownButton and CommandButton implemented; extended catalog remains in the full backlog |
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

Existing gaps carried forward: splitter dragging,
multi-select, copy/paste of controls, and C# generation.
Dragging controls between free-position containers now updates their parent tree.
Save/Open and session Undo/Redo are implemented. Windows file association is pending.

FilePicker and FolderPicker select a single existing file or directory through
native dialogs in Builder Preview. Their inspector Choose actions are undoable
design edits. PropertyGrid supports categorized string values, per-entry and
whole-grid read-only settings, and edit events. Selected-object reflection,
typed editors and automatic data binding remain future PropertyGrid enhancements.

MenuStrip and Toolbar/ToolStrip share validated command trees with nested items,
disabled states, separators and checkable commands. ItemClicked reports the
chosen leaf command; application code supplies its action. StatusBar displays
left and right text. Automatic docking, shortcut bindings, item icons and a
visual command editor remain enhancements; current command configuration uses
the contextual JSON property.

Context menus attach to a named target and share the existing command item
format. Dialogs are modal message/confirmation components with configurable
title, message, buttons and Escape dismissal. Arbitrary dialog content trees,
dialog windows, task dialogs and native system dialog components remain later
roadmap items.

Tooltip is a nonvisual component with a named Target, text, initial delay,
duration, and top/bottom/left/right placement. It supports mouse hover and
keyboard focus in Preview, maintains aria-describedby, and closes on Escape,
scroll/resize, target removal, or timeout. The simpler per-control ToolTip property
continues to use a native browser title.

DataGridView supports stable numeric/text column sorting, case-insensitive
filtering over displayed columns, row selection by click or keyboard, and
editing when Read only is off. Selection and edit events use the original Rows
index; sorting/filtering do not reorder the source data. Configuration persists
through save/open and design undo/redo. Grouping, virtualization, binding,
column resizing/reordering, multi-row selection and typed column editors remain
future advanced data features.
