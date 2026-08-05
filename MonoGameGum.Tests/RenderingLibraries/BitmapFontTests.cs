using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;
public class BitmapFontTests
{
    const string basicBMFontFileData =
@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight=21 base=17 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""Font18Arial_0.png""
chars count=5
char id=32   x=206   y=102   width=3     height=1     xoffset=-1    yoffset=20    xadvance=5     page=0  chnl=15
char id=33   x=247   y=74    width=4     height=13    xoffset=1     yoffset=4     xadvance=6     page=0  chnl=15
char id=34   x=113   y=103   width=6     height=5     xoffset=0     yoffset=4     xadvance=6     page=0  chnl=15
char id=35   x=200   y=48    width=11    height=13    xoffset=-1    yoffset=4     xadvance=10    page=0  chnl=15
char id=36   x=165   y=18    width=10    height=16    xoffset=0     yoffset=3     xadvance=10    page=0  chnl=15
char id=37   x=161   y=0     width=22    height=20    xoffset=1     yoffset=6     xadvance=24    page=0  chnl=15
";

    const string basicBMFontXMLData = @"<?xml version=""1.0""?>
<font>
  <info face=""Arial"" size=""-18"" bold=""0"" italic=""0"" charset="""" unicode=""1"" stretchH=""100"" smooth=""1"" aa=""1"" padding=""0,0,0,0"" spacing=""1,1"" outline=""0""/>
  <common lineHeight=""21"" base=""17"" scaleW=""256"" scaleH=""256"" pages=""1"" packed=""0"" alphaChnl=""0"" redChnl=""4"" greenChnl=""4"" blueChnl=""4""/>
  <pages>
    <page id=""0"" file=""Font18Arial_0.png""/>
  </pages>
  <chars count=""5"">
    <char id=""32"" x=""206"" y=""102"" width=""3"" height=""1"" xoffset=""-1"" yoffset=""20"" xadvance=""5"" page=""0"" chnl=""15""/>
    <char id=""33"" x=""247"" y=""74"" width=""4"" height=""13"" xoffset=""1"" yoffset=""4"" xadvance=""6"" page=""0"" chnl=""15""/>
    <char id=""34"" x=""113"" y=""103"" width=""6"" height=""5"" xoffset=""0"" yoffset=""4"" xadvance=""6"" page=""0"" chnl=""15""/>
    <char id=""35"" x=""200"" y=""48"" width=""11"" height=""13"" xoffset=""-1"" yoffset=""4"" xadvance=""10"" page=""0"" chnl=""15""/>
    <char id=""36"" x=""165"" y=""18"" width=""10"" height=""16"" xoffset=""0"" yoffset=""3"" xadvance=""10"" page=""0"" chnl=""15""/>
    <char id=""37"" x=""161"" y=""0"" width=""22"" height=""20"" xoffset=""1"" yoffset=""6"" xadvance=""24"" page=""0"" chnl=""15""/>
  </chars>
</font>";

    [Theory]
    [InlineData(basicBMFontFileData)]
    [InlineData(basicBMFontXMLData)]
    public void Constructor_ShouldParseFile(string bmFontData)
    {
        BitmapFont font = new BitmapFont((Texture2D)null!, bmFontData);

        // We have to explicitly set the font pattern because the
        // font doesn't know its own texture size:

        font.SetFontPattern(256, 256);

        font.Characters.Length.ShouldBe(38, 
            "because BitmapFonts always contain characters up to the last index, which in this case is 36. We start counting at 0, so that's 38");

        font.Characters[32].TULeft.ShouldBe(206f / 256f);
        font.Characters[32].TVTop.ShouldBe(102f / 256f);
        font.Characters[32].TURight.ShouldBe(209f / 256f);
        font.Characters[32].TVBottom.ShouldBe(103f / 256f);
        font.Characters[32].XAdvance.ShouldBe(5);
        font.Characters[32].XOffsetInPixels.ShouldBe(-1);
        font.Characters[32].PageNumber.ShouldBe(0);
    }

    [Theory]
    [InlineData("Fake invalid data")]
    [InlineData("BMF binary not yet supported")]
    public void Constructor_ShouldErrorWhenInvalidFileFormat(string bmFontData)
    {
        Assert.Throws<InvalidOperationException>(() => new BitmapFont((Texture2D)null!, bmFontData));
    }

    const string bmfontTextDataMissingCommon =
@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
page id=0 file=""Font18Arial_0.png""
chars count=5
char id=32   x=206   y=102   width=3     height=1     xoffset=-1    yoffset=20    xadvance=5     page=0  chnl=15
char id=33   x=247   y=74    width=4     height=13    xoffset=1     yoffset=4     xadvance=6     page=0  chnl=15
char id=34   x=113   y=103   width=6     height=5     xoffset=0     yoffset=4     xadvance=6     page=0  chnl=15
char id=35   x=200   y=48    width=11    height=13    xoffset=-1    yoffset=4     xadvance=10    page=0  chnl=15
char id=36   x=165   y=18    width=10    height=16    xoffset=0     yoffset=3     xadvance=10    page=0  chnl=15
char id=37   x=161   y=0     width=22    height=20    xoffset=1     yoffset=6     xadvance=24    page=0  chnl=15
";

    const string bmfontXMLDataMissingInfo = @"<?xml version=""1.0""?>
<font>
  <common lineHeight=""21"" base=""17"" scaleW=""256"" scaleH=""256"" pages=""1"" packed=""0"" alphaChnl=""0"" redChnl=""4"" greenChnl=""4"" blueChnl=""4""/>
  <pages>
    <page id=""0"" file=""Font18Arial_0.png""/>
  </pages>
  <chars count=""5"">
    <char id=""32"" x=""206"" y=""102"" width=""3"" height=""1"" xoffset=""-1"" yoffset=""20"" xadvance=""5"" page=""0"" chnl=""15""/>
    <char id=""33"" x=""247"" y=""74"" width=""4"" height=""13"" xoffset=""1"" yoffset=""4"" xadvance=""6"" page=""0"" chnl=""15""/>
    <char id=""34"" x=""113"" y=""103"" width=""6"" height=""5"" xoffset=""0"" yoffset=""4"" xadvance=""6"" page=""0"" chnl=""15""/>
    <char id=""35"" x=""200"" y=""48"" width=""11"" height=""13"" xoffset=""-1"" yoffset=""4"" xadvance=""10"" page=""0"" chnl=""15""/>
    <char id=""36"" x=""165"" y=""18"" width=""10"" height=""16"" xoffset=""0"" yoffset=""3"" xadvance=""10"" page=""0"" chnl=""15""/>
    <char id=""37"" x=""161"" y=""0"" width=""22"" height=""20"" xoffset=""1"" yoffset=""6"" xadvance=""24"" page=""0"" chnl=""15""/>
  </chars>
</font>";

    const string bmfontXMLDataMissingCommon = @"<?xml version=""1.0""?>
<font>
  <info face=""Arial"" size=""-18"" bold=""0"" italic=""0"" charset="""" unicode=""1"" stretchH=""100"" smooth=""1"" aa=""1"" padding=""0,0,0,0"" spacing=""1,1"" outline=""0""/>
  <pages>
    <page id=""0"" file=""Font18Arial_0.png""/>
  </pages>
  <chars count=""5"">
    <char id=""32"" x=""206"" y=""102"" width=""3"" height=""1"" xoffset=""-1"" yoffset=""20"" xadvance=""5"" page=""0"" chnl=""15""/>
    <char id=""33"" x=""247"" y=""74"" width=""4"" height=""13"" xoffset=""1"" yoffset=""4"" xadvance=""6"" page=""0"" chnl=""15""/>
    <char id=""34"" x=""113"" y=""103"" width=""6"" height=""5"" xoffset=""0"" yoffset=""4"" xadvance=""6"" page=""0"" chnl=""15""/>
    <char id=""35"" x=""200"" y=""48"" width=""11"" height=""13"" xoffset=""-1"" yoffset=""4"" xadvance=""10"" page=""0"" chnl=""15""/>
    <char id=""36"" x=""165"" y=""18"" width=""10"" height=""16"" xoffset=""0"" yoffset=""3"" xadvance=""10"" page=""0"" chnl=""15""/>
    <char id=""37"" x=""161"" y=""0"" width=""22"" height=""20"" xoffset=""1"" yoffset=""6"" xadvance=""24"" page=""0"" chnl=""15""/>
  </chars>
</font>";

