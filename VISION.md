Forma — Project Vision

A modern C# desktop UI framework that combines the productivity of WinForms with the rendering, styling, and component model of the modern web.

1. Vision

Forma is a Windows-first C# desktop UI framework designed to sit between traditional WinForms and modern web-based desktop frameworks such as Electron/Tauri.

The goal is not to replace WinForms, WPF, Electron, or Tauri.

The goal is to create something new:

WinForms-like developer productivity

C#/.NET application logic

Visual drag-and-drop designer

Property editor

Event handlers and code-behind

Modern HTML/CSS-based rendering

Web-style layouts and components

Modern themes and styling

Custom animations

Extensible UI providers/adapters

User-editable UI definitions

A live visual designer and code editor

Definition of success

Forma is successful when a developer can:

Open Forma Builder.

Create/open a form.

Drag a component onto the form.

Modify its properties visually.

Open and edit the generated/user-owned UI definition.

Write C# code in the editor.

Run the project.

See the resulting desktop application.

Interact with the application normally.

Have events and C# code execute correctly.

At that point, we made Forma.

2. Target User

V0/V1 primary target

The primary user is the creator/developer of Forma.

The project is initially a personal engineering and learning project.

The architecture should still be designed so that it can eventually serve other C# developers.

3. Core Philosophy

3.1 WinForms productivity, modern web rendering

Forma should preserve the things that made WinForms productive:

Forms

Controls

Properties

Events

Designer

Drag-and-drop

Double-click event generation

Code-behind

Simple application startup

But it should replace the traditional visual model with a modern web-oriented rendering model.

C# Application
      |
      v
Forma Runtime
      |
      v
Persistent UI Tree
      |
      v
Incremental Renderer
      |
      v
WebView2
      |
      v
HTML / CSS / JavaScript
      |
      v
Browser rendering/compositing
      |
      v
Desktop Window

3.2 Designer-first AND code-first

The designer should never lock the developer into a visual-only workflow.

Everything should ultimately be accessible through code.

Developers should be able to use:

Designer

Code

Event handlers

Data binding

Custom components

Custom styles

without one approach invalidating the others.

3.3 UI definition belongs to the UI definition file

Forma should NOT recreate the generated *.Designer.cs model used by traditional WinForms.

Instead:

MainForm.forma.json5
MainForm.cs

The UI definition owns the visual structure.

The C# file owns application behavior.

MainForm.forma.json5
        +
MainForm.cs
        |
        v
   Forma Runtime

Developers may manually edit the UI definition.

The designer is an editor for that definition, not its sole owner.

4. UI Definition Format

The initial UI definition format will be JSON5.

Example:

{
    type: "Form",
    name: "MainForm",

    layout: {
        type: "stack",
        direction: "vertical"
    },

    children: [
        {
            type: "Label",
            name: "title",
            text: "Hello Forma"
        },

        {
            type: "Button",
            name: "saveButton",
            text: "Save"
        }
    ]
}

Why JSON5?

JSON5 provides:

JSON compatibility

comments

trailing commas

unquoted keys

predictable hierarchical structure

easy parsing

easy tooling

easy manual editing

YAML may be reconsidered later, but it is not required for V1.

Forma will not invent a proprietary UI markup language unless there is a compelling reason.

5. Controls

Forma's control model should be a hybrid between WinForms controls and web components.

Layout components

Form

Panel

Container

Stack

Grid

Card

Basic controls

Label

Button

TextBox

CheckBox

ComboBox

Image

Modern components

Eventually:

Dialog

Toast

Tabs

Accordion

Badge

Avatar

Navbar

Table

DataGrid

etc.

Forma should NOT attempt to ship hundreds of controls in V1.

6. Layout

Forma should eventually support both traditional desktop positioning and modern layout.

Traditional

X/Y positioning

Width/Height

Anchor

Dock

Modern

Stack

Grid

Flex-like layouts

Fill

