using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace TextureCoordinateSelectionPlugin.Logic;

public class ScrollBarLogicWpf
{
    private ScrollBar _verticalScrollBar;
    private ScrollBar _horizontalScrollBar;
    private Camera _camera;

    public ScrollBarLogicWpf()
    {
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
    private void HandleVerticalScrollValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (isInScrollBarUpdate) return;
        _camera.Y = (float)_verticalScrollBar.Value;

    }

    private void HandleHorizontalScrollValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (isInScrollBarUpdate) return;
        _camera.X = (float)_horizontalScrollBar.Value;
    }

    public void UpdateScrollBarsToCamera(int spriteWidth, int spriteHeight)
    {
        isInScrollBarUpdate = true;
        var viewableArea = _camera.ClientWidth / _camera.Zoom;
        _horizontalScrollBar.Minimum = -viewableArea / 2;
        var maximum = spriteWidth + viewableArea / 2;
        _horizontalScrollBar.Maximum = Math.Max(_horizontalScrollBar.Minimum, maximum - viewableArea);
        _horizontalScrollBar.ViewportSize = viewableArea;
        _horizontalScrollBar.Value = _camera.X;

        viewableArea = _camera.ClientHeight / _camera.Zoom;
        _verticalScrollBar.Minimum = -viewableArea / 2;
        maximum = spriteHeight + viewableArea / 2;
        _verticalScrollBar.Maximum = Math.Max(_verticalScrollBar.Minimum, maximum - viewableArea);
        _verticalScrollBar.ViewportSize = viewableArea;
        _verticalScrollBar.Value = _camera.Y;
        isInScrollBarUpdate = false;

    }
}
