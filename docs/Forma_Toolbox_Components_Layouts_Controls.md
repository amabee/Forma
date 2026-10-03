# Forma Builder Toolbox — Components, Layouts, Controls & Future Items

This document proposes the toolbox catalog for **Forma Builder**. The goal is not to reproduce WinForms exactly. Forma should include familiar desktop controls while also providing modern UI, layout, data, navigation, feedback, and application components.

## Recommended Top-Level Categories

```text
Toolbox
├── Layout
├── Controls
├── Inputs
├── Data
├── Navigation
├── Feedback
├── Media
├── Display
├── Components
├── Containers
├── Windows
└── Advanced
```

# 1. Layout

## Basic Layout
- Panel
- GroupBox
- FlowLayoutPanel
- TableLayoutPanel
- SplitContainer
- TabControl
- ScrollablePanel

## Modern Layout
- StackPanel
- HStack
- VStack
- Grid
- WrapPanel
- UniformGrid
- DockPanel
- CenterPanel
- Overlay
- AbsolutePanel
- ResponsivePanel
- ConstraintPanel

## Responsive Layout
- BreakpointContainer
- ResponsiveGrid
- ResponsiveStack
- AspectRatioContainer

**Recommended priority:** Panel, FlowLayoutPanel, TableLayoutPanel, SplitContainer, TabControl, StackPanel, Grid, WrapPanel, ScrollView, ResponsivePanel.

# 2. Controls

## Basic Controls
- Button
- Label
- LinkLabel
- TextBox
- RichTextBox
- MaskedTextBox
- CheckBox
- RadioButton
- ComboBox
- ListBox
- CheckedListBox
- PictureBox

## Selection Controls
- ToggleSwitch
- ToggleButton
- RadioGroup
- CheckBoxGroup
- SegmentedControl
- Chip
- ChipGroup

## Numeric Controls
- NumericUpDown
- Slider
- RangeSlider
- Rating
- ProgressBar
- CircularProgress

## Date and Time
- DatePicker
- TimePicker
- DateTimePicker
- Calendar
- DateRangePicker
- MonthCalendar

## Menus
- MenuStrip
- ContextMenu
- ContextMenuStrip
- ToolStrip
- Toolbar
- CommandBar

## Modern Buttons
- IconButton
- SplitButton
- DropdownButton
- FloatingActionButton
- ButtonGroup
- CommandButton

# 3. Inputs

## Text Inputs
- TextBox
- TextArea
- RichTextEditor
- SearchBox
- PasswordBox
- CodeEditor
- MarkdownEditor
- NumericInput
- CurrencyInput
- PercentageInput

## Specialized Inputs
- ColorPicker
- FilePicker
- FolderPicker
- FileUpload
- ImagePicker
- FontPicker
- IconPicker
- TagInput
- TokenInput
- Autocomplete
- MultiSelect
- CascadingSelect

## Form Inputs
- FormField
- InputGroup
- InputAddon
- ValidationMessage
- FieldLabel
- FieldDescription

# 4. Data

## Tables
- DataGridView
- DataGrid
- DataTable
- TreeDataGrid
- VirtualDataGrid

## Data Navigation
- Pagination
- PageSizeSelector
- SortSelector
- FilterBar
- SearchFilter
- ColumnChooser

## Data Display
- DataList
- ListView
- TreeView
- PropertyGrid
- CardList
- Timeline
- KanbanBoard

## Charts
- Chart
- LineChart
- BarChart
- AreaChart
- PieChart
- DoughnutChart
- ScatterChart
- RadarChart
- Heatmap
- Gauge
- Sparkline

### DataGrid features to support later
- Sorting
- Filtering
- Grouping
- Column resizing
- Column reordering
- Column hiding
- Frozen columns
- Pagination
- Virtual scrolling
- Row selection
- Multi-selection
- Inline editing
- Cell templates
- Custom formatting
- Export
- Keyboard navigation
- Context menus

# 5. Navigation

- TabControl
- Tab
- NavigationView
- SideNavigation
- Sidebar
- Breadcrumb
- Stepper
- Wizard
- Pagination
- NavigationBar
- BottomNavigation
- CommandPalette
- Menu
- MegaMenu
- Accordion
- TreeNavigation

Modern application navigation should support a first-class Sidebar/AppShell pattern.

