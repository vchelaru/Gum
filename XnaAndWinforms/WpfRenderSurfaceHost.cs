using Microsoft.Xna.Framework.Graphics;
using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace XnaAndWinforms;

/// <summary>
/// The real implementation of <see cref="IWpfRenderSurfaceHost"/>. Owns the WPF
/// <see cref="Image"/> element and the render-loop trigger; delegates bitmap sizing/pixel
/// conversion to <see cref="IWriteableBitmapRenderSurface"/>.
/// </summary>
/// <remarks>
/// Diagnostic swap (#3833): a <c>DispatcherTimer(DispatcherPriority.Render)</c>-driven loop measured
/// a ~43ms/frame gap outside all known per-frame work (draw/readback/push), independent of buffer
/// size - i.e. the timer itself, not the copy cost, was the ceiling. <see cref="CompositionTarget"/>.
/// <c>Rendering</c> is WPF's actual per-render-pass hook (driven by the composition engine, not a
/// dispatcher timer) and is used here instead, unthrottled, to isolate whether that closes the gap.
/// <paramref name="desiredFramesPerSecond"/> on <see cref="Initialize"/> is currently unused while
/// this experiment is in place.
/// </remarks>
public class WpfRenderSurfaceHost : IWpfRenderSurfaceHost
{
    private readonly IWriteableBitmapRenderSurface _surface;
    private bool _isRunning;

    /// <inheritdoc/>
    public Image ImageElement { get; } = new Image { Stretch = Stretch.None };

    /// <inheritdoc/>
    public byte[] RawImageBuffer => _surface.RawImageBuffer;

    /// <inheritdoc/>
    public bool IsRunning => _isRunning;

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

        CompositionTarget.Rendering += OnRendering;
        _isRunning = true;
    }

    private void OnRendering(object? sender, EventArgs e) => RenderFrame?.Invoke();

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
        if (_isRunning)
        {
            CompositionTarget.Rendering -= OnRendering;
            _isRunning = false;
        }
    }
}
