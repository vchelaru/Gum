namespace Gum.Avalonia.Canvas;

/// <summary>
/// One canvas's side of on-demand rendering: on top of what the shared
/// <see cref="ICanvasRedrawScheduler"/> asks for, a canvas draws its first frame, after its surface
/// changes size, after frames it skipped while hidden or minimized, and while a failed frame retries.
/// </summary>
public sealed class CanvasFrameGate
{
    private readonly ICanvasRedrawScheduler _scheduler;
    private bool _isDrawOwed;
    private int _drawnWidth;
    private int _drawnHeight;

    /// <summary>Creates the gate; the first frame is always drawn.</summary>
    public CanvasFrameGate(ICanvasRedrawScheduler scheduler)
    {
        _scheduler = scheduler;
        _isDrawOwed = true;
    }

    /// <summary>Whether the canvas should draw a frame at the given surface size.</summary>
    public bool ShouldDraw(int width, int height, bool hasRenderError) =>
        _isDrawOwed ||
        width != _drawnWidth ||
        height != _drawnHeight ||
        hasRenderError ||
        _scheduler.IsRedrawNeeded;

    /// <summary>Records a drawn and presented frame.</summary>
    public void MarkDrawn(int width, int height)
    {
        _isDrawOwed = false;
        _drawnWidth = width;
        _drawnHeight = height;
    }

    /// <summary>
    /// Records a frame the canvas could not draw (hidden or minimized). What it shows may be stale
    /// by the time it is visible again, so its next frame draws.
    /// </summary>
    public void MarkSkipped() => _isDrawOwed = true;
}
