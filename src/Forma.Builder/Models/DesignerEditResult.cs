using Control = Forma.Core.Controls.Control;

namespace Forma.Builder;

/// <summary>Presentation feedback after an edit; added controls need view event subscriptions.</summary>
public sealed record DesignerEditResult(string Status, Control? AddedControl = null, IReadOnlyList<Control>? AddedControls = null)
{
    /// <summary>Every top-level control the edit added (one for a drop, several for a paste).</summary>
    public IReadOnlyList<Control> Added => AddedControls ?? (AddedControl is null ? [] : [AddedControl]);
}