Auto sizing

Responsive sizing

Modern layout should be preferred where appropriate, while absolute positioning remains available.

7. Styling

Forma's styling model should be web-oriented.

Core goals:

CSS-based styling

Themes

Design tokens

Dark/light themes

Custom CSS

Custom fonts

SVG

Modern visual effects

Responsive styling

The developer should be able to customize the UI deeply without fighting a fixed native-control styling system.

8. UI Providers / Adapters

Forma should support an extensible UI provider system.

Conceptually:

Forma
  |
  +-- Forma Default
  |
  +-- Bootstrap Provider
  |
  +-- DaisyUI Provider
  |
  +-- Tailwind Provider
  |
  +-- Community Provider
  |
  +-- Custom Provider

A provider may supply:

Styles

Components

Themes

Design tokens

Icons

Animations

Templates

Users should eventually be able to install providers dynamically.

Example:

forma ui add bootstrap
forma ui add daisyui

Exact package-management behavior will be designed later.

9. Events and Code

Forma should support traditional C# event handling.

Example:

button.Click += (_, _) =>
{
    Save();
};

The designer should be able to generate event handlers through actions such as double-clicking a control.

However, the framework should not force generated handlers.

Advanced developers should be able to use:

Events

Lambdas

Binding

Commands

Programmatic control creation

Custom components

The event system should remain extensible.

10. Rendering Architecture

WebView2 will be the initial rendering backend.

The framework should abstract the renderer so the core runtime does not depend directly on WebView2.

Forma.Core
     |
     v
IRenderer
     |
     +-- WebView2Renderer
     |
     +-- FutureRenderer

This allows future experimentation with other rendering backends without rewriting the entire framework.

11. Performance Principles

Avoid reproducing traditional WinForms repaint/flicker behavior.

Forma should use a retained UI tree and incremental rendering model.

Rules

11.1 Do not rebuild the entire UI for every change

A property change should update only the affected element.

button.Text = "Save"
        |
        v
Property change
        |
        v
Incremental update
        |
        v
Only the button changes

11.2 Batch renderer communication

Avoid excessive C# ↔ WebView communication.

Prefer:

Multiple state changes
        |
        v
Update queue
        |
        v
Batched update
        |
        v
WebView

11.3 Let the browser handle layout

Do not unnecessarily recreate a browser layout engine in C#.

11.4 Let the browser handle animation

CSS/Web Animations should handle visual animation whenever practical.

11.5 Keep application logic in C#

C# should own:

Application logic

State

Events

Networking

Database access

Business logic

JavaScript should primarily provide:

DOM/runtime glue

Rendering integration

Browser-side behavior

Web APIs required by the renderer

12. Animation

Animations are not a V0.1 requirement.

The architecture should allow them later.

Possible future API:

button.Animate(...);

and/or UI definition:

{
    animation: {
        enter: "fade",
        hover: "scale",
        exit: "slide"
    }
}

Animation should preferably use browser-native mechanisms rather than C# frame-by-frame timers.

13. Builder

The Forma Builder is a future application, not the first milestone.

Target architecture:

+----------------------------------------------------------+
| Forma Builder                                            |
+--------------+--------------------------+-----------------+
| Toolbox      | Designer                 | Properties      |
|              |                          |                 |
| Button       |       Form Canvas        | Text            |
| Label        |                          | Width           |
| TextBox      |       [ Button ]        | Height          |
| Panel        |                          | Style           |
| Card         |                          | Events          |
+--------------+--------------------------+-----------------+
| Code | Design | Split                                    |
+----------------------------------------------------------+
| C# Code Editor                                           |
+----------------------------------------------------------+

The builder should eventually provide:

Toolbox

Designer

Drag-and-drop

Properties panel

Hierarchy/tree

Code editor

Live preview

Save/load

Event handler generation

The builder should NOT become a full Visual Studio replacement.

14. Project Structure

A Forma application will initially aim toward a structure similar to:

