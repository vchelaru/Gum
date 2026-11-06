using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Wireframe;
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
/// While oversampling pins a measurement font, positions within a line follow the drawn glyphs: the
/// display font's whole-pixel advances times the effective scale, not the measurement font's (#7675).
/// Each test pins a font whose W advances 17 pixels and draws from one whose W advances 30 at 18 / 32.4,
/// as Arial 18 does at a zoom of 1.8, so forty W's are drawn about 666.7 wide while layout measures 680.
/// </summary>
public class OversampledTextPositionTests : BaseTestClass
{
    // Issue #6814: the display font draws at exactly the inverse raster ratio so that each atlas pixel
    // lands on one screen pixel. Its whole-pixel advances then add up to a different width than the
    // native measurement, and the glyphs keep them rather than being fitted to the measured width.
    [Fact]
    public void MeasureDrawnString_WhileOversampled_IsTheDisplayFontAdvancesTimesTheEffectiveScale()
    {
        const int measuredAdvance = 17;
        const int drawnAdvance = 30;
        const float compensation = 18f / 32.4f;
        string text = new string('W', 40);
        TextRuntime textRuntime = CreateText(HorizontalAlignment.Left);
        Text contained = Oversample(textRuntime, measuredAdvance, drawnAdvance, compensation);
        textRuntime.Text = text;

        for (int index = 0; index <= text.Length; index++)
        {
            contained.MeasureDrawnString(text.Substring(0, index), HorizontalMeasurementStyle.Full)
                .ShouldBe(index * drawnAdvance * compensation, tolerance: 0.001f, $"pen position before character {index}");
        }
        contained.UpdateIpsoForRenderingForTests().Width.ShouldBe(text.Length * drawnAdvance * compensation, tolerance: 0.001f,
            "because the drawn text is as wide as the display font's advances, about 666.7, not the measured 680");
        contained.WrappedTextWidth.ShouldBe(text.Length * measuredAdvance,
            "because layout keeps measuring with the pinned font (#4309)");
    }

    [Theory]
    [InlineData(HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right)]
    public void GetCharacterIndexAtPosition_WhileOversampled_ReturnsTheCharacterDrawnAtThePoint(HorizontalAlignment alignment)
    {
        const int measuredAdvance = 17;
        const int drawnAdvance = 30;
        const float compensation = 18f / 32.4f;
        string text = new string('W', 40);
        TextRuntime textRuntime = CreateText(alignment);
        Text contained = Oversample(textRuntime, measuredAdvance, drawnAdvance, compensation);
        textRuntime.Text = text;
        float drawnLeft = contained.UpdateIpsoForRenderingForTests().X;
        float y = contained.UpdateIpsoForRenderingForTests().Y + 1;
        float advance = drawnAdvance * compensation;

        for (int character = 0; character < text.Length; character++)
        {
            float start = drawnLeft + character * advance;
            contained.GetCharacterIndexAtPosition(start + advance * 0.25f, y)
                .ShouldBe(character, $"left quarter of character {character}");
            contained.GetCharacterIndexAtPosition(start + advance * 0.75f, y)
                .ShouldBe(character + 1, $"right quarter of character {character}");
        }
    }

    [Theory]
    [InlineData(HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right)]
    public void TextBoxCaret_WhileOversampled_SitsAtTheDrawnPenPosition(HorizontalAlignment alignment)
    {
        const int measuredAdvance = 17;
        const int drawnAdvance = 30;
        const float compensation = 18f / 32.4f;
        string text = new string('W', 40);
        TextBox textBox = CreateTextBox();
        TextRuntime textInstance = GetTextInstance(textBox);
        textInstance.HorizontalAlignment = alignment;
        Text contained = Oversample(textInstance, measuredAdvance, drawnAdvance, compensation);
        textBox.Text = text;
        GraphicalUiElement caret = ((TextBoxBaseVisual)textBox.Visual).CaretInstance;
        float drawnLeft = contained.UpdateIpsoForRenderingForTests().X;

        for (int index = 0; index <= text.Length; index++)
        {
            textBox.CaretIndex = index;

            (caret.GetAbsoluteLeft() - drawnLeft)
                .ShouldBe(index * drawnAdvance * compensation, tolerance: 0.001f, $"caret index {index}");
        }
    }

