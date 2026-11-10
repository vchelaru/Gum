using RenderingLibrary;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// A Text whose BBCode changes every frame reuses its parse and style state between changes (#7818). These pin
/// that each change draws and breaks lines exactly like a fresh Text given the same markup.
/// </summary>
public class TextBbCodeChangedOutputTests
{
    private static readonly string[] PreviousTexts =
    {
        "[Color=Blue]Before[/Color] [FontSize=40]big[/FontSize] text that wraps over lines",
        "[IsBold=true][IsItalic=true]Bold italic[/IsItalic][/IsBold] [Color=Green]green[/Color]",
        "Plain text",
        "[FontSize=12]tiny[/FontSize] [Font=Courier New]mono[/Font] [Color=Red]red[/Color] and a long tail to wrap",
    };

    private static Text CreateText(string rawText)
    {
        Text text = new Text();
        text.RawText = rawText;
        text.FontSize = 24;
        text.Width = 200;
        text.Height = 100;
        return text;
    }

    private static byte[] Render(Text text)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(260, 160));
        SystemManagers managers = new SystemManagers { Canvas = surface.Canvas };
        surface.Canvas.Clear(SKColors.Transparent);
        text.Render(managers);
        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.Bytes;
    }

    private static (int Start, int Length)[] LineBreaks(Text text) =>
        text.GetCachedTextBlock().Lines.Select(line => (line.Start, line.Length)).ToArray();

    [Theory]
    [InlineData("[Color=Red]outer [IsBold=true]nested[/IsBold] tail[/Color] after")]
    [InlineData("HP [Color=Orange]12[/Color] of [Color=Red]999[/Color]")]
    [InlineData("small [FontSize=40]big[/FontSize] small again, wrapped over lines")]
    [InlineData("[Font=Courier New]mono[/Font] and default")]
    [InlineData("[IsBold=true]bold[/IsBold] [IsItalic=true]italic[/IsItalic] [IsBold=true][IsItalic=true]both[/IsItalic][/IsBold]")]
    [InlineData("[Color=Red]never closed [IsBold=true]bold[/IsBold] text")]
    [InlineData("a [[Color=Red]escaped[/Color]] bracket")]
    [InlineData("[Color=Red]a[/Color]")]
    [InlineData("[Color=Red]a much longer line of red text[/Color] [FontSize=30]that[/FontSize] keeps going and wraps")]
    public void Render_BbCodeChangedFromOtherBbCode_MatchesAFreshText(string rawText)
    {
        Text changed = CreateText(PreviousTexts[0]);
        foreach (string previous in PreviousTexts)
        {
            changed.RawText = previous;
            Render(changed);
        }
        changed.RawText = rawText;

        byte[] actual = Render(changed);

        Text fresh = CreateText(rawText);
        LineBreaks(changed).ShouldBe(LineBreaks(fresh));
        actual.ShouldBe(Render(fresh));
    }

    [Fact]
    public void GetStyledRuns_BbCodeChanged_RunsMatchAFreshText()
    {
        Text changed = CreateText("[Color=Blue]x[/Color] [FontSize=40]y[/FontSize]");
        changed.GetStyledRuns();
        changed.RawText = "[IsBold=true]HP[/IsBold] [Color=Red]1[/Color] [FontSize=24]of[/FontSize]";

        var actual = changed.GetStyledRuns()
            .Select(run => (run.Text, run.Style.FontFamily, run.Style.FontSize, run.Style.TextColor, run.Style.FontWeight, run.Style.FontItalic, run.Style.LineHeight))
            .ToArray();

        var expected = CreateText("[IsBold=true]HP[/IsBold] [Color=Red]1[/Color] [FontSize=24]of[/FontSize]").GetStyledRuns()
            .Select(run => (run.Text, run.Style.FontFamily, run.Style.FontSize, run.Style.TextColor, run.Style.FontWeight, run.Style.FontItalic, run.Style.LineHeight))
            .ToArray();
        actual.ShouldBe(expected);
    }
}
