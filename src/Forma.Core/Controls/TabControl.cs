using Forma.Core.Rendering;

namespace Forma.Core.Controls;

public class TabControl : LayoutContainer
{
    protected override void OnRuntimeEvent(RuntimeEvent e)
    {
        base.OnRuntimeEvent(e);
        if (e.Name == "tab" && e.TryGetInt("selectedTab", out var tab)) SelectedTab = tab;
    }
}