    [Theory]
    [InlineData(bmfontTextDataMissingCommon)]
    [InlineData(bmfontXMLDataMissingInfo)]
    [InlineData(bmfontXMLDataMissingCommon)]
    public void Constructor_ShouldErrorWhenMissingInfoOrCommon(string bmFontData)
    {
        Assert.Throws<InvalidOperationException>(() => new BitmapFont((Texture2D)null!, bmFontData));
    }

    [Theory]
    [InlineData("FontCache/Font24Arial_ds2.fnt", "FontCache/Font24Arial_ds2-shadow.fnt")]
    [InlineData("Font18Arial.FNT", "Font18Arial-shadow.fnt")]
    public void GetShadowSiblingFntPath_AppendsShadowSuffix(string fontFile, string expected)
    {
        BitmapFont.GetShadowSiblingFntPath(fontFile).ShouldBe(expected);
    }

    [Theory]
    [InlineData("Font18Arial_0.png")]
    [InlineData("FontCache/Font24Arial")]
    public void GetShadowSiblingFntPath_ReturnsNull_ForNonFntPath(string fontFile)
    {
        BitmapFont.GetShadowSiblingFntPath(fontFile).ShouldBeNull();
    }

    [Fact]
    public void MeasureString_ShouldProperlyMeasureWhitespace()
    {
        BitmapFont font = new BitmapFont((Texture2D)null!, basicBMFontFileData);

        // We have to explicitly set the font pattern because the
        // font doesn't know its own texture size:

        font.SetFontPattern(256, 256);

        var spaceCharacter = font.Characters[' '];
        spaceCharacter.XAdvance = 10;
        spaceCharacter.PixelRight = 5;
        spaceCharacter.PixelLeft = 0;
        spaceCharacter.XOffsetInPixels = 1;

        font.MeasureString("     ").ShouldBe(40 + 5 + 1);
    }

