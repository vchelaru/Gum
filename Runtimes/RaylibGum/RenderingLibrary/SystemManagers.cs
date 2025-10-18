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
    public bool EnableTouchEvents { get; set; }

    public static SystemManagers Default
    {
        get;
        set;
    }

    Renderer _renderer;
    public Renderer Renderer => _renderer;

    IRenderer ISystemManagers.Renderer => Renderer;

    public static string AssemblyPrefix =>
        "RaylibGum.Content";


    public SystemManagers()
    {
        _renderer = new Renderer();
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
