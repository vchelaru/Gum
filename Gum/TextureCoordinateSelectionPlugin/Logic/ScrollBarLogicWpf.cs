using RenderingLibrary;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace TextureCoordinateSelectionPlugin.Logic;

public class ScrollBarLogicWpf
{
    private readonly ScrollBarLogic _scrollBarLogic;
    private ScrollBar _verticalScrollBar;
    private ScrollBar _horizontalScrollBar;
    private Camera _camera;

    public ScrollBarLogicWpf(ScrollBarLogic scrollBarLogic)
    {
        _scrollBarLogic = scrollBarLogic;
    }

    public void Initialize (ScrollBar verticalScrollBar, ScrollBar horizontalScrollBar, Camera camera)
    {
        _verticalScrollBar = verticalScrollBar;
        _verticalScrollBar.ValueChanged += HandleVerticalScrollValueChanged;
        _horizontalScrollBar = horizontalScrollBar;
        _horizontalScrollBar.ValueChanged += HandleHorizontalScrollValueChanged;

        _camera = camera;
    }

    bool isInScrollBarUpdate = false;
    private void HandleVerticalScrollValueChanged(object? sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (isInScrollBarUpdate) return;
        _camera.Y = (float)_verticalScrollBar.Value;

    }

    private void HandleHorizontalScrollValueChanged(object? sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (isInScrollBarUpdate) return;
        _camera.X = (float)_horizontalScrollBar.Value;
    }

    public void UpdateScrollBarsToCamera(int spriteWidth, int spriteHeight)
    {
        isInScrollBarUpdate = true;

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

        isInScrollBarUpdate = false;
    }
}
