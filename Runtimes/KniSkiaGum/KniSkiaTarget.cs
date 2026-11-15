#if KNISKIA
#nullable enable

using SkiaGameRendering;
using SkiaGum.Helpers;
using SkiaSharp;
using XnaGraphicsDevice = Microsoft.Xna.Framework.Graphics.GraphicsDevice;

namespace Gum;

/// <summary>
/// The render target <see cref="GumService"/> draws into, as <see cref="SkiaTargetHolder{TTarget}"/> sees
/// it. <see cref="End"/> composites the target onto the back buffer.
/// </summary>
internal interface IKniSkiaTarget : ISkiaHostTarget
{
    /// <summary>Ends the pass and composites the target onto the back buffer at the origin, at native size.</summary>
    void End();
}

/// <summary>
/// A SkiaGameRendering <see cref="SkiaRenderTarget2D"/> as an <see cref="IKniSkiaTarget"/>. Creating it
/// attaches <see cref="SkiaRenderer"/> to the device, so the service never calls the renderer to start it.
/// </summary>
internal sealed class KniSkiaTarget : IKniSkiaTarget
{
    private readonly SkiaRenderTarget2D _target;

    public KniSkiaTarget(XnaGraphicsDevice graphicsDevice, int width, int height) =>
        _target = new SkiaRenderTarget2D(graphicsDevice, width, height);

    public SKCanvas Canvas => _target.Canvas;

    // SkiaRenderTarget2D.Begin clears the target to transparent.
    public void Begin() => _target.Begin();

    public void End() => _target.End();

    public void EndWithoutDrawing() => _target.EndWithoutDrawing();

    public void Dispose() => _target.Dispose();
}
#endif
