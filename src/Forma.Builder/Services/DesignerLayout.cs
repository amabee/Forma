using FControl = Forma.Core.Controls.Control;

namespace Forma.Builder;

/// <summary>
/// Design-time geometry: the space a container offers its children, clamped positions,
/// resizing within size limits, and Dock/Anchor arrangement after a container changes size.
/// </summary>
public sealed class DesignerLayout(BuilderViewModel viewModel)
{
    private readonly BuilderViewModel _viewModel = viewModel;

    /// <summary>
    /// The space a container gives its children. <paramref name="content"/> excludes Padding,
    /// which (as in WinForms) only applies to docked and anchored layout; freely positioned
    /// children may use the whole client area.
    /// </summary>
    private (int Width, int Height) AvailableBounds(FControl parent, bool content = true)
    {
        var a = _viewModel.Appearance[parent.Id];
        if (parent is Forma.Core.Controls.ScrollablePanel) return (1600, 1600);
        if (parent == _viewModel.Form)
            return (a.Width, a.Height);
        var header =
            parent is Forma.Core.Controls.Card card && card.HeaderVisible ? 56
            : parent is Forma.Core.Controls.GroupBox ? 28
            : parent is Forma.Core.Controls.Accordion accordion ? (accordion.Tabs.Length + 1) * 36
            : parent is Forma.Core.Controls.TabControl tabs && tabs.Orientation != "vertical" ? 36
            : 0;
        var width = a.Width - a.PaddingLeft - a.PaddingRight - a.BorderOn("left") - a.BorderOn("right");
        var height = a.Height - a.PaddingTop - a.PaddingBottom - a.BorderOn("top") - a.BorderOn("bottom") - header;
        if (parent is Forma.Core.Controls.TabControl verticalTabs && parent is not Forma.Core.Controls.Accordion && verticalTabs.Orientation == "vertical") width -= 120;
        if (parent is Forma.Core.Controls.SplitContainer split)
        {
            if (split.Orientation == "vertical") height = (height - split.Gap) / 2;
            else width = (width - split.Gap) / 2;
        }
        if (parent is Forma.Core.Controls.TableLayoutPanel table)
        {
            width = (width - (table.Columns - 1) * table.Gap) / table.Columns;
            var rows = Math.Max(table.RowCount, (table.Children.Select(child => child.LayoutSlot).DefaultIfEmpty(1).Max() + table.Columns - 1) / table.Columns);
            height = (height - (rows - 1) * table.Gap) / rows;
        }
        else if (!content && parent is not Forma.Core.Controls.SplitContainer)
        {
            width += a.PaddingLeft + a.PaddingRight;
            height += a.PaddingTop + a.PaddingBottom;
        }
        return (Math.Max(24, width), Math.Max(20, height));
    }

    public void Position(FControl control, int x, int y)
    {
        var bounds = _viewModel.Appearance[control.Id];
        // Free positions use the client area: the designer page lets a drag reach the padding,
        // so clamping to the content box made controls jump back on release.
        var parentBounds = AvailableBounds(control.Parent ?? _viewModel.Form!, content: false);
        control.X = Math.Clamp(
            x,
            0,
            Math.Max(0, parentBounds.Width - bounds.Width - bounds.MarginLeft - bounds.MarginRight)
        );
        control.Y = Math.Clamp(
            y,
            0,
            Math.Max(
                0,
                parentBounds.Height - bounds.Height - bounds.MarginTop - bounds.MarginBottom
            )
        );
    }

