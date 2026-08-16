using Forma.Core.Controls;

namespace Forma.Core.Rendering;

/// <summary>
/// How an individual control is rendered, 
/// updated, and removed from the UI.
/// </summary>

public interface IControlRenderer
{
    Task CreateAsync(Control control);

    Task UpdateAsync(Control control, 
    string propertyName, object? value);

    Task RemoveAsync(Control control);
}
