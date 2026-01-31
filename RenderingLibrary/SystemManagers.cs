using System;
using System.Collections.Generic;
using RenderingLibrary.Graphics;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Math.Geometry;
using Microsoft.Xna.Framework;
using RenderingLibrary.Content;
using ToolsUtilities;
using System.Text;
using System.Net;
using System.Xml.Linq;

#if WEB
using nkast.Wasm.XHR;
using static System.Runtime.InteropServices.JavaScript.JSType;
#endif



#if USE_GUMCOMMON
using MonoGameGum.GueDeriving;
using Gum.Managers;
using GumRuntime;

using MonoGameGum.Renderables;
using Gum.Wireframe;
#endif

namespace RenderingLibrary;

public partial class SystemManagers : ISystemManagers
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

    
    public void Initialize(GraphicsDevice graphicsDevice, bool fullInstantiation = false, ContentLoader? contentLoader = null)
    {
#if NET6_0_OR_GREATER
        var usesTitleContainer = System.OperatingSystem.IsAndroid() || 
            System.OperatingSystem.IsIOS() ||
            System.OperatingSystem.IsBrowser() ||
            FileManager.CustomGetStreamFromFile != null;

        if(usesTitleContainer)
        {
            FileManager.CustomGetStreamFromFile = (fileName) =>
            {
                if(FileManager.IsRelative(fileName) == false)
                {
                    fileName = FileManager.MakeRelative(fileName, FileManager.ExeLocation, preserveCase:true);
                }


#if WEB
                if(fileName.StartsWith("./"))
                {
                   fileName = fileName.Substring(2);
                }
#endif

                if(StreamByteDictionary.ContainsKey(fileName))
                {
                    var bytes = StreamByteDictionary[fileName];
                    return new System.IO.MemoryStream(bytes);
                }

#if WEB



                XMLHttpRequest request = new XMLHttpRequest();

                var suffix = string.Empty;
#if DEBUG
                suffix = "?token=" + DateTime.Now.Ticks;
#endif

                request.Open("GET", fileName + suffix, false);
                request.OverrideMimeType("text/plain; charset=x-user-defined");
                request.Send();

                if (request.Status == 200)
                {
                    string responseText = request.ResponseText;

                    var bytes =
                        System.Text.Encoding.UTF8.GetBytes(responseText);
                    return new MemoryStream(bytes);

                    //byte[] buffer = new byte[responseText.Length];
                    //for (int i = 0; i < responseText.Length; i++)
                    //    buffer[i] = (byte)(responseText[i] & 0xff);

                    //Stream ms = new MemoryStream(buffer);

                    //return ms;
                }
                else
                {
                    throw new IOException("HTTP request failed. Status:" + request.Status);
                }


#else

                if(IsMobile && fileName.StartsWith ("./"))
                {
                    fileName = fileName.Substring(2);
                }

                var stream = TitleContainer.OpenStream(fileName);
                return stream;
#endif
            };
        }

#endif

        mPrimaryThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

        Renderer = new Renderer();

        if(contentLoader != null)
        {
            LoaderManager.Self.ContentLoader = contentLoader;
        }

        if (fullInstantiation)
        {
#if USE_GUMCOMMON
            // Do the content loader here, because otherwise we won't have it ready for the renderer
            LoaderManager.Self.ContentLoader = LoaderManager.Self.ContentLoader ?? new ContentLoader();
#endif
        }

        Renderer.Initialize(graphicsDevice, this, (LoaderManager.Self.ContentLoader as ContentLoader)?.XnaContentManager);

        SpriteManager = new SpriteManager();

        ShapeManager = new ShapeManager();

        TextManager = new TextManager();


        SpriteManager.Managers = this;
        ShapeManager.Managers = this;
        TextManager.Managers = this;

#if USE_GUMCOMMON
        //Initialized GraphicalUiElement.CanvasWidth and CanvasHeight
        //from the current GraphicsDevice viewport even when
        //SystemManagers is not fully instantiated,
        //ensuring component previews receive valid dimensions for render targets
        if(graphicsDevice != null)
        {
            GraphicalUiElement.CanvasWidth = graphicsDevice.Viewport.Width;
            GraphicalUiElement.CanvasHeight = graphicsDevice.Viewport.Height;
        }
#endif
        if(fullInstantiation)
        {
#if USE_GUMCOMMON

            // Load the default font, and then the bold, italic, and italic_bold options for bbcode
            var loadedFont = LoadEmbeddedFont("Font18Arial");
            Text.DefaultBitmapFont = loadedFont;
            Renderer.InternalShapesTexture = loadedFont.Texture;

            LoadEmbeddedFont("Font18Arial_Bold");
            LoadEmbeddedFont("Font18Arial_Italic");
            LoadEmbeddedFont("Font18Arial_Italic_Bold");

            //GraphicalUiElement.CanvasWidth = graphicsDevice.Viewport.Width;
            //GraphicalUiElement.CanvasHeight = graphicsDevice.Viewport.Height;
            GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
            GraphicalUiElement.UpdateFontFromProperties = CustomSetPropertyOnRenderable.UpdateToFontValues;
            GraphicalUiElement.ThrowExceptionsForMissingFiles = CustomSetPropertyOnRenderable.ThrowExceptionsForMissingFiles;

            GraphicalUiElement.AddRenderableToManagers = CustomSetPropertyOnRenderable.AddRenderableToManagers;
            GraphicalUiElement.RemoveRenderableFromManagers = CustomSetPropertyOnRenderable.RemoveRenderableFromManagers;
            Renderer.ApplyCameraZoomOnWorldTranslation = true;

            Renderer.Camera.CameraCenterOnScreen = CameraCenterOnScreen.TopLeft;

            ElementSaveExtensions.CustomCreateGraphicalComponentFunc = RenderableCreator.HandleCreateGraphicalComponent;

            StandardElementsManager.Self.Initialize();

            Text.RenderBoundaryDefault = false;

            ToolsUtilities.FileManager.RelativeDirectory = "Content/";

            RegisterComponentRuntimeInstantiations();

            GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ThrowException;

#endif
        }
    }

    /// <summary>
    /// Loads a font and associated PNG file.  Files are embedded and stored with names like:
    ///     {AssemblyPrefix}.{filename}.{extension}
    ///     KniGum.Font18Arial_0.png
    ///     FnaGum.Font18Arial_0.png
    ///     MonoGameGum.Content.Font18Arial_0.png
    /// </summary>
    /// <param name="fontName">The filename without the extension</param>
    /// <returns></returns>
    private BitmapFont LoadEmbeddedFont(string fontName)
    {
        var assembly = typeof(SystemManagers).Assembly;

        var resourceName = $"{AssemblyPrefix}.{fontName}.fnt";

        var bitmapPattern = ToolsUtilities.FileManager.GetStringFromEmbeddedResource(assembly, resourceName);
        var defaultFontTexture = LoadEmbeddedTexture2d($"{fontName}_0.png");
        var bitmapFont = new BitmapFont(defaultFontTexture, bitmapPattern);

        // qualify for Android:
        Content.LoaderManager.Self.AddDisposable($"EmbeddedResource.{resourceName}", bitmapFont, 
            // This allows unit tests, and can avoid confusiong errors
            Content.LoaderManager.ExistingContentBehavior.Replace);

        return bitmapFont;
    }

    /// <summary>
    /// Loads a texture into the Disposable cache from the Embedded Resource within the application.
    /// </summary>
    /// <param name="embeddedTexture2dName"></param>
    /// <returns></returns>
    public Texture2D? LoadEmbeddedTexture2d(string embeddedTexture2dName)
    {
        // tolerate nulls for unit tests:
        if (Renderer.GraphicsDevice == null) return null;

        var assembly = typeof(SystemManagers).Assembly;
        using var stream = ToolsUtilities.FileManager.GetStreamFromEmbeddedResource(assembly, $"{AssemblyPrefix}.{embeddedTexture2dName}");

        Texture2D texture = Texture2D.FromStream(Renderer.GraphicsDevice, stream);

        var resourceName = $"{AssemblyPrefix}.{embeddedTexture2dName}";
        Content.LoaderManager.Self.AddDisposable($"EmbeddedResource.{resourceName}", texture);

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

    public void Draw()
    {
        Renderer.Draw(this);
    }

    public void Draw(Layer layer)
    {
        Renderer.Draw(this, layer);
    }

    public void Draw(List<Layer> layers)
    {
        Renderer.Draw(this, layers);
    }



#if USE_GUMCOMMON

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
            () => new RectangleRuntime(systemManagers:this));

        ElementSaveExtensions.RegisterGueInstantiation(
            "Sprite",
            () => new SpriteRuntime());

        ElementSaveExtensions.RegisterGueInstantiation(
            "Text",
            () => new TextRuntime(systemManagers: this));
    }
#endif

    public override string ToString()
    {
        return Name;
    }

    public void InvalidateSurface()
    {
        throw new NotImplementedException();
    }
}
