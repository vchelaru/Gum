using Shouldly;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// #7106: a tag name is recognized (and stripped) ignoring case, so it must also be applied ignoring case.
/// Each row pairs the canonically cased markup with a variant that differs only in the case of the tag
/// names; the styled runs must be identical.
/// </summary>
public class TextBbCodeTagCaseTests
{
    private static string Describe(string markup)
    {
        Text text = new();
        text.FontName = "Arial";
        text.FontSize = 20;
        text.Width = 1000;
        text.Color = SKColors.Black;
        text.RawText = markup;

        IEnumerable<string> runs = text.GetStyledRuns().Select(r =>
            $"{r.Text}|{r.Style.TextColor}|{r.Style.FontSize}|{r.Style.FontWeight}|{r.Style.FontItalic}|{r.Style.HaloWidth}|{r.Style.FontFamily}");
        return string.Join("\n", runs);
    }

    [Theory]
    [InlineData("a[Color=Red]b[/Color]", "a[color=Red]b[/color]")]
    [InlineData("a[Color=Red]b[/Color]", "a[COLOR=Red]b[/COLOR]")]
    [InlineData("a[Color=Red]b[/Color]", "a[color=Red]b[/Color]")]
    [InlineData("a[Color=0xFF112233]b[/Color]", "a[color=0xFF112233]b[/color]")]
    [InlineData("a[Red=255]b[/Red]", "a[red=255]b[/red]")]
    [InlineData("a[Green=255]b[/Green]", "a[green=255]b[/green]")]
    [InlineData("a[Blue=255]b[/Blue]", "a[blue=255]b[/blue]")]
    [InlineData("a[FontScale=2]b[/FontScale]", "a[fontscale=2]b[/fontscale]")]
    [InlineData("a[FontScale=2]b[/FontScale]", "a[FONTSCALE=2]b[/FONTSCALE]")]
    [InlineData("a[Font=Courier New]b[/Font]", "a[font=Courier New]b[/font]")]
    [InlineData("a[FontSize=40]b[/FontSize]", "a[fontsize=40]b[/fontsize]")]
    [InlineData("a[IsBold=true]b[/IsBold]", "a[isbold=true]b[/isbold]")]
    [InlineData("a[IsItalic=true]b[/IsItalic]", "a[isitalic=true]b[/isitalic]")]
    [InlineData("a[OutlineThickness=2]b[/OutlineThickness]", "a[outlinethickness=2]b[/outlinethickness]")]
    [InlineData("a[Color=Red]b[IsBold=true]c[/IsBold]d[/Color]", "a[color=Red]b[isbold=true]c[/isbold]d[/color]")]
    public void Text_WithDifferentCaseTagName_AppliesTheSameAsCanonicalCase(string canonical, string variant)
    {
        string expected = Describe(canonical);
        expected.Split('\n').Length.ShouldBeGreaterThan(1, "Because the canonical markup must apply at least one run");

        Describe(variant).ShouldBe(expected);
    }
}
