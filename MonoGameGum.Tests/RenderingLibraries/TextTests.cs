using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

using Text = RenderingLibrary.Graphics.Text;

namespace MonoGameGum.Tests.RenderingLibraries;

public class TextTests
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

    private static BitmapFont CreateBitmapFontWithDivergentLastChar()
    {
        BitmapFont font = new BitmapFont((Texture2D)null!, basicBMFontFileData);
        font.SetFontPattern(256, 256);

        // Force the last character's XAdvance to differ from its pixel extents
        // so Full and TrimRight styles produce different results.
        BitmapCharacterInfo character = font.Characters['!'];
        character.XAdvance = 20;
        character.PixelLeft = 0;
        character.PixelRight = 5;
        character.XOffsetInPixels = 1;

        // BitmapFont.MeasureString only honors TrimRight when Texture != null
        // (see the guard in BitmapFont.MeasureString). We can't construct a
        // real Texture2D without a GraphicsDevice in a unit test, so we plant
        // an uninitialized placeholder directly into the internal texture
        // array via reflection. MeasureString only reads the reference, never
        // dereferences it, so this is safe.
        Texture2D placeholderTexture = (Texture2D)RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
        FieldInfo mTexturesField = typeof(BitmapFont).GetField("mTextures", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Texture2D[] textures = (Texture2D[])mTexturesField.GetValue(font)!;
        textures[0] = placeholderTexture;

        return font;
    }

    // Regression guard for the #3372 fix: when the height is a real, independent constraint,
    // TextOverflowVerticalMode.TruncateLine MUST still clip to the number of fully-fitting
    // lines. The fix only removes the content-derived (RelativeToChildren) height->lines
    // feedback; it must not disable height-based truncation for fixed-height text (the
    // DialogBox pagination path relies on this). Tested at the renderable level because this
    // is the pure UpdateLines contract — the lineHeight is 21 (from basicBMFontFileData), so
    // a height of 52 leaves room for two full lines and a partial third.
    [Fact]
    public void UpdateLines_TruncateLine_WithFixedHeight_ClipsToFittingLines()
    {
        BitmapFont font = new BitmapFont((Texture2D)null!, basicBMFontFileData);
        font.SetFontPattern(256, 256);

        Text text = new Text();
        text.BitmapFont = font;
        text.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;
        text.Width = 1000; // wide enough that the explicit lines never wrap
        text.Height = 52;  // two full 21px lines plus a partial third

        text.RawText = "Line1\nLine2\nLine3";

        text.WrappedText.Count.ShouldBe(2,
            "Because TruncateLine with a fixed height must clip to the number of fully-fitting lines");
    }

    // The #3372 fix at the renderable level: when the layout marks the Height as content-derived
    // (IsHeightDependentOnLines = true, set by GraphicalUiElement for RelativeToChildren height),
    // TruncateLine must NOT cap the lines by Height — every line renders even when Height is smaller
    // than the content. Same arrangement as the clip guard above, only the flag differs.
    [Fact]
    public void UpdateLines_TruncateLine_WithHeightDependentOnLines_DoesNotClip()
    {
        BitmapFont font = new BitmapFont((Texture2D)null!, basicBMFontFileData);
        font.SetFontPattern(256, 256);

        Text text = new Text();
        text.BitmapFont = font;
        text.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;
        text.IsHeightDependentOnLines = true;
        text.Width = 1000;
        text.Height = 52; // smaller than the three 21px lines, but content-derived so it must not clip

        text.RawText = "Line1\nLine2\nLine3";

        text.WrappedText.Count.ShouldBe(3,
            "Because a content-derived Height must not be used to truncate the very lines it was derived from");
    }

    [Fact]
    public void MeasureString_WithStyleAndNoBitmapFont_DoesNotThrow()
    {
        Text text = new Text();
        // No BitmapFont set on the instance. We also expect no DefaultBitmapFont in a
        // plain unit-test run; the overload must not throw in that case.
        BitmapFont? savedDefault = Text.DefaultBitmapFont;
        Text.DefaultBitmapFont = null!;
        try
        {
            Should.NotThrow(() => text.MeasureString("hi", HorizontalMeasurementStyle.Full));
        }
        finally
        {
            Text.DefaultBitmapFont = savedDefault!;
        }
    }

    [Fact]
    public void MeasureString_WithStyleFull_MatchesBitmapFontFull()
    {
        BitmapFont font = CreateBitmapFontWithDivergentLastChar();
        Text text = new Text();
        text.BitmapFont = font;

        float expected = (float)font.MeasureString("hi!", HorizontalMeasurementStyle.Full);
        float actual = text.MeasureString("hi!", HorizontalMeasurementStyle.Full);

        actual.ShouldBe(expected);
    }

    [Fact]
    public void MeasureString_WithStyleTrimRight_MatchesBitmapFontTrimRight()
    {
        BitmapFont font = CreateBitmapFontWithDivergentLastChar();
        Text text = new Text();
        text.BitmapFont = font;

        float expectedFull = (float)font.MeasureString("hi!", HorizontalMeasurementStyle.Full);
        float expectedTrim = (float)font.MeasureString("hi!", HorizontalMeasurementStyle.TrimRight);
        float actualTrim = text.MeasureString("hi!", HorizontalMeasurementStyle.TrimRight);

        actualTrim.ShouldBe(expectedTrim);
        // Sanity: the two styles should diverge for this font, otherwise the test
        // would pass even if style were ignored.
        expectedTrim.ShouldNotBe(expectedFull);
    }
}
