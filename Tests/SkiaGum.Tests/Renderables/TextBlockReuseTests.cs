using Gum;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum;
using SkiaSharp;
using Topten.RichTextKit;

namespace SkiaGum.Tests.Renderables;

/// <summary>A Text whose reveal or line-break block is cleared and reused draws what a fresh Text draws (#7818).</summary>
public class TextBlockReuseTests : BaseTestClass
{
    private const string FontSizeRunText = "Hello [FontSize=40]world[/FontSize], wrapped over lines";

    private static Text CreateText(string rawText, float fontSize = 24)
    {
        Text text = new Text();
        text.RawText = rawText;
        text.FontSize = fontSize;
        text.Width = 160;
        text.Height = 60;
        return text;
    }

    private static byte[] Render(Text text)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(240, 100));
        SystemManagers managers = new SystemManagers { Canvas = surface.Canvas };
        surface.Canvas.Clear(SKColors.Transparent);
        text.Render(managers);
        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.Bytes;
    }

    private static int PaintLineCount(Text text)
    {
        TextBlock full = text.GetCachedTextBlock();
        return text.GetPaintTextBlock(full).Lines.Count;
    }

    private static void ShouldMatchFresh(Text changed, Text fresh, string because)
    {
        byte[] actual = Render(changed);
        actual.ShouldBe(Render(fresh), because);
        PaintLineCount(changed).ShouldBe(PaintLineCount(fresh), because);
    }

    [Theory]
    [InlineData("Hel[Custom=Bump]lo w[/Custom]orld, wrapped over lines")]
    [InlineData("Hel[Custom=Spin]lo w[/Custom]orld, wrapped over lines")]
    public void Render_RevealAdvancingOverCustomTag_MatchesFresh(string rawText)
    {
        Text.Customizations["Bump"] = (index, block) => new LetterCustomization { YOffset = 6, XOffset = 2 };
        Text.Customizations["Spin"] = (index, block) => new LetterCustomization { RotationDegrees = 30, ScaleX = 1.5f };
        Text changed = CreateText(rawText);
        for (int letters = 1; letters < 20; letters++)
        {
            changed.MaxLettersToShow = letters;
            Text fresh = CreateText(rawText);
            fresh.MaxLettersToShow = letters;
            ShouldMatchFresh(changed, fresh, $"letters {letters}");
        }
    }

    [Theory]
    [InlineData("Hello world, wrapped over lines")]
    [InlineData(FontSizeRunText)]
    public void Render_RevealShrinking_MatchesFresh(string rawText)
    {
        Text changed = CreateText(rawText);
        for (int letters = 25; letters >= 0; letters--)
        {
            changed.MaxLettersToShow = letters;
            Text fresh = CreateText(rawText);
            fresh.MaxLettersToShow = letters;
            ShouldMatchFresh(changed, fresh, $"letters {letters}");
        }
    }

    [Theory]
    [InlineData("Hello world, wrapped over lines")]
    [InlineData(FontSizeRunText)]
    public void Render_RevealAcrossFontSizeAndNameChange_MatchesFresh(string rawText)
    {
        Text changed = CreateText(rawText);
        for (int letters = 1; letters < 10; letters++)
        {
            changed.MaxLettersToShow = letters;
            Render(changed);
        }

        changed.FontSize = 12;
        changed.MaxLettersToShow = 14;
        Text fresh = CreateText(rawText, fontSize: 12);
        fresh.MaxLettersToShow = 14;
        ShouldMatchFresh(changed, fresh, "font size 12");

        changed.FontName = "Courier New";
        changed.MaxLettersToShow = 15;
        fresh = CreateText(rawText, fontSize: 12);
        fresh.FontName = "Courier New";
        fresh.MaxLettersToShow = 15;
        ShouldMatchFresh(changed, fresh, "font name");
    }

    [Theory]
    [InlineData("Hello world, wrapped over many many lines here", "Hi")]
    [InlineData("Hello [FontSize=40]world[/FontSize], wrapped over lines", "A [FontSize=30]b[/FontSize]")]
    public void Render_TextGettingShorterAndLosingLines_MatchesFresh(string longText, string shortText)
    {
        Text changed = CreateText(longText);
        changed.MaxLettersToShow = 30;
        Render(changed);
        Text longFresh = CreateText(longText);
        longFresh.MaxLettersToShow = 30;
        PaintLineCount(longFresh).ShouldBeGreaterThan(1);

        changed.RawText = shortText;
        Text fresh = CreateText(shortText);
        fresh.MaxLettersToShow = 30;
        ShouldMatchFresh(changed, fresh, "short text revealed");
        PaintLineCount(changed).ShouldBe(1);

        changed.MaxLettersToShow = null;
        fresh.MaxLettersToShow = null;
        ShouldMatchFresh(changed, fresh, "short text unrevealed");
        changed.GetCachedTextBlock().Lines.Count.ShouldBe(fresh.GetCachedTextBlock().Lines.Count);
    }

    [Theory]
    [InlineData("Hello world, wrapped over lines and more")]
    [InlineData("Hello [FontSize=40]world[/FontSize], wrapped over lines and more")]
    public void Render_NonDefaultBlockSettingsThenDefaults_DoNotLeak(string rawText)
    {
        Text changed = CreateText(rawText);
        changed.HorizontalAlignment = HorizontalAlignment.Right;
        changed.MaxNumberOfLines = 1;
        changed.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;
        changed.Height = 20;
        changed.IsTruncatingWithEllipsisOnLastLine = false;
        changed.Width = 90;
        for (int letters = 3; letters < 12; letters++)
        {
            changed.MaxLettersToShow = letters;
            Render(changed);
        }

        Text defaults = CreateText(rawText);
        changed.HorizontalAlignment = defaults.HorizontalAlignment;
        changed.MaxNumberOfLines = defaults.MaxNumberOfLines;
        changed.TextOverflowVerticalMode = defaults.TextOverflowVerticalMode;
        changed.Height = defaults.Height;
        changed.IsTruncatingWithEllipsisOnLastLine = defaults.IsTruncatingWithEllipsisOnLastLine;
        changed.Width = defaults.Width;
        changed.RawText = rawText + " x";
        changed.MaxLettersToShow = 30;

        Text fresh = CreateText(rawText + " x");
        fresh.MaxLettersToShow = 30;
        ShouldMatchFresh(changed, fresh, "revealed with defaults");

        changed.MaxLettersToShow = null;
        fresh.MaxLettersToShow = null;
        ShouldMatchFresh(changed, fresh, "full with defaults");
    }
}
