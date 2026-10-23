using Gum;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum;
using SkiaSharp;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Topten.RichTextKit;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Verifies the <c>[Custom=Name]</c> per-letter callback (issue #3692) mirrors the MonoGame/Raylib
/// <c>Text.Customizations</c>/<c>ContextCustomizations</c> mechanism: one styled run per character,
/// each resolved against the registered callback for Color (a normal RichTextKit <see cref="Style"/>
/// override) and XOffset/YOffset (applied as an actual glyph-position nudge via RichTextKit's
/// <c>FontRun.MoveGlyphs</c>, since <see cref="Style"/> has no offset concept).
/// </summary>
public class TextCustomizationTests
{
    private static Text MakeText()
    {
        Text text = new();
        text.FontName = "Arial";
        text.FontSize = 20;
        text.Width = 1000;
        text.Color = SKColors.Black;
        return text;
    }

    [Fact]
    public void GetStyledRuns_CustomTag_ProducesOneRunPerCharacter()
    {
        Text.Customizations["Identity"] = (index, block) => new LetterCustomization();
        Text text = MakeText();
        text.RawText = "[Custom=Identity]AB[/Custom]";

        List<Text.StyledTextRun> runs = text.GetStyledRuns();

        runs.Count(r => r.Text == "A" || r.Text == "B").ShouldBe(2);
    }

    [Fact]
    public void GetStyledRuns_CustomTag_AppliesCallbackColor()
    {
        Text.Customizations["Red"] = (index, block) => new LetterCustomization
        {
            Color = Color.Red,
        };
        Text text = MakeText();
        text.RawText = "[Custom=Red]X[/Custom]";

        List<Text.StyledTextRun> runs = text.GetStyledRuns();

        runs.Single(r => r.Text == "X").Style.TextColor.ShouldBe(new SKColor(255, 0, 0, 255));
    }

    [Fact]
    public void GetStyledRuns_CustomTag_AppliesCallbackYOffset()
    {
        Text.Customizations["Bump"] = (index, block) => new LetterCustomization
        {
            YOffset = 12,
        };
        Text text = MakeText();
        text.RawText = "[Custom=Bump]X[/Custom]";

        List<Text.StyledTextRun> runs = text.GetStyledRuns();

        runs.Single(r => r.Text == "X").YOffset.ShouldBe(12f);
    }

    [Fact]
    public void GetStyledRuns_CustomTag_AppliesReplacementCharacter()
    {
        Text.Customizations["Star"] = (index, block) => new LetterCustomization
        {
            ReplacementCharacter = '*',
        };
        Text text = MakeText();
        text.RawText = "[Custom=Star]X[/Custom]";

        List<Text.StyledTextRun> runs = text.GetStyledRuns();

        runs.ShouldContain(r => r.Text == "*");
    }

    [Fact]
    public void GetTextBlock_CustomTag_MovesGlyphPositionByYOffset()
    {
        Text.Customizations["BigBump"] = (index, block) => new LetterCustomization
        {
            YOffset = 50,
        };
        Text plain = MakeText();
        plain.RawText = "X";
        float baselineY = plain.GetTextBlock().FontRuns[0].GlyphPositions[0].Y;

        Text customized = MakeText();
        customized.RawText = "[Custom=BigBump]X[/Custom]";
        float bumpedY = customized.GetTextBlock().FontRuns[0].GlyphPositions[0].Y;

        (bumpedY - baselineY).ShouldBe(50f, tolerance: 0.01);
    }

    [Fact]
    public void GetStyledRuns_CustomTag_ContextFunctionReceivesPriorColor()
    {
        Text.ContextCustomizations["Darken"] = (index, block, context) => new LetterCustomization
        {
            Color = Color.FromArgb(context.Color!.Value.A, 10, 10, 10),
        };
        Text text = MakeText();
        text.RawText = "[Custom=Darken]X[/Custom]";

        List<Text.StyledTextRun> runs = text.GetStyledRuns();

        runs.Single(r => r.Text == "X").Style.TextColor.ShouldBe(new SKColor(10, 10, 10, 255));
    }

    private static (float Left, float Top, float Right, float Bottom) RenderInkBounds(string rawText, float fontSize = 40)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 200));
        surface.Canvas.Clear(SKColors.Transparent);
        SystemManagers managers = new() { Canvas = surface.Canvas };
        Text text = MakeText();
        text.FontSize = fontSize;
        text.X = 80;
        text.Y = 80;
        text.RawText = rawText;

        text.Render(managers);

        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        int left = int.MaxValue, top = int.MaxValue, right = int.MinValue, bottom = int.MinValue;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).Alpha > 128)
                {
                    left = System.Math.Min(left, x);
                    top = System.Math.Min(top, y);
                    right = System.Math.Max(right, x + 1);
                    bottom = System.Math.Max(bottom, y + 1);
                }
            }
        }
        return (left, top, right, bottom);
    }

    [Fact]
    public void Render_CustomTagWithNoTransform_DrawsSamePixelsAsPlainText()
    {
        Text.Customizations["NoTransform"] = (index, block) => new LetterCustomization();

        var plain = RenderInkBounds("I");
        var customized = RenderInkBounds("[Custom=NoTransform]I[/Custom]");

        customized.ShouldBe(plain);
    }

    [Fact]
    public void Render_CustomTagScale_ScalesLetterAboutItsCenter()
    {
        Text.Customizations["Double"] = (index, block) => new LetterCustomization
        {
            ScaleX = 2,
            ScaleY = 2,
        };

        var plain = RenderInkBounds("I");
        var scaled = RenderInkBounds("[Custom=Double]I[/Custom]");

        (scaled.Right - scaled.Left).ShouldBe(2 * (plain.Right - plain.Left), tolerance: 2);
        (scaled.Bottom - scaled.Top).ShouldBe(2 * (plain.Bottom - plain.Top), tolerance: 2);
        ((scaled.Left + scaled.Right) / 2f).ShouldBe((plain.Left + plain.Right) / 2f, tolerance: 1.5f);
    }

    [Fact]
    public void Render_CustomTagScaleWithLeftTopOrigin_KeepsCellTopLeftFixed()
    {
        Text.Customizations["DoubleFromTopLeft"] = (index, block) => new LetterCustomization
        {
            ScaleX = 2,
            ScaleY = 2,
            ScaleXOrigin = HorizontalAlignment.Left,
            ScaleYOrigin = VerticalAlignment.Top,
        };

        var plain = RenderInkBounds("I");
        var scaled = RenderInkBounds("[Custom=DoubleFromTopLeft]I[/Custom]");

        // The cell's top-left is the text's origin (80, 80), so every ink edge doubles its distance from it.
        (scaled.Left - 80).ShouldBe(2 * (plain.Left - 80), tolerance: 2);
        (scaled.Top - 80).ShouldBe(2 * (plain.Top - 80), tolerance: 2);
        (scaled.Right - 80).ShouldBe(2 * (plain.Right - 80), tolerance: 2);
        (scaled.Bottom - 80).ShouldBe(2 * (plain.Bottom - 80), tolerance: 2);
    }

    [Fact]
    public void Render_CustomTagRotation_RotatesLetterCounterclockwiseAboutCellTopLeft()
    {
        Text.Customizations["Quarter"] = (index, block) => new LetterCustomization
        {
            RotationDegrees = 90,
        };

        var plain = RenderInkBounds("I");
        var rotated = RenderInkBounds("[Custom=Quarter]I[/Custom]");

        // Like MonoGame, the letter turns about its cell's top-left (the text origin, 80, 80): a
        // counterclockwise quarter turn maps (dx, dy) to (dy, -dx), so the letter lies above the origin.
        rotated.Left.ShouldBe(80 + (plain.Top - 80), tolerance: 2);
        rotated.Right.ShouldBe(80 + (plain.Bottom - 80), tolerance: 2);
        rotated.Top.ShouldBe(80 - (plain.Right - 80), tolerance: 2);
        rotated.Bottom.ShouldBe(80 - (plain.Left - 80), tolerance: 2);
    }

    [Fact]
    public void GetTextBlock_CustomTagScaleAndRotation_DoesNotMoveFollowingLetters()
    {
        Text.Customizations["BigTurn"] = (index, block) => new LetterCustomization
        {
            ScaleX = 3,
            ScaleY = 3,
            RotationDegrees = 45,
        };
        Text plain = MakeText();
        plain.RawText = "AB";
        Text customized = MakeText();
        customized.RawText = "[Custom=BigTurn]A[/Custom]B";

        TextBlock plainBlock = plain.GetTextBlock();
        TextBlock customizedBlock = customized.GetTextBlock();

        customizedBlock.MeasuredWidth.ShouldBe(plainBlock.MeasuredWidth, tolerance: 0.01);
        customizedBlock.MeasuredHeight.ShouldBe(plainBlock.MeasuredHeight, tolerance: 0.01);
        customizedBlock.FontRuns.Last().GlyphPositions[0].X
            .ShouldBe(plainBlock.FontRuns[0].GlyphPositions[1].X, tolerance: 0.01);
    }
}
