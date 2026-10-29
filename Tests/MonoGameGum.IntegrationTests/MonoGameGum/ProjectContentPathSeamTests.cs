using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

// The enclosing MonoGameGum namespace holds an obsolete GumService shim; bind to the real type.
using GumService = global::Gum.GumService;

/// <summary>
/// The files between the tool and the game: a project whose textures and fonts sit in subfolders is
/// loaded the way a game loads it (<c>Initialize(game, "GumProject/GumProject.gumx")</c>), and what a
/// missing or moved file looks like to the person running the game. Real GraphicsDevice.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class ProjectContentPathSeamTests : BaseTestClass
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "GumContentSeam_" + Guid.NewGuid().ToString("N"));

    private readonly string _contentFolder = "GumContentSeam_" + Guid.NewGuid().ToString("N");

    public override void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
        try { Directory.Delete(Path.Combine("Content", _contentFolder), recursive: true); } catch { /* best effort */ }
        base.Dispose();
    }

    private static void Set(StateSave state, string name, string type, object value) =>
        state.Variables.Add(new VariableSave { Name = name, Type = type, Value = value, SetsValue = true });

    // A screen with a sprite and a text, saved the way the tool saves a project.
    private string WriteProject(string folder, string spriteFile, string? customFontFile, Action<StateSave>? editLabel = null)
    {
        StandardElementsManager.Self.Initialize();
        GumProjectSave project = new GumProjectSave();
        foreach (string standard in new[] { "Container", "Sprite", "Text" })
        {
            StandardElementSave element = new StandardElementSave { Name = standard };
            element.States.Add(StandardElementsManager.Self.GetDefaultStateFor(standard)!.Clone());
            element.DefaultState!.ParentContainer = element;
            project.StandardElements.Add(element);
            project.StandardElementReferences.Add(new ElementReference { Name = standard, ElementType = ElementType.Standard });
        }
        ScreenSave screen = new ScreenSave { Name = "Main" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(state);
        screen.Instances.Add(new InstanceSave { Name = "Pic", BaseType = "Sprite", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Label", BaseType = "Text", ParentContainer = screen });
        Set(state, "Pic.SourceFile", "string", spriteFile);
        if (customFontFile != null)
        {
            Set(state, "Label.UseCustomFont", "bool", true);
            Set(state, "Label.CustomFontFile", "string", customFontFile);
        }
        editLabel?.Invoke(state);
        project.Screens.Add(screen);
        project.ScreenReferences.Add(new ElementReference { Name = "Main", ElementType = ElementType.Screen });
        project.Initialize();
        Directory.CreateDirectory(folder);
        string gumx = Path.Combine(folder, "GumProject.gumx");
        project.Save(gumx, saveElements: true);
        return gumx;
    }

    private static void WriteTextures(string folder, byte[] png)
    {
        Directory.CreateDirectory(Path.Combine(folder, "Textures", "Sub"));
        File.WriteAllBytes(Path.Combine(folder, "Textures", "Sub", "pic.png"), png);
        Directory.CreateDirectory(Path.Combine(folder, "Fonts", "Sub"));
        File.WriteAllBytes(Path.Combine(folder, "Fonts", "Sub", "Page.png"), png);
        File.WriteAllText(Path.Combine(folder, "Fonts", "Sub", "My.fnt"),
            "info face=\"My\" size=-14 bold=0 italic=0 charset=\"\" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0\n" +
            "common lineHeight=16 base=12 scaleW=4 scaleH=2 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4\n" +
            "page id=0 file=\"Page.png\"\n" +
            "chars count=1\n" +
            "char id=65 x=0 y=0 width=2 height=2 xoffset=0 yoffset=0 xadvance=3 page=0 chnl=15\n");
    }

    private static byte[] CreatePng(GraphicsDevice graphicsDevice)
    {
        using Texture2D source = new(graphicsDevice, 4, 2, false, SurfaceFormat.Color);
        XnaColor[] pixels = new XnaColor[8];
        Array.Fill(pixels, XnaColor.White);
        source.SetData(pixels);
        using MemoryStream stream = new();
        source.SaveAsPng(stream, 4, 2);
        return stream.ToArray();
    }

    private static GraphicalUiElement BuildMain()
    {
        ScreenSave screen = ObjectFinder.Self.GumProjectSave!.Screens.Single(item => item.Name == "Main");
        return screen.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
    }

    [Fact]
    public void Initialize_WithTexturesAndFontsInSubfolders_ShowsThemWhenTheProjectIsLoadedByAbsolutePath()
    {
        using ServiceTestGame game = new();
        game.Start();
        string folder = Path.Combine(_root, "GumProject");
        WriteProject(folder, "Textures/Sub/pic.png", "Fonts/Sub/My.fnt");
        WriteTextures(folder, CreatePng(game.GraphicsDevice));

        GumService service = game.CreateService();
        service.Initialize(game, Path.Combine(folder, "GumProject.gumx"));
        GraphicalUiElement main = BuildMain();

        ((SpriteRuntime)main.GetGraphicalUiElementByName("Pic")!).Texture.ShouldNotBeNull().Width.ShouldBe(4);
        ((Text)((TextRuntime)main.GetGraphicalUiElementByName("Label")!).RenderableComponent).BitmapFont.ShouldNotBeNull();
    }

    [Fact]
    public void Initialize_AfterTheProjectFolderWasMoved_StillFindsTexturesAndFonts()
    {
        using ServiceTestGame game = new();
        game.Start();
        string original = Path.Combine(_root, "Original", "GumProject");
        WriteProject(original, "Textures/Sub/pic.png", "Fonts/Sub/My.fnt");
        WriteTextures(original, CreatePng(game.GraphicsDevice));
        string moved = Path.Combine(_root, "Elsewhere With Space", "Renamed Folder");
        Directory.CreateDirectory(Path.GetDirectoryName(moved)!);
        Directory.Move(original, moved);

        GumService service = game.CreateService();
        service.Initialize(game, Path.Combine(moved, "GumProject.gumx"));
        GraphicalUiElement main = BuildMain();

        ((SpriteRuntime)main.GetGraphicalUiElementByName("Pic")!).Texture.ShouldNotBeNull();
        ((Text)((TextRuntime)main.GetGraphicalUiElementByName("Label")!).RenderableComponent).BitmapFont.ShouldNotBeNull();
        File.ReadAllText(Path.Combine(moved, "GumProject.gumx")).ShouldNotContain(_root);
        File.ReadAllText(Path.Combine(moved, "Screens", "Main.gusx")).ShouldNotContain(_root);
    }

    [Fact]
    public void Initialize_WithAMissingTexture_SaysWhichFileIsMissing()
    {
        using ServiceTestGame game = new();
        game.Start();
        string folder = Path.Combine(_root, "GumProject");
        WriteProject(folder, "Textures/Missing.png", null);

        GumService service = game.CreateService();
        service.Initialize(game, Path.Combine(folder, "GumProject.gumx"));
        string message = "";
        try
        {
            BuildMain();
            message = "<built without saying anything>";
        }
        catch (Exception exception)
        {
            message = exception.Message;
        }

        message.ShouldContain("Missing.png", customMessage: "a missing texture should name the file; saw: " + message);
    }

    [Fact]
    public void Initialize_WithAFontThatWasNeverGenerated_SaysWhichFontFileIsMissing()
    {
        using ServiceTestGame game = new();
        game.Start();
        string folder = Path.Combine(_root, "GumProject");
        WriteProject(folder, "Textures/Sub/pic.png", null, state =>
        {
            Set(state, "Label.Font", "string", "Arial");
            Set(state, "Label.FontSize", "int", 77);
        });
        WriteTextures(folder, CreatePng(game.GraphicsDevice));

        GumService service = game.CreateService();
        service.Initialize(game, Path.Combine(folder, "GumProject.gumx"));
        TextWriter previous = Console.Error;
        StringWriter written = new StringWriter();
        string message;
        Console.SetError(written);
        try
        {
            BuildMain();
            message = written.ToString();
        }
        catch (Exception exception)
        {
            message = exception.Message;
        }
        finally
        {
            Console.SetError(previous);
        }

        message.ShouldContain("Font77Arial", customMessage: "a font that was never generated should name the file, by exception or by a logged line; saw: " + message);
    }
    // The call every getting-started page shows: a path relative to the Content folder.
    [Fact]
    public void Initialize_WithAContentRelativePath_LoadsTexturesAndFontsFromSubfolders()
    {
        using ServiceTestGame game = new();
        game.Start();
        string folder = Path.Combine("Content", _contentFolder, "GumProject");
        WriteProject(folder, "Textures/Sub/pic.png", "Fonts/Sub/My.fnt");
        WriteTextures(folder, CreatePng(game.GraphicsDevice));

        GumService service = game.CreateService();
        service.Initialize(game, _contentFolder + "/GumProject/GumProject.gumx");
        GraphicalUiElement main = BuildMain();

        ((SpriteRuntime)main.GetGraphicalUiElementByName("Pic")!).Texture.ShouldNotBeNull().Width.ShouldBe(4);
        ((Text)((TextRuntime)main.GetGraphicalUiElementByName("Label")!).RenderableComponent).BitmapFont.ShouldNotBeNull();
    }
}
