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
/// canvas has no context and gets CPU raster surfaces as the fallback. Each context (and the raster case) has its
/// own cache, so a host that alternates canvases (a raster export canvas next to the GPU canvas, via
/// <c>GumBatch.Begin</c>) never throws away the other canvas's surfaces or manual caches.
/// </summary>
internal sealed class SkiaRenderTargetService
{
    private readonly SkiaContextRenderTargets _raster = new SkiaContextRenderTargets(null);
    private readonly Dictionary<GRRecordingContext, SkiaContextRenderTargets> _gpu = new Dictionary<GRRecordingContext, SkiaContextRenderTargets>();
    private SkiaContextRenderTargets _current;

    // Raster first, then each context's cache; indexed so the per-frame walks allocate nothing.
    private readonly List<SkiaContextRenderTargets> _all = new List<SkiaContextRenderTargets>();

    public SkiaRenderTargetService()
    {
        _all.Add(_raster);
        _current = _raster;
    }

    /// <summary>The context new surfaces are created from; null when they are CPU raster.</summary>
    internal GRRecordingContext? Context => _current.Context;

    /// <summary>
    /// Selects the cache for the draw canvas's <c>Context</c> (null for a raster canvas). A context is recognized by
    /// reference, so a host that recreates its context after a loss gets fresh surfaces and the old context's cache is
    /// released once its owners stop being drawn. A context that was abandoned (lost) or disposed is unusable: its
    /// cache is released and drawing falls back to raster, so the same wrapper object can never serve stale surfaces.
    /// </summary>
    internal void UseContext(GRRecordingContext? context)
    {
        if (context == null)
        {
            _current = _raster;
        }
        else if (SkiaContextRenderTargets.IsUnusable(context))
        {
            if (_gpu.TryGetValue(context, out SkiaContextRenderTargets? lost))
            {
                Forget(context, lost);
            }
            _current = _raster;
        }
        else
        {
            if (!_gpu.TryGetValue(context, out SkiaContextRenderTargets? targets))
            {
                targets = new SkiaContextRenderTargets(context);
                _gpu[context] = targets;
                _all.Add(targets);
            }
            _current = targets;
        }
    }

    /// <summary>A premultiplied RGBA surface for the current canvas: GPU-backed when it has a context, CPU raster otherwise.</summary>
    internal SKSurface CreateSurface(int width, int height) => _current.CreateSurface(width, height);

    public SkiaRenderTarget? GetFor(IRenderableIpso owner, int width, int height)
    {
        MarkUsedOnOtherCanvases(owner);
        return _current.GetFor(owner, width, height);
    }

    public void MarkUsed(IRenderableIpso owner)
    {
        MarkUsedOnOtherCanvases(owner);
        _current.MarkUsed(owner);
    }

    // An owner drawn on one canvas keeps its surfaces for the other canvases alive (a raster export canvas drawn
    // now and then must not cost the GPU canvas its manual cache), unless that canvas's context was disposed.
    private void MarkUsedOnOtherCanvases(IRenderableIpso owner)
    {
        for (int i = 0; i < _all.Count; i++)
        {
            SkiaContextRenderTargets targets = _all[i];
            if (!ReferenceEquals(targets, _current) && !targets.IsContextUnusable && targets.HasCachedRenderTarget(owner))
            {
                targets.MarkUsed(owner);
            }
        }
    }

    public SkiaRenderTarget? GetValidImageTarget(IRenderableIpso owner, float containerWidth, float containerHeight, float zoom, int refreshVersion)
        => _current.GetValidImageTarget(owner, containerWidth, containerHeight, zoom, refreshVersion);

    public void CommitImage(IRenderableIpso owner, SkiaCachedImage image) => _current.CommitImage(owner, image);

    /// <summary>The cached surface for <paramref name="owner"/>, preferring the current canvas's.</summary>
    public SkiaRenderTarget? TryGetExisting(IRenderableIpso owner)
    {
        SkiaRenderTarget? target = _current.TryGetExisting(owner);
        if (target != null)
        {
            return target;
        }
        for (int i = 0; i < _all.Count; i++)
        {
            SkiaContextRenderTargets targets = _all[i];
            target = targets.TryGetExisting(owner);
            if (target != null)
            {
                return target;
            }
        }
        return null;
    }

