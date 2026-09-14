using System;
using RenderingLibrary;
using TextureCoordinateSelectionPlugin.Views;

namespace TextureCoordinateSelectionPlugin.Logic;

/// <summary>
/// Keeps a pair of <see cref="ICameraScrollBar"/>s and the region-selection camera in step:
/// dragging a bar moves the camera, and <see cref="UpdateScrollBarsToCamera"/> re-derives the
/// bars' ranges from the camera and the texture size through <see cref="ScrollBarLogic"/>.
/// </summary>
public class CameraScrollBarBinder
{
    private readonly ScrollBarLogic _scrollBarLogic;
    private ICameraScrollBar? _verticalScrollBar;
    private ICameraScrollBar? _horizontalScrollBar;
    private Camera? _camera;
    private bool _isInScrollBarUpdate;

    /// <summary>Creates the binder over the range math.</summary>
    public CameraScrollBarBinder(ScrollBarLogic scrollBarLogic)
    {
        _scrollBarLogic = scrollBarLogic;
    }

    /// <summary>Attaches the bars to <paramref name="camera"/>.</summary>
    public void Initialize(ICameraScrollBar verticalScrollBar, ICameraScrollBar horizontalScrollBar, Camera camera)
    {
        _verticalScrollBar = verticalScrollBar;
        _verticalScrollBar.ValueChanged += HandleVerticalScrollValueChanged;
        _horizontalScrollBar = horizontalScrollBar;
        _horizontalScrollBar.ValueChanged += HandleHorizontalScrollValueChanged;
        _camera = camera;
    }

    private void HandleVerticalScrollValueChanged(object? sender, EventArgs e)
    {
        if (_isInScrollBarUpdate || _camera == null || _verticalScrollBar == null)
        {
            return;
        }
        _camera.Y = (float)_verticalScrollBar.Value;
    }

    private void HandleHorizontalScrollValueChanged(object? sender, EventArgs e)
    {
        if (_isInScrollBarUpdate || _camera == null || _horizontalScrollBar == null)
        {
            return;
        }
        _camera.X = (float)_horizontalScrollBar.Value;
    }

    /// <summary>Re-derives both bars from the camera and the texture size.</summary>
    public void UpdateScrollBarsToCamera(int spriteWidth, int spriteHeight)
    {
        if (_camera == null || _verticalScrollBar == null || _horizontalScrollBar == null)
        {
            return;
        }

        _isInScrollBarUpdate = true;

        ScrollBarRange horizontalRange = _scrollBarLogic.CalculateHorizontalRange(_camera, spriteWidth);
        _horizontalScrollBar.Minimum = horizontalRange.Minimum;
        _horizontalScrollBar.Maximum = horizontalRange.Maximum;
        _horizontalScrollBar.ViewportSize = horizontalRange.ViewportSize;
        _horizontalScrollBar.Value = horizontalRange.Value;

        ScrollBarRange verticalRange = _scrollBarLogic.CalculateVerticalRange(_camera, spriteHeight);
        _verticalScrollBar.Minimum = verticalRange.Minimum;
        _verticalScrollBar.Maximum = verticalRange.Maximum;
        _verticalScrollBar.ViewportSize = verticalRange.ViewportSize;
        _verticalScrollBar.Value = verticalRange.Value;

        _isInScrollBarUpdate = false;
    }
}
