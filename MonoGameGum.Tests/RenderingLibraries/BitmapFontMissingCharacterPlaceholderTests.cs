using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

// Issue #6818: a character the font doesn't have draws the font's '?' glyph, and measures with its advance.
public class BitmapFontMissingCharacterPlaceholderTests : BaseTestClass
{
    private const string Header =
@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight=21 base=17 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""Stub_0.png""
";

    private const string SpaceLine = "char id=32   x=0   y=0   width=3   height=1   xoffset=-1  yoffset=20  xadvance=5   page=0  chnl=15\n";
    private const string QuestionLine = "char id=63   x=20  y=0   width=8   height=12  xoffset=0   yoffset=4   xadvance=7   page=0  chnl=15\n";
    private const string ALine = "char id=65   x=0   y=0   width=10  height=10  xoffset=0   yoffset=0   xadvance=10  page=0  chnl=15\n";
    private const string WLine = "char id=250  x=40  y=0   width=10  height=10  xoffset=0   yoffset=0   xadvance=12  page=0  chnl=15\n";

    private static BitmapFont CreateFont(params string[] charLines)
    {
        string data = Header + $"chars count={charLines.Length}\n" + string.Concat(charLines);
        BitmapFont font = new BitmapFont((Texture2D)null!, data);
        font.SetFontPattern(256, 256);
        return font;
    }

    private static void ShouldDrawAs(BitmapCharacterInfo? actual, BitmapCharacterInfo expected)
    {
        actual.ShouldNotBeNull();
        actual.PixelLeft.ShouldBe(expected.PixelLeft);
        actual.PixelRight.ShouldBe(expected.PixelRight);
        actual.PixelBottom.ShouldBe(expected.PixelBottom);
        actual.XAdvance.ShouldBe(expected.XAdvance);
    }

    [Fact]
    public void GetCharacterInfo_WhenCharacterIsMissing_ReturnsQuestionMarkGlyph()
    {
        // 'É' (201) is inside the character table, '€' (8364) is past its end.
        BitmapFont font = CreateFont(SpaceLine, QuestionLine, ALine, WLine);
        BitmapCharacterInfo question = font.GetCharacterInfo('?')!;

        ShouldDrawAs(font.GetCharacterInfo('É'), question);
        ShouldDrawAs(font.GetCharacterInfo('€'), question);
        font.MeasureString("AÉ€").ShouldBe(font.MeasureString("A??"));
    }

    [Fact]
    public void GetCharacterInfo_WhenQuestionMarkIsMissingToo_ReturnsSpace()
    {
        BitmapFont font = CreateFont(SpaceLine, ALine, WLine);
        BitmapCharacterInfo space = font.GetCharacterInfo(' ')!;

        ShouldDrawAs(font.GetCharacterInfo('É'), space);
        ShouldDrawAs(font.GetCharacterInfo('€'), space);
    }

    [Fact]
    public void GetCharacterInfo_ForControlCharacter_StaysBlank()
    {
        BitmapFont font = CreateFont(SpaceLine, QuestionLine, ALine);

        ShouldDrawAs(font.GetCharacterInfo('\r'), font.GetCharacterInfo(' ')!);
        font.GetCharacterInfo('\t')!.XAdvance.ShouldBe(20);
    }

    [Fact]
    public void AddOrUpdateCharacter_WhenQuestionMarkIsAddedLater_MissingCharactersUseIt()
    {
        BitmapFont font = CreateFont(SpaceLine, ALine, WLine);

        font.AddOrUpdateCharacter(new FontFileCharLine { Id = '?', Width = 8, Height = 12, XAdvance = 7 }, 256, 256);
        font.AddOrUpdateCharacter(new FontFileCharLine { Id = 300, Width = 10, Height = 10, XAdvance = 9 }, 256, 256);

        BitmapCharacterInfo question = font.GetCharacterInfo('?')!;
        ShouldDrawAs(font.GetCharacterInfo('É'), question);
        ShouldDrawAs(font.GetCharacterInfo((char)299), question);
        ShouldDrawAs(font.GetCharacterInfo('€'), question);
        font.GetCharacterInfo((char)300)!.XAdvance.ShouldBe(9);
        font.GetCharacterInfo('A')!.XAdvance.ShouldBe(10);
    }
}