    /// <summary>Whether <paramref name="owner"/> holds a manual-mode cached image on any canvas.</summary>
    public bool HasImage(IRenderableIpso owner)
    {
        for (int i = 0; i < _all.Count; i++)
        {
            SkiaContextRenderTargets targets = _all[i];
            if (targets.HasImage(owner))
            {
                return true;
            }
        }
        return false;
    }

    public bool TryGetImage(IRenderableIpso owner, out SkiaCachedImage image)
    {
        if (_current.TryGetImage(owner, out image))
        {
            return true;
        }
        for (int i = 0; i < _all.Count; i++)
        {
            SkiaContextRenderTargets targets = _all[i];
            if (targets.TryGetImage(owner, out image))
            {
                return true;
            }
        }
        return false;
    }

    public void Remove(IRenderableIpso owner)
    {
        for (int i = 0; i < _all.Count; i++)
        {
            SkiaContextRenderTargets targets = _all[i];
            targets.Remove(owner);
        }
    }

    /// <summary>How many owners hold a cached surface, across every canvas. For tests and diagnostics.</summary>
    public int CachedCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < _all.Count; i++)
            {
                SkiaContextRenderTargets targets = _all[i];
                count += targets.CachedCount;
            }
            return count;
        }
    }

    /// <summary>Frame-boundary sweep of every canvas's cache; a context whose cache emptied is forgotten, as is one whose context was disposed.</summary>
    public void ClearUnusedRenderTargetsLastFrame()
    {
        _raster.ClearUnusedRenderTargetsLastFrame();
        List<GRRecordingContext>? emptied = null;
        foreach (KeyValuePair<GRRecordingContext, SkiaContextRenderTargets> entry in _gpu)
        {
            entry.Value.ClearUnusedRenderTargetsLastFrame();
            if (!ReferenceEquals(entry.Value, _current) && (entry.Value.CachedCount == 0 || entry.Value.IsContextUnusable))
            {
                (emptied ??= new List<GRRecordingContext>()).Add(entry.Key);
            }
        }
        if (emptied != null)
        {
            foreach (GRRecordingContext context in emptied)
            {
                Forget(context, _gpu[context]);
            }
        }
    }

    private void Forget(GRRecordingContext context, SkiaContextRenderTargets targets)
    {
        targets.DisposeAll();
        _all.Remove(targets);
        _gpu.Remove(context);
    }

    /// <summary>
    /// Disposes every surface and drops every context reference. Call on shutdown, ideally with the GL context current;
    /// surfaces of a context that is already disposed are leaked on purpose (see <see cref="SkiaContextRenderTargets"/>),
    /// and nothing here throws.
    /// </summary>
    public void DisposeAll()
    {
        for (int i = 0; i < _all.Count; i++)
        {
            SkiaContextRenderTargets targets = _all[i];
            targets.DisposeAll();
        }
        _gpu.Clear();
        _all.Clear();
        _all.Add(_raster);
        _current = _raster;
    }

}

/// <summary>The render targets and manual-mode cached images created from one context (null: CPU raster).</summary>
internal sealed class SkiaContextRenderTargets : RenderTargetServiceBase<SkiaRenderTarget>
{
    public GRRecordingContext? Context { get; }

    public SkiaContextRenderTargets(GRRecordingContext? context)
    {
        Context = context;
    }

    /// <summary>The host disposed or abandoned this context (a lost context), so nothing drawn through it is current.</summary>
    public bool IsContextUnusable => Context != null && IsUnusable(Context);

    internal static bool IsUnusable(GRRecordingContext context) => context.Handle == System.IntPtr.Zero || context.IsAbandoned;

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

    // Shutdown must never crash a game. A surface of a disposed context cannot be freed safely (the GL objects are
    // gone), so it is leaked and its finalizer suppressed; an abandoned context frees without touching GL. A host
    // that destroyed its GL context but not the GRContext wrapper cannot be detected: call Uninitialize first.
    protected override void Destroy(SkiaRenderTarget renderTarget)
    {
        try
        {
            if (Context != null && Context.Handle == System.IntPtr.Zero)
            {
                System.GC.SuppressFinalize(renderTarget.Surface);
                return;
            }
            renderTarget.Surface.Dispose();
        }
        catch (System.Exception)
        {
            // A teardown failure leaves a leaked surface at worst; it must not take the game down at exit.
        }
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
