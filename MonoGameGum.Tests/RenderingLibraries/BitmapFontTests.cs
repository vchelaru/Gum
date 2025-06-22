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
    [Fact]
    public void BitmapFont_ShouldParseTextFile()
    {

        const string fontPattern =
@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight=21 base=17 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""Font18Arial_0.png""
chars count=5
char id=32   x=206   y=102   width=3     height=1     xoffset=-1    yoffset=20    xadvance=5     page=0  chnl=15
char id=33   x=247   y=74    width=4     height=13    xoffset=1     yoffset=4     xadvance=6     page=0  chnl=15
char id=34   x=113   y=103   width=6     height=5     xoffset=0     yoffset=4     xadvance=6     page=0  chnl=15
char id=35   x=200   y=48    width=11    height=13    xoffset=-1    yoffset=4     xadvance=10    page=0  chnl=15
char id=36   x=165   y=18    width=10    height=16    xoffset=0     yoffset=3     xadvance=10    page=0  chnl=15
";      
        BitmapFont font = new BitmapFont((Texture2D)null, fontPattern);

        // We have to explicitly set the font pattern because the
        // font doesn't know its own texture size:

        font.SetFontPattern(256, 256);

        font.Characters.Length.ShouldBe(37, 
            "because BitmapFonts always contain characters up to the last index, which in this case is 36. We start counting at 0, so that's 37");

        font.Characters[32].TULeft.ShouldBe(206f / 256f);
        font.Characters[32].TVTop.ShouldBe(102f / 256f);
        font.Characters[32].TURight.ShouldBe(209f / 256f);
        font.Characters[32].TVBottom.ShouldBe(103f / 256f);
        font.Characters[32].XAdvance.ShouldBe(5);
        font.Characters[32].XOffsetInPixels.ShouldBe(-1);
        font.Characters[32].PageNumber.ShouldBe(0);
    }

    [Fact]
    public void BitmapFont_ShouldParseXmlFile()
    {
        const string fontPattern =
@"<?xml version=""1.0""?>
<font>
  <info face=""Arial"" size=""32"" bold=""0"" italic=""0"" charset="""" unicode=""1"" stretchH=""100"" smooth=""1"" aa=""1"" padding=""0,0,0,0"" spacing=""1,1"" outline=""0""/>
  <common lineHeight=""32"" base=""26"" scaleW=""256"" scaleH=""256"" pages=""1"" packed=""0"" alphaChnl=""1"" redChnl=""0"" greenChnl=""0"" blueChnl=""0""/>
  <pages>
    <page id=""0"" file=""XmlExample_0.png"" />
  </pages>
  <chars count=""95"">
    <char id=""32"" x=""94"" y=""24"" width=""3"" height=""1"" xoffset=""-1"" yoffset=""31"" xadvance=""8"" page=""0"" chnl=""15"" />
    <char id=""33"" x=""184"" y=""63"" width=""4"" height=""20"" xoffset=""2"" yoffset=""6"" xadvance=""8"" page=""0"" chnl=""15"" />
    <char id=""34"" x=""240"" y=""79"" width=""10"" height=""7"" xoffset=""0"" yoffset=""6"" xadvance=""10"" page=""0"" chnl=""15"" />
    <char id=""35"" x=""18"" y=""48"" width=""16"" height=""20"" xoffset=""-1"" yoffset=""6"" xadvance=""15"" page=""0"" chnl=""15"" />
    <char id=""36"" x=""94"" y=""0"" width=""15"" height=""23"" xoffset=""0"" yoffset=""5"" xadvance=""15"" page=""0"" chnl=""15"" />
    <char id=""37"" x=""161"" y=""0"" width=""22"" height=""20"" xoffset=""1"" yoffset=""6"" xadvance=""24"" page=""0"" chnl=""15"" />
  </chars>
</font>

";
        BitmapFont font = new BitmapFont((Texture2D)null, fontPattern);

        // We have to explicitly set the font pattern because the
        // font doesn't know its own texture size:

        font.SetFontPattern(256, 256);

        font.Characters.Length.ShouldBe(38,
            "because BitmapFonts always contain characters up to the last index, which in this case is 37. " +
            "We start counting at 0, so that's 38");

        font.Characters[32].TULeft.ShouldBe(94f / 256f);
        font.Characters[32].TVTop.ShouldBe(24f / 256f);
        font.Characters[32].TURight.ShouldBe(97f / 256f);
        font.Characters[32].TVBottom.ShouldBe(25f / 256f);
        font.Characters[32].XAdvance.ShouldBe(8);
        font.Characters[32].XOffsetInPixels.ShouldBe(-1);
        font.Characters[32].PageNumber.ShouldBe(0);
    }
}
