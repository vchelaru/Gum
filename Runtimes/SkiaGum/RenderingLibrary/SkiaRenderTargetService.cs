using SkiaSharp;
using System.Collections.Generic;

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
/// What a <see cref="RenderTargetRefreshMode.Manual"/> container's cached surface was baked at. The cache is valid
/// while the container's size, the camera zoom and the refresh version all still match. The device-pixel fraction is
/// kept because the surface holds the content offset by it, so the composite must undo the same amount even after the
/// container moved.
/// </summary>
internal sealed class SkiaCachedImage
{
    public float ContainerWidth { get; }
    public float ContainerHeight { get; }
    public float Zoom { get; }
    public int RefreshVersion { get; }
    public float FractionX { get; }
    public float FractionY { get; }

    public SkiaCachedImage(float containerWidth, float containerHeight, float zoom, int refreshVersion, float fractionX, float fractionY)
    {
        ContainerWidth = containerWidth;
        ContainerHeight = containerHeight;
        Zoom = zoom;
        RefreshVersion = refreshVersion;
        FractionX = fractionX;
        FractionY = fractionY;
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

    // Cached results of manual-mode containers, by owner. The surface itself lives in the base class's
    // dictionary, so the frame-boundary sweep releases it; OnReleased drops the matching entry.
    private readonly Dictionary<IRenderableIpso, SkiaCachedImage> _images = new Dictionary<IRenderableIpso, SkiaCachedImage>();

    protected override void OnReleased(IRenderableIpso owner) => _images.Remove(owner);

    /// <summary>Whether <paramref name="owner"/> holds a manual-mode cached image.</summary>
    public bool HasImage(IRenderableIpso owner) => _images.ContainsKey(owner);

    public bool TryGetImage(IRenderableIpso owner, out SkiaCachedImage image)
        => _images.TryGetValue(owner, out image!);

    /// <summary>
    /// The cached image for <paramref name="owner"/> when it was baked at exactly this container size, zoom and
    /// refresh version; null when it has to be baked. Does not mark the owner used.
    /// </summary>
    public SkiaRenderTarget? GetValidImageTarget(IRenderableIpso owner, float containerWidth, float containerHeight, float zoom, int refreshVersion)
    {
        if (_images.TryGetValue(owner, out SkiaCachedImage? image)
            && image.ContainerWidth == containerWidth && image.ContainerHeight == containerHeight
            && image.Zoom == zoom && image.RefreshVersion == refreshVersion)
        {
            return TryGetExisting(owner);
        }

        return null;
    }

    public void CommitImage(IRenderableIpso owner, SkiaCachedImage image) => _images[owner] = image;

    protected override int GetWidth(SkiaRenderTarget renderTarget) => renderTarget.Width;

    protected override int GetHeight(SkiaRenderTarget renderTarget) => renderTarget.Height;
}