    const string smallCharSetFontData =
@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight=21 base=17 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""Font18Arial_0.png""
chars count=1
char id=5   x=0   y=0   width=3     height=1     xoffset=-1    yoffset=20    xadvance=5     page=0  chnl=15
";

    [Fact]
    public void GetCharacterInfo_ShouldNotThrow_WhenFontIsSmallerThanSpaceCharacterIndex()
    {
        BitmapFont font = new BitmapFont((Texture2D)null!, smallCharSetFontData);

        font.SetFontPattern(256, 256);

        // The font's character array only extends to index 5 (its last defined char id),
        // short of the space character's index (32). An out-of-range lookup falls back
        // to the space character, which used to re-index the same too-short array and throw.
        Should.NotThrow(() => font.GetCharacterInfo('A'));
    }

    [Fact]
    public void GetCharacterInfo_ShouldFallBackToRealSpaceGlyphMetrics_NotPlaceholderMutatedByTabOrNewline()
    {
        BitmapFont font = new BitmapFont((Texture2D)null!, basicBMFontFileData);

        font.SetFontPattern(256, 256);

        // Codepoint 200 is out of range (the font only defines up to id 37) and falls back to
        // the space character. SetFontPattern initially fills every array slot with a shared
        // placeholder object, then mutates that same object in place for the tab/newline special
        // cases, before finally overwriting index 32 with the font's real space glyph (id 32
        // above). The fallback must reflect that real, final glyph -- not the placeholder, which
        // by then has its texture coordinates zeroed out by the newline special-case.
        BitmapCharacterInfo fallback = font.GetCharacterInfo((char)200);

        fallback.TULeft.ShouldBe(206f / 256f);
    }

