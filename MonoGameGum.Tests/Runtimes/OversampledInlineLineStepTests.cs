using Gum.GueDeriving;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Text = RenderingLibrary.Graphics.Text;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// While oversampling pins a native measurement font and draws from a bigger one, every line of a text steps by
/// the native font's line height, whether or not the text has inline runs (#7733). The native font is size 12
/// with a line height of 15 drawn at a font scale of 0.5, so a line is 7.5 tall. The display fonts are the ones
/// a zoom of 3 and 4 choose (size 18 with line height 21, size 24 with line height 27) and one whose line
/// height is an exact multiple (size 24, line height 30), which hides the drift.
/// </summary>
public class OversampledInlineLineStepTests : BaseTestClass
{
    const float FontScale = 0.5f;
    const int NativeSize = 12;
    const int NativeLineHeight = 15;

    // The glyphs of a line are placed on whole pixels, so a drawn top is within one pixel of the line's top. The
    // drift this guards against is a pixel and a half by the second line and grows with every line.
    const float GlyphRounding = 1f;

    public static IEnumerable<object[]> DisplayFonts()
    {
        yield return new object[] { 18, 21 };
        yield return new object[] { 24, 27 };
        yield return new object[] { 24, 30 };
        yield return new object[] { 36, 40 };
    }

    public static IEnumerable<object[]> DisplayFontsAndMultipliers()
    {
        foreach (float multiplier in new[] { 1f, 1.3f, 0.8f })
        {
            foreach (object[] font in DisplayFonts())
            {
                yield return new object[] { font[0], font[1], multiplier };
            }
        }
    }

    static string Lines(int count, string firstLine = "one") =>
        string.Join("\n", new[] { firstLine }.Concat(Enumerable.Range(1, count - 1).Select(i => "line" + i)));

    [Theory]
    [MemberData(nameof(DisplayFontsAndMultipliers))]
    public void DrawnLineTops_WithAColorRun_StepByTheNativeLineHeight(int displaySize, int displayLineHeight, float multiplier)
    {
        Text styled = Create(Lines(10, "[Color=Orange]one[/Color]"), displaySize, displayLineHeight, multiplier);

        List<float> tops = DrawnLineTops(styled);

        float step = NativeLineHeight * FontScale * multiplier;
        tops.Count.ShouldBe(10);
        for (int i = 0; i < tops.Count; i++)
        {
            tops[i].ShouldBe(tops[0] + i * step, tolerance: GlyphRounding, $"line {i}");
        }
    }

    [Theory]
    [MemberData(nameof(DisplayFontsAndMultipliers))]
    public void DrawnLineTops_WithAColorRun_EqualThoseOfTheSameTextWithoutIt(int displaySize, int displayLineHeight, float multiplier)
    {
        Text plain = Create(Lines(10), displaySize, displayLineHeight, multiplier);
        Text styled = Create(Lines(10, "[Color=Orange]one[/Color]"), displaySize, displayLineHeight, multiplier);

        DrawnLineTops(styled).ShouldBe(DrawnLineTops(plain), tolerance: 0.001f);
    }

    [Theory]
    [MemberData(nameof(DisplayFontsAndMultipliers))]
    public void GetDrawnLineTop_WithAColorRun_StepsByTheNativeLineHeight(int displaySize, int displayLineHeight, float multiplier)
    {
        Text styled = Create(Lines(10, "[Color=Orange]one[/Color]"), displaySize, displayLineHeight, multiplier);

        float step = NativeLineHeight * FontScale * multiplier;
        for (int i = 0; i <= 10; i++)
        {
            styled.GetDrawnLineTop(i).ShouldBe(i * step, tolerance: 0.001f, $"line {i}");
        }
    }

    [Theory]
    [MemberData(nameof(DisplayFonts))]
    public void LastDrawnLine_WithAColorRun_EndsAtTheBottomOfTheLaidOutText(int displaySize, int displayLineHeight)
    {
        Text styled = Create(Lines(10, "[Color=Orange]one[/Color]"), displaySize, displayLineHeight, 1f);

        styled.GetDrawnLineTop(styled.WrappedText.Count).ShouldBe(styled.WrappedTextHeight, tolerance: 0.001f);
    }

