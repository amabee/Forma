# Forma architecture

Forma uses a layered architecture with an incremental MVVM implementation in the Builder: a C# control model, a WebView2 rendering adapter, and a browser-based designer view. The boundaries below describe the existing code and the direction for future refactoring.

## Existing layers

Linear layout containers share `LinearLayout : LayoutContainer` so the editing
service and designer use the same child ordering rules. StackPanel/HStack/VStack,
WrapPanel/FlowLayoutPanel, and CenterPanel keep children in managed flow; free
positions are retained for ScrollablePanel. Navigation controls reuse ChoiceControl
and the selection renderer, with application scripts handling navigation events.

| Layer | Responsibility | Examples |
| --- | --- | --- |
| Core | Control state, validation, parent/child relationships, events, rendering contracts | `Control`, `NumericControl`, `IRenderer`, `IBridge` |
| WebView2 | Translate model updates into browser messages and browser interactions into model events; render controls in the DOM | `WebView2Renderer`, `WebView2Bridge`, `forma.js` |
| Builder view model and editing service | Form, selection, appearance, preview state, project identity, dirty/title notifications, document creation/application, editing rules, and history commands | `BuilderViewModel`, `DesignerEditingService` |
| Builder view and services | Native window integration, bridge dispatch, rendering, inspector descriptors, and project persistence | `BuilderWindow`, `InspectorCatalog`, `ProjectFile` |
| Designer browser UI | Toolbox, inspector editors, drag/resize gestures, alignment guides, and preview interaction | `designer.js`, generated Tailwind styles |

Core must remain independent of WinForms, WebView2, DOM, CSS, and native file dialogs. Renderer interfaces provide a boundary for a different rendering implementation. Browser gestures send commands to the C# host, where authoritative control state and validation live; the browser keeps transient interaction state.

`DesignerWeb/reactivity.js` owns the script-side observer scopes for refs,
shallow reactive state, computed values, effects, and control bindings.
`component-customization.js` connects those scopes to the runtime get/set bridge
and disposes them with their component behavior. Binding reads reflect live DOM
input and projected runtime state; writes remain C#-validated bridge commands.
This is a browser runtime service supporting the View, separate from the Builder's
document ViewModel and the Core model. Component-local script state is not saved
into the design or shared implicitly across behaviors.

The control tree follows the **Composite** pattern: controls contain child controls. Property notifications and control events follow the **Observer** pattern. The renderer and bridge form an **adapter** between C# models and the browser. These describe specific parts of the code, not a claim that the whole application already follows one formal pattern.

## Current folder structure and MVVM roles

This is the current source layout, with representative control files shown. Build output, dependencies, and individual icon files are omitted.

