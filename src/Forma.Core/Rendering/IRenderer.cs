using Forma.Core.Controls;

namespace Forma.Core.Rendering
{

    /// <summary>
    /// For overall UI rendering, 
    /// this interface defines the methods that a renderer must 
    /// implement to handle the lifecycle of controls in the UI. 
    /// It provides methods for initializing the renderer,
    ///  rendering controls, updating controls, 
    /// and removing controls from the UI. 
    /// Implementations of this interface can vary
    ///  depending on the rendering technology being used 
    /// (e.g., web, desktop, mobile).
    /// </summary>
   
    public interface IRenderer
    {
        Task InitializeAsync();
        Task RenderAsync(Control control);
        Task UpdateAsync(Control control);
        Task RemoveAsync(Control control);
    }
}
