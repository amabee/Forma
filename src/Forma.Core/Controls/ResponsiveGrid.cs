namespace Forma.Core.Controls;

/// <summary>An ordered grid whose column count changes at the container breakpoint.</summary>
public sealed class ResponsiveGrid : ResponsiveLayout
{
    private int _narrowColumns = 1;
    public int NarrowColumns
    {
        get => _narrowColumns;
        set => SetProperty(ref _narrowColumns, Math.Clamp(value, 1, 12));
    }
}
