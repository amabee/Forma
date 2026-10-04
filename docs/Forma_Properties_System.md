# Forma Properties System

Implementation status: Builder now generates its contextual inspector from
`src/Forma.Builder/Services/InspectorCatalog.cs`. General, Appearance, Typography, Layout,
Behavior, and collapsed Advanced categories are implemented for the current
Form, Button, Label, TextBox, and Panel controls. See
[properties-implementation.md](properties-implementation.md) for supported
properties, validation, and remaining work. The sections below remain the
broader design proposal.

This document defines the proposed Properties Inspector architecture for
**Forma Builder**.

The goal is to provide a modern, contextual, extensible property system
rather than simply copying every traditional WinForms property.

------------------------------------------------------------------------

## 1. Properties Panel Structure

The Properties panel should be organized into contextual categories:

``` text
Properties
├── General
├── Appearance
├── Layout
├── Behavior
├── Typography
├── Advanced
└── Events
```

The available properties should change depending on the selected
control.

------------------------------------------------------------------------

# 2. General

These properties should exist on most controls.

  Property   Example           Description
  ---------- ----------------- ----------------------------------------------
  Name       `button1`         C# identifier for the control
  ID         `button-01`       Unique identifier for the rendered control
  Text       `Click Me`        Displayed text
  Tag        `submit-button`   User-defined metadata
  Enabled    `true`            Enables or disables the control
  Visible    `true`            Controls visibility
  Locked     `false`           Prevents editing the control in the designer
  TabIndex   `0`               Keyboard tab order

### Name

The C# identifier:

``` text
Name: button1
```

Could generate:

``` csharp
var button1 = new Button();
```

### ID

The ID should be separate from the C# name:

``` text
ID: submit-button
```

This can be used by the WebView2/DOM side.

------------------------------------------------------------------------

# 3. Appearance

Appearance controls the visual presentation of a control.

``` text
Appearance
├── Background
├── Foreground
├── Border
├── Border Radius
├── Opacity
├── Shadow
├── Cursor
└── Style
```

  Property       Example
  -------------- ------------------------------------------
  BackColor      `#FFFFFF`
  ForeColor      `#1F2937`
  BorderColor    `#D1D5DB`
  BorderWidth    `1`
  BorderRadius   `6`
  Opacity        `100%`
  Shadow         `None`
  Cursor         `Default`
  Style          `Default / Primary / Secondary / Danger`

## Style Presets

Forma can provide predefined styles:

``` text
Default
Primary
Secondary
Success
Warning
Danger
Custom
```

The user should still be able to override individual visual properties
when using `Custom`.

------------------------------------------------------------------------

# 4. Layout

Layout is one of the most important parts of the visual designer.

``` text
Layout
├── Position
├── Size
├── Margin
├── Padding
├── Alignment
└── Anchoring
```

  Property                     Example
  --------------------- --------------
  X                              `220`
  Y                              `170`
  Width                          `120`
  Height                          `36`
  MinimumWidth                     `0`
  MinimumHeight                    `0`
  MaximumWidth                     `∞`
  MaximumHeight                    `∞`
  Margin                  `0, 0, 0, 0`
  Padding                 `8, 8, 8, 8`
  HorizontalAlignment           `Left`
  VerticalAlignment              `Top`
  Anchor                        `None`
  Dock                          `None`

Example:

``` text
X                    220
Y                    170
Width                120
Height               36

Margin               0  0  0  0
Padding              8  8  8  8

HorizontalAlignment  Center
VerticalAlignment    Center
```

------------------------------------------------------------------------

# 5. Typography

Because Forma uses WebView2 and modern web-based rendering, typography
should be a first-class category.

``` text
Typography
├── Font Family
├── Font Size
├── Font Weight
├── Font Style
├── Text Alignment
├── Line Height
└── Letter Spacing
```

  Property        Example
  --------------- ------------
  FontFamily      `Segoe UI`
  FontSize        `14`
  FontWeight      `400`
  FontStyle       `Normal`
  TextAlign       `Center`
  LineHeight      `1.5`
  LetterSpacing   `0`

------------------------------------------------------------------------

# 6. Behavior