    [Theory]
    [MemberData(nameof(DisplayFonts))]
    public void DrawnLineTops_WithABlankLineBetweenStyledLines_StepByTheNativeLineHeight(int displaySize, int displayLineHeight)
    {
        Text styled = Create("[Color=Red]a[/Color]\n\nb\n[Color=Red]c[/Color]", displaySize, displayLineHeight, 1f);

        List<float> tops = DrawnLineTops(styled);

        // The blank line still takes a row, so the last line starts three rows below the first.
        float step = NativeLineHeight * FontScale;
        tops.Last().ShouldBe(tops[0] + 3 * step, tolerance: GlyphRounding);
        styled.GetDrawnLineTop(4).ShouldBe(4 * step, tolerance: 0.001f);
    }

    [Theory]
    [MemberData(nameof(DisplayFonts))]
    public void DrawnLineTops_OfWrappedLinesWithAColorRun_EqualThoseOfTheSameTextWithoutIt(int displaySize, int displayLineHeight)
    {
        const string words = "aa bb cc dd ee ff gg hh";
        Text plain = Create(words, displaySize, displayLineHeight, 1f, width: 30);
        Text styled = Create("[Color=Red]aa[/Color] bb cc dd ee ff gg hh", displaySize, displayLineHeight, 1f, width: 30);

        plain.WrappedText.Count.ShouldBeGreaterThan(2);
        styled.WrappedText.Count.ShouldBe(plain.WrappedText.Count);
        DrawnLineTops(styled).ShouldBe(DrawnLineTops(plain), tolerance: 0.001f);
    }

    [Theory]
    [MemberData(nameof(DisplayFonts))]
    public void DrawnLineTops_WithoutOversampling_StepTheSameWithAndWithoutARun(int displaySize, int displayLineHeight)
    {
        Text plain = Create(Lines(10), displaySize, displayLineHeight, 1f, oversample: false);
        Text styled = Create(Lines(10, "[Color=Orange]one[/Color]"), displaySize, displayLineHeight, 1f, oversample: false);

        DrawnLineTops(styled).ShouldBe(DrawnLineTops(plain), tolerance: 0.001f);
        styled.GetDrawnLineTop(3).ShouldBe(3 * displayLineHeight * FontScale, tolerance: 0.001f);
    }

    [Theory]
    [MemberData(nameof(DisplayFonts))]
    public void GetDrawnLineTop_WithASizeRun_GrowsTheLineByTheRunAndEndsAtTheLaidOutBottom(int displaySize, int displayLineHeight)
    {
        Text styled = Create("a\nb\n[FontScale=1]c[/FontScale]\nd", displaySize, displayLineHeight, 1f);

        // The run draws at a font scale of 1, twice the text's 0.5, so its line is 15 tall instead of 7.5.
        float step = NativeLineHeight * FontScale;
        styled.GetDrawnLineTop(1).ShouldBe(step, tolerance: 0.001f);
        styled.GetDrawnLineTop(2).ShouldBe(2 * step, tolerance: 0.001f);
        styled.GetDrawnLineTop(3).ShouldBe(2 * step + NativeLineHeight, tolerance: 0.001f);
        styled.GetDrawnLineTop(4).ShouldBe(3 * step + NativeLineHeight, tolerance: 0.001f);
        styled.GetDrawnLineTop(4).ShouldBe(styled.WrappedTextHeight, tolerance: 0.001f);
    }

    [Theory]
    [InlineData(18, 21, VerticalAlignment.Top)]
    [InlineData(18, 21, VerticalAlignment.Center)]
    [InlineData(18, 21, VerticalAlignment.Bottom)]
    [InlineData(24, 27, VerticalAlignment.Center)]
    [InlineData(24, 27, VerticalAlignment.Bottom)]
    public void DrawnLineTops_WithAColorRun_StartWhereThoseOfTheSameTextWithoutItDo_ForEveryVerticalAlignment(
        int displaySize, int displayLineHeight, VerticalAlignment alignment)
    {
        Text plain = Create(Lines(10), displaySize, displayLineHeight, 1f, verticalAlignment: alignment, height: 200);
        Text styled = Create(Lines(10, "[Color=Orange]one[/Color]"), displaySize, displayLineHeight, 1f, verticalAlignment: alignment, height: 200);

        DrawnLineTops(styled).ShouldBe(DrawnLineTops(plain), tolerance: 0.001f);
    }

