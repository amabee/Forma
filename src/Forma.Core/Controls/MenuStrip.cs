namespace Forma.Core.Controls;

public sealed class MenuStrip : CommandControl
{
    public MenuStrip() => Items = [
        new("file", "File", Items: [new("new", "New"), new("open", "Open"), new("save", "Save")]),
        new("edit", "Edit", Items: [new("undo", "Undo"), new("redo", "Redo")]),
        new("help", "Help", Items: [new("about", "About")])
    ];
}
