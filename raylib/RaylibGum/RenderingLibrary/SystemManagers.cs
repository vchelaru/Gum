using Raylib_cs;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Raylib_cs.Raylib;


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
}
