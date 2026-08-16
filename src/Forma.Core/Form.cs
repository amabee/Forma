using Forma.Core.Controls;

namespace Forma.Core;

// inherits from Control, 
// but represents a top-level window 
// or form in the application.
public class Form : Control
{
    public string Title
    {
        get => Text ?? string.Empty;
        set => Text = value;
    }

    public int Width { get; set; } = 1366;
    public int Height { get; set; } = 768;
}
