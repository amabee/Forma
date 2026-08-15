# ✨ Forma

### Modern desktop UI development with the productivity of WinForms and the power of the web.

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10%2B-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET">
  <img src="https://img.shields.io/badge/C%23-Modern-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C#">
  <img src="https://img.shields.io/badge/WebView2-Chromium-0078D4?style=for-the-badge&logo=microsoftedge&logoColor=white" alt="WebView2">
  <img src="https://img.shields.io/badge/Platform-Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white" alt="Windows">
  <img src="https://img.shields.io/badge/Status-Early%20Development-orange?style=for-the-badge" alt="Status">
</p>

<p align="center"><strong>Drag it. Style it. Code it. Ship it.</strong></p>

---

## 🖼️ What is Forma?

**Forma** is an experimental, Windows-first desktop UI framework for **C#/.NET**.

It aims to combine the developer experience that made WinForms so productive with the rendering, styling, animation, and component ideas of modern web UI.

```text
                 ┌──────────────────────┐
                 │        FORMA          │
                 └──────────┬───────────┘
                            │
             ┌──────────────┴──────────────┐
             │                             │
        WinForms DNA                  Web DNA
             │                             │
        Forms & Controls              HTML / CSS
        Properties                    Themes
        Events                        Animations
        Designer                      Components
        Code-behind                   Modern Layout
             │                             │
             └──────────────┬──────────────┘
                            │
                         C# / .NET
                            │
                         WebView2
                            │
                         Windows
```

Forma is **not** intended to be a replacement for WinForms, WPF, Electron, or Tauri.

It is an attempt to build something that sits somewhere between them.

---

# 🎯 The Goal

> **WinForms productivity with modern web-based UI.**

The long-term developer experience should look something like:

```csharp
var app = new Application();

var form = new Form
{
    Title = "My Forma App"
};

var button = new Button
{
    Text = "Click Me"
};

button.Click += (_, _) =>
{
    Console.WriteLine("Hello from Forma!");
};

form.Controls.Add(button);

app.Run(form);
```

But instead of relying on traditional native WinForms rendering, Forma will use a web-based rendering pipeline:

```text
C# Application
      │
      ▼
Forma Runtime
      │
      ▼
Persistent UI Tree
      │
      ▼
Incremental Renderer
      │
      ▼
WebView2
      │
      ▼
HTML / CSS / JavaScript
      │
      ▼
Browser Rendering + GPU Compositing
```

---

# ✨ Planned Features

> Most of these are planned features, not promises of the current development build.

### 🧱 Core Framework
- [ ] C#/.NET runtime
- [ ] Forms
- [ ] Controls
- [ ] Control tree
- [ ] Events
- [ ] Properties
- [ ] Layout system
- [ ] WebView2 renderer
- [ ] Incremental UI updates

### 🎨 Modern UI
- [ ] HTML/CSS-based styling
- [ ] Dark/light themes
- [ ] Design tokens
- [ ] Custom CSS
- [ ] Modern layouts
- [ ] SVG support
- [ ] Custom fonts
- [ ] Responsive sizing

### 🖱️ Visual Designer
- [ ] Drag-and-drop designer
- [ ] Toolbox
- [ ] Properties panel
- [ ] Component hierarchy
- [ ] Visual selection
- [ ] Live preview
- [ ] Save/load
- [ ] Event handler generation

### 💻 Developer Experience
- [ ] C# code editor
- [ ] Code-behind
- [ ] Designer/code split view
- [ ] Live reload
- [ ] Debugging workflow
- [ ] Project templates
- [ ] CLI tooling

### 🧩 Extensibility
- [ ] Custom components
- [ ] UI providers
- [ ] Theme providers
- [ ] Component packages
- [ ] Community providers
- [ ] Custom design systems

### 🎬 Visual Effects
- [ ] CSS animations
- [ ] Web Animations API integration
- [ ] Transitions
- [ ] Hover effects
- [ ] Enter/exit animations
- [ ] Custom animation APIs

### 📦 Deployment
- [ ] Portable applications
- [ ] Self-contained deployment
- [ ] Release packaging
- [ ] Windows installer

---

# 🧩 UI Providers

One of Forma's long-term goals is to make the visual layer extensible.

```text
Forma
  │
  └── UI Provider
       ├── Forma Default
       ├── Bootstrap
       ├── DaisyUI
       ├── Tailwind-based
       ├── Community Provider
       └── Your Custom Provider
```

A provider may supply:

- Components
- CSS
- Themes
- Design tokens
- Icons
- Animations
- Templates

Developers should eventually be able to install providers dynamically.

```bash
forma ui add bootstrap
forma ui add daisyui
```

---

# 📄 UI Definitions

Forma is planned to use **JSON5** for editable UI definitions.

```json5
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
```

Forma will **not** rely on generated `*.Designer.cs` files for the UI definition.

Instead:

```text
MainForm.forma.json5  → UI structure
MainForm.cs           → application behavior
```

The developer can manually edit the UI definition whenever they want.

---

# ⚡ Performance Philosophy

One of the reasons Forma exists is to avoid reproducing the classic repaint/flicker experience associated with traditional desktop controls.

Forma therefore aims for a **retained UI tree + incremental rendering model**.

```text
Property changed
      │
      ▼
Detect affected element
      │
      ▼
Generate minimal update
      │
      ▼
Update DOM
      │
      ▼
Browser compositor
```

### Core performance principles

- Don't rebuild the entire UI unnecessarily.
- Batch C# ↔ WebView communication.
- Keep the DOM persistent.
- Let the browser handle layout where practical.
- Let the browser handle visual animation.
- Keep application logic in C#.
- Measure before optimizing.