# 6. Feedback

## Basic
- MessageBox
- Dialog
- Alert
- Notification
- Toast
- Tooltip
- StatusBar
- StatusMessage

## Loading
- ProgressBar
- CircularProgress
- Spinner
- Skeleton
- LoadingOverlay
- BusyIndicator

## Modern
- Snackbar
- Banner
- Callout
- EmptyState
- ErrorState
- SuccessState
- ConfirmationDialog
- CommandDialog

# 7. Media

- Image
- PictureBox
- ImageViewer
- ImageGallery
- ImageCarousel
- VideoPlayer
- AudioPlayer
- MediaPlayer
- PDFViewer
- DocumentViewer
- WebView
- BrowserView

Future:
- CameraView
- ScreenCapture
- AudioRecorder
- VideoRecorder
- WebcamView
- MapView

# 8. Display

- Label
- Heading
- Text
- RichText
- Icon
- Image
- Avatar
- Badge
- Chip
- Divider
- Separator
- Spacer
- Card
- Callout
- Quote
- CodeBlock
- MarkdownView

Modern display components:
- StatCard
- MetricCard
- ProfileCard
- UserCard
- EmptyState
- Timeline
- ActivityFeed
- StatusBadge
- AvatarGroup

# 9. Components

Components are higher-level reusable UI building blocks and may contain multiple controls.

## Common
- Card
- Header
- Footer
- Toolbar
- SearchBar
- UserMenu
- ProfileCard
- NotificationCenter
- StatusPanel
- ActionBar

## Forms
- Form
- FormSection
- FormGroup
- FormField
- ValidationSummary
- StepForm
- Wizard
- FormActions

## Application
- AppShell
- Sidebar
- HeaderBar
- NavigationBar
- CommandBar
- StatusBar
- Page
- PageHeader
- PageFooter
- ContentArea

## Dashboard
- Dashboard
- DashboardGrid
- StatCard
- MetricCard
- ChartCard
- ActivityCard
- KPI
- Widget
- WidgetContainer

# 10. Containers

- Panel
- GroupBox
- Card
- ScrollView
- TabPage
- SplitPanel
- Drawer
- Modal
- Dialog
- Sheet
- Accordion
- Expander
- CollapsiblePanel

Modern:
- BottomSheet
- SideSheet
- ModalSheet
- Popover
- FloatingPanel
- ResizablePanel
- SplitView

# 11. Windows

- Form
- Window
- DialogWindow
- ModalWindow
- ToolWindow
- SplashScreen
- AboutWindow
- SettingsWindow
- WizardWindow

Future window features:
- Custom title bar
- Borderless window
- Window resizing
- Window snapping
- Always-on-top
- Minimize
- Maximize
- Fullscreen
- System tray integration

# 12. Application / Non-Visual Components

These may eventually belong in a separate Application or Services category rather than the visual toolbox.

- Timer
- BackgroundWorker
- NotificationService
- ClipboardService
- FileDialog
- FolderDialog
- OpenFileDialog
- SaveFileDialog
- PrintDialog
- ColorDialog
- FontDialog
- KeyboardManager
- HotkeyManager
- DragDropManager
- ClipboardManager

# 13. System / Desktop Components

- NotifyIcon
- TrayIcon
- SystemTrayMenu
- Screen
- Monitor
- Clipboard
- Printer
- PrintPreview
- FileSystemWatcher
- ProcessLauncher
- ShortcutManager
- GlobalHotkey

# 14. Advanced Developer Controls

- CodeEditor
- JSONEditor
- MarkdownEditor
- LogViewer
- Terminal
- ConsoleOutput
- PropertyGrid
- ObjectInspector
- DebugPanel
- EventLog
- DiffViewer
- JsonTreeViewer
- XMLViewer
- APIExplorer

# 15. Modern Productivity Controls

## Command Palette
A keyboard-first command launcher, commonly opened with `Ctrl+K`.

Features:
- Search
- Keyboard shortcuts
- Command grouping
- Recent commands
- Fuzzy search
- Icons

## Context Menu
Should support:
- Icons
- Keyboard shortcuts
- Separators
- Nested menus
- Disabled items
- Checkable items
- Command binding

## Toast / Notification
Support:
- Success
- Information
- Warning
- Error
- Auto-dismiss
- Action button
- Progress
- Stacking