    public void ResizeControl(FControl control, int width, int height)
    {
        var a = _viewModel.Appearance[control.Id];
        var oldBounds = AvailableBounds(control);
        if (control == _viewModel.Form)
        {
            var minWidth = Math.Max(
                240,
                _viewModel.Form
                    .Children.Where(c => c is not Forma.Core.Controls.INonvisualControl)
                    .Select(c =>
                        (ControlsWidth(_viewModel.Appearance[c.Id]) ? Math.Max(24, _viewModel.Appearance[c.Id].MinimumWidth) : _viewModel.Appearance[c.Id].Width)
                        + _viewModel.Appearance[c.Id].MarginLeft
                        + _viewModel.Appearance[c.Id].MarginRight
                        + (_viewModel.Appearance[c.Id].Locked ? c.X ?? 0 : 0)
                    )
                    .DefaultIfEmpty(0)
                    .Max()
            );
            var minHeight = Math.Max(
                160,
                _viewModel.Form
                    .Children.Where(c => c is not Forma.Core.Controls.INonvisualControl)
                    .Select(c =>
                        (ControlsHeight(_viewModel.Appearance[c.Id]) ? Math.Max(20, _viewModel.Appearance[c.Id].MinimumHeight) : _viewModel.Appearance[c.Id].Height)
                        + _viewModel.Appearance[c.Id].MarginTop
                        + _viewModel.Appearance[c.Id].MarginBottom
                        + (_viewModel.Appearance[c.Id].Locked ? c.Y ?? 0 : 0)
                    )
                    .DefaultIfEmpty(0)
                    .Max()
            );
            _viewModel.Form.Width = a.Width = ConstrainedSize(
                width,
                minWidth,
                1600,
                a.MinimumWidth,
                a.MaximumWidth
            );
            _viewModel.Form.Height = a.Height = ConstrainedSize(
                height,
                minHeight,
                1200,
                a.MinimumHeight,
                a.MaximumHeight
            );
        }
        else
        {
            var parentBounds = AvailableBounds(control.Parent ?? _viewModel.Form!);
            a.Width = ConstrainedSize(
                width,
                24,
                Math.Max(24, parentBounds.Width - a.MarginLeft - a.MarginRight),
                a.MinimumWidth,
                a.MaximumWidth
            );
            a.Height = ConstrainedSize(
                height,
                20,
                Math.Max(20, parentBounds.Height - a.MarginTop - a.MarginBottom),
                a.MinimumHeight,
                a.MaximumHeight
            );
            if (control.Parent is not Forma.Core.Controls.LinearLayout and not Forma.Core.Controls.TableLayoutPanel)
                Position(control, control.X ?? 0, control.Y ?? 0);
        }
        if (control is Forma.Core.Controls.AspectRatioContainer ratioContainer) a.Height = LayoutGeometry.AspectHeight(a, ratioContainer.AspectRatio);
        ArrangeChildren(control, oldBounds);
    }

    public void ReflowLayouts()
    {
        if (_viewModel.Form is not null) ArrangeChildren(_viewModel.Form, AvailableBounds(_viewModel.Form));
    }

    private static bool ControlsWidth(Appearance appearance) => appearance.Dock is "top" or "bottom" or "fill"
        || appearance.Anchor.Contains("left") && appearance.Anchor.Contains("right");

    private static bool ControlsHeight(Appearance appearance) => appearance.Dock is "left" or "right" or "fill"
        || appearance.Anchor.Contains("top") && appearance.Anchor.Contains("bottom");

    private void ArrangeChildren(FControl parent, (int Width, int Height) oldBounds)
    {
        var size = AvailableBounds(parent);
        var managed = parent is Forma.Core.Controls.LinearLayout or Forma.Core.Controls.TableLayoutPanel;
        var children = parent.Children.Where(c => c is not Forma.Core.Controls.INonvisualControl);
        foreach (var group in children.GroupBy(c => parent is Forma.Core.Controls.TabControl or Forma.Core.Controls.SplitContainer or Forma.Core.Controls.BreakpointContainer ? c.LayoutSlot : 0))
        {
            var remaining = new LayoutGeometry.Bounds(0, 0, size.Width, size.Height);
            foreach (var child in group.OrderBy(c => _viewModel.Appearance[c.Id].Dock == "fill" ? 1 : 0))
            {
                var appearance = _viewModel.Appearance[child.Id];
                var current = new LayoutGeometry.Bounds(child.X ?? 0, child.Y ?? 0, appearance.Width, appearance.Height);
                var target = !managed && appearance.Dock != "none" && appearance.Visible
                    ? LayoutGeometry.Dock(appearance.Dock, appearance, ref remaining, (child as Forma.Core.Controls.AspectRatioContainer)?.AspectRatio)
                    : !managed && appearance.Dock == "none"
                        ? LayoutGeometry.Anchor(current, appearance.Anchor, size.Width - oldBounds.Width, size.Height - oldBounds.Height)
                        : current;
                if (target.Width != appearance.Width || target.Height != appearance.Height || size != oldBounds)
                    ResizeControl(child, target.Width, target.Height);
                else ArrangeChildren(child, AvailableBounds(child));
                if (!managed && (target.X != current.X || target.Y != current.Y)) Position(child, target.X, target.Y);
            }
        }
    }

    private static int ConstrainedSize(int value, int floor, int limit, int minimum, int maximum)
    {
        var low = Math.Clamp(minimum, floor, limit);
        var high = maximum == 0 ? limit : Math.Clamp(maximum, low, limit);
        return Math.Clamp(value, low, high);
    }
}