```text
Forma/
├── src/
│   ├── Forma.Core/                         Model and framework contracts
│   │   ├── Form.cs                         Root of the control tree
│   │   ├── Controls/                       One control/supporting type per file
│   │   │   ├── Control.cs                  Shared state, children, notifications
│   │   │   ├── NumericControl.cs           Shared numeric behavior
│   │   │   ├── Button.cs
│   │   │   └── TextBox.cs                  Other controls live alongside these
│   │   ├── Events/                         Framework input event arguments
│   │   └── Rendering/                      Renderer and bridge interfaces
│   ├── Forma.WebView2/                     Rendering infrastructure
│   │   ├── WebView2Renderer.cs             Model-to-browser rendering adapter
│   │   ├── WebView2Bridge.cs               Browser message transport
│   │   └── Web/                            Framework runtime view assets
│   │       ├── index.html
│   │       ├── scripts/forma.js
│   │       └── css/forma.css
│   ├── Forma.Builder/                      Designer application
│   │   ├── Program.cs                     Application entry point
│   │   ├── Models/
│   │   │   ├── Appearance.cs              Designer-specific Model data
│   │   │   ├── ComponentCustomization.cs  Portable CSS, behavior and custom values
│   │   │   └── DesignerEditResult.cs      Edit feedback for the View
│   │   ├── ViewModels/
│   │   │   └── BuilderViewModel.cs        Designer state and edit/history commands
│   │   ├── Views/
│   │   │   ├── BuilderWindow.cs           Native View and bridge integration
│   │   │   ├── PreviewWindow.cs           Runtime window and native dialogs
│   │   │   └── ComponentEditorView.cs   Built-in/external source editor View
│   │   ├── Services/
│   │   │   ├── CoalescedRefresh.cs       Merge same-turn view state requests
│   │   │   ├── DesignerEditingService.cs  Add/delete/move/resize/property/layer rules
│   │   │   ├── DesignHistory.cs           Undo/redo support service
│   │   │   ├── ProjectFile.cs             Persistence service and file DTOs
│   │   │   ├── ComponentEditorService.cs  Boilerplate and source file round trips
│   │   │   ├── ComponentSaveSession.cs    Save commits and duplicate suppression
│   │   │   ├── PreviewSession.cs          Isolated runtime tree and script setters
│   │   │   └── InspectorCatalog.cs        Inspector property descriptors
│   │   ├── DesignerWeb/                   Browser portion of the View
│   │   │   ├── index.html
│   │   │   ├── designer.js
│   │   │   ├── component-customization.js Scoped styles and preview behavior adapter
│   │   │   ├── preview.html               Runtime-only browser View
│   │   │   ├── designer.css               Generated stylesheet
│   │   │   └── icons/                     Local SVG assets
│   │   └── Frontend/                      View asset build tooling
│   │       ├── input.css                  Tailwind source
│   │       ├── build.cjs
│   │       └── package.json
│   └── Forma.Demo/                         Example consumer of the framework
├── tests/
│   ├── Forma.Tests/                       Model, ViewModel, service/renderer tests
│   └── WebRuntime/                        Browser runtime and designer DOM tests
└── docs/                                  Architecture, behavior, and roadmap
```

**Model:** `Forma.Core` contains reusable control state and rules. Builder-specific data such as `Appearance` stays in `Forma.Builder`, because the framework does not need to know how the designer stores styling. `Rendering/` contains infrastructure contracts, so not every file in Core is an MVVM Model.

**ViewModel:** `BuilderViewModel.cs` exposes the designer's state, notifications, and history command methods. It coordinates model data without referencing the native window or browser. Future inspector and toolbox view models belong to the Builder application too.

**View:** `BuilderWindow.cs` and `DesignerWeb/` together present the designer. The window hosts WebView2 and native dialogs; browser assets display controls and capture gestures. `Forma.WebView2/Web/` supplies reusable control rendering beneath that designer View. `Frontend/` builds the View's assets and contains no application state.

**Services and infrastructure:** `DesignerEditingService`, `DesignHistory`, `ProjectFile`, `InspectorCatalog`, and the WebView2 adapter support these roles. The editing service owns control creation, bounds, property validation, deletion, and layer ordering; the view model wraps editing batches in history tracking. MVVM does not require every helper or project to be classified as Model, View, or ViewModel.

### How an interaction crosses these folders

```mermaid
flowchart LR
    Browser[View: DesignerWeb] -->|gesture or command| Window[View: BuilderWindow]
    Window -->|command methods| VM[ViewModel: BuilderViewModel]
    VM -->|editing rules| Editing[Service: DesignerEditingService]
    Editing -->|read or change state| Model[Model: Core controls and Appearance]
    VM -->|PropertyChanged| Window
    Window -->|designer state through bridge| Browser
    Model -->|control notifications| Renderer[Infrastructure: Forma.WebView2]
    Renderer -->|render messages| Browser
```

For example, moving a button travels from `designer.js` through `BuilderWindow` to `BuilderViewModel.ExecuteEdit()`. The view model captures history, calls the editing service to validate and clamp the position, and records the result. The window then projects the updated state to the browser. Preview and lock guards are enforced by the service as well as by the browser.

Dragging a control into a panel changes its parent in the control tree, rather than only placing it visually over the panel. The browser hit-tests containers beneath the dragged control and sends the destination parent ID with coordinates relative to its content area. The editing service validates the destination, prevents cycles, and preserves the control and its descendants. Children move with their panel because their coordinates are relative to that parent. Dragging back onto the form removes the control from the panel. Parent changes are saved in project files and recorded as one undoable move.