## Skeleton
A loading placeholder for content.

## EmptyState
A standard component for lists, dashboards, and pages with no data.

# 16. Drag & Drop

- DropZone
- FileDropZone
- SortableList
- DragHandle
- DragPreview
- DropIndicator
- KanbanBoard

Capabilities:
- Drag
- Drop
- Reorder
- Multi-select drag
- File drag/drop
- Cross-container drag/drop
- Custom drag preview

# 17. Forms & Validation

Components:
- Form
- FormField
- FormSection
- FormGroup
- FieldLabel
- FieldDescription
- ValidationMessage
- ValidationSummary
- RequiredIndicator
- ErrorMessage
- SuccessMessage

Validation rules:
- Required
- MinLength
- MaxLength
- Min
- Max
- Pattern
- Email
- URL
- Numeric
- Date
- Custom

# 18. Authentication / Account UI

These are better as an optional component library rather than core controls.

- LoginForm
- RegisterForm
- ForgotPasswordForm
- OTPInput
- TwoFactorInput
- UserAvatar
- UserMenu
- AccountPanel
- PermissionSelector
- RoleSelector

# 19. Charts & Analytics

- LineChart
- BarChart
- AreaChart
- PieChart
- DoughnutChart
- ScatterChart
- RadarChart
- Heatmap
- Gauge
- Sparkline
- KPI
- MetricCard
- TrendIndicator
- ComparisonCard
- ProgressMetric
- GoalProgress
- DashboardWidget

# 20. Calendar & Scheduling

- Calendar
- MonthView
- WeekView
- DayView
- AgendaView
- TimelineView
- Scheduler
- DateRangePicker
- Event
- EventEditor
- ResourceScheduler

Potential features:
- Drag events
- Resize events
- Recurring events
- Resource columns
- Day/week/month views
- Time slots
- Conflict detection

# 21. Kanban & Project Management

- KanbanBoard
- KanbanColumn
- KanbanCard
- TaskList
- TaskCard
- Timeline
- GanttChart
- ProjectTimeline
- WorkloadView

# 22. AI-Ready Components

These should be optional/future components rather than core controls.

- ChatView
- ChatMessage
- AIInput
- AIResponse
- StreamingText
- PromptInput
- TokenUsage
- ToolCallViewer
- MarkdownAIViewer
- CodeGenerationView
- ConversationList

The framework should not require a specific AI provider.

# 23. Accessibility

Every applicable Forma control should eventually support:

- AccessibleName
- AccessibleDescription
- AccessibleRole
- Keyboard navigation
- Focus management
- High contrast
- Screen reader support
- Reduced motion
- Visible focus indicator
- Tab navigation

These should generally be framework-level properties rather than separate controls.

# 24. Theming

Built-in themes:

```text
Light
Dark
System
High Contrast
```

Theme values:

```text
PrimaryColor
SecondaryColor
BackgroundColor
SurfaceColor
TextColor
MutedTextColor
BorderColor
SuccessColor
WarningColor
DangerColor
FontFamily
Radius
Spacing
Shadow
```

Controls should inherit theme values unless explicitly overridden.

# 25. Experimental / Future

## Web / Hybrid
- WebView
- BrowserView
- HTMLView
- MarkdownView
- RichWebView
- EmbeddedWebApp

## Collaboration
- PresenceIndicator
- UserCursor
- CollaborativeEditor
- ActivityFeed

## Advanced Data
- PivotGrid
- TreeGrid
- FormulaGrid
- Spreadsheet
- SpreadsheetEditor

## Developer Tools
- CodeEditor
- Terminal
- GitDiffViewer
- LogViewer
- JSONEditor
- APIExplorer

## Graphics
- Canvas
- SVGViewer
- VectorCanvas
- DrawingSurface
- DiagramEditor

# 26. Diagramming

- Diagram
- Node
- Connector
- Port
- MiniMap
- DiagramToolbar
- GraphEditor
- FlowEditor

Use cases:
- Workflow builder
- ER diagram
- Network diagram
- Process designer
- Visual programming
- Organization chart

# 27. Maps

Optional/pluggable:
- Map
- MapMarker
- MapPolyline
- MapPolygon
- MapCluster
- MapPopup
- LocationPicker
- RouteViewer

The map provider should be pluggable.

# 28. Printing & Documents