Behavior controls how a control operates.

  Property    Example
  ----------- -------------------
  Enabled     `true`
  Visible     `true`
  ReadOnly    `false`
  Focusable   `true`
  TabIndex    `0`
  AllowDrop   `false`
  ToolTip     `Submit the form`

Not every control needs every behavior property.

For example, a `TextBox` could expose:

``` text
Enabled       ✓
Visible       ✓
ReadOnly      ✓
Multiline     ✓
MaxLength     255
Password      ✓
```

------------------------------------------------------------------------

# 7. Events

Events should preferably be placed in a separate tab rather than mixed
into the normal property list.

``` text
Properties | Events
```

Example:

``` text
Events

Click
[ OnButtonClick       ▼ ]

DoubleClick
[                     ▼ ]

MouseEnter
[                     ▼ ]

MouseLeave
[                     ▼ ]

GotFocus
[                     ▼ ]

LostFocus
[                     ▼ ]
```

## Button Events

``` text
Click
DoubleClick
MouseDown
MouseUp
MouseEnter
MouseLeave
MouseMove
GotFocus
LostFocus
```

## TextBox Events

``` text
TextChanged
KeyDown
KeyUp
KeyPress
GotFocus
LostFocus
```

Eventually, Forma can generate event handlers such as:

``` csharp
button1.Click += (_, _) =>
{
    // User code
};
```

------------------------------------------------------------------------

# 8. Advanced

Advanced properties should remain collapsed by default.

``` text
Advanced
├── CSS Class
├── CSS ID
├── Custom Attributes
├── Custom CSS
├── Z-Index
├── Render Mode
└── Data Binding
```

  Property            Example
  ------------------- --------------------------
  CSS Class           `form-button primary`
  CSS ID              `submitButton`
  Custom Attributes   `data-role="submit"`
  Custom CSS          `letter-spacing: 0.5px;`
  Z-Index             `10`
  Render Mode         `Default`
  Data Binding        `user.name`

These properties should be considered optional escape hatches for
advanced users.

------------------------------------------------------------------------

# 9. Form Properties

When the root Form is selected, the property inspector should show
form-specific properties.

``` text
FORM
────────────────────

General
  Name
  Title
  ID

Appearance
  BackColor
  ForeColor
  BackgroundImage
  Opacity

Layout
  Width
  Height
  MinimumSize
  MaximumSize

Window
  StartPosition
  BorderStyle
  Resizable
  MinimizeBox
  MaximizeBox
  ShowIcon

Behavior
  Enabled
  Visible

Advanced
  CSS Class
  CSS
```

Additional window-specific properties can be added as Forma's native
window implementation evolves.

------------------------------------------------------------------------

# 10. Button Properties

Example Button property layout:

``` text
BUTTON
────────────────────

General
  Name
  Text
  ID
  Tag

Appearance
  Style
  BackColor
  ForeColor
  BorderColor
  BorderWidth
  BorderRadius
  Opacity
  Cursor

Typography
  Font
  FontSize
  FontWeight
  TextAlign

Layout
  X
  Y
  Width
  Height
  Margin
  Padding
  Anchor
  Dock

Behavior
  Enabled
  Visible
  Focusable
  TabIndex
  ToolTip

Advanced
  CSS Class
  CSS ID
  Custom CSS
  Attributes

Events
  Click
  DoubleClick
  MouseEnter
  MouseLeave
  MouseDown
  MouseUp
```

------------------------------------------------------------------------

# 11. TextBox Properties

Example TextBox property layout:

``` text
TEXTBOX
────────────────────

General
  Name
  Text
  Placeholder
  ID

Appearance
  BackColor
  ForeColor
  BorderColor
  BorderWidth
  BorderRadius

Typography
  Font
  FontSize
  FontWeight
  TextAlign

Layout
  X
  Y
  Width
  Height
  Margin
  Padding

Behavior
  Enabled
  Visible
  ReadOnly
  Multiline
  Password
  MaxLength
  CharacterCasing
  ScrollBars
  WordWrap

Events
  TextChanged
  KeyDown
  KeyUp
  KeyPress
  GotFocus
  LostFocus
```

------------------------------------------------------------------------

# 12. Recommended Property Inspector UI

The right-side inspector should look approximately like this:

