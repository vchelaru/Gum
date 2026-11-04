using Gum.GueDeriving;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace MonoGameGum.Tests.V3.GueDeriving;

/// <summary>
/// #7604: Gum embeds Arial 18 (regular, bold, italic) as Latin-1-only fonts. A project's own generated
/// Arial 18 font in FontCache, which can hold more glyphs (Polish ł, ą, ę), must win over them.
/// </summary>
public class TextRuntimeEmbeddedFontPrecedenceTests : IDisposable
{
    private readonly string _projectDirectory;
    private readonly string _previousRelativeDirectory;

    public TextRuntimeEmbeddedFontPrecedenceTests()
    {
        _previousRelativeDirectory = ToolsUtilities.FileManager.RelativeDirectory;
        _projectDirectory = Path.Combine(Path.GetTempPath(), "GumEmbeddedFont_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_projectDirectory, "FontCache"));
        ToolsUtilities.FileManager.RelativeDirectory = _projectDirectory + Path.DirectorySeparatorChar;
    }

    public void Dispose()
    {
        ToolsUtilities.FileManager.RelativeDirectory = _previousRelativeDirectory;
        try { Directory.Delete(_projectDirectory, recursive: true); } catch { }
    }

    [Fact]
    public void ArialBold18_WithAFontCacheFile_UsesTheProjectsFont()
    {
        string fileName = WriteProjectFont("Font18Arial_Bold");

        TextRuntime text = new TextRuntime();
        text.Font = "Arial";
        text.FontSize = 18;
        text.IsBold = true;

        // The test assembly registers one stub font as both the embedded Arial 18 fonts and the default.
        text.BitmapFont.ShouldNotBeSameAs(RenderingLibrary.Graphics.Text.DefaultBitmapFont);
        text.BitmapFont!.FontFile.ShouldContain(fileName);
    }

    [Fact]
    public void ArialBold18_WithNoFontCacheFile_FallsBackToTheEmbeddedFont()
    {
        TextRuntime text = new TextRuntime();
        text.Font = "Arial";
        text.FontSize = 18;
        text.IsBold = true;

        // The test assembly registers one stub font as both the embedded Arial 18 fonts and the default.
        text.BitmapFont.ShouldBeSameAs(RenderingLibrary.Graphics.Text.DefaultBitmapFont);
    }

    private string WriteProjectFont(string baseName)
    {
        string fnt =
            "info face=\"Arial\" size=18 bold=1 italic=0 charset=\"\" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0\n" +
            "common lineHeight=21 base=17 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4\n" +
            $"page id=0 file=\"{baseName}_0.png\"\n" +
            "chars count=1\n" +
            "char id=322 x=225 y=14 width=5 height=13 xoffset=0 yoffset=4 xadvance=5 page=0 chnl=15\n";

        File.WriteAllText(Path.Combine(_projectDirectory, "FontCache", baseName + ".fnt"), fnt);
        return baseName + ".fnt";
    }
}
