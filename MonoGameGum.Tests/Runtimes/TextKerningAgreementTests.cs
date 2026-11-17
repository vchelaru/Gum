using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;
using Shouldly;
using Xunit;
using Text = RenderingLibrary.Graphics.Text;

namespace MonoGameGum.Tests.Runtimes;

// Issue #8208: with the shipped default font (Font18Arial, which carries kerning pairs), measuring, drawing,
// hit testing, wrapping and the TextBox caret all agree on the kerned pen positions.
public class TextKerningAgreementTests : BaseTestClass
{
    const string Alphabet = "AVTo LyWa.,";

    static readonly Lazy<BitmapFont> arial = new(LoadArial);

    static BitmapFont Arial => arial.Value;

    // Reads MonoGameGum/Content/Font18Arial.fnt from the repo; only advances and pairs are used, so the texture is a placeholder.
    static BitmapFont LoadArial()
    {
        string relative = System.IO.Path.Combine("MonoGameGum", "Content", "Font18Arial.fnt");
        string? directory = AppContext.BaseDirectory;
        while (directory != null && !System.IO.File.Exists(System.IO.Path.Combine(directory, relative)))
        {
            directory = System.IO.Path.GetDirectoryName(directory);
        }
        string data = System.IO.File.ReadAllText(System.IO.Path.Combine(directory.ShouldNotBeNull(), relative));
        Texture2D placeholderTexture = (Texture2D)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
        BitmapFont font = new BitmapFont(placeholderTexture, data);
        font.SetFontPattern(256, 256);
        return font;
    }

    static int Advance(BitmapFont font, char c) => font.GetCharacterInfo(c)!.XAdvance;

    static float PenBefore(BitmapFont font, string s, int index) =>
        index == 0 ? 0 : font.MeasureString(s.Substring(0, index), HorizontalMeasurementStyle.Full) + font.GetKerning(s[index - 1], s[index]);

    [Fact]
    public void MeasureString_DefaultFont_KernsAVAndTo()
    {
        BitmapFont font = Arial;

        font.MeasureString("AV", HorizontalMeasurementStyle.Full).ShouldBe(Advance(font, 'A') + Advance(font, 'V') - 1);
        font.MeasureString("To", HorizontalMeasurementStyle.Full).ShouldBe(Advance(font, 'T') + Advance(font, 'o') - 2);
    }

    // The widest header of MenuNestedSubmenuSizeTests' More popup; its "standard" pin is 1 narrower than before #8208.
    [Fact]
    public void GetKerning_DefaultFont_MenuHeaderSumsToMinusOne()
    {
        BitmapFont font = Arial;
        string s = "A much longer entry";
        int sum = 0;
        for (int i = 1; i < s.Length; i++)
        {
            sum += font.GetKerning(s[i - 1], s[i]);
        }

        sum.ShouldBe(-1);
        font.GetKerning('A', ' ').ShouldBe(-1);
    }

    [Fact]
    public void DrawAndHitTest_RandomStrings_MatchMeasuredPenPositions()
    {
        BitmapFont font = Arial;
        Random random = new(8209);
        for (int n = 0; n < 300; n++)
        {
            int length = random.Next(2, 9);
            string s = new string(Enumerable.Range(0, length).Select(_ => Alphabet[random.Next(Alphabet.Length)]).ToArray());

            SpriteRenderer spriteRenderer = new() { CurrentZoom = 1 };
            List<float> lefts = new();
            spriteRenderer.DrawObserver = (position, scale, source) => lefts.Add(position.X);
            List<string> lines = new() { s };
            List<int> widths = new();
            font.GetRequiredWidthAndHeight(lines, out int requiredWidth, out int _, widths);
            FloatRectangle drawn = font.DrawTextLines(lines, HorizontalAlignment.Left, new object(), requiredWidth, widths, spriteRenderer, System.Drawing.Color.White);

            int drawnIndex = 0;
            for (int i = 0; i < length; i++)
            {
                float pen = PenBefore(font, s, i);
                float expectedLeft = pen + font.GetCharacterInfo(s[i])!.XOffsetInPixels;
                // A space may draw nothing; it is skipped when no glyph sits at its pen.
                bool isDrawn = s[i] != ' ' || (drawnIndex < lefts.Count && Math.Abs(lefts[drawnIndex] - expectedLeft) <= 0.01f);
                if (isDrawn)
                {
                    lefts[drawnIndex].ShouldBe(expectedLeft, 0.01f, $"draw '{s}' i={i}");
                    drawnIndex++;
                }

                // The hit-test boundary is the middle of the glyph's kerned advance.
                float middle = pen + Advance(font, s[i]) / 2f + (i + 1 < length ? font.GetKerning(s[i], s[i + 1]) / 2f : 0);
                TextHitTesting.GetIndexInLine(s, middle - 0.3f, c => Advance(font, c), (a, b) => font.GetKerning(a, b)).ShouldBe(i, $"hit '{s}' i={i}");
                TextHitTesting.GetIndexInLine(s, middle + 0.3f, c => Advance(font, c), (a, b) => font.GetKerning(a, b)).ShouldBe(i + 1, $"hit '{s}' i={i}");
            }
            drawn.Width.ShouldBe(font.MeasureString(s, HorizontalMeasurementStyle.Full), $"width '{s}'");
        }
    }

