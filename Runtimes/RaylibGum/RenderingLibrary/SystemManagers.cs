using Gum.GueDeriving;
using Gum.Localization;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RaylibGum.Renderables;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using static Raylib_cs.Raylib;
using Gum.Renderables;
using RenderingLibrary.Content;


namespace RenderingLibrary;
public partial class SystemManagers : ISystemManagers
{
    int mPrimaryThreadId;
#if !RAYLIB
    // Mirrors RenderingLibrary/SystemManagers.cs's _lastActivityTime, which gates the
    // Renderer.NotifyHostFrameAdvanced() call in Activity below. Dead in this build; kept for
    // cross-file diff parity.
    private double _lastActivityTime = double.NaN;
#endif

    static bool IsMobile =>
    System.OperatingSystem.IsAndroid() ||
        System.OperatingSystem.IsIOS();

    public static SystemManagers Default
    {
        get;
        set;
    }

    /// <summary>
    /// The Renderer used by this SystemManagers. This is created automatically when
    /// calling Initialize, and this should only be set in unit tests.
    /// </summary>
    public Renderer Renderer
    {
        get;
        set;
    }

    IRenderer ISystemManagers.Renderer => Renderer;

#if !RAYLIB
    public SpriteManager SpriteManager
    {
        get;
        private set;
    }

    public ShapeManager ShapeManager
    {
        get;
        private set;
    }

    public TextManager TextManager
    {
        get;
        private set;
    }
#endif

    public string Name
    {
        get;
        set;
    }

    public bool IsCurrentThreadPrimary
    {
        get
        {
            int threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            return threadId == mPrimaryThreadId;
        }
    }

    /// <summary>
    /// The font scale value. This can be used to scale all fonts globally, 
    /// generally in response to a font scaling value like the Android font scale setting.
    /// </summary>
    public static float GlobalFontScale { get; set; } = 1.0f;

