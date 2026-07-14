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
}
