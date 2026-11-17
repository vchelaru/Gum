using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using RenderingLibrary.Math;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

// Issue #8208: kerning pairs from the .fnt move the pen between two consecutive characters, in both
// measurement and drawing, so the measured width matches the drawn glyphs.
public class BitmapFontKerningTests
{
    // 'A' (65) and 'V' (86): 10 advance, 8 wide, xoffset 1.
    static BitmapFont CreateFont(string kerningLines)
    {
        string data =
"info face=\"Stub\" size=-12 bold=0 italic=0 charset=\"\" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0\n" +
"common lineHeight=12 base=10 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4\n" +
"page id=0 file=\"x.png\"\n" +
"chars count=3\n" +
"char id=32 x=0  y=0 width=0 height=0  xoffset=0 yoffset=10 xadvance=4  page=0 chnl=15\n" +
"char id=65 x=10 y=0 width=8 height=10 xoffset=1 yoffset=0  xadvance=10 page=0 chnl=15\n" +
"char id=86 x=30 y=0 width=8 height=10 xoffset=1 yoffset=0  xadvance=10 page=0 chnl=15\n" +
kerningLines;
        Texture2D placeholderTexture = (Texture2D)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
        BitmapFont font = new BitmapFont(placeholderTexture, data);
        font.SetFontPattern(256, 256);
        return font;
    }

    [Theory]
    // Full: A advance 10, pair -3, V advance 10.
    [InlineData(HorizontalMeasurementStyle.Full, 17)]
    // TrimRight: A advance 10, pair -3, V trimmed to width 8 + xoffset 1.
    [InlineData(HorizontalMeasurementStyle.TrimRight, 16)]
    public void MeasureString_AppliesKerningPairBetweenConsecutiveCharacters(HorizontalMeasurementStyle style, int expectedWidth)
    {
        BitmapFont font = CreateFont("kernings count=1\nkerning first=65 second=86 amount=-3\n");

        font.MeasureString("AV", style).ShouldBe(expectedWidth);
        // The pair is ordered: V followed by A has no pair.
        font.MeasureString("VA", style).ShouldBe(expectedWidth + 3);
    }

    [Fact]
    public void MeasureString_WithoutKerningPairs_SumsAdvances()
    {
        BitmapFont font = CreateFont("");

        font.MeasureString("AV", HorizontalMeasurementStyle.Full).ShouldBe(20);
        font.GetKerning('A', 'V').ShouldBe(0);
    }

    [Fact]
    public void GetKerning_ReturnsPairAmount_AndZeroForCharactersTheFontLacks()
    {
        BitmapFont font = CreateFont("kernings count=1\nkerning first=65 second=86 amount=-3\n");

        font.GetKerning('A', 'V').ShouldBe(-3);
        font.GetKerning('V', 'A').ShouldBe(0);
        font.GetKerning('一', 'V').ShouldBe(0);
        font.GetKerning('A', '一').ShouldBe(0);
    }

    [Fact]
    public void GetIndexInLine_UsesTheKernedPenPosition()
    {
        BitmapFont font = CreateFont("kernings count=1\nkerning first=65 second=86 amount=-3\n");

        // Kerned, V spans 7 to 17, so 13 is past its middle (12); unkerned it spans 10 to 20 with its middle at 15.
        int index = TextHitTesting.GetIndexInLine("AV", 13,
            c => font.GetCharacterInfo(c)!.XAdvance, (first, second) => font.GetKerning(first, second));

        index.ShouldBe(2);
    }

    [Fact]
    public void DrawTextLines_MovesTheSecondGlyphByThePair_AndNotAcrossALineBreak()
    {
        BitmapFont font = CreateFont("kernings count=1\nkerning first=65 second=86 amount=-3\n");
        SpriteRenderer spriteRenderer = new() { CurrentZoom = 1 };
        List<float> drawnLefts = new();
        spriteRenderer.DrawObserver = (position, scale, source) => drawnLefts.Add(position.X);

        List<string> lines = new() { "AV", "A", "V" };
        List<int> widths = new();
        font.GetRequiredWidthAndHeight(lines, out int requiredWidth, out int _, widths);
        FloatRectangle drawn = font.DrawTextLines(lines, HorizontalAlignment.Left, new object(), requiredWidth, widths,
            spriteRenderer, System.Drawing.Color.White);

        drawnLefts.Count.ShouldBe(4);
        // A at xoffset 1; V at A's advance 10 + pair -3 + xoffset 1.
        drawnLefts[0].ShouldBe(1);
        drawnLefts[1].ShouldBe(8);
        // "A" then "V" on separate lines: each starts at the line start, no pair applied.
        drawnLefts[2].ShouldBe(1);
        drawnLefts[3].ShouldBe(1);
        // The drawn pen width matches the Full measurement of the kerned line.
        drawn.Width.ShouldBe(font.MeasureString("AV", HorizontalMeasurementStyle.Full));
    }
}
