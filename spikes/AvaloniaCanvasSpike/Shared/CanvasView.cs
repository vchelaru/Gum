using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Gum.Wireframe;

namespace AvaloniaCanvasSpike;

/// <summary>
/// Hosts an <see cref="ICanvasRenderer"/> inside Avalonia: presents its pixel buffer through a
/// <see cref="WriteableBitmap"/> sized in device pixels, drives frames from a UI-thread timer,
/// maps pointer events to world coordinates, and draws the hit element's bounds as an overlay.
/// </summary>
public sealed class CanvasView : Grid
{
    private readonly Func<ICanvasRenderer> _rendererFactory;
    private readonly Image _image;
    private readonly Border _selectionOverlay;
    private readonly TextBlock _status;
    private readonly DispatcherTimer _frameTimer;
    private readonly Queue<double> _recentFrameTimes;

    private ICanvasRenderer? _renderer;
    private WriteableBitmap? _bitmap;
    private GraphicalUiElement? _selected;
    private double _scaling;
    private Size _lastLayoutSize;

    /// <summary>Creates the view; the renderer is created on first attach to a window.</summary>
    public CanvasView(Func<ICanvasRenderer> rendererFactory)
    {
        _rendererFactory = rendererFactory;
        _recentFrameTimes = new Queue<double>();
        _scaling = 1.0;
        _lastLayoutSize = default;

        Background = Brushes.Black;
        ClipToBounds = true;

        _image = new Image
        {
            Stretch = Stretch.None,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        RenderOptions.SetBitmapInterpolationMode(_image, BitmapInterpolationMode.None);

        _selectionOverlay = new Border
        {
            BorderBrush = Brushes.Yellow,
            BorderThickness = new Thickness(2),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            IsHitTestVisible = false,
            IsVisible = false,
        };

        _status = new TextBlock
        {
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)),
            Padding = new Thickness(6, 3),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            IsHitTestVisible = false,
            FontFamily = new FontFamily("monospace"),
        };

        Children.Add(_image);
        Children.Add(_selectionOverlay);
        Children.Add(_status);

        _frameTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / 60.0),
        };
        _frameTimer.Tick += (_, _) => RenderAndPresent();

        Focusable = true;
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        _scaling = topLevel?.RenderScaling ?? 1.0;
        _renderer = _rendererFactory();
        ResizeToBounds();
        _frameTimer.Start();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _frameTimer.Stop();
        _renderer?.Dispose();
        _renderer = null;
        _bitmap?.Dispose();
        _bitmap = null;
        base.OnDetachedFromVisualTree(e);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        Size arranged = base.ArrangeOverride(finalSize);
        if (arranged != _lastLayoutSize)
        {
            _lastLayoutSize = arranged;
            ResizeToBounds();
        }
        return arranged;
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
        if (_renderer == null)
        {
            return;
        }

        Point dip = e.GetPosition(this);
        (float worldX, float worldY) = DipToWorld(dip);
        _selected = _renderer.HitTest(worldX, worldY);
        UpdateOverlay();
    }

    /// <inheritdoc/>
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (_renderer == null)
        {
            return;
        }

        float factor = e.Delta.Y > 0 ? 1.25f : 0.8f;
        _renderer.Zoom *= factor;
        UpdateOverlay();
        e.Handled = true;
    }

    private void ResizeToBounds()
    {
        if (_renderer == null || Bounds.Width < 1 || Bounds.Height < 1)
        {
            return;
        }

        int pixelWidth = Math.Max(1, (int)Math.Round(Bounds.Width * _scaling));
        int pixelHeight = Math.Max(1, (int)Math.Round(Bounds.Height * _scaling));
        if (_bitmap != null && _bitmap.PixelSize.Width == pixelWidth && _bitmap.PixelSize.Height == pixelHeight)
        {
            return;
        }

        _renderer.Resize(pixelWidth, pixelHeight);

        _bitmap?.Dispose();
        _bitmap = new WriteableBitmap(
            new PixelSize(pixelWidth, pixelHeight),
            new Vector(96 * _scaling, 96 * _scaling),
            PixelFormat.Rgba8888,
            AlphaFormat.Opaque);
        _image.Source = _bitmap;
        _image.Width = pixelWidth / _scaling;
        _image.Height = pixelHeight / _scaling;
        UpdateOverlay();
    }

    private void RenderAndPresent()
    {
        if (_renderer == null || _bitmap == null)
        {
            return;
        }

        _renderer.RenderFrame();
        CopyIntoBitmap(_renderer.PixelBuffer, _renderer.PixelWidth, _renderer.PixelHeight, _bitmap);
        _image.InvalidateVisual();

        _recentFrameTimes.Enqueue(_renderer.LastFrameMilliseconds);
        while (_recentFrameTimes.Count > 60)
        {
            _recentFrameTimes.Dequeue();
        }
        double average = _recentFrameTimes.Average();
        string selection = _selected == null ? "none" : $"{_selected.Name} ({_selected.GetType().Name})";
        _status.Text =
            $"{_renderer.LoadedElementName}  {_renderer.PixelWidth}x{_renderer.PixelHeight}px  scale {_scaling:0.##}  zoom {_renderer.Zoom:0.##}\n" +
            $"render+readback {average:0.0} ms avg ({_renderer.LastFrameMilliseconds:0.0} ms last)  selected: {selection}\n" +
            $"click to select, wheel to zoom";
    }

    private static void CopyIntoBitmap(byte[] rgba, int width, int height, WriteableBitmap bitmap)
    {
        using ILockedFramebuffer framebuffer = bitmap.Lock();
        int rowBytes = width * 4;
        if (framebuffer.RowBytes == rowBytes)
        {
            Marshal.Copy(rgba, 0, framebuffer.Address, rowBytes * height);
            return;
        }

        for (int y = 0; y < height; y++)
        {
            Marshal.Copy(rgba, y * rowBytes, framebuffer.Address + y * framebuffer.RowBytes, rowBytes);
        }
    }

    private (float worldX, float worldY) DipToWorld(Point dip)
    {
        float zoom = _renderer?.Zoom ?? 1f;
        float pixelX = (float)(dip.X * _scaling);
        float pixelY = (float)(dip.Y * _scaling);
        return (pixelX / zoom, pixelY / zoom);
    }

    private void UpdateOverlay()
    {
        if (_selected == null || _renderer == null)
        {
            _selectionOverlay.IsVisible = false;
            return;
        }

        float zoom = _renderer.Zoom;
        double left = _selected.AbsoluteX * zoom / _scaling;
        double top = _selected.AbsoluteY * zoom / _scaling;
        double width = _selected.AbsoluteWidth * zoom / _scaling;
        double height = _selected.AbsoluteHeight * zoom / _scaling;

        _selectionOverlay.Margin = new Thickness(left, top, 0, 0);
        _selectionOverlay.Width = Math.Max(1, width);
        _selectionOverlay.Height = Math.Max(1, height);
        _selectionOverlay.IsVisible = true;
    }
}