    [Fact]
    public void TextBoxSelection_WhileOversampled_CoversTheDrawnGlyphs()
    {
        const int measuredAdvance = 17;
        const int drawnAdvance = 30;
        const float compensation = 18f / 32.4f;
        TextBox textBox = CreateTextBox();
        Text contained = Oversample(GetTextInstance(textBox), measuredAdvance, drawnAdvance, compensation);
        textBox.Text = new string('W', 40);
        textBox.IsFocused = true;
        textBox.SelectionStart = 10;
        textBox.SelectionLength = 20;

        GraphicalUiElement selection = ((TextBoxBaseVisual)textBox.Visual).ClipContainer.Children
            .OfType<GraphicalUiElement>()
            .Single(child => child.Name == "SelectionInstance" && child.Visible);

        (selection.GetAbsoluteLeft() - contained.GetAbsoluteLeft()).ShouldBe(10 * drawnAdvance * compensation, tolerance: 0.001f);
        // A single-line selection is drawn one unit wider than the characters it covers.
        selection.GetAbsoluteWidth().ShouldBe(20 * drawnAdvance * compensation + 1, tolerance: 0.001f);
    }

    [Fact]
    public void TextBoxGetCaretIndexAtPosition_WhileOversampled_ReturnsTheCharacterDrawnAtThePoint()
    {
        const int measuredAdvance = 17;
        const int drawnAdvance = 30;
        const float compensation = 18f / 32.4f;
        string text = new string('W', 40);
        TextBox textBox = CreateTextBox();
        Text contained = Oversample(GetTextInstance(textBox), measuredAdvance, drawnAdvance, compensation);
        textBox.Text = text;
        float drawnLeft = contained.UpdateIpsoForRenderingForTests().X;
        float y = contained.GetAbsoluteTop() + 1;
        float advance = drawnAdvance * compensation;

        for (int character = 0; character < text.Length; character++)
        {
            float start = drawnLeft + character * advance;
            textBox.GetCaretIndexAtPosition(start + advance * 0.25f, y)
                .ShouldBe(character, $"left quarter of character {character}");
            textBox.GetCaretIndexAtPosition(start + advance * 0.75f, y)
                .ShouldBe(character + 1, $"right quarter of character {character}");
        }
    }

