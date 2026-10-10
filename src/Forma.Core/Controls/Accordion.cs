namespace Forma.Core.Controls;

/// <summary>A single expanded section; children use one-based LayoutSlot values.</summary>
public sealed class Accordion : TabControl
{
    private bool _expanded = true;
    public bool Expanded { get => _expanded; set => SetProperty(ref _expanded, value); }
    public Accordion() => Tabs = ["Section 1", "Section 2"];
}
