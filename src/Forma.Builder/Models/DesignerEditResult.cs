using Control = Forma.Core.Controls.Control;

namespace Forma.Builder;

/// <summary>Presentation feedback after an edit; added controls need view event subscriptions.</summary>
public sealed record DesignerEditResult(string Status, Control? AddedControl = null);
