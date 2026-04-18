using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Sokol;
using Gum.GueDeriving;
using static Sokol.SApp;
using static Sokol.SG;

namespace SokolGum;

/// <summary>
/// Minimal <see cref="ISystemManagers"/> implementation. Owns:
/// - Shared samplers (one per filter mode, reused across textures).
/// - A <see cref="FontAtlas"/> — fontstash context whose render callbacks
///   emit into sokol_gp, so text shares the same batched pipeline as
///   sprites/rectangles/nine-slices (aligned with how MonoGameGum /
///   FnaGum / RaylibGum all use a single unified 2D renderer).
/// </summary>
public sealed class SystemManagers : ISystemManagers, IDisposable
{
    public static SystemManagers? Default { get; set; }

    public Renderer Renderer { get; }

    IRenderer ISystemManagers.Renderer => Renderer;

    public bool EnableTouchEvents { get; set; }

    /// <summary>Linear-filter sampler — bilinear interpolation for smooth textures.</summary>
    public sg_sampler LinearSampler { get; private set; }

    /// <summary>Nearest-filter sampler — pixel-perfect for art and small UI.</summary>
    public sg_sampler NearestSampler { get; private set; }

    /// <summary>
    /// Which sampler <see cref="Renderables.Sprite"/> and
    /// <see cref="Renderables.NineSlice"/> use by default. Defaults to
    /// <see cref="NearestSampler"/> matching Gum core's
    /// <c>TextureFilter.Point</c>. Set to <see cref="LinearSampler"/> for
    /// smoothly scaled photographic textures.
    /// </summary>
    public sg_sampler SpriteSampler { get; set; }

    /// <summary>Font atlas + fontstash context. IntPtr.Zero until <see cref="Initialize"/>.</summary>
    public FontAtlas? Fonts { get; private set; }

    /// <summary>Convenience accessor for the raw fontstash context handle.</summary>
    public IntPtr FontStash => Fonts?.Stash ?? IntPtr.Zero;

    private bool _disposed;

    public SystemManagers()
    {
        Renderer = new Renderer();
    }

    /// <summary>
    /// Must be called after <c>sg_setup</c> and <c>sgp_setup</c>.
    /// Creates samplers and the font atlas. Paired with <see cref="Dispose"/>.
    /// </summary>
    public void Initialize(int fontAtlasSize = 1024)
    {
        Renderer.Camera.CameraCenterOnScreen = CameraCenterOnScreen.TopLeft;

        // InteractiveGue.CheckHasCursorOverOnThis walks up to ISystemManagers.Default
        // to get a camera for screen→world projection when an element has no
        // EffectiveManagers chain. Register ourselves so hit-testing works out
        // of the box, not just for elements explicitly attached to managers.
        ISystemManagers.Default = this;

        LinearSampler = sg_make_sampler(new sg_sampler_desc
        {
            min_filter = sg_filter.SG_FILTER_LINEAR,
            mag_filter = sg_filter.SG_FILTER_LINEAR,
            wrap_u = sg_wrap.SG_WRAP_CLAMP_TO_EDGE,
            wrap_v = sg_wrap.SG_WRAP_CLAMP_TO_EDGE,
            label = "SokolGum.LinearSampler",
        });

        NearestSampler = sg_make_sampler(new sg_sampler_desc
        {
            min_filter = sg_filter.SG_FILTER_NEAREST,
            mag_filter = sg_filter.SG_FILTER_NEAREST,
            wrap_u = sg_wrap.SG_WRAP_CLAMP_TO_EDGE,
            wrap_v = sg_wrap.SG_WRAP_CLAMP_TO_EDGE,
            label = "SokolGum.NearestSampler",
        });

        // Default to point filtering to match Gum core's TextureFilter.Point
        // default. Users with photographic textures that need smooth scaling
        // can reassign this before drawing.
        SpriteSampler = NearestSampler;

        // Font atlas with 2× oversampling: fontstash rasterizes glyphs at
        // FontSize × OversampleFactor into a denser atlas, and FontAtlas's
        // RenderDraw divides emitted vertex positions back down so on-screen
        // size stays at FontSize. With LinearSampler each physical pixel
        // samples a 2×2 region of the oversampled atlas — proper bilinear
        // downsampling, which is both sharp AND smooth on curves. The atlas
        // is bumped to 2× the requested size so the extra texels fit without
        // forcing a mid-frame grow.
        int atlasPixelSize = fontAtlasSize * (int)FontAtlas.Oversample;
        Fonts = new FontAtlas(atlasPixelSize, atlasPixelSize, LinearSampler);

        LoaderManager.Self.ContentLoader = new ContentLoader();

        // Lets Gum load .gumx files and instantiate our runtime types by name.
        //
        // These assignments mutate process-wide statics on
        // ElementSaveExtensions / GraphicalUiElement / LoaderManager that
        // every other Gum backend (MonoGame, Raylib, Skia) also writes to.
        // Running two SystemManagers instances concurrently — or mixing
        // two backends in a single process — will have the last
        // Initialize() call win. This is intentional for the typical
        // single-backend app and matches how MonoGameGum / RaylibGum
        // wire themselves up in their respective SystemManagers.
        StandardElementsManager.Self.Initialize();
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = RenderableCreator.HandleCreateGraphicalComponent;
        RegisterComponentRuntimeInstantiations();

        // Route .gumx property assignments through our helper so "SourceFile"
        // loads textures, "Text" translates text names to RawText, "Font"
        // accepts both string paths and pre-loaded Font instances, etc.
        // Unhandled property names fall through to reflection.
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        GraphicalUiElement.AddRenderableToManagers = CustomSetPropertyOnRenderable.AddRenderableToManagers;
    }

    private static void RegisterComponentRuntimeInstantiations()
    {
        ElementSaveExtensions.RegisterGueInstantiation("ColoredRectangle", () => new ColoredRectangleRuntime());
        ElementSaveExtensions.RegisterGueInstantiation("Container",        () => new ContainerRuntime());
        ElementSaveExtensions.RegisterGueInstantiation("NineSlice",        () => new NineSliceRuntime());
        ElementSaveExtensions.RegisterGueInstantiation("Sprite",           () => new SpriteRuntime());
        ElementSaveExtensions.RegisterGueInstantiation("Text",             () => new TextRuntime());
    }

    public void InvalidateSurface() { }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Fonts?.Dispose();
        Fonts = null;
    }
}
