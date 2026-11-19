using System.Linq;
using Gum.Wireframe;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

// What a [Color] tag's value does to the run it wraps. The MonoGame (TextRuntimeBbCodeTagValueTests) and
// raylib classes run the same color rows.
public class TextBbCodeTagValueTests : BaseTestClass
{
    [Theory]
    [InlineData("[Color=NotAColor]a[/Color]")]
    // Only 6 (RRGGBB) or 8 (AARRGGBB) hex digits are a color.
    [InlineData("[Color=0xFFF]a[/Color]")]
    [InlineData("[Color=0xFFFFF]a[/Color]")]
    [InlineData("[Color=0xFFFFFFF]a[/Color]")]
    [InlineData("[Color=0x0FF00FF00]a[/Color]")]
    public void Text_ColorTagWithUnparseableValue_KeepsBaseColor(string markup)
    {
        Text plain = new Text();
        plain.RawText = "a";
        plain.FontSize = 24;
        plain.Width = 10000;
        SKColor baseColor = plain.GetStyledRuns().Single().Style.TextColor;

        Text text = new Text();
        text.RawText = markup;
        text.FontSize = 24;
        text.Width = 10000;

        text.GetStyledRuns().Single(run => run.Text == "a").Style.TextColor.ShouldBe(baseColor,
            "Because a Color value that is not a color must leave the run in the base color");
    }

    [Theory]
    [InlineData("[Color=red]a[/Color]", 255, 255, 0, 0)]
    [InlineData("[Color=0xFF00FF00]a[/Color]", 255, 0, 255, 0)]
    [InlineData("[Color=0x8000FF00]a[/Color]", 128, 0, 255, 0)]
    // 8 digits are 0xAARRGGBB, so a 00 alpha stays transparent.
    [InlineData("[Color=0x00FF0000]a[/Color]", 0, 255, 0, 0)]
    // 6 digits are 0xRRGGBB and draw opaque.
    [InlineData("[Color=0xFF0000]a[/Color]", 255, 255, 0, 0)]
    public void Text_ColorTagWithValidValue_AppliesColor(string markup, int a, int r, int g, int b)
    {
        Text text = new Text();
        text.RawText = markup;
        text.FontSize = 24;
        text.Width = 10000;

        SKColor color = text.GetStyledRuns().Single(run => run.Text == "a").Style.TextColor;
        (color.Alpha, color.Red, color.Green, color.Blue).ShouldBe(((byte)a, (byte)r, (byte)g, (byte)b));
    }
}
