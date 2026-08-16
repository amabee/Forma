namespace Forma.Core.Controls;


// props props props

/// <summary>
/// Represents a base class for all controls in the Forma framework.
/// </summary>

public abstract class Control
{
    public string Id{ get; set; } = Guid.NewGuid().ToString();
    public string? Name  {get; set; }
    public String? Text { get; set; }
    public Control? Parent { get; internal set; }
    public List<Control> Children { get; } = [];
}