    [Fact]
    public void WrappedText_RandomWords_EachLineFitsAndTheNextWordWouldNot()
    {
        BitmapFont font = Arial;
        Random random = new(8208);
        string[] words = { "AV", "To", "LT", "Ay", "WA", "Vo", "A", "T", "yo" };
        int checkedLines = 0;
        for (int n = 0; n < 300; n++)
        {
            string s = string.Join(" ", Enumerable.Range(0, random.Next(2, 7)).Select(_ => words[random.Next(words.Length)]));
            int width = random.Next(20, 90);
            Text text = CreateText(font, width, s);
            if (s.Split(' ').Max(word => font.MeasureString(word)) > width)
            {
                continue;
            }

            List<string> wrapped = text.WrappedText;
            for (int i = 0; i < wrapped.Count; i++)
            {
                string line = wrapped[i];
                if (line.Trim().Contains(' '))
                {
                    font.MeasureString(line).ShouldBeLessThanOrEqualTo(width, $"'{s}' w={width} line {i} '{line}'");
                }
                if (i + 1 < wrapped.Count && wrapped[i + 1].Length > 0)
                {
                    string next = wrapped[i + 1].Split(' ')[0];
                    font.MeasureString(line + next).ShouldBeGreaterThan(width, $"'{s}' w={width} line {i} '{line}' + '{next}'");
                }
                checkedLines++;
            }
        }
        checkedLines.ShouldBeGreaterThan(100);
    }

    [Fact]
    public void WrappedText_MessySpacing_DoesNotThrowAtAnyWidth()
    {
        BitmapFont font = Arial;
        string[] messy = { " AV", "AV ", "AV  To", "  To  AV  ", "A V", " ", "  ", "T  ", "To AV ", "To  ", "A  V" };
        foreach (string s in messy)
        {
            for (int width = 1; width <= 60; width += 3)
            {
                Should.NotThrow(() => CreateText(font, width, s), $"'{s}' w={width}");
            }
        }
    }

    [Fact]
    public void TextBox_CaretAndClick_AgreeOnKernedPen()
    {
        BitmapFont font = Arial;
        TextBox textBox = new();
        textBox.Width = 1000;
        Text text = (Text)((TextBoxBaseVisual)textBox.Visual).TextInstance.RenderableComponent!;
        text.BitmapFont = font;
        string s = "To AV";
        textBox.Text = s;
        float left = text.UpdateIpsoForRenderingForTests().X;
        GraphicalUiElement caret = textBox.Visual.GetGraphicalUiElementByName("CaretInstance")!;
        float y = text.GetAbsoluteTop() + 1;

        for (int i = 0; i <= s.Length; i++)
        {
            textBox.CaretIndex = i;
            float pen = i == s.Length ? font.MeasureString(s, HorizontalMeasurementStyle.Full) : PenBefore(font, s, i);
            (caret.GetAbsoluteLeft() - left).ShouldBe(pen, 0.001f, $"caret {i}");
            textBox.GetCaretIndexAtPosition(left + pen + 0.5f, y).ShouldBe(i, $"click {i}");
        }
    }

    // Inline (BBCode) runs are measured and drawn run by run, so a pair across a run boundary applies in neither.
    [Fact]
    public void InlineRunBoundary_SkipsThePairInMeasureAndDraw()
    {
        BitmapFont font = CreateStubFont();
        Text plain = CreateText(font, 1000, "AV");
        Text styled = CreateText(font, 1000, "AV", colorRunOnV: true);

        styled.MeasureString("AV", 0).ShouldBe(plain.MeasureString("AV", 0) + 3);
        DrawnLefts(plain).ShouldBe(new[] { 1f, 8f });
        DrawnLefts(styled).ShouldBe(new[] { 1f, 11f });
    }

    static Text CreateText(BitmapFont font, float width, string s, bool colorRunOnV = false)
    {
        TextRuntime runtime = new();
        runtime.WidthUnits = DimensionUnitType.Absolute;
        runtime.Width = width;
        Text text = (Text)runtime.RenderableComponent!;
        text.BitmapFont = font;
        if (colorRunOnV)
        {
            text.InlineVariables.Add(new InlineVariable
            {
                VariableName = "Color",
                Value = System.Drawing.Color.Orange,
                StartIndex = 1,
                CharacterCount = 1
            });
            text.RawText = s;
        }
        else
        {
            runtime.Text = s;
        }
        return text;
    }

    // Glyph lefts relative to the Text's left.
    static float[] DrawnLefts(Text text)
    {
        SpriteRenderer spriteRenderer = new() { CurrentZoom = 1 };
        List<float> lefts = new();
        spriteRenderer.DrawObserver = (position, scale, source) => lefts.Add(position.X);
        text.OverrideTextRenderingPositionMode = TextRenderingPositionMode.FreeFloating;
        text.DrawCharacterByCharacterForTests(spriteRenderer);
        float origin = text.GetAbsoluteLeft();
        return lefts.Select(x => MathF.Round(x - origin, 3)).ToArray();
    }

    // 'A' and 'V' advance 10, are 8 wide with xoffset 1, and kern by -3.
    static BitmapFont CreateStubFont()
    {
        string data =
"info face=\"Stub\" size=-12 bold=0 italic=0 charset=\"\" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0\n" +
"common lineHeight=12 base=10 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4\n" +
"page id=0 file=\"x.png\"\n" +
"chars count=2\n" +
"char id=65 x=10 y=0 width=8 height=10 xoffset=1 yoffset=0  xadvance=10 page=0 chnl=15\n" +
"char id=86 x=40 y=0 width=8 height=10 xoffset=1 yoffset=0  xadvance=10 page=0 chnl=15\n" +
"kernings count=1\nkerning first=65 second=86 amount=-3\n";
        Texture2D placeholderTexture = (Texture2D)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
        BitmapFont font = new BitmapFont(placeholderTexture, data);
        font.SetFontPattern(256, 256);
        return font;
    }
}
