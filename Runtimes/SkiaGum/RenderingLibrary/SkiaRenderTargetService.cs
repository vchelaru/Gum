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
/// contained renderable. Surfaces are GPU-backed from the host's <c>GRContext</c>, taken from the canvas the
/// renderer draws to (see <see cref="UseContext"/>), so baking and compositing stay on the GPU (#8202). A raster
/// canvas has no context and gets CPU raster surfaces as the fallback.
/// </summary>
internal sealed class SkiaRenderTargetService : RenderTargetServiceBase<SkiaRenderTarget>
{
    /// <summary>The context the cached surfaces were created from; null when they are CPU raster.</summary>
    internal GRRecordingContext? Context { get; private set; }

    /// <summary>
    /// Sets the context new surfaces are created from (the draw canvas's <c>Context</c>, null for a raster canvas).
    /// A different context than the cached surfaces were made from (the host recreated it after a loss, or moved
    /// between a GPU and a raster canvas) drops every cached surface, since a surface only draws on its own context.
    /// </summary>
    internal void UseContext(GRRecordingContext? context)
    {
        if (!ReferenceEquals(Context, context))
        {
            DisposeAll();
            Context = context;
        }
    }

    /// <summary>A premultiplied RGBA surface: GPU-backed when a context is set, CPU raster otherwise.</summary>
    internal SKSurface CreateSurface(int width, int height)
    {
        SKImageInfo info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        SKSurface? gpuSurface = Context != null ? SKSurface.Create(Context, false, info) : null;
        return gpuSurface ?? SKSurface.Create(info);
    }

    protected override SkiaRenderTarget Create(int width, int height)
    {
        return new SkiaRenderTarget(CreateSurface(width, height), width, height);
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
