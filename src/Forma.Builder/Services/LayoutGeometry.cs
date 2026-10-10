namespace Forma.Builder;

/// <summary>Layout math shared by design resize and docking edits. Coordinates are parent-relative.</summary>
public static class LayoutGeometry
{
    public readonly record struct Bounds(int X, int Y, int Width, int Height);
    public static Bounds Anchor(Bounds bounds, string anchor, int dx, int dy)
    {
        var edges = anchor.Split(',');
        var left = edges.Contains("left"); var right = edges.Contains("right");
        var top = edges.Contains("top"); var bottom = edges.Contains("bottom");
        return new(bounds.X + (left ? 0 : right ? dx : dx / 2),
            bounds.Y + (top ? 0 : bottom ? dy : dy / 2),
            bounds.Width + (left && right ? dx : 0), bounds.Height + (top && bottom ? dy : 0));
    }
    public static Bounds Dock(string dock, Appearance appearance, ref Bounds remaining)
    {
        var width = Math.Clamp(appearance.Width + appearance.MarginLeft + appearance.MarginRight, 0, remaining.Width);
        var height = Math.Clamp(appearance.Height + appearance.MarginTop + appearance.MarginBottom, 0, remaining.Height);
        var occupied = remaining;
        switch (dock)
        {
            case "top": occupied = remaining with { Height = height }; remaining = remaining with { Y = remaining.Y + height, Height = remaining.Height - height }; break;
            case "bottom": occupied = remaining with { Y = remaining.Y + remaining.Height - height, Height = height }; remaining = remaining with { Height = remaining.Height - height }; break;
            case "left": occupied = remaining with { Width = width }; remaining = remaining with { X = remaining.X + width, Width = remaining.Width - width }; break;
            case "right": occupied = remaining with { X = remaining.X + remaining.Width - width, Width = width }; remaining = remaining with { Width = remaining.Width - width }; break;
        }
        return new(occupied.X, occupied.Y,
            Math.Max(24, occupied.Width - appearance.MarginLeft - appearance.MarginRight),
            Math.Max(20, occupied.Height - appearance.MarginTop - appearance.MarginBottom));
    }
}
