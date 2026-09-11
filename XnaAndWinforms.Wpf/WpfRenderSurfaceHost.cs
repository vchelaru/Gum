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
/// Uses <see cref="CompositionTarget"/>.<c>Rendering</c> (WPF's actual per-render-pass hook, driven
/// by the composition engine) rather than a <c>DispatcherTimer</c> - a timer-driven loop measured a
/// ~43ms/frame gap outside all known per-frame work (draw/readback/push), independent of buffer
/// size, i.e. the timer itself was the ceiling, not the copy cost. Unthrottled on
/// <see cref="CompositionTarget.Rendering"/>, measured throughput is vsync-bound: ~60fps at
/// moderate sizes, ~45fps at a maximized 4K window - well within a smooth, usable range, so no
/// throttle knob is exposed here. If a future caller needs to cap the rate (e.g. to save power when
/// idle), add it at that call site rather than reintroducing it speculatively here.
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
    public void Initialize(int width, int height)
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
