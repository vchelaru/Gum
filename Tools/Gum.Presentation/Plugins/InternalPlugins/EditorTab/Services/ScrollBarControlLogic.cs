using System;
using Gum.Controls;
using RenderingLibrary;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

/// <summary>
/// Keeps the wireframe editor's two scroll bars in sync with the camera: the bars' ranges follow
/// the displayed area and the surface size, and moving either bar pans the camera.
/// </summary>
public class ScrollBarControlLogic
{
    #region Fields

    private readonly IScrollBar _verticalScrollBar;
    private readonly IScrollBar _horizontalScrollBar;
    private readonly IScrollSurface _surface;

    private int _minimumX;
    private int _minimumY;

    private int _displayedAreaWidth;
    private int _displayedAreaHeight;

    private Camera? _camera;

    #endregion

    #region Properties

    /// <summary>
    /// The camera the bars scroll. Null until the rendering surface has been initialized, during
    /// which time the bars are left alone.
    /// </summary>
    public Camera? Camera
    {
        get => _camera;
        set
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Camera value should not be null");
            }
            _camera = value;
        }
    }

    #endregion

    /// <summary>
    /// Creates the logic against an already-constructed pair of bars. The caller owns creating and
    /// positioning them, so the same logic serves a WinForms and a WPF host.
    /// </summary>
    public ScrollBarControlLogic(IScrollBar horizontalScrollBar, IScrollBar verticalScrollBar, IScrollSurface surface)
    {
        _horizontalScrollBar = horizontalScrollBar;
        _verticalScrollBar = verticalScrollBar;
        _surface = surface;

        _verticalScrollBar.ValueChanged += HandleVerticalScroll;
        _horizontalScrollBar.ValueChanged += HandleHorizontalScroll;

        SetDisplayedArea(2048, 2048);

        _surface.SizeChanged += HandleSurfaceSizeChanged;
    }

    private void HandleSurfaceSizeChanged(object? sender, EventArgs e)
    {
        UpdateScrollBars();
    }

    private void HandleVerticalScroll(object? sender, EventArgs e)
    {
        if (_camera != null)
        {
            _camera.Y = _verticalScrollBar.Value;
        }
    }

    private void HandleHorizontalScroll(object? sender, EventArgs e)
    {
        if (_camera != null)
        {
            _camera.X = _horizontalScrollBar.Value;
        }
    }

    /// <summary>
    /// Snaps both bars to the camera's current position, clamped to each bar's range.
    /// </summary>
    public void UpdateScrollBarsToCameraPosition()
    {
        if (_camera == null)
        {
            return;
        }

        // Setting Value below round-trips synchronously through ValueChanged -> HandleVerticalScroll
        // / HandleHorizontalScroll, which write the (int), clamped bar value back into _camera.X/Y.
        // Restore the camera's own fractional position afterward, the same way UpdateScrollBars()
        // does, so this doesn't truncate/clamp the camera position on every call.
        var x = _camera.X;
        var y = _camera.Y;

        _verticalScrollBar.Value =
            Math.Min(Math.Max(_verticalScrollBar.Minimum, (int)y), _verticalScrollBar.Maximum);

        _horizontalScrollBar.Value =
            Math.Min(Math.Max(_horizontalScrollBar.Minimum, (int)x), _horizontalScrollBar.Maximum);

        _camera.X = x;
        _camera.Y = y;
    }

    /// <summary>
    /// Sets the size of the area the bars can scroll over. The area is centered on the origin, so it
    /// starts half its size to the left of and above the origin.
    /// </summary>
    public void SetDisplayedArea(int? width = null, int? height = null)
    {
        if (width != null)
        {
            _displayedAreaWidth = width.Value;
            _minimumX = -width.Value / 2;
        }

        if (height != null)
        {
            _displayedAreaHeight = height.Value;
            _minimumY = -height.Value / 2;
        }

        UpdateScrollBars();
    }

    /// <summary>
    /// Recomputes both bars' ranges from the displayed area, the surface size, and the camera zoom.
    /// </summary>
    public void UpdateScrollBars()
    {
        if (_camera == null)
        {
            return;
        }

        // This clamps the scroll bar, but we don't want to adjust the position of the camera when this is called
        // because the user may manually move the camera beyond the bounds:
        var x = _camera.X;
        var horizontalValue = Math.Max(x, _horizontalScrollBar.Minimum);
        horizontalValue = Math.Min(horizontalValue, _horizontalScrollBar.Maximum);
        _horizontalScrollBar.Value = (int)horizontalValue;

        var y = _camera.Y;
        var verticalValue = Math.Max(y, _verticalScrollBar.Minimum);
        verticalValue = Math.Min(verticalValue, _verticalScrollBar.Maximum);
        _verticalScrollBar.Value = (int)verticalValue;

        // now preserve the values:
        _camera.X = x;
        _camera.Y = y;

        var effectiveAreaHeight = -_minimumY + _displayedAreaHeight;

        var visibleAreaHeight = _surface.Height / _camera.Zoom;
        _verticalScrollBar.Minimum = _minimumY;
        _verticalScrollBar.Maximum = _minimumY + (int)(effectiveAreaHeight + visibleAreaHeight);
        _verticalScrollBar.LargeChange = (int)visibleAreaHeight;

        var visibleAreaWidth = _surface.Width / _camera.Zoom;

        var effectiveAreaWidth = -_minimumX + _displayedAreaWidth;

        _horizontalScrollBar.Minimum = _minimumX; // The minimum value for the scroll bar, which should be 0, since that's the furthest left the scrollbar can go

        // The total amount that the scrollbar can cover. This is the width of the area plus the screen width since we can scroll until the edges
        // are at the middle, meaning we can see half a screen width on either side
        _horizontalScrollBar.Maximum = _minimumX + (int)(effectiveAreaWidth + visibleAreaWidth);
        _horizontalScrollBar.LargeChange = (int)visibleAreaWidth; // the amount of visible area. It's called LargeChange but it really means how much the scrollbar can see
    }
}
