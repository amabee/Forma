# Builder workspace

Run `dotnet run --project src/Forma.Builder` from the repository root. Close the
old running Builder before rebuilding its executable. The host is a Windows
form; menus, toolbar, toolbox, canvas, inspector, and status bar are web UI.

## Manual checks

1. Drag Button, Label, TextBox, or Panel from the toolbox onto the white form.
   Double-click a toolbox entry to insert it at (32, 32).
2. Drag a control already on the form. X/Y in the inspector and status bar should
   follow it, and its position should persist when another control is selected.
3. Change zoom to 125% and drag again. Movement should use form coordinates,
   rather than screen pixels. Controls must stay inside the form's edges.
4. Press Escape during a drag to cancel it. A click without movement should
   select the control without changing its position.
5. Edit Text, font size, colors, Width, Height, X, and Y. Select another control
   and return to confirm each control keeps its own properties.
6. Use arrow keys to move the selected control by one pixel; Shift+arrow uses ten.
   Delete removes the selected control. Input fields keep normal editing keys.
7. Click empty form space to edit the form title and background. Root dimensions
   are fixed at 640 × 440 for this milestone.
8. Preview hides design outlines and lets TextBox accept input. Enabled and
   Visible affect controls in Preview; in Design they appear dimmed so they can
   still be selected. Escape or Back to design returns to editing.
9. Search the toolbox, hide/show side panels through View, and open Help → About.
10. New Form creates a blank design and resets selection. Designs are in memory.

Save/Open, undo/redo, event binding, layout providers, nested panel drops, and
resizing by selection handles are not implemented. Their corresponding UI is
disabled where present. Preview is a visual mode, not a compiled application.

## Architecture

`BuilderWindow.cs` owns the control tree and committed positions in C#.
Appearance and dimensions are currently Builder-owned metadata keyed by control
ID; they are not yet a shared framework styling API. Property updates, drops,
selection, and movement use dedicated designer messages through the bridge.

`DesignerWeb/designer.js` handles pointer capture and immediate drag feedback.
It divides pointer deltas by canvas zoom, clamps bounds, and sends a final move
on pointer release. C# validates the target and clamps positions again before
updating the control. Cancellation restores the original position. Normal
application Click events are suppressed in Design and available in Preview.

Control X/Y properties stay optional: null uses normal flow, explicit coordinates
use absolute positioning. The greeting demo retains its normal layout.

## Your next piece: CheckBox

- Add a `CheckBox` control to Forma.Core with checked state and an event.
- Extend the runtime's DOM control map and event handling to support it.
- Enable its toolbox entry and add its kind to the designer drop whitelist.
- Add its factory case and checked property to the C# inspector state.
- Cover checked-state round trips with a test.

Your About dialog text is preserved in the C# host.

## Automated checks

```powershell
dotnet build Forma.slnx
dotnet test Forma.slnx
node --test tests/WebRuntime/designer.test.cjs
```

JavaScript tests cover drops, selection, dragging at zoom, edge clamping,
cancellation, and Preview mode using a mock DOM. Desktop checks cover integration
and layout; they complement these automated checks.
