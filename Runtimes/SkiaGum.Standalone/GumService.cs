// Gum.GumService is the concrete, instantiable Skia-family service -- the type every SkiaGum-based
// host (WPF, MAUI, bring-your-own-canvas) actually types. All Skia rendering/init/update logic
// lives in the shared GumServiceSkiaBase (issue #4452 phase 1); this class exists to carry the
// public GumService name and a Default singleton scoped to this assembly. Each Skia-family host
// assembly (this project, and previously each of SkiaGum.Wpf/SkiaGum.Maui before they compiled
// this type via ProjectReference instead of file-linking it) gets its own Gum.GumService with its
// own Default -- never two in one app, mirroring how MonoGame/Raylib/SilkNet each keep their own.
// EXPERIMENTAL (#4452 phase 2 spike): moved out of SkiaGum.csproj into its own project so that
// project can be referenced as a pure base (GumServiceSkiaBase + shared render source) by hosts
// that need their own differently-behaved concrete GumService, without a Gum.GumService naming
// collision -- see Runtimes/SilkNetGum/GumService.Silk.cs.
using SkiaSharp;

namespace Gum;

public class GumService : GumServiceSkiaBase
{
    static GumService? _default;

    /// <summary>
    /// The singleton service instance.
    /// </summary>
    public static GumService Default => _default ??= new GumService();

    // GumServiceSkiaBase keeps the canvas-based API protected so an interactive host (Silk, Stride,
    // Brutal, KniSkia, Unity), which owns its render target, does not publish SKCanvas to games (#7788).
    // This service has no window or render target of its own, so the caller supplies the canvas.

    /// <summary>
    /// Initializes Gum for a Skia canvas, optionally loading a Gum project. The canvas
    /// size is read from <see cref="SKCanvas.DeviceClipBounds"/> to size the root container.
    /// If that does not produce the size you expect (for example if the canvas's clip has not
    /// yet been configured), use the overload that takes explicit width and height instead.
    /// </summary>
    /// <param name="canvas">The SkiaSharp canvas Gum should render to.</param>
    /// <param name="gumProjectFile">An optional .gumx project file to load.</param>
    public new void Initialize(SKCanvas canvas, string? gumProjectFile = null) =>
        base.Initialize(canvas, gumProjectFile);

    /// <summary>
    /// Initializes Gum for a Skia canvas with an explicit canvas size, optionally loading
    /// a Gum project.
    /// </summary>
    /// <param name="canvas">The SkiaSharp canvas Gum should render to.</param>
    /// <param name="width">The width to use for the root container and canvas coordinate space.</param>
    /// <param name="height">The height to use for the root container and canvas coordinate space.</param>
    /// <param name="gumProjectFile">An optional .gumx project file to load.</param>
    public new void Initialize(SKCanvas canvas, int width, int height, string? gumProjectFile = null) =>
        base.Initialize(canvas, width, height, gumProjectFile);

    /// <summary>
    /// Updates the canvas coordinate space and re-runs layout on the root container.
    /// Call this from your platform's window-resized callback so Gum-layouted elements
    /// reposition to match the new window size.
    /// </summary>
    /// <param name="width">The new canvas width.</param>
    /// <param name="height">The new canvas height.</param>
    public new void HandleResize(int width, int height) => base.HandleResize(width, height);

    /// <summary>
    /// Draws Gum into <paramref name="canvas"/> and keeps drawing into it on later <see cref="GumServiceSkiaBase.Draw()"/>
    /// calls. For a host whose canvas is only readable inside a render pass: pass the pass's canvas each frame.
    /// </summary>
    /// <param name="canvas">The canvas to draw into this frame.</param>
    public new void Draw(SKCanvas canvas) => base.Draw(canvas);
}