Undo travels through the same View to `BuilderViewModel.Undo()`. The view model supplies a history snapshot; the window currently restores it through `ProjectFile`, applies the document to the view model, and updates the renderer and browser state. This explicit bridge serves the binding role that XAML would normally provide.

### Builder folder conventions

The Builder now uses these folders. Place future extractions alongside the existing responsibilities:

| Folder under `Forma.Builder/` | Files or responsibilities |
| --- | --- |
| `Models/` | `Appearance.cs` and other designer data |
| `ViewModels/` | `BuilderViewModel.cs`, future inspector/toolbox view models |
| `Views/` | `BuilderWindow.cs` and native presentation adapters |
| `Services/` | History, persistence, inspector metadata, and application command services |
| `DesignerWeb/` | Existing browser View assets |
| `Frontend/` | Existing asset build tooling |

`Program.cs` stays at the project root as the composition entry point. Public C# namespaces remain `Forma.Builder`; source links in the test project follow the folder paths. Dependencies matter more than folder names: view models and editing services must stay independent of Views, while Views may depend on view models.

Preview uses `Services/PreviewSession.cs` to clone the form and appearance into
an independent runtime tree. `Views/PreviewWindow.cs` hosts that tree with the
same WebView2 renderer and shared appearance presentation used by the designer.
`DesignerWeb/preview.html` contains only the running form, without editor chrome.
Runtime edits and timers belong to the preview session; closing the native window
disposes them. The Builder remains in Design mode. This is an in-process runtime
preview; generated C# projects and distributable executables are future export work.
The native Preview window supports resize, maximize and restore. Its form surface
fills larger viewports; controls retain their designed coordinates. Smaller
viewports scroll rather than shrink the designed canvas.

## Control source organization

Each public control, shared base class, event argument type, and document record has its own named file in `src/Forma.Core/Controls`. Namespaces and public APIs remain unchanged. A tiny derived control can have a tiny file; this gives it a clear home when its behavior grows.

Keep state, validation, and model events with the control. Share behavior through focused bases such as `NumericControl` and `ChoiceControl`, or small helpers when duplication warrants them. Do not place DOM rendering, inspector editors, native dialogs, or persistence logic inside controls. Category folders can be introduced later without changing public namespaces.

`INonvisualControl` marks controls that belong in the component tray and do not
participate in form geometry. It allows a context menu to inherit shared command
behavior while remaining nonvisual. `Component` implements the same marker for
timers, workers and dialogs. Temporary context-menu popups and modal dialog
elements belong to the browser View; their model configuration remains in Core.

Splitting files does not by itself separate responsibilities. The runtime and Builder still contain large switches and orchestration methods; those require their own incremental refactors.

## MVVM in the Builder

The Model is the Core control tree and appearance data. `BuilderViewModel` implements `INotifyPropertyChanged` and owns designer session state. `BuilderWindow` and the browser workspace are the View. The window binds its title to view-model notifications; browser state is explicitly projected through the existing bridge rather than XAML bindings. History command methods are invoked by bridge dispatch, so an `ICommand` wrapper is not required for this view technology.

The view model has no WinForms, WebView2, DOM, or dialog dependency and can be tested without opening a window. Native dialogs and renderer lifecycle belong to the view/infrastructure. The bridge refreshes computed dirty state after each mutation batch, including changes to appearance metadata.

Designer editing now goes through `BuilderViewModel.ExecuteEdit()` and `DesignerEditingService`, including add, delete, move, resize, property changes, image source changes, and layer ordering. The detailed inspector state projection and native save/open orchestration still live in `BuilderWindow`; they can move to focused view-model/application services as the migration continues. Core and renderer layers do not need to become view models.

## Next architectural refactors

1. Build on the extracted `BuilderViewModel` designer session, keeping it distinct from serialized project DTOs. Extract an inspector view model when its editing responsibilities grow.
2. Extract the inspector state projection and project lifecycle orchestration. Keep native file dialogs and WebView2 lifecycle in the View, while application services coordinate document operations. Editing commands already use the shared validation/history boundary.
3. Introduce an explicit control definition registry for construction and supported capabilities. Currently type knowledge is repeated across the toolbox, inspector, persistence whitelist, renderer, and browser runtime. Migrate incrementally, keeping an explicit allowlist for file loading; do not instantiate arbitrary types from project files.
4. Separate browser rendering implementations by control family or control as they grow, with a small dispatcher and shared DOM helpers. Preserve the current browser message contract and tests during this change.