``` text
┌────────────────────────────────┐
│ Properties                 ×   │
├────────────────────────────────┤
│ Properties       Events        │
├────────────────────────────────┤
│ 🔘 Button1                  ▼  │
├────────────────────────────────┤
│                                │
│ ▼ General                      │
│   Name            button1      │
│   Text            Continue     │
│   ID              button-1     │
│   Tag             ...          │
│                                │
│ ▼ Appearance                   │
│   Style           Primary      │
│   BackColor       ■ #3B82F6   │
│   ForeColor       ■ #FFFFFF   │
│   BorderColor     ■ #2563EB   │
│   BorderWidth     1            │
│   Radius          6            │
│   Opacity         100%         │
│                                │
│ ▼ Typography                   │
│   Font            Segoe UI     │
│   Size            14           │
│   Weight          600          │
│   Align           Center       │
│                                │
│ ▼ Layout                       │
│   X               220          │
│   Y               170          │
│   Width           120          │
│   Height          36           │
│   Margin          0 0 0 0      │
│   Padding         8 8 8 8      │
│                                │
│ ▼ Behavior                     │
│   Enabled         ●            │
│   Visible         ●            │
│   Focusable       ●            │
│   TabIndex        0            │
│                                │
│ ▶ Advanced                     │
│                                │
└────────────────────────────────┘
```

------------------------------------------------------------------------

# 13. Do Not Hardcode the Property Inspector

The property system should be **descriptor-driven**.

Forma should not require the Builder to manually know every property of
every control.

A possible abstraction:

``` csharp
public interface IPropertyDescriptor
{
    string Name { get; }
    string Category { get; }
    Type PropertyType { get; }

    object? GetValue(object target);
    void SetValue(object target, object? value);
}
```

Controls can expose properties using metadata:

``` csharp
[Category("Appearance")]
public string BackColor { get; set; }

[Category("Appearance")]
public string ForeColor { get; set; }

[Category("Layout")]
public int Width { get; set; }

[Category("Layout")]
public int Height { get; set; }

[Category("Behavior")]
public bool Enabled { get; set; }
```

The Builder can then discover these properties and generate the property
editor dynamically.

------------------------------------------------------------------------

# 14. Long-Term Architecture

The property system should follow this relationship:

``` text
                 ┌─────────────────────┐
                 │    Forma Control    │
                 │                     │
                 │  C# Properties      │
                 └──────────┬──────────┘
                            │
                            ▼
                 ┌─────────────────────┐
                 │ Property Descriptor │
                 │                     │
                 │ Name                │
                 │ Category            │
                 │ Type                │
                 │ Get / Set           │
                 └──────────┬──────────┘
                            │
                            ▼
                 ┌─────────────────────┐
                 │  Forma Builder      │
                 │                     │
                 │ Property Inspector  │
                 └──────────┬──────────┘
                            │
                            ▼
                 ┌─────────────────────┐
                 │ WebView2 Renderer   │
                 │                     │
                 │ HTML / CSS / JS     │
                 └─────────────────────┘
```

The **C# control model should remain the source of truth**.

WebView2 should be responsible for rendering the control, not becoming
the primary source of application state.

This helps prevent Forma from becoming overly dependent on JavaScript.

------------------------------------------------------------------------

# 15. Recommended Initial Implementation

Do not implement every property immediately.

For the first usable version, implement:

### Common

``` text
Name
Text
ID
Enabled
Visible
```

### Appearance

``` text
BackColor
ForeColor
BorderColor
BorderWidth
BorderRadius
Opacity
```

### Typography

``` text
FontFamily
FontSize
FontWeight
TextAlign
```

### Layout

``` text
X
Y
Width
Height
Margin
Padding
```

### Advanced

``` text
CSS Class
CSS ID
Custom CSS
```

### Events

``` text
Click
TextChanged
MouseEnter
MouseLeave
GotFocus
LostFocus
```

Then expand the system as more controls are implemented.

------------------------------------------------------------------------

# 16. Design Principle

Forma's Properties system should follow one core principle:

> **Simple by default, powerful when needed.**

A beginner should be able to select a Button and immediately understand:

``` text
Text
Color
Size
Position
```

An advanced developer should be able to expand the inspector and access:

``` text
CSS
Attributes
Data Binding
Events
Layout
Rendering
```

This gives Forma a simple designer experience without preventing
advanced customization.
