using System;
using Microsoft.Xna.Framework.Graphics;

namespace XnaAndWinforms;

/// <summary>
/// Owns one client's share of the process-wide graphics device: it reference-counts the shared
/// device, exposes the service provider content loading needs, and keeps a render target sized to
/// whatever surface the client draws at. Hosting framework agnostic - the client supplies a window
/// handle and a surface size, so a WPF element and a WinForms control can each own one without
/// splitting the shared device (and with it <c>LoaderManager</c>'s process-wide texture cache).
/// </summary>
public interface ISharedRenderDeviceHost : IRenderDeviceHost, IDisposable
{
    /// <summary>
    /// The render target drawing goes into, sized by the last <see cref="EnsureSurfaceSize"/> call.
    /// Null until that first call.
    /// </summary>
    RenderTarget2D? RenderTarget { get; }

    /// <summary>
    /// Raised with the new pixel width and height whenever <see cref="RenderTarget"/> is replaced,
    /// so clients can resize any CPU-side buffers they blit it into.
    /// </summary>
    event Action<int, int>? RenderTargetRecreated;

    /// <summary>
    /// Resets the shared device and recreates the render target as needed so the client can draw at
    /// the given pixel size. Failures are reported through <paramref name="error"/> rather than
    /// thrown.
    /// </summary>
    void EnsureSurfaceSize(int surfaceWidth, int surfaceHeight, RenderingError error);
}
