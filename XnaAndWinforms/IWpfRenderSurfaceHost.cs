using Microsoft.Xna.Framework.Graphics;
using System;
using System.Windows.Controls;

namespace XnaAndWinforms;

/// <summary>
/// A WPF-native render surface: a plain <see cref="Image"/> element backed by a
/// <see cref="System.Windows.Media.Imaging.WriteableBitmap"/>, updated on its own render-loop timer.
/// Deliberately does not own a <c>GraphicsDevice</c> or <c>RenderTarget2D</c> - the caller draws
/// into its own render target on <see cref="RenderFrame"/>, reads it back into
/// <see cref="RawImageBuffer"/>, then calls <see cref="PushFrame"/> to blit it into the bitmap.
/// This keeps the host reusable by any caller that owns a render target (e.g. a future WPF-native
/// replacement for <c>WireframeControl</c>/<c>ImageRegionSelectionControl</c>), and keeps the sizing
/// logic in <see cref="IWriteableBitmapRenderSurface"/> unit-testable independent of the timer.
/// </summary>
public interface IWpfRenderSurfaceHost : IDisposable
{
    /// <summary>The element to host in a WPF visual tree.</summary>
    Image ImageElement { get; }

    /// <summary>
    /// The buffer to pass to <c>RenderTarget2D.GetData</c> before calling <see cref="PushFrame"/>.
    /// </summary>
    byte[] RawImageBuffer { get; }

    /// <summary>Whether the render-loop timer is currently running.</summary>
    bool IsRunning { get; }

    /// <summary>
    /// Raised on the render-loop timer's tick. Subscribers should draw into their render target,
    /// read it back into <see cref="RawImageBuffer"/>, then call <see cref="PushFrame"/>.
    /// </summary>
    event Action? RenderFrame;

    /// <summary>
    /// Sizes the backing bitmap and starts the render-loop timer. Safe to call only once; use
    /// <see cref="Resize"/> for subsequent size changes.
    /// </summary>
    void Initialize(int width, int height, double desiredFramesPerSecond = 30);

    /// <summary>(Re)sizes the backing bitmap and <see cref="RawImageBuffer"/> to match.</summary>
    void Resize(int width, int height);

    /// <summary>
    /// Converts <see cref="RawImageBuffer"/> (already filled by the caller) into the backing
    /// bitmap and refreshes <see cref="ImageElement"/>.
    /// </summary>
    void PushFrame(SurfaceFormat sourceFormat);
}
