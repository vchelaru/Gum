namespace XnaAndWinforms;

/// <summary>
/// Decides whether enough time has passed to render another frame. Pulled out of the render loop
/// so the decision is testable without a graphics device: a WPF render surface is driven by
/// <c>CompositionTarget.Rendering</c>, which fires at the compositor's own cadence rather than at a
/// requested rate, so the requested rate has to be honored by skipping passes.
/// </summary>
public interface IFrameRateThrottle
{
    /// <summary>
    /// Returns whether a frame should be rendered now. A non-positive
    /// <paramref name="desiredFramesPerSecond"/> means unthrottled - every pass renders.
    /// </summary>
    bool ShouldRenderFrame(double millisecondsSinceLastFrame, float desiredFramesPerSecond);
}
