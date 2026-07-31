namespace XnaAndWinforms;

/// <inheritdoc/>
public class FrameRateThrottle : IFrameRateThrottle
{
    /// <inheritdoc/>
    public bool ShouldRenderFrame(double millisecondsSinceLastFrame, float desiredFramesPerSecond)
    {
        if (desiredFramesPerSecond <= 0)
        {
            return true;
        }

        return millisecondsSinceLastFrame >= 1000.0 / desiredFramesPerSecond;
    }
}
