using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Raylib_cs;
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
public class SystemManagers : ISystemManagers
{
    int mPrimaryThreadId;

    static bool IsMobile =>
#if NET6_0_OR_GREATER
    System.OperatingSystem.IsAndroid() ||
        System.OperatingSystem.IsIOS();
#elif ANDROID || IOS
    true;
#else
    false;
#endif

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
        Renderer = new Renderer();
    }

    public void Initialize()
    {

        Renderer.Camera.CameraCenterOnScreen = CameraCenterOnScreen.TopLeft;

        bool fullInstantiation = true;

        if(fullInstantiation)
        {
            LoaderManager.Self.ContentLoader = new ContentLoader();

            GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
            GraphicalUiElement.AddRenderableToManagers = CustomSetPropertyOnRenderable.AddRenderableToManagers;

            // raylib seems to use a resources folder, but I don't think we should make any
            // assumptions
            //ToolsUtilities.FileManager.RelativeDirectory = "Content/";

            ElementSaveExtensions.CustomCreateGraphicalComponentFunc = RenderableCreator.HandleCreateGraphicalComponent;

            StandardElementsManager.Self.Initialize();

            RegisterComponentRuntimeInstantiations();
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

        var resourceName = $"{AssemblyPrefix}.{embeddedTexture2dName}";
        // raylib textures aren't disposable...
        //Content.LoaderManager.Self.AddDisposable($"EmbeddedResource.{resourceName}", texture);

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

        //ElementSaveExtensions.RegisterGueInstantiation(
        //    "Polygon",
        //    () => new PolygonRuntime(systemManagers: this));

        //ElementSaveExtensions.RegisterGueInstantiation(
        //    "Rectangle",
        //    () => new RectangleRuntime(systemManagers: this));

        ElementSaveExtensions.RegisterGueInstantiation(
            "Sprite",
            () => new SpriteRuntime());

        ElementSaveExtensions.RegisterGueInstantiation(
            "Text",
            () => new TextRuntime(systemManagers: this));
    }
}
