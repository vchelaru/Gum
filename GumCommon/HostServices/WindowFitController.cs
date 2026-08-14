using System;

namespace Gum;

/// <summary>
/// Tracks a window-fit policy (zoom-to-window or expand-to-window) and recomputes canvas size and
/// camera zoom when the window resizes. Composed by <c>GumService</c> (issue #3608) and
/// <c>GumServiceSkiaBase</c> (issue #4452) instead of each owning its own inline copy of this state.
/// </summary>
/// <remarks>
/// The owning service supplies how to read the current window size and how to apply a computed fit
/// (camera zoom + canvas size) back onto its own <c>SystemManagers</c>/<c>Root</c> — this class holds
/// only the policy state and the pure math delegation, so it has no dependency on any specific host.
/// </remarks>
public class WindowFitController
{
    private enum FitPolicy
    {
        None,
        Zoom,
        Expand,
    }

    private readonly Func<(int width, int height)> _getWindowSize;
    // Applies a computed (zoom, canvasWidth, canvasHeight) fit to the host.
    private readonly Action<float, float, float> _applyFit;

    private FitPolicy _fitPolicy;
    private WindowZoomMode _fitZoomMode;
    private float _fitDefaultZoom;
    private int? _zoomReferenceWidth;
    private int? _zoomReferenceHeight;

    // Update() polls these against the current window size each frame so resize
    // detection is platform-agnostic — no resize-event subscription on either backend.
    private int _lastSeenWindowWidth;
    private int _lastSeenWindowHeight;

    /// <param name="getWindowSize">Returns the current window (or canvas, for windowless hosts) size.</param>
    /// <param name="applyFit">Applies a computed (zoom, canvasWidth, canvasHeight) fit to the host.</param>
    public WindowFitController(
        Func<(int width, int height)> getWindowSize,
        Action<float, float, float> applyFit)
    {
        _getWindowSize = getWindowSize;
        _applyFit = applyFit;
    }

    /// <summary>
    /// Enables a zoom-based fit policy: the camera scales so the canvas tracks the current window
    /// size, using the window dimensions at the first call as the 1:1 reference. The fit is applied
    /// immediately and then re-applied automatically on each <see cref="PollAndApplyFit"/> call
    /// whenever the window size changes.
    /// </summary>
    /// <param name="mode">
    /// Whether window height or window width drives the zoom factor. The dominant axis fully fills
    /// the window; the other axis gets extra space or is cropped depending on the window's aspect
    /// ratio relative to the reference. Defaults to height-dominant.
    /// </param>
    /// <param name="defaultZoom">
    /// A multiplier applied on top of the computed zoom — i.e., the zoom factor at the reference
    /// resolution.
    /// </param>
    /// <remarks>
    /// Calling this replaces any previously enabled fit policy (including one set by
    /// <see cref="EnableExpandToWindow"/>). The reference resolution is captured on the first call
    /// and persists for the lifetime of this instance.
    /// </remarks>
    public void EnableZoomToWindow(WindowZoomMode mode = WindowZoomMode.HeightDominant, float defaultZoom = 1f)
    {
        _fitPolicy = FitPolicy.Zoom;
        _fitZoomMode = mode;
        _fitDefaultZoom = defaultZoom;
        ApplyCurrentFit();
    }

    /// <summary>
    /// Enables an expand-based fit policy: the canvas is resized to match the current window so
    /// authored UI gets more (or less) space rather than scaling. The fit is applied immediately and
    /// then re-applied automatically on each <see cref="PollAndApplyFit"/> call whenever the window
    /// size changes.
    /// </summary>
    /// <param name="defaultZoom">
    /// A camera zoom multiplier. With <c>1f</c> the canvas matches the window pixel-for-pixel.
    /// </param>
    /// <remarks>
    /// Calling this replaces any previously enabled fit policy (including one set by
    /// <see cref="EnableZoomToWindow"/>).
    /// </remarks>
    public void EnableExpandToWindow(float defaultZoom = 1f)
    {
        _fitPolicy = FitPolicy.Expand;
        _fitDefaultZoom = defaultZoom;
        ApplyCurrentFit();
    }

    private void ApplyCurrentFit()
    {
        if (_fitPolicy == FitPolicy.None)
        {
            return;
        }

        var (windowWidth, windowHeight) = _getWindowSize();
        _lastSeenWindowWidth = windowWidth;
        _lastSeenWindowHeight = windowHeight;
        ApplyFitForSize(windowWidth, windowHeight);
    }

    /// <summary>
    /// Call once per frame. Re-applies the active fit policy only when the window size has changed
    /// since the last call.
    /// </summary>
    public void PollAndApplyFit()
    {
        if (_fitPolicy == FitPolicy.None)
        {
            return;
        }

        var (windowWidth, windowHeight) = _getWindowSize();
        if (windowWidth == _lastSeenWindowWidth && windowHeight == _lastSeenWindowHeight)
        {
            return;
        }

        _lastSeenWindowWidth = windowWidth;
        _lastSeenWindowHeight = windowHeight;
        ApplyFitForSize(windowWidth, windowHeight);
    }

    /// <summary>
    /// Applies the active fit policy for an explicit window size, bypassing the change-detection
    /// <see cref="PollAndApplyFit"/> does. Exposed for tests; hosts should normally call
    /// <see cref="PollAndApplyFit"/> instead.
    /// </summary>
    public void ApplyFitForSize(int windowWidth, int windowHeight)
    {
        switch (_fitPolicy)
        {
            case FitPolicy.Zoom:
                _zoomReferenceWidth ??= windowWidth;
                _zoomReferenceHeight ??= windowHeight;
                var (zoom, zoomCanvasW, zoomCanvasH) = WindowFitMath.ComputeZoom(
                    windowWidth, windowHeight,
                    _zoomReferenceWidth.Value, _zoomReferenceHeight.Value,
                    _fitZoomMode, _fitDefaultZoom);
                _applyFit(zoom, zoomCanvasW, zoomCanvasH);
                break;
            case FitPolicy.Expand:
                var (expandZoom, expandCanvasW, expandCanvasH) = WindowFitMath.ComputeExpand(
                    windowWidth, windowHeight, _fitDefaultZoom);
                _applyFit(expandZoom, expandCanvasW, expandCanvasH);
                break;
        }
    }

    /// <summary>
    /// Clears the active fit policy and captured reference size. Called by the owning service's
    /// <c>Uninitialize</c>.
    /// </summary>
    public void Reset()
    {
        _zoomReferenceWidth = null;
        _zoomReferenceHeight = null;
        _fitPolicy = FitPolicy.None;
        _lastSeenWindowWidth = 0;
        _lastSeenWindowHeight = 0;
    }
}
