namespace Forma.Core.Controls;

public class ContextMenu : CommandControl, INonvisualControl
{
    private string _targetId = "";
    public ContextMenu() => Items = [new("action", "Action"), new("more", "More actions", Items: [new("details", "Details")])];
    /// <summary>Empty targets the parent form. Otherwise this is a control's stable ID.</summary>
    public string TargetId
    {
        get => _targetId;
        set { ArgumentNullException.ThrowIfNull(value); SetProperty(ref _targetId, value); }
    }
}
