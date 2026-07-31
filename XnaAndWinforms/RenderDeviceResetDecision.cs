namespace XnaAndWinforms;

/// <summary>
/// The outcome of <see cref="IRenderDeviceResetPolicy.Evaluate"/>: whether the shared graphics
/// device needs resetting before the next frame, and the pixel size the render target should be.
/// </summary>
public readonly struct RenderDeviceResetDecision
{
    /// <summary>
    /// Whether the device is unrecoverably lost. A lost device is not reset - the app must restart.
    /// </summary>
    public bool IsDeviceLost { get; }

    /// <summary>
    /// Whether the device should be reset before drawing.
    /// </summary>
    public bool NeedsReset { get; }

    /// <summary>
    /// The error message describing why a reset (or restart) is needed, or null when the device is
    /// usable as-is.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// The render target width to use, clamped to at least 1.
    /// </summary>
    public int TargetWidth { get; }

    /// <summary>
    /// The render target height to use, clamped to at least 1.
    /// </summary>
    public int TargetHeight { get; }

    public RenderDeviceResetDecision(bool isDeviceLost, bool needsReset, string? message, int targetWidth, int targetHeight)
    {
        IsDeviceLost = isDeviceLost;
        NeedsReset = needsReset;
        Message = message;
        TargetWidth = targetWidth;
        TargetHeight = targetHeight;
    }
}
