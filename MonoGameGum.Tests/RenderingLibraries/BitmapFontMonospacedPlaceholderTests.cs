using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

// Issue #6818 boyscout: a missing digit shares the placeholder glyph, so MakeNumbersMonospaced must not resize it.
public class BitmapFontMonospacedPlaceholderTests : BaseTestClass
{
    private const string Data =
@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight=21 base=17 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""Stub_0.png""
chars count=4
char id=32   x=0   y=0   width=3   height=1   xoffset=-1  yoffset=20  xadvance=5   page=0  chnl=15
char id=48   x=0   y=0   width=10  height=10  xoffset=0   yoffset=0   xadvance=11  page=0  chnl=15
char id=49   x=20  y=0   width=6   height=10  xoffset=0   yoffset=0   xadvance=6   page=0  chnl=15
char id=63   x=40  y=0   width=8   height=12  xoffset=0   yoffset=4   xadvance=7   page=0  chnl=15
";

    [Theory]
    [InlineData('?', 7)]
    [InlineData(' ', 5)]
    public void MakeNumbersMonospaced_WhenADigitIsMissing_LeavesSharedGlyphsUnchanged(char shared, int expectedAdvance)
    {
        BitmapFont font = new BitmapFont((Texture2D)null!, Data);
        font.SetFontPattern(256, 256);

        font.MakeNumbersMonospaced();

        font.GetCharacterInfo(shared)!.XAdvance.ShouldBe(expectedAdvance);
        font.GetCharacterInfo('1')!.XAdvance.ShouldBe(11);
        font.GetCharacterInfo('2')!.XAdvance.ShouldBe(7);
    }
}