    // Issue #7683: oversampled text draws each atlas pixel as one screen pixel (scale * zoom == 1), so every glyph
    // must land at the block's snapped origin plus its whole-pixel atlas offset. Before the fix a block on an
    // exact half screen pixel at zoom 3 or 5 let each glyph round to either side on its own float noise.
    public static IEnumerable<object[]> OversampledBlockCases()
    {
        foreach (int zoom in new[] { 2, 3, 4, 5 })
        {
            foreach (double originInScreenPixels in new[] { 30.0, 31.0, 31.5, 30.5, 52.5, 30.25, 31.4, 30.75 })
            {
                foreach (HorizontalAlignment alignment in new[] { HorizontalAlignment.Left, HorizontalAlignment.Center, HorizontalAlignment.Right })
                {
                    yield return new object[] { zoom, originInScreenPixels, alignment };
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(OversampledBlockCases))]
    public void DrawTextLines_WhileOversampled_PlacesEveryGlyphAtTheSameWholePixelOffsetFromTheBlock(
        int zoom, double originInScreenPixels, HorizontalAlignment alignment)
    {
        List<string> lines = new() { "Statistics Window", "Second line of text" };
        List<DrawnGlyph> drawn = DrawOversampled(lines, zoom, (float)(originInScreenPixels / zoom), alignment);

        // The first glyph of each line anchors the line; the rest must keep their atlas distance from it.
        int glyphIndex = 0;
        double previousLineBottom = double.NaN;
        for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            string line = lines[lineIndex];
            int firstOfLine = glyphIndex;
            DrawnGlyph first = drawn[firstOfLine];
            int penAtlasX = 0;
            for (int charIndex = 0; charIndex < line.Length; charIndex++, glyphIndex++)
            {
                DrawnGlyph glyph = drawn[glyphIndex];
                char c = line[charIndex];
                int expectedFromFirstLeft = penAtlasX + StubXOffset(c) - StubXOffset(line[0]);

                glyph.ScreenWidth.ShouldBe(StubWidth(c), 0.001, $"width of '{c}' in line {lineIndex}");
                glyph.ScreenHeight.ShouldBe(StubHeight(c), 0.001, $"height of '{c}' in line {lineIndex}");
                (glyph.ScreenLeft - first.ScreenLeft).ShouldBe(expectedFromFirstLeft, 0.001,
                    $"'{c}' (index {charIndex}) of line {lineIndex} at zoom {zoom}, origin {originInScreenPixels}, {alignment}");
                // All glyphs of the stub font share one baseline, so their bottoms are on one screen row.
                glyph.ScreenBottom.ShouldBe(first.ScreenBottom, 0.001,
                    $"bottom of '{c}' (index {charIndex}) of line {lineIndex} at zoom {zoom}, origin {originInScreenPixels}, {alignment}");
                penAtlasX += StubAdvance(c);
            }

            if (!double.IsNaN(previousLineBottom))
            {
                (first.ScreenBottom - previousLineBottom).ShouldBe(StubLineHeight, 0.001,
                    $"line {lineIndex} sits one atlas line below the previous one");
            }
            previousLineBottom = first.ScreenBottom;
        }
    }

    // Styled (BBCode) text draws each run with its own DrawTextLines call, starting where the previous run's
    // width ended, so the second run's origin carries float noise relative to the first.
    [Theory]
    [MemberData(nameof(OversampledBlockCases))]
    public void DrawTextLines_WhileOversampled_KeepsASecondRunStartedAtTheFirstRunsWidthOnTheSameGrid(
        int zoom, double originInScreenPixels, HorizontalAlignment alignment)
    {
        const string firstRun = "Statistics ";
        const string secondRun = "Window";
        float originWorld = (float)(originInScreenPixels / zoom);
        BitmapFont font = CreateVariedGlyphFont();
        SpriteRenderer spriteRenderer = new() { CurrentZoom = zoom };
        List<DrawnGlyph> drawn = new();
        spriteRenderer.DrawObserver = (position, scale, source) =>
        {
            System.Drawing.Rectangle sourceRectangle = source!.Value;
            drawn.Add(new DrawnGlyph(position.X * zoom, position.Y * zoom,
                sourceRectangle.Width * scale.X * zoom, sourceRectangle.Height * scale.Y * zoom));
        };
        float scale = 1f / zoom;

        List<int> firstWidths = new();
        List<string> firstLines = new() { firstRun };
        font.GetRequiredWidthAndHeight(firstLines, out int firstRequiredWidth, out int _, firstWidths);
        var firstRect = font.DrawTextLines(firstLines, alignment, new object(), firstRequiredWidth, firstWidths,
            spriteRenderer, System.Drawing.Color.White, originWorld, originWorld, scaleX: scale, scaleY: scale);

        List<int> secondWidths = new();
        List<string> secondLines = new() { secondRun };
        font.GetRequiredWidthAndHeight(secondLines, out int secondRequiredWidth, out int _, secondWidths);
        font.DrawTextLines(secondLines, alignment, new object(), secondRequiredWidth, secondWidths,
            spriteRenderer, System.Drawing.Color.White, originWorld + firstRect.Width, originWorld,
            scaleX: scale, scaleY: scale, shiftForOutline: false);

        int firstRunGlyphs = firstRun.Length;
        int penAtlasX = 0;
        foreach (char c in firstRun)
        {
            penAtlasX += StubAdvance(c);
        }
        double expectedGap = penAtlasX + StubXOffset(secondRun[0]) - StubXOffset(firstRun[0]);
        // Alignment moves each run by its own fraction; only a left-aligned pair is expected to line up.
        if (alignment == HorizontalAlignment.Left)
        {
            (drawn[firstRunGlyphs].ScreenLeft - drawn[0].ScreenLeft).ShouldBe(expectedGap, 0.001,
                $"second run at zoom {zoom}, origin {originInScreenPixels}");
        }
        drawn[firstRunGlyphs].ScreenBottom.ShouldBe(drawn[0].ScreenBottom, 0.001, "both runs share a baseline");
    }

    // Draws through the real BitmapFont.DrawTextLines and SpriteRenderer.Draw at an oversampled scale, recording
    // each glyph in screen pixels.
    static List<DrawnGlyph> DrawOversampled(List<string> lines, int zoom, float originWorld, HorizontalAlignment alignment)
    {
        BitmapFont font = CreateVariedGlyphFont();
        SpriteRenderer spriteRenderer = new() { CurrentZoom = zoom };
        List<DrawnGlyph> drawn = new();
        spriteRenderer.DrawObserver = (position, scale, source) =>
        {
            System.Drawing.Rectangle sourceRectangle = source!.Value;
            drawn.Add(new DrawnGlyph(
                position.X * zoom, position.Y * zoom,
                sourceRectangle.Width * scale.X * zoom, sourceRectangle.Height * scale.Y * zoom));
        };

        List<int> widths = new();
        font.GetRequiredWidthAndHeight(lines, out int requiredWidth, out int _, widths);
        float scale = 12f / (12f * zoom);
        font.DrawTextLines(lines, alignment, objectRequestingChange: new object(), requiredWidth, widths, spriteRenderer,
            System.Drawing.Color.White, originWorld, originWorld, scaleX: scale, scaleY: scale);
        return drawn;
    }

    record struct DrawnGlyph(double ScreenLeft, double ScreenTop, double ScreenWidth, double ScreenHeight)
    {
        public double ScreenBottom => ScreenTop + ScreenHeight;
    }

    // Glyph metrics vary per character, like a real font, but every glyph ends on the same baseline row.
    const int StubLineHeight = 44;
    const int StubBaseline = 34;
    static int StubWidth(char c) => 5 + c % 20;
    static int StubHeight(char c) => 6 + c % 17;
    static int StubXOffset(char c) => c % 7;
    static int StubAdvance(char c) => 14 + c % 9;

    static BitmapFont CreateVariedGlyphFont()
    {
        System.Text.StringBuilder builder = new();
        builder.AppendLine("info face=\"Stub\" size=-12 bold=0 italic=0 charset=\"\" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0");
        builder.AppendLine($"common lineHeight={StubLineHeight} base={StubBaseline} scaleW=512 scaleH=512 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4");
        builder.AppendLine("page id=0 file=\"x.png\"");
        List<char> distinct = new();
        for (char c = ' '; c <= '~'; c++)
        {
            distinct.Add(c);
        }
        builder.AppendLine($"chars count={distinct.Count}");
        int x = 0;
        foreach (char c in distinct)
        {
            builder.AppendLine(
                $"char id={(int)c} x={x} y=0 width={StubWidth(c)} height={StubHeight(c)} xoffset={StubXOffset(c)} " +
                $"yoffset={StubBaseline - StubHeight(c)} xadvance={StubAdvance(c)} page=0 chnl=15");
            x += 24;
        }

        Texture2D placeholderTexture = (Texture2D)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
        BitmapFont font = new BitmapFont(placeholderTexture, builder.ToString());
        font.SetFontPattern(512, 512);
        return font;
    }

    // A text made before any font exists, even a default one, has nothing to draw, so it is 0 wide.
    [Fact]
    public void MeasureDrawnString_WithNoFont_IsZero()
    {
        BitmapFont? savedDefault = Text.DefaultBitmapFont;
        try
        {
            Text.DefaultBitmapFont = null;
            Text text = new();
            text.BitmapFont = null;

            text.MeasureDrawnString("WW", HorizontalMeasurementStyle.Full).ShouldBe(0);
            text.GetDrawnCharacterAdvance('W').ShouldBe(0);
        }
        finally
        {
            Text.DefaultBitmapFont = savedDefault;
        }
    }

    // Pins a font of the measured advance for layout and draws from one of the drawn advance at the
    // compensation scale, as TextRuntime.RegenerateOversampledFont does.
    static Text Oversample(TextRuntime textRuntime, int measuredAdvance, int drawnAdvance, float compensation)
    {
        Text contained = (Text)textRuntime.RenderableComponent!;
        contained.BitmapFont = CreateFont(measuredAdvance);
        contained.SetOversampledDisplayFont(CreateFont(drawnAdvance));
        contained.OversampleCompensationScale = compensation;
        return contained;
    }

    // A font whose W advances a whole number of pixels, as a rasterizer gives at one size.
    static BitmapFont CreateFont(int wAdvance)
    {
        string fontData =
$@"info face=""Stub"" size=-{wAdvance} bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight={wAdvance} base={wAdvance} scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""x.png""
chars count=1
char id=87 x=0 y=0 width={wAdvance} height=13 xoffset=0 yoffset=4 xadvance={wAdvance} page=0 chnl=15
";
        BitmapFont font = new BitmapFont((Texture2D)null!, fontData);
        font.SetFontPattern(256, 256);
        return font;
    }

    static TextRuntime CreateText(HorizontalAlignment alignment)
    {
        TextRuntime textRuntime = new();
        textRuntime.X = 20;
        textRuntime.Y = 30;
        textRuntime.Width = 1000;
        textRuntime.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        textRuntime.HorizontalAlignment = alignment;
        return textRuntime;
    }

    static TextBox CreateTextBox()
    {
        TextBox textBox = new();
        textBox.Width = 1000;
        textBox.Visual.X = 20;
        textBox.Visual.Y = 30;
        return textBox;
    }

    static TextRuntime GetTextInstance(TextBox textBox) => ((TextBoxBaseVisual)textBox.Visual).TextInstance;
}
