using Microsoft.Xna.Framework.Graphics;
using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace XnaAndWinforms;

/// <summary>
/// The real implementation of <see cref="IWpfRenderSurfaceHost"/>. Owns the WPF
/// <see cref="Image"/> element and <see cref="DispatcherTimer"/>; delegates bitmap sizing/pixel
/// conversion to <see cref="IWriteableBitmapRenderSurface"/>.
/// </summary>
public class WpfRenderSurfaceHost : IWpfRenderSurfaceHost
{
    private readonly IWriteableBitmapRenderSurface _surface;
    private DispatcherTimer? _timer;

    /// <inheritdoc/>
    public Image ImageElement { get; } = new Image { Stretch = Stretch.None };

    /// <inheritdoc/>
    public byte[] RawImageBuffer => _surface.RawImageBuffer;

    /// <inheritdoc/>
    public bool IsRunning => _timer?.IsEnabled ?? false;

    /// <inheritdoc/>
    public event Action? RenderFrame;

    public WpfRenderSurfaceHost() : this(new WriteableBitmapRenderSurface(new WriteableBitmapPixelBufferWriter()))
    {
    }

    public WpfRenderSurfaceHost(IWriteableBitmapRenderSurface surface)
    {
        _surface = surface;
    }

    /// <inheritdoc/>
    public void Initialize(int width, int height, double desiredFramesPerSecond = 30)
    {
        Resize(width, height);

        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromSeconds(1.0 / desiredFramesPerSecond)
        };
        _timer.Tick += (_, _) => RenderFrame?.Invoke();
        _timer.Start();
    }

    /// <inheritdoc/>
    public void Resize(int width, int height)
    {
        _surface.Resize(width, height);
        ImageElement.Source = _surface.Bitmap;
    }

    /// <inheritdoc/>
    public void PushFrame(SurfaceFormat sourceFormat)
    {
        _surface.Push(sourceFormat);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _timer?.Stop();
    }
}