    [Theory]
    [MemberData(nameof(DisplayFonts))]
    public void GetCharacterIndexAtPosition_WithAColorRun_FindsEachRowAtItsLaidOutPosition(int displaySize, int displayLineHeight)
    {
        TextRuntime runtime = CreateRuntime(Lines(10, "[Color=Orange]one[/Color]"), displaySize, displayLineHeight, 1f,
            verticalAlignment: VerticalAlignment.Top);
        Text styled = (Text)runtime.RenderableComponent!;
        float step = NativeLineHeight * FontScale;
        float top = runtime.GetAbsoluteTop();
        // Inside the first half of the first glyph, which is a pixel or two wide.
        float left = runtime.GetAbsoluteLeft() + 0.01f;

        int rowStart = 0;
        for (int row = 0; row < 10; row++)
        {
            styled.GetCharacterIndexAtPosition(left, top + (row + 0.5f) * step)
                .ShouldBe(rowStart, $"row {row}");
            rowStart += styled.WrappedText[row].Length;
        }
    }

    // The distinct tops of the glyphs drawn, one per line that has any. Every glyph of the test fonts has the
    // same height and offset, so these step exactly as the lines do.
    static List<float> DrawnLineTops(Text text)
    {
        SpriteRenderer spriteRenderer = new() { CurrentZoom = 1 };
        List<float> tops = new();
        spriteRenderer.DrawObserver = (position, scale, source) => tops.Add(position.Y);

        text.OverrideTextRenderingPositionMode = TextRenderingPositionMode.FreeFloating;
        text.DrawCharacterByCharacterForTests(spriteRenderer);

        return tops.Select(top => MathF.Round(top, 3)).Distinct().OrderBy(top => top).ToList();
    }

    static Text Create(string text, int displaySize, int displayLineHeight, float multiplier, bool oversample = true, float width = 1000,
        VerticalAlignment? verticalAlignment = null, float? height = null) =>
        (Text)CreateRuntime(text, displaySize, displayLineHeight, multiplier, oversample, width, verticalAlignment, height).RenderableComponent!;

    static TextRuntime CreateRuntime(string text, int displaySize, int displayLineHeight, float multiplier, bool oversample = true, float width = 1000,
        VerticalAlignment? verticalAlignment = null, float? height = null)
    {
        TextRuntime runtime = new();
        runtime.Width = width;
        runtime.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        if (height != null)
        {
            runtime.Height = height.Value;
            runtime.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        }
        if (verticalAlignment != null)
        {
            runtime.VerticalAlignment = verticalAlignment.Value;
        }
        runtime.FontScale = FontScale;
        runtime.LineHeightMultiplier = multiplier;
        Text contained = (Text)runtime.RenderableComponent!;
        if (oversample)
        {
            contained.BitmapFont = CreateFont(NativeSize, NativeLineHeight);
            contained.SetOversampledDisplayFont(CreateFont(displaySize, displayLineHeight));
            contained.OversampleCompensationScale = (float)NativeSize / displaySize;
        }
        else
        {
            contained.BitmapFont = CreateFont(displaySize, displayLineHeight);
        }
        runtime.Text = text;
        return runtime;
    }

    // Every printable character is the same glyph, so the lines of a text are the only thing that moves them.
    static BitmapFont CreateFont(int size, int lineHeight)
    {
        System.Text.StringBuilder builder = new();
        builder.AppendLine($"info face=\"Stub\" size=-{size} bold=0 italic=0 charset=\"\" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0");
        builder.AppendLine($"common lineHeight={lineHeight} base={lineHeight * 4 / 5} scaleW=512 scaleH=512 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4");
        builder.AppendLine("page id=0 file=\"x.png\"");
        builder.AppendLine($"chars count={'~' - ' ' + 1}");
        for (char c = ' '; c <= '~'; c++)
        {
            // A space advances but draws nothing, as in a real font.
            int glyphWidth = c == ' ' ? 0 : 6;
            int glyphHeight = c == ' ' ? 0 : 8;
            builder.AppendLine($"char id={(int)c} x=0 y=0 width={glyphWidth} height={glyphHeight} xoffset=0 yoffset=2 xadvance=7 page=0 chnl=15");
        }

        Texture2D placeholderTexture = (Texture2D)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
        BitmapFont font = new BitmapFont(placeholderTexture, builder.ToString());
        font.SetFontPattern(512, 512);
        return font;
    }
}
