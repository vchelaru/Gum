using System.Collections.Generic;

namespace RenderingLibrary.Graphics;

/// <summary>
/// Per-renderable offscreen-render-target cache shared across backends. Each renderable that
/// needs to render to an offscreen surface (clipping, dropshadow blur, post-effects) gets its
/// own platform-specific target, keyed by the renderable instance itself. The lifecycle hook
/// is <see cref="ClearUnusedRenderTargetsLastFrame"/> — call it once per frame from the
/// backend's renderer, and any renderable that wasn't seen since the last call has its target
/// disposed. This handles removal-from-scene, resize, and visibility-toggle uniformly.
///
/// <para>Backends provide a subclass that supplies the platform's create / destroy / size
/// primitives. The MonoGame side (<c>Renderer.RenderTargetService</c>) wraps <see cref="GetFor"/>
/// in a <c>GetRenderTargetFor(GraphicsDevice, IRenderableIpso, Camera)</c> helper that derives
/// the size from the renderable's bounds; raylib (see <c>RaylibRenderTextureService</c>) passes
/// explicit dimensions because dropshadow blur needs padding around the shape, not just the
/// shape bounds.</para>
///
/// <para>Replacement-on-resize policy: exact-size match. If the cached target's dimensions
/// don't match the requested ones, the cached one is destroyed and a fresh one allocated.
/// Per-renderable caching means each owner has its own target, so this doesn't churn across
/// frames as long as the renderable's size is stable.</para>
/// </summary>
public abstract class RenderTargetServiceBase<TRenderTarget>
{
    private readonly Dictionary<IRenderableIpso, TRenderTarget> _renderTargets =
        new Dictionary<IRenderableIpso, TRenderTarget>();
    private readonly HashSet<IRenderableIpso> _usedThisFrame =
        new HashSet<IRenderableIpso>();
    private readonly List<IRenderableIpso> _keysToRemove =
        new List<IRenderableIpso>();

    /// <summary>Allocate a fresh render target of the given size in the backend.</summary>
    protected abstract TRenderTarget Create(int width, int height);

    /// <summary>Release the backend's render target. Called on resize and on
    /// <see cref="ClearUnusedRenderTargetsLastFrame"/>.</summary>
    protected abstract void Destroy(TRenderTarget renderTarget);

    /// <summary>Width of the existing render target — compared against requested width to
    /// detect resize.</summary>
    protected abstract int GetWidth(TRenderTarget renderTarget);

    /// <summary>Height of the existing render target — compared against requested height to
    /// detect resize.</summary>
    protected abstract int GetHeight(TRenderTarget renderTarget);

    /// <summary>
    /// Returns a render target sized to (width, height) for the given owner, allocating a
    /// fresh one if none exists or resizing if the existing one's dimensions differ. Marks
    /// the owner as "used this frame" so <see cref="ClearUnusedRenderTargetsLastFrame"/>
    /// won't sweep it. Returns <c>default</c> for non-positive sizes.
    /// </summary>
    public TRenderTarget? GetFor(IRenderableIpso owner, int width, int height)
    {
        _usedThisFrame.Add(owner);
        if (width <= 0 || height <= 0)
        {
            return default;
        }

        if (_renderTargets.TryGetValue(owner, out TRenderTarget? existing))
        {
            if (GetWidth(existing) != width || GetHeight(existing) != height)
            {
                Destroy(existing);
                _renderTargets.Remove(owner);
            }
            else
            {
                return existing;
            }
        }

        TRenderTarget fresh = Create(width, height);
        _renderTargets[owner] = fresh;
        return fresh;
    }

    /// <summary>
    /// Whether a render target is currently cached for <paramref name="owner"/>, regardless of
    /// whether it was marked used this frame. Intended for tests and diagnostics.
    /// </summary>
    public bool HasCachedRenderTarget(IRenderableIpso owner)
    {
        return _renderTargets.ContainsKey(owner);
    }

    /// <summary>
    /// Returns the cached render target for this owner without allocating or marking it used.
    /// Returns <c>default</c> if none exists. Useful when a later pass needs to read what an
    /// earlier pass rendered without bumping the lifecycle.
    /// </summary>
    public TRenderTarget? TryGetExisting(IRenderableIpso owner)
    {
        return _renderTargets.TryGetValue(owner, out TRenderTarget? existing)
            ? existing
            : default;
    }

    /// <summary>
    /// Frame-boundary sweep. Any render target whose owner wasn't passed to <see cref="GetFor"/>
    /// since the last call gets destroyed and removed. Call this once per frame from the
    /// backend's renderer, typically at the top of its Draw method.
    /// </summary>
    public void ClearUnusedRenderTargetsLastFrame()
    {
        _keysToRemove.Clear();
        foreach (KeyValuePair<IRenderableIpso, TRenderTarget> entry in _renderTargets)
        {
            if (!_usedThisFrame.Contains(entry.Key))
            {
                _keysToRemove.Add(entry.Key);
            }
        }
        foreach (IRenderableIpso key in _keysToRemove)
        {
            Destroy(_renderTargets[key]);
            _renderTargets.Remove(key);
        }
        _usedThisFrame.Clear();
    }

    /// <summary>Destroys every cached render target. Call on backend shutdown.</summary>
    public void DisposeAll()
    {
        foreach (TRenderTarget renderTarget in _renderTargets.Values)
        {
            Destroy(renderTarget);
        }
        _renderTargets.Clear();
        _usedThisFrame.Clear();
    }
}
