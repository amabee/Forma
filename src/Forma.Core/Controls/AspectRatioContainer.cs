namespace Forma.Core.Controls;

/// <summary>Free-position content in a frame with a preferred width/height ratio.</summary>
public sealed class AspectRatioContainer : LayoutContainer
{
    private double _aspectRatio = 16d / 9d;
    public double AspectRatio
    {
        get => _aspectRatio;
        set {
            if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value), "Aspect ratio must be finite.");
            SetProperty(ref _aspectRatio, Math.Clamp(value, .1, 10));
        }
    }

    protected override void WriteRenderState(IDictionary<string, object?> state)
    {
        base.WriteRenderState(state);
        state["aspectRatio"] = AspectRatio;
    }
}