    [Fact]
    public void MeasureString_ShouldIgnoreTrailingNewlines()
    {

        BitmapFont font = new BitmapFont((Texture2D)null!, basicBMFontFileData);

        var character = font.Characters['\n'];
        character.XAdvance = 10;
        character.XOffsetInPixels = 10;
        character.PixelLeft = 0;
        character.PixelRight = 10;

        var withoutNewline = font.MeasureString("a");
        var withNewline = font.MeasureString("a\n");

        withoutNewline.ShouldBe(withNewline, "Because a trailing newline should not affect the width of a text, regardless of its XAdavance");
    }

    [Fact]
    public void CreateFromDesignMetrics_ScalesDesignUnitsToPixelsByExactMultiplication()
    {
        Dictionary<int, GlyphDesignMetrics> glyphMetrics = new()
        {
            ['A'] = new GlyphDesignMetrics(AdvanceWidth: 1000, LeftSideBearing: 100),
            ['B'] = new GlyphDesignMetrics(AdvanceWidth: 1200, LeftSideBearing: 50),
            [' '] = new GlyphDesignMetrics(AdvanceWidth: 500, LeftSideBearing: 0),
        };
        FontDesignMetrics designMetrics = new(unitsPerEm: 2000, lineHeight: 2400, glyphMetrics: glyphMetrics);

        BitmapFont font = BitmapFont.CreateFromDesignMetrics(designMetrics, fontSizeInPixels: 20f)!;

        // scale factor = fontSizeInPixels / UnitsPerEm = 20 / 2000 = 0.01
        font.LineHeightInPixels.ShouldBe(24, "because 2400 design units * 0.01 = 24");
        font.GetCharacterInfo('A').XAdvance.ShouldBe(10, "because 1000 design units * 0.01 = 10");
        font.GetCharacterInfo('B').XAdvance.ShouldBe(12, "because 1200 design units * 0.01 = 12");
        font.GetCharacterInfo(' ').XAdvance.ShouldBe(5, "because 500 design units * 0.01 = 5");
    }

    [Fact]
    public void CreateFromDesignMetrics_TwoDifferentFontSizes_ProduceExactlyProportionalMeasurements()
    {
        // The whole point of design-unit metrics (issue #4309): unlike a rasterized BitmapFont
        // (whose hinted, pixel-snapped advances at two different sizes are NOT exact multiples of
        // each other), a design-metrics-built font must scale by an EXACT ratio -- measuring "AB"
        // at size 40 must be precisely 2x measuring it at size 20, every time, with zero jitter.
        Dictionary<int, GlyphDesignMetrics> glyphMetrics = new()
        {
            ['A'] = new GlyphDesignMetrics(AdvanceWidth: 1024, LeftSideBearing: 64),
            ['B'] = new GlyphDesignMetrics(AdvanceWidth: 896, LeftSideBearing: 32),
        };
        FontDesignMetrics designMetrics = new(unitsPerEm: 2048, lineHeight: 2500, glyphMetrics: glyphMetrics);

        BitmapFont fontAt20 = BitmapFont.CreateFromDesignMetrics(designMetrics, fontSizeInPixels: 20f)!;
        BitmapFont fontAt40 = BitmapFont.CreateFromDesignMetrics(designMetrics, fontSizeInPixels: 40f)!;

        float widthAt20 = fontAt20.MeasureString("AB");
        float widthAt40 = fontAt40.MeasureString("AB");

        widthAt40.ShouldBe(widthAt20 * 2f, tolerance: 0.01f);
    }
}

