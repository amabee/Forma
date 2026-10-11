using Forma.Core.Rendering;

namespace Forma.Core.Controls;

/// <summary>A single expanded section; children use one-based LayoutSlot values.</summary>
public sealed class Accordion : TabControl
{
    private bool _expanded = true;
    public bool Expanded { get => _expanded; set => SetProperty(ref _expanded, value); }
    public Accordion() => Tabs = ["Section 1", "Section 2"];

    protected override void WriteRenderState(IDictionary<string, object?> state)
    {
        base.WriteRenderState(state);
        state["expanded"] = Expanded;
    }

    protected override void OnRuntimeEvent(RuntimeEvent e)
    {
        base.OnRuntimeEvent(e);
        if (e.Name == "tab" && e.TryGetInt("selectedTab", out _) && e.TryGetBool("expanded", out var expanded)) Expanded = expanded;
    }
}
