using SkiaSharp;

namespace RenderingLibrary.Graphics;

/// <summary>
/// A cached offscreen surface a render-target container bakes its subtree into. Held across frames
/// by <see cref="SkiaRenderTargetService"/> so the snapshot taken each frame stays pixel-valid via
/// SkiaSharp's copy-on-write until the next bake replaces it.
/// </summary>
internal sealed class SkiaRenderTarget
{
    public SKSurface Surface { get; }
    public int Width { get; }
    public int Height { get; }

    public SkiaRenderTarget(SKSurface surface, int width, int height)
    {
        Surface = surface;
        Width = width;
        Height = height;
    }
}

/// <summary>
/// Per-render-target-container offscreen surface cache for SkiaGum (#3988), keyed by the container's
/// contained renderable. Allocates CPU raster (premultiplied) surfaces — no <c>GRContext</c> — so it
/// works in every Skia host regardless of GPU backing. A GPU-backed surface would be faster on GPU
/// hosts but requires threading the host's <c>GRContext</c> into the renderer; deferred (#3989).
/// </summary>
internal sealed class SkiaRenderTargetService : RenderTargetServiceBase<SkiaRenderTarget>
{
    protected override SkiaRenderTarget Create(int width, int height)
    {
        SKSurface surface = SKSurface.Create(
            new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        return new SkiaRenderTarget(surface, width, height);
    }

    protected override void Destroy(SkiaRenderTarget renderTarget)
    {
        renderTarget.Surface.Dispose();
    }

    protected override int GetWidth(SkiaRenderTarget renderTarget) => renderTarget.Width;

    protected override int GetHeight(SkiaRenderTarget renderTarget) => renderTarget.Height;
}
