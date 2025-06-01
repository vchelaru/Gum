using Gum.Wireframe;
using MonoGameGum.Forms;
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
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: Xunit.TestFramework("MonoGameGum.Tests.TestAssemblyInitialize", "MonoGameGum.Tests")]

namespace MonoGameGum.Tests;
public sealed class TestAssemblyInitialize : XunitTestFramework
{
    public TestAssemblyInitialize(IMessageSink messageSink) : base(messageSink)
    {
        SystemManagers.Default = new();
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        FormsUtilities.InitializeDefaults();
        CreateStubbedFonts();

    }

    private static void CreateStubbedFonts()
    {
        var fontPattern =
$"info face=\"Arial\" size=-18 bold=0 italic=0 charset=\"\" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0\n" +
$"common lineHeight=21 base=17 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4\r\n";

        var bitmapFont = new BitmapFont(fontTextureGraphic: null, fontPattern: fontPattern);


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
    }
}