- DocumentViewer
- PDFViewer
- PrintPreview
- PrintDialog
- PagePreview
- ReportViewer
- ReportDesigner
- DocumentEditor

Future:
- PDF generation
- Report templates
- Page headers/footers
- Printing
- Export to PDF
- Export to Excel

# 29. Recommended Initial Toolbox

Do not implement everything at once.

```text
Layout
├── Panel
├── GroupBox
├── FlowLayoutPanel
├── TableLayoutPanel
├── SplitContainer
└── TabControl

Controls
├── Button
├── Label
├── TextBox
├── CheckBox
├── RadioButton
├── ComboBox
├── ListBox
├── PictureBox
├── ProgressBar
└── Slider

Inputs
├── SearchBox
├── PasswordBox
├── NumericInput
├── DatePicker
├── ColorPicker
└── FilePicker

Data
├── DataGrid
├── ListView
├── TreeView
├── Pagination
└── PropertyGrid

Navigation
├── Sidebar
├── Breadcrumb
├── Tabs
├── Accordion
└── CommandPalette

Feedback
├── Dialog
├── Toast
├── Notification
├── Tooltip
├── Spinner
└── Skeleton

Display
├── Card
├── Badge
├── Avatar
├── Icon
├── Divider
└── EmptyState

Components
├── Form
├── FormField
├── Header
├── Footer
├── Toolbar
├── AppShell
└── Dashboard
```

# 30. Architecture: Not Everything Should Be a Control

Do not force every toolbox item into the same base class.

```text
Control
├── Button
├── Label
├── TextBox
├── CheckBox
└── Image

Container
├── Panel
├── Grid
├── StackPanel
├── Card
└── TabControl

Component
├── Form
├── Dashboard
├── Sidebar
├── DataGrid
└── CommandPalette

Service / Non-Visual
├── Timer
├── BackgroundWorker
├── FileSystemWatcher
└── NotificationService
```

This distinction will keep Forma maintainable as it grows.

# 31. What Should Differentiate Forma

Forma should not be "WinForms with prettier buttons."

Major differentiators:

1. Modern layout primitives
2. Responsive layouts
3. Modern controls
4. Reusable components
5. Data-aware controls
6. Theme inheritance
7. Web-based styling
8. C#-first development
9. Visual designer
10. Descriptor-driven Properties panel
11. Strong event system
12. Accessibility
13. Built-in validation
14. Modern navigation
15. Composable components

Core philosophy:

> **Familiar enough for a C# developer, modern enough for today's applications.**

# 32. Suggested Development Priority

## Phase 1 — Core Designer

```text
Panel
Button
Label
TextBox
CheckBox
RadioButton
ComboBox
ListBox
PictureBox
```

Layouts:

```text
Panel
FlowLayoutPanel
TableLayoutPanel
SplitContainer
```

## Phase 2 — Practical Desktop Apps

```text
DataGrid
TreeView
TabControl
ProgressBar
Slider
DatePicker
Menu
Toolbar
StatusBar
Dialog
Tooltip
ContextMenu
```

## Phase 3 — Modern UI

```text
Card
Badge
Avatar
ToggleSwitch
SearchBox
Sidebar
Breadcrumb
Toast
Notification
Skeleton
EmptyState
CommandPalette
```

## Phase 4 — Business Applications

```text
Form
FormField
ValidationSummary
DataGrid
Pagination
FilterBar
Charts
Calendar
Scheduler
Kanban
Dashboard
```

## Phase 5 — Advanced

```text
CodeEditor
MarkdownEditor
Diagram
Canvas
Map
PDFViewer
DocumentViewer
Spreadsheet
AIChat
WebView
```

# 33. Final Toolbox Recommendation

Use these primary categories:

```text
Layout
Controls
Inputs
Data
Navigation
Feedback
Media
Display
Components
Containers
Windows
Advanced
```

Major future differentiators:

```text
Responsive layouts
DataGrid
CommandPalette
Toast / Notification
Sidebar / AppShell
Form validation
Charts
Calendar / Scheduler
Kanban
Dashboard
CodeEditor
DiagramEditor
Map
Spreadsheet
AI components
WebView
Accessibility
Theme system
```

The overall goal is to make Forma a **modern C# application framework with a visual designer**, rather than a direct WinForms replacement with additional controls.