    public bool EnableTouchEvents { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public static Dictionary<string, byte[]> StreamByteDictionary { get; private set; } = new Dictionary<string, byte[]>();

    public static string AssemblyPrefix =>
#if KNI
        "KniGum";
#elif FNA
        "FnaGum";
#elif RAYLIB
        "RaylibGum.Content";
#else
        "MonoGameGum.Content";
#endif


    public SystemManagers()
    {
        // Unlike XNA/KNI/FNA's Initialize() (which recreates Renderer on every call), raylib creates
        // its Renderer once here and Initialize() never touches it - raylib doesn't have MonoGame's
        // graphics-device-loss concept that motivated the XNA behavior. Not unified for now; revisit
        // only if raylib ever needs to re-run Initialize() on an existing instance (#4577).
        Renderer = new Renderer();
    }

    public void Initialize()
    {
        bool fullInstantiation = true;

        // Order below mirrors RenderingLibrary/SystemManagers.cs's fullInstantiation block
        // (MonoGame/KNI/FNA) line-for-line wherever raylib has an equivalent, so the two files
        // stay easy to diff against each other as they converge (#4576: raylib's copy had
        // silently dropped the LocalizationService/ThrowExceptionsForMissingFiles wiring below
        // because nothing kept the two in sync). Embedded font preloading has no raylib equivalent
        // and is intentionally omitted. Renderer.ApplyCameraZoomOnWorldTranslation and
        // Text.RenderBoundaryDefault don't exist on raylib's own Renderer/Text at all, and the
        // Content/-folder default is intentionally XNA/KNI/FNA-only - all three are mirrored below
        // as dead #if !RAYLIB code so the two files stay line-for-line comparable (#4577).
        if(fullInstantiation)
        {
            LoaderManager.Self.ContentLoader = new ContentLoader();

            GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
            GraphicalUiElement.ApplyCachedTextureFromPixelData = PixelDataTextureApplier.ApplyCached;
            GraphicalUiElement.ApplyPooledTextureFromPixelData = PixelDataTextureApplier.ApplyPooled;
            CustomSetPropertyOnRenderable.LocalizationService ??= new LocalizationService();
            // Wire the font loader here (not in a renderable's static ctor) so it is re-established on
            // every Initialize, matching the other delegates and MonoGame's SystemManagers. GumService
            // teardown nulls UpdateFontFromProperties; without re-wiring here, the direct font-property
            // setters silently stop loading fonts after a teardown/reinitialize cycle.
            GraphicalUiElement.UpdateFontFromProperties = CustomSetPropertyOnRenderable.UpdateToFontValues;
            GraphicalUiElement.ThrowExceptionsForMissingFiles = CustomSetPropertyOnRenderable.ThrowExceptionsForMissingFiles;

            GraphicalUiElement.AddRenderableToManagers = CustomSetPropertyOnRenderable.AddRenderableToManagers;
            GraphicalUiElement.RemoveRenderableFromManagers = CustomSetPropertyOnRenderable.RemoveRenderableFromManagers;

#if !RAYLIB
            Renderer.ApplyCameraZoomOnWorldTranslation = true;
#endif

            Renderer.Camera.CameraCenterOnScreen = CameraCenterOnScreen.TopLeft;

            ElementSaveExtensions.CustomCreateGraphicalComponentFunc = RenderableCreator.HandleCreateGraphicalComponent;

            StandardElementsManager.Self.Initialize();

#if !RAYLIB
            Text.RenderBoundaryDefault = false;
#endif

#if !RAYLIB
            ToolsUtilities.FileManager.RelativeDirectory = "Content/";
#endif

            RegisterComponentRuntimeInstantiations();

            GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ThrowException;
        }
    }

    public Texture2D? LoadEmbeddedTexture2d(string embeddedTexture2dName)
    {
        if(IsWindowReady() == false)
        {
            throw new InvalidOperationException("Cannot attempt to load a texture because IsWindowReady() is false - did you remember to call InitWindow first?");
        }
        // tolerate nulls for unit tests:
        //if (Renderer.GraphicsDevice == null) return null;

        var assembly = typeof(SystemManagers).Assembly;
        using var stream = ToolsUtilities.FileManager.GetStreamFromEmbeddedResource(assembly, 
            $"{AssemblyPrefix}.{embeddedTexture2dName}");
        using var memoryStream = new System.IO.MemoryStream();  

        // Read the stream into a byte array
        byte[] fileData;
        stream.Position = 0;
        stream.CopyTo(memoryStream);
        fileData = memoryStream.ToArray();



        //Load the image into the cpu
        //Image image = LoadImage("resources/gum-logo-normal-64.png");

        //Transform it as a texture
        var image =
            LoadImageFromMemory(".png", fileData);
        var texture = 
            LoadTextureFromImage(image);

        //Texture2D texture = Texture2D.FromStream(Renderer.GraphicsDevice, stream);

        // Deliberately uncached: this always uploads a fresh texture. Use
        // GetOrLoadEmbeddedTexture2d below to share one.
        return texture;
    }

    /// <summary>
    /// Returns the embedded texture cached under <paramref name="embeddedTexture2dName"/>, loading
    /// and caching it via <see cref="LoadEmbeddedTexture2d"/> on the first call. Prefer this over
    /// <see cref="LoadEmbeddedTexture2d"/> anywhere the same texture may be requested more than
    /// once, so the callers share one GPU texture instead of each uploading a fresh copy.
    /// </summary>
    public Texture2D GetOrLoadEmbeddedTexture2d(string embeddedTexture2dName)
    {
        var cacheName = $"EmbeddedResource.{AssemblyPrefix}.{embeddedTexture2dName}";

        if (Content.LoaderManager.Self.GetDisposable(cacheName) is ManagedTexture cached)
        {
            return cached.Texture;
        }

        var texture = LoadEmbeddedTexture2d(embeddedTexture2dName)!.Value;

        // raylib's Texture2D is a struct, so it reaches the IDisposable cache through
        // ManagedTexture, the same wrapper ContentLoader uses for file-loaded textures.
        Content.LoaderManager.Self.AddDisposable(cacheName, new ManagedTexture(texture),
            Content.LoaderManager.ExistingContentBehavior.Replace);

        return texture;
    }

    /// <summary>
    /// Performs every-frame activity for all contained systems in the SystemManager.
    /// </summary>
    /// <param name="currentTime">The amount of time that has passed since the game started.</param>
    /// <exception cref="InvalidOperationException">Exception thrown if the SystemManagers hasn't yet been initialized.</exception>
    public void Activity(double currentTime)
    {
#if !RAYLIB
        // XNALIKE-only, and intentionally never ported to raylib (#4598). NotifyHostFrameAdvanced()
        // resets the once-per-host-frame latches on the XNA Renderer (render-target sweep, layer
        // pre-render, referenced-RT collection, perf-stat reset). Those latches exist only because
        // GumBatch lets a host run several Begin/Draw/End cycles per frame, so the work behind them
        // has to be skipped after the first cycle. raylib has no GumBatch: Renderer.Draw runs once
        // per host frame and does that same work unconditionally at the top of it, so there is
        // nothing to latch and no member to call here.
        if (currentTime != _lastActivityTime)
        {
            _lastActivityTime = currentTime;
            Renderer.NotifyHostFrameAdvanced();
        }
#endif

#if !RAYLIB
#if FULL_DIAGNOSTICS
        if (SpriteManager == null)
        {
            throw new InvalidOperationException("The SpriteManager is null - did you remember to initialize the SystemManagers?");
        }
#endif

        SpriteManager.Activity(currentTime);
#endif
    }

    public void InvalidateSurface()
    {

    }

    internal void Draw()
    {
        Renderer.Draw(this);
    }


    private void RegisterComponentRuntimeInstantiations()
    {
        ElementSaveExtensions.RegisterGueInstantiation(
            "ColoredRectangle",
            () => new ColoredRectangleRuntime());

        ElementSaveExtensions.RegisterGueInstantiation(
            "Container",
            () => new ContainerRuntime());

        ElementSaveExtensions.RegisterGueInstantiation(
            "NineSlice",
            () => new NineSliceRuntime());

        ElementSaveExtensions.RegisterGueInstantiation(
            "Polygon",
            () => new PolygonRuntime(systemManagers: this));

        ElementSaveExtensions.RegisterGueInstantiation(
            "Rectangle",
            () => new RectangleRuntime(systemManagers: this));

        ElementSaveExtensions.RegisterGueInstantiation(
            "Sprite",
            () => new SpriteRuntime());

        ElementSaveExtensions.RegisterGueInstantiation(
            "Text",
            () => new TextRuntime(systemManagers: this));
    }
}
