using System;
using Microsoft.Xna.Framework.Graphics;

namespace XnaAndWinforms;

/// <summary>
/// What a canvas control supplies to <see cref="RenderTargetFrameLoop"/>: the per-frame logic
/// that runs before drawing, the draw itself, how a finished render target reaches the screen,
/// and how an error is shown. Framework-neutral so the WPF and Avalonia controls share one loop.
/// </summary>
public interface IRenderTargetFrameClient
{
    /// <summary>Runs per-frame logic (input polling, editor activity) before the draw.</summary>
    void PreDrawUpdate();

    /// <summary>Draws the frame into the render target the loop has already bound and cleared.</summary>
    void Draw();

    /// <summary>
    /// Reads <paramref name="renderTarget"/> back and shows it. Called after the draw with the
    /// device's render target unbound.
    /// </summary>
    void Present(RenderTarget2D renderTarget);

    /// <summary>Shows <paramref name="message"/> over the canvas, or hides the message when null.</summary>
    void ShowError(string? message);

    /// <summary>Called when a frame throws; the loop also records the message for display.</summary>
    void ReportError(Exception exception);
}
