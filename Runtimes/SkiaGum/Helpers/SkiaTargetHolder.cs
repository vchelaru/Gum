using SkiaSharp;
using System;

namespace SkiaGum.Helpers;

/// <summary>
/// A host's GPU render target (for example a SkiaGameRendering <c>SkiaRenderTarget2D</c>) as
/// <see cref="SkiaTargetHolder{TTarget}"/> sees it. SkiaGameRendering's target types differ per engine
/// and per graphics API and share no interface, and Gum references none of them, so each host adapts
/// its own type to this. There is deliberately no CPU/raster implementation: Skia hosts draw on the GPU.
/// </summary>
internal interface ISkiaHostTarget : IDisposable
{
    /// <summary>The Skia canvas. Only readable between <see cref="Begin"/> and an End call.</summary>
    SKCanvas Canvas { get; }

    /// <summary>Starts a pass and clears the target to transparent.</summary>
    void Begin();

    /// <summary>Ends a pass without presenting anything.</summary>
    void EndWithoutDrawing();
}

/// <summary>
/// Owns a host's render target for Gum: creates it, recreates it when the host size changes, captures
/// its canvas, and tells Gum about both. Each host supplies the create delegate (and the End call
/// that presents the target, which differs per engine, on <see cref="Target"/>).
/// </summary>
/// <typeparam name="TTarget">The host's target type, so the host can reach its own members.</typeparam>
internal sealed class SkiaTargetHolder<TTarget> : IDisposable where TTarget : class, ISkiaHostTarget
{
    private readonly Func<int, int, TTarget> _create;
    private readonly Action<SKCanvas> _applyCanvas;
    private readonly Action<int, int> _handleResize;

    /// <summary>The current target, or null before <see cref="Create"/> succeeds or after <see cref="Dispose"/>.</summary>
    public TTarget? Target { get; private set; }

    /// <summary>
    /// The canvas captured from the current target. Stable until the target is recreated for targets
    /// whose canvas outlives a pass; <see cref="BeginFrame"/> refreshes it for targets whose canvas does not.
    /// </summary>
    public SKCanvas? Canvas { get; private set; }

    /// <summary>The width in pixels of the current target.</summary>
    public int Width { get; private set; }

    /// <summary>The height in pixels of the current target.</summary>
    public int Height { get; private set; }

    /// <param name="create">Creates a target of the given width and height.</param>
    /// <param name="applyCanvas">Points Gum at a new canvas (<c>SystemManagers.Default.Canvas = canvas</c>).</param>
    /// <param name="handleResize">Tells Gum the size changed (<c>GumService.HandleResize</c>).</param>
    public SkiaTargetHolder(Func<int, int, TTarget> create, Action<SKCanvas> applyCanvas, Action<int, int> handleResize)
    {
        _create = create ?? throw new ArgumentNullException(nameof(create));
        _applyCanvas = applyCanvas ?? throw new ArgumentNullException(nameof(applyCanvas));
        _handleResize = handleResize ?? throw new ArgumentNullException(nameof(handleResize));
    }

    /// <summary>
    /// Disposes the current target, creates one of the given size, and captures its canvas through a
    /// throwaway Begin/EndWithoutDrawing so <see cref="Canvas"/> is readable outside a frame.
    /// Does not notify Gum; use it before Gum is initialized. Returns false (and changes nothing) for a non-positive size.
    /// </summary>
    public bool Create(int width, int height)
    {
        if (width <= 0 || height <= 0) return false;

        Target?.Dispose();
        Target = _create(width, height);
        Width = width;
        Height = height;

        Target.Begin();
        Canvas = Target.Canvas;
        Target.EndWithoutDrawing();
        return true;
    }

    /// <summary>
    /// When the size differs from the current target's, recreates the target, points Gum at its canvas
    /// and calls HandleResize once. Returns true when it recreated.
    /// </summary>
    public bool EnsureSize(int width, int height)
    {
        if (width <= 0 || height <= 0) return false;
        if (width == Width && height == Height) return false;

        Create(width, height);
        _applyCanvas(Canvas!);
        _handleResize(width, height);
        return true;
    }

    /// <summary>
    /// Begins a pass on the current target and points Gum at the pass's canvas. The host draws, then
    /// ends the pass with its own End call on <see cref="Target"/>. Returns null before <see cref="Create"/>.
    /// </summary>
    public SKCanvas? BeginFrame()
    {
        if (Target == null) return null;

        Target.Begin();
        Canvas = Target.Canvas;
        _applyCanvas(Canvas);
        return Canvas;
    }

    /// <summary>Disposes the current target.</summary>
    public void Dispose()
    {
        Target?.Dispose();
        Target = null;
        Canvas = null;
    }
}
