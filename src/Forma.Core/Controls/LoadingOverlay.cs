namespace Forma.Core.Controls;

public sealed class LoadingOverlay : Component
{
    private string _targetId = "";
    private bool _isActive;
    public LoadingOverlay() => Text = "Please wait…";
    public string TargetId { get => _targetId; set { ArgumentNullException.ThrowIfNull(value); SetProperty(ref _targetId, value); } }
    public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }

    protected override void WriteRenderState(IDictionary<string, object?> state)
    {
        base.WriteRenderState(state);
        state["isActive"] = IsActive; state["targetId"] = TargetId == "" ? Parent?.Id : TargetId;
    }
}