WebView2 does not magically guarantee perfect performance. The framework architecture still has to be designed carefully.

---

# 🏗️ Architecture

```text
┌─────────────────────────────────────────┐
│               Application               │
├─────────────────────────────────────────┤
│             Forma Runtime               │
├─────────────────────────────────────────┤
│               Forma.Core                │
│                                         │
│ Controls │ Events │ Layout │ UI Tree    │
└────────────────────┬────────────────────┘
                     │
                  Renderer
                     │
              ┌──────▼──────┐
              │  WebView2    │
              └──────┬──────┘
                     │
              HTML / CSS / JS
                     │
                 Chromium
```

The core framework should avoid depending directly on WebView2 where practical.

---

# 📁 Planned Project Structure

```text
Forma/
│
├── Forma.sln
│
├── src/
│   ├── Forma.Core/
│   ├── Forma.WebView2/
│   └── Forma.Demo/
│
├── tests/
├── docs/
├── examples/
│
├── build/
├── release/
│
├── VISION.md
└── README.md
```

---

# 🛣️ Roadmap

## Phase 0 — Foundation
- [ ] Create solution
- [ ] Create core project
- [ ] Create WebView2 renderer
- [ ] Create demo host
- [ ] Establish C# ↔ WebView communication

## Phase 1 — Runtime
- [ ] Application
- [ ] Form
- [ ] Control
- [ ] Panel
- [ ] Label
- [ ] Button
- [ ] TextBox
- [ ] Events
- [ ] Basic layout
- [ ] Persistent UI tree

## Phase 2 — UI Definition
- [ ] JSON5 support
- [ ] Serialization
- [ ] Deserialization
- [ ] Editable UI definitions
- [ ] Live reload

## Phase 3 — Layout & Styling
- [ ] Absolute positioning
- [ ] Stack
- [ ] Grid
- [ ] Docking
- [ ] CSS integration
- [ ] Themes
- [ ] Design tokens

## Phase 4 — Builder
- [ ] Visual designer
- [ ] Toolbox
- [ ] Drag-and-drop
- [ ] Properties panel
- [ ] Component hierarchy
- [ ] Save/load

## Phase 5 — Code Experience
- [ ] C# editor
- [ ] Code-behind
- [ ] Event handler generation
- [ ] Designer/code split
- [ ] Live preview

## Phase 6 — Ecosystem
- [ ] Custom components
- [ ] UI providers
- [ ] Package management
- [ ] CLI
- [ ] Community providers

## Phase 7 — Polish
- [ ] Animations
- [ ] Accessibility
- [ ] Performance optimization
- [ ] Hot reload
- [ ] Packaging
- [ ] Installer
- [ ] Documentation

---

# 🧪 Current Status

**Early development — architecture and foundation phase.**

The immediate milestone is intentionally tiny:

```text
Create Forma solution
        ↓
Create Forma.Core
        ↓
Create Forma.WebView2
        ↓
Create Forma.Demo
        ↓
Host WebView2
        ↓
Render HTML
        ↓
Send an event from JavaScript
        ↓
Receive the event in C#
```

Once this works, the first brick of Forma has been laid.

---

# 🛑 Scope Boundaries

Forma will **not** initially:

- ❌ Become a full IDE
- ❌ Replace Visual Studio
- ❌ Implement its own programming language
- ❌ Implement its own browser engine
- ❌ Support mobile in V1
- ❌ Support every CSS/UI framework in V1
- ❌ Ship hundreds of controls
- ❌ Recreate the entire .NET desktop ecosystem

The project should remain focused on:

> **A modern, productive C# desktop UI framework.**

---

# 🧠 Development Philosophy

Forma is being built as an engineering and learning project first.

> **Understand it before abstracting it.**

Prefer a small working implementation over a massive architecture designed for problems that don't exist yet.

```text
Idea
 ↓
Small implementation
 ↓
Working demo
 ↓
Measure
 ↓
Refactor
 ↓
Abstract
 ↓
Next feature
```

---

# 🤝 Contributing

Forma is currently a personal project and is not yet ready for external contribution.

Contribution guidelines may be added once the core architecture stabilizes.

---

# 📜 License

**TBD**

---

# ❤️ The Reason

This project started from a simple frustration:

> *"If WinForms isn't going to become the modern desktop UI framework I want, why don't I build one?"*

WinForms is where many developers first discovered the joy of building software.

Forma is about taking that feeling of:

```text
Drag a control
    ↓
Change a property
    ↓
Double-click an event
    ↓
Write C#
    ↓
Run
```

and asking:

> **What would that experience look like if it had been designed today?**

---

# ⭐ Definition of Success

One day, we want this to work:

```text
┌───────────────────────────────────────────────┐
│                  Forma Builder                │
├──────────────┬──────────────────┬─────────────┤
│   Toolbox    │     Designer     │  Properties │
│              │                  │             │
│   Button     │   ┌──────────┐   │ Text: Save  │
│   Label      │   │   Save   │   │ Width: 120  │
│   TextBox    │   │  Button  │   │ Height: 40  │
│   Panel      │   └──────────┘   │             │
└──────────────┴──────────────────┴─────────────┘
                       │
                       ▼
                 C# Code Editor
                       │
                       ▼
                     Run
                       │
                       ▼
              ┌──────────────────┐
              │    My App        │
              │                  │
              │   [   Save   ]   │
              └──────────────────┘
```

If we can open the builder, drag a component, modify it, write C#, run it, and have the application behave correctly:

# **We fucking made Forma. 🚀**

---

<p align="center">
  <strong>Forma — Build desktop applications the modern way.</strong>
</p>
