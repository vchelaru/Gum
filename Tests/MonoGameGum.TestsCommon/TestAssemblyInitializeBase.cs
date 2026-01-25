using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum.Renderables;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;


[assembly: Xunit.TestFramework("MonoGameGum.Tests.V2.TestAssemblyInitialize", "MonoGameGum.Tests.V2")]
// Gum uses some statics internally. Although parallel execution is nice,
// it can cause some tests to fail randomly.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace MonoGameGum.TestsCommon;

public class TestAssemblyInitializeBase : XunitTestFramework
{
    public TestAssemblyInitializeBase(IMessageSink messageSink, Gum.Forms.DefaultVisualsVersion visualVersion) : base(messageSink)
    {
        SystemManagers.Default = new();
        SystemManagers.Default.Renderer = new Renderer();
        SystemManagers.Default.Renderer.AddLayer(new Layer());
        ISystemManagers.Default = SystemManagers.Default;

        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        GraphicalUiElement.UpdateFontFromProperties = CustomSetPropertyOnRenderable.UpdateToFontValues;
        
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = RenderableCreator.HandleCreateGraphicalComponent;


        FormsUtilities.InitializeDefaults(defaultVisualsVersion: visualVersion);
        CreateStubbedFonts();

        InitializeGumService();

        FrameworkElement.KeyboardsForUiControl.Clear();

        GraphicalUiElement.CanvasWidth = 800;
        GraphicalUiElement.CanvasHeight = 600;

        Renderer.Self.Camera.ClientWidth = 800;
        Renderer.Self.Camera.ClientHeight = 600;

        GumService.Default.Root.UpdateLayout();

    }


    private void InitializeGumService()
    {

        GumService.Default.Root.Dock(Dock.Fill);
        GumService.Default.Root.Name = "Main Root";
        GumService.Default.Root.HasEvents = false;

        GumService.Default.Root.AddToManagers(SystemManagers.Default);
    }

    private static void CreateStubbedFonts()
    {
        var fontPattern =
$"info face=\"Arial\" size=-18 bold=0 italic=0 charset=\"\" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0\n" +
$"common lineHeight=21 base=17 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4\r\n" +
$"chars count=223\r\n";

        var stringBuilder = new StringBuilder();
        for (int i = 32; i < 255; i++)
        {
            stringBuilder.AppendLine(
                $"char id={i}  x=231   y=89    width=10    height=8     xoffset=0     yoffset=7     xadvance=10    page=0  chnl=15\r\n");
        }
        fontPattern += stringBuilder.ToString();

        var bitmapFont = new BitmapFont(fontTextureGraphic: null!, fontPattern: fontPattern);

        // Since we don't have a Texture2D, the characters are set to null. Need to create them here:
        for (int i = 0; i < bitmapFont.Characters.Length; i++)
        {
            bitmapFont.Characters[i] = new BitmapCharacterInfo
            {
                XAdvance = 8
            };
        }

        // should be "EmbeddedResource.MonoGameGum.Content.Font18Arial.fnt"
        var prefix = "EmbeddedResource.MonoGameGum.Content.";

        var arial18Name = FileManager.RemovePath(
            BmfcSave.GetFontCacheFileNameFor(18, "Arial", 0, true));

        LoaderManager.Self.AddDisposable(prefix + arial18Name, bitmapFont);

        var arial18BoldName = FileManager.RemovePath(
            BmfcSave.GetFontCacheFileNameFor(18, "Arial", 0, true, isBold: true));
        LoaderManager.Self.AddDisposable(prefix + arial18BoldName, bitmapFont);

        var arial18ItalicName = FileManager.RemovePath(
            BmfcSave.GetFontCacheFileNameFor(18, "Arial", 0, true, isItalic: true));
        LoaderManager.Self.AddDisposable(prefix + arial18ItalicName, bitmapFont);

        var arial18BoldItalicName = FileManager.RemovePath(
            BmfcSave.GetFontCacheFileNameFor(18, "Arial", 0, true, isBold: true, isItalic: true));
        LoaderManager.Self.AddDisposable(prefix + arial18BoldItalicName, bitmapFont);

        Text.DefaultBitmapFont = bitmapFont;
    }

}
