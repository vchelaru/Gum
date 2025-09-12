using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
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
        BitmapFont font = new BitmapFont((Texture2D)null, bmFontData);

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
        Assert.Throws<InvalidOperationException>(() => new BitmapFont((Texture2D)null, bmFontData));
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
        Assert.Throws<InvalidOperationException>(() => new BitmapFont((Texture2D)null, bmFontData));
    }

    [Fact]
    public void MeasureString_ShouldProperlyMeasureWhitespace()
    {
        BitmapFont font = new BitmapFont((Texture2D)null, basicBMFontFileData);

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

    [Fact]
    public void MeasureString_ShouldIgnoreTrailingNewlines()
    {

        BitmapFont font = new BitmapFont((Texture2D)null, basicBMFontFileData);

        var character = font.Characters['\n'];
        character.XAdvance = 10;
        character.XOffsetInPixels = 10;
        character.PixelLeft = 0;
        character.PixelRight = 10;

        var withoutNewline = font.MeasureString("a");
        var withNewline = font.MeasureString("a\n");

        withoutNewline.ShouldBe(withNewline, "Because a trailing newline should not affect the width of a text, regardless of its XAdavance");
    }
}