Use these extractions when expanding the roadmap rather than doing a wholesale framework rewrite. Keep browser bindings explicit and view models independent of their rendering technology.

## Persistence and verification

Project files contain versioned data, not live renderer objects, native resources,
or .NET event-handler delegates. Optional component customization stores portable
CSS/JavaScript source strings and custom JSON values in Appearance. Styles apply
in Design and Preview; JavaScript is initialized only in the runtime window.
`ComponentEditorService` creates editing files, while the native editor View opens
installed editors. Save and Ctrl+S apply immediately; a debounced file watcher
applies external saves while the editor is open. `ComponentSaveSession` suppresses
duplicate notifications and retains the last successful commit after validation
or application failures. `BuilderViewModel.ExecuteEdit("customize")` validates
and commits each changed save as one undoable operation. The browser behavior
adapter sends typed property changes to `PreviewSession`, which owns the runtime
copy. Core control classes do not depend on the source editor or JavaScript.
`ProjectFile` validates and restores models; history uses document snapshots.
Preserve existing `.forma` compatibility when reorganizing code.

Use Core tests for control invariants and events, renderer tests for message contracts, project/history tests for restoration and undo/redo, and DOM tests for browser behavior. A source-only split should leave existing tests and public declarations unchanged.


### Modern controls and MVVM

Each modern control has its own model file under `src/Forma.Core/Controls`.
Card extends LayoutContainer for ownership; Avatar extends Image for image
selection/persistence; Toast extends Component for its nonvisual lifecycle.
DesignerEditingService and InspectorCatalog implement edit validation and property
metadata. BuilderViewModel owns the design and history; PreviewSession owns an
independent runtime copy. WebView2Renderer translates model changes/events to the
browser. `Web/scripts/modern-controls.js` is View code for rendering, positioning,
animation and notification/overlay cleanup. Preview scripts issue commands through
the bridge; PreviewWindow dispatches Toast commands and PreviewSession changes
Active on Spinner/LoadingOverlay. DOM elements never become the saved model.

### Docked workspace and code assistance

`Views/ComponentEditorView.cs` is a native UserControl hosting the code WebView.
BuilderWindow owns its lifecycle and connects saves to BuilderViewModel commands.
`DesignerWeb/workspace.js` is View code: the activity rail, property search, divider
and dock rectangle. It sends bounds after layout; the native editor initializes
once those bounds are available. DesignerWeb stores no committed source state.

`Frontend/code-editor.mjs` owns CodeMirror views, format/save commands and bridge
messages. `code-assistance.mjs` supplies syntax diagnostics and completions from
current component names and the runtime API contract. Sources remain
ComponentCustomization model data; ComponentSaveSession and ComponentEditorService
still validate, save and watch external files. Editor diagnostics are authoring
feedback, not an execution engine or a generated C# application.

### Responsive and section containers

`Controls/ResponsiveLayout.cs` is the shared Model for AppShell and
ResponsivePanel, with a validated Breakpoint. Sidebar reuses LinearLayout;
Accordion extends TabControl with Expanded and section child slots. Editing,
Undo/Redo, serialization and PreviewSession remain the ViewModel boundary.
The browser View uses one ResizeObserver per responsive container, lays out
children using flex, and disconnects observers when a subtree is removed.
Accordion renders native section buttons and sends selection/expanded commands
through the bridge. Browser geometry and transient selection styling do not
become additional persisted models.

Dock and Anchor belong to the Builder Appearance model. LayoutGeometry holds
pure docking/anchoring calculations; DesignerEditingService applies them within
the current edit before history capture. Parent resizing carries child anchors
through nested containers. layout-properties.js is the browser View counterpart:
it observes free content hosts, applies runtime geometry, and releases observers
on removal/reset. It does not replace stack/table/AppShell layout rules or store
DOM coordinates as project data. Script edits change PreviewSession appearance,
then the refreshed runtime snapshot updates the View.
