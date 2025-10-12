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
    private readonly ScrollBar _verticalScrollBar;
    private readonly ScrollBar _horizontalScrollBar;
    private readonly Camera _camera;

    public ScrollBarLogicWpf(ScrollBar verticalScrollBar, ScrollBar horizontalScrollBar, Camera camera)
    {
        _verticalScrollBar = verticalScrollBar;
        _verticalScrollBar.ValueChanged += HandleVerticalScrollValueChanged;
        _horizontalScrollBar = horizontalScrollBar;
        _horizontalScrollBar.ValueChanged += HandleHorizontalScrollValueChanged;

        _camera = camera;
    }

    private void HandleVerticalScrollValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _camera.X = (float)_verticalScrollBar.Value;
    }

    private void HandleHorizontalScrollValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _camera.Y = (float)_horizontalScrollBar.Value;
    }

    public void UpdateScrollBarsToCamera(int spriteWidth, int spriteHeight)
    {
        _horizontalScrollBar.Minimum = -spriteWidth / 2;
        var viewableArea = _camera.ClientWidth / _camera.Zoom;
        var maximum = spriteWidth + spriteWidth / 2;
        _horizontalScrollBar.Maximum = Math.Max(_horizontalScrollBar.Minimum, maximum - viewableArea);
        _horizontalScrollBar.ViewportSize = viewableArea;

        _verticalScrollBar.Minimum = -spriteHeight / 2;
        viewableArea = _camera.ClientHeight / _camera.Zoom;
        maximum = spriteHeight + spriteHeight / 2;
        _verticalScrollBar.Maximum = Math.Max(_verticalScrollBar.Minimum, maximum - viewableArea);
        _verticalScrollBar.ViewportSize = viewableArea;
    }
}