MyApp/
|
+-- MyApp.csproj
+-- app.forma.json5
|
+-- Forms/
|   +-- MainForm.forma.json5
|   +-- SettingsForm.forma.json5
|
+-- Code/
|   +-- MainForm.cs
|   +-- SettingsForm.cs
|
+-- Components/
|   +-- UserCard/
|       +-- UserCard.forma.json5
|       +-- UserCard.cs
|
+-- Styles/
|   +-- app.css
|   +-- theme.css
|
+-- Assets/
|
+-- Packages/
|
+-- build/
|
+-- release/

This structure is provisional and can evolve.

build/ may contain messy/intermediate output.

release/ should contain only files required to distribute the application.

15. Deployment

Long-term goals:

Portable executable

Installer

Self-contained deployment

Release packaging

Windows is the initial target.

Cross-platform desktop support is intentionally deferred.

16. V0.1 — Runtime Foundation

V0.1 is deliberately small.

Required:

C# runtime

.NET

WebView2 renderer

Application

Form

Control

Panel

Label

Button

TextBox

Events

Basic layout

Persistent control tree

JSON5 form definition

C# code-behind

V0.1 success test

A developer should be able to write something conceptually similar to:

var form = new Form
{
    Title = "Hello Forma"
};

var button = new Button
{
    Text = "Click Me"
};

button.Click += (_, _) =>
{
    Console.WriteLine("Clicked!");
};

form.Controls.Add(button);

Application.Run(form);

and see a functioning desktop application rendered through WebView2.

17. V0.5 — Builder

V0.5 adds:

Visual designer

Toolbox

Properties panel

Drag-and-drop

Selection

Save/load

UI definition editing

Code editor

Live preview

Basic project management

18. V1.0 — Usable Framework

V1.0 aims to provide:

Designer

Code editor

UI provider/package system

Themes

Animations

Custom components

Build tooling

Documentation

Windows installer

Portable/self-contained deployment

19. Explicitly Out of Scope

Forma will NOT initially:

become a full IDE

replace Visual Studio

implement its own programming language

implement its own browser engine

support mobile in V1

support every CSS framework in V1

contain hundreds of controls

attempt to recreate the entire .NET desktop ecosystem

20. Technology Direction

Initial stack:

Language:       C#
Runtime:        .NET
Platform:       Windows
Renderer:       WebView2
UI definition:  JSON5
Styling:        HTML/CSS
Browser glue:   JavaScript
IDE/Builder:    C# + WebView2
Code editor:    Future Monaco-based implementation

The core framework should remain renderer-agnostic where practical.

21. Development Principles

Build the smallest working thing first.

Do not build the designer before the runtime works.

Do not optimize by guessing; measure.

Keep rendering incremental.

Keep C# responsible for application logic.

Keep the UI definition editable by humans.

Do not generate ugly Designer.cs files.

Prefer composition and extensibility over giant inheritance trees.

Treat UI providers as an extension point.

Keep the core independent from specific UI providers.

Do not let V1 scope explode.

Every major feature should have a working vertical slice before adding another major subsystem.

22. First Milestone

The first milestone is intentionally tiny:

Create Forma solution
        |
        v
Create Forma.Core
        |
        v
Create Forma.WebView2
        |
        v
Create Forma.Demo
        |
        v
Create Application
        |
        v
Create Form
        |
        v
Create Button
        |
        v
Render Button through WebView2
        |
        v
Receive Click event in C#

Once this works, the project has officially begun.

23. Long-Term Vision

Forma should feel like:

WinForms
   +
Modern Web UI
   +
C#
   +
Visual Designer
   +
Extensible UI Ecosystem

The ideal developer experience is:

Drag it if you want. Code it if you want. Customize it however you want.

And the final test remains simple:

If I can open Forma Builder, drag a component onto a form, modify it, write C# code, run it, and see the application behave correctly, then we fucking made it.
