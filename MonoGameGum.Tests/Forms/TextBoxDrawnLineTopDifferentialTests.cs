using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Text = RenderingLibrary.Graphics.Text;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Without font oversampling, row positions (caret Y, click-to-line, line tops) equal the formulas the
/// TextBox used before #7705, written out inline: lines step by lineHeight * scale * multiplier and a
/// glyph row is lineHeight * scale. A line with a [FontScale] run is taller, so the lines after it move down
/// by the tall line's real height and the lines before it do not move (#7705).
/// </summary>
public class TextBoxDrawnLineTopDifferentialTests : BaseTestClass
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (float scale in new[] { 1f, 1.5f, 2f })
        {
            foreach (float multiplier in new[] { 1f, 1.3f, 0.8f })
            {
                for (int lines = 0; lines <= 5; lines++)
                {
                    yield return new object[] { scale, multiplier, lines };
                }
            }
        }
    }

    static string Join(int lines) => string.Join("\n", Enumerable.Range(0, lines).Select(i => "line" + i));

    [Theory]
    [MemberData(nameof(Cases))]
    public void LineTops_EqualTheOldUniformStep(float scale, float multiplier, int lines)
    {
        TextRuntime runtime = new() { FontScale = scale, LineHeightMultiplier = multiplier, Text = Join(lines) };
        Text text = (Text)runtime.RenderableComponent!;
        IFormsText forms = text;

        float oldStep = text.LineHeightInPixels * scale * multiplier;
        for (int i = 0; i <= text.WrappedText.Count + 1; i++)
        {
            forms.GetDrawnLineTop(i).ShouldBe(i * oldStep, tolerance: 0.001f, $"line {i}");
        }
        forms.DrawnGlyphRowHeight.ShouldBe(text.LineHeightInPixels * scale, tolerance: 0.001f);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ClickAndCaret_EqualTheOldFormulas(float scale, float multiplier, int lines)
    {
        TextBox textBox = new();
        textBox.AcceptsReturn = true;
        textBox.Width = 600;
        textBox.Height = 800;
        textBox.Visual.X = 20;
        textBox.Visual.Y = 30;
        TextRuntime runtime = ((TextBoxBaseVisual)textBox.Visual).TextInstance;
        runtime.VerticalAlignment = RenderingLibrary.Graphics.VerticalAlignment.Top;
        runtime.FontScale = scale;
        runtime.LineHeightMultiplier = multiplier;
        textBox.Text = Join(lines);
        textBox.IsFocused = true;
        Text text = (Text)runtime.RenderableComponent!;
        GraphicalUiElement caret = ((TextBoxBaseVisual)textBox.Visual).CaretInstance;

        float oldStep = text.LineHeightInPixels * scale * multiplier;
        float oldGlyph = text.LineHeightInPixels * scale;
        for (int row = 0; row < lines; row++)
        {
            float rowStartIndex = Enumerable.Range(0, row).Sum(i => ("line" + i).Length + 1);
            textBox.CaretIndex = (int)rowStartIndex + 1;
            float oldCenter = runtime.GetAbsoluteTop() + row * oldStep + oldGlyph / 2f;
            (caret.GetAbsoluteTop() + caret.AbsoluteHeight / 2f).ShouldBe(oldCenter, tolerance: 0.01f, $"caret row {row}");

            // The old hit test: row = (int)(y / step), clicked mid-row.
            float clickY = runtime.GetAbsoluteTop() + (row + 0.5f) * oldStep;
            textBox.GetCaretIndexAtPosition(runtime.GetAbsoluteLeft() + 1, clickY)
                .ShouldBe((int)rowStartIndex, $"click row {row}");
        }
    }

    [Fact]
    public void SizeRun_LinesBeforeItKeepTheBaseStep_AndLinesAfterItMoveDownByItsHeight()
    {
        TextRuntime runtime = new() { Text = "a\nb\n[FontScale=3]c[/FontScale]\nd" };
        Text text = (Text)runtime.RenderableComponent!;
        IFormsText forms = text;
        float baseStep = text.LineHeightInPixels;

        forms.GetDrawnLineTop(1).ShouldBe(baseStep, tolerance: 0.001f);
        forms.GetDrawnLineTop(2).ShouldBe(2 * baseStep, tolerance: 0.001f);
        forms.GetDrawnLineTop(3).ShouldBe(2 * baseStep + 3 * baseStep, tolerance: 0.001f);
        forms.GetDrawnLineTop(4).ShouldBe(3 * baseStep + 3 * baseStep, tolerance: 0.001f);
    }

    [Fact]
    public void SizeRun_ClickOnTheLinesBeforeAndAfterIt_HitsThoseLines()
    {
        TextRuntime runtime = new() { Text = "aa\nbb\n[FontScale=3]cc[/FontScale]\ndd" };
        runtime.VerticalAlignment = RenderingLibrary.Graphics.VerticalAlignment.Top;
        Text text = (Text)runtime.RenderableComponent!;
        float baseStep = text.LineHeightInPixels;
        float top = runtime.GetAbsoluteTop();
        float left = runtime.GetAbsoluteLeft() + 1;

        text.GetCharacterIndexAtPosition(left, top + 0.5f * baseStep).ShouldBe(0);
        text.GetCharacterIndexAtPosition(left, top + 1.5f * baseStep).ShouldBe(3);
        text.GetCharacterIndexAtPosition(left, top + 2f * baseStep + 1.5f * baseStep).ShouldBe(6);
        text.GetCharacterIndexAtPosition(left, top + 5f * baseStep + 0.5f * baseStep).ShouldBe(9);
    }
}
