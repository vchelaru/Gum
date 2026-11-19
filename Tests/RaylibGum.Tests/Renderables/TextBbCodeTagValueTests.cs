using Gum.GueDeriving;
using Gum.Renderables;
using Gum.Wireframe;
using KernSmith.Gum;
using RaylibGum.Renderables;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Text = Gum.Renderables.Text;

namespace RaylibGum.Tests.Renderables;

// What a tag's value does to the run it wraps: a value that parses styles the run, and a value that does
// not parse leaves the run as if the tag were absent (the base style, or the style of an enclosing tag of
// the same name). The MonoGame (TextRuntimeBbCodeTagValueTests) and SkiaGum classes run the same rows.
public class TextBbCodeTagValueTests : BaseTestClass
{
    private static Text MakeText(string markup)
    {
        IRaylibFontCreator? previousCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();
        try
        {
            TextRuntime textRuntime = new();
            textRuntime.Font = "Arial";
            textRuntime.FontSize = 18;
            textRuntime.Text = markup;
            return (Text)textRuntime.RenderableComponent!;
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = previousCreator;
        }
    }

    // The font of the last per-run font variable covering the character, or "base".
    private static string FontAt(Text text, int index)
    {
        InlineVariable? covering = text.InlineVariables.LastOrDefault(v =>
            v.Value is Raylib_cs.Font && index >= v.StartIndex && index < v.StartIndex + v.CharacterCount);
        if (covering == null)
        {
            return "base";
        }
        Raylib_cs.Font font = (Raylib_cs.Font)covering.Value;
        return $"{font.BaseSize}/{font.Texture.Id}/{font.GlyphCount}";
    }

    [Theory]
    [InlineData("[Color=NotAColor]a[/Color]")]
    [InlineData("[Color=]a[/Color]")]
    [InlineData("[Color=0xZZ]a[/Color]")]
    [InlineData("[Color=0x]a[/Color]")]
    // A # prefix is not a color form on any runtime (Skia's ParseColor reads only 0x and names).
    [InlineData("[Color=#FFFEFEFE]a[/Color]")]
    // Only 6 (RRGGBB) or 8 (AARRGGBB) hex digits are a color.
    [InlineData("[Color=0xFFF]a[/Color]")]
    [InlineData("[Color=0xFFFFF]a[/Color]")]
    [InlineData("[Color=0xFFFFFFF]a[/Color]")]
    [InlineData("[Color=0x0FF00FF00]a[/Color]")]
    public void Text_ColorTagWithUnparseableValue_KeepsBaseColor(string markup)
    {
        Text text = MakeText(markup);

        text.RawText.ShouldBe("a");
        text.InlineVariables.Where(v => v.VariableName == "Color")
            .Select(v => ((System.Drawing.Color)v.Value).ToArgb().ToString("X8"))
            .ShouldBeEmpty("Because a Color value that is not a color must leave the run in the base color");
    }

    [Theory]
    [InlineData("[Color=red]a[/Color]", 255, 255, 0, 0)]
    [InlineData("[Color=CornflowerBlue]a[/Color]", 255, 100, 149, 237)]
    [InlineData("[Color=0xFF00FF00]a[/Color]", 255, 0, 255, 0)]
    [InlineData("[Color=0x8000FF00]a[/Color]", 128, 0, 255, 0)]
    // 8 digits are 0xAARRGGBB, so a 00 alpha stays transparent.
    [InlineData("[Color=0x00FF0000]a[/Color]", 0, 255, 0, 0)]
    // 6 digits are 0xRRGGBB and draw opaque.
    [InlineData("[Color=0xFF0000]a[/Color]", 255, 255, 0, 0)]
    public void Text_ColorTagWithValidValue_AppliesColor(string markup, int a, int r, int g, int b)
    {
        Text text = MakeText(markup);

        InlineVariable color = text.InlineVariables.Single(v => v.VariableName == "Color");
        System.Drawing.Color value = (System.Drawing.Color)color.Value;
        (value.A, value.R, value.G, value.B).ShouldBe(((byte)a, (byte)r, (byte)g, (byte)b));
    }

    [Theory]
    [InlineData("[Red=300]a[/Red]")]
    [InlineData("[Green=-1]a[/Green]")]
    [InlineData("[Blue=1.5]a[/Blue]")]
    [InlineData("[FontScale=abc]a[/FontScale]")]
    [InlineData("[FontScale=]a[/FontScale]")]
    public void Text_ColorOrScaleTagWithUnparseableValue_AddsNoRun(string markup)
    {
        Text text = MakeText(markup);

        text.RawText.ShouldBe("a");
        text.InlineVariables.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("[FontScale=1.5]a[/FontScale]", 1.5f)]
    [InlineData("[FontScale=.5]a[/FontScale]", 0.5f)]
    public void Text_FontScaleTagWithDecimalValue_AppliesScale(string markup, float expected)
    {
        Text text = MakeText(markup);

        text.InlineVariables.Single(v => v.VariableName == "FontScale").Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData("[FontSize=40]a[FontSize=abc]b[/FontSize]c[/FontSize]")]
    [InlineData("[FontSize=40]a[FontSize=]b[/FontSize]c[/FontSize]")]
    [InlineData("[IsBold=true]a[IsBold=maybe]b[/IsBold]c[/IsBold]")]
    [InlineData("[IsItalic=true]a[IsItalic=]b[/IsItalic]c[/IsItalic]")]
    [InlineData("[OutlineThickness=2]a[OutlineThickness=x]b[/OutlineThickness]c[/OutlineThickness]")]
    [InlineData("[HasDropshadow=true]a[HasDropshadow=x]b[/HasDropshadow]c[/HasDropshadow]")]
    // An open tag with no value at all must not act as a close tag.
    [InlineData("[FontSize=40]a[FontSize]b[/FontSize]c[/FontSize]")]
    public void Text_FontTagWithUnparseableValue_InsideValidTag_KeepsEnclosingFont(string markup)
    {
        Text text = MakeText(markup);

        text.RawText.ShouldBe("abc");
        string outer = FontAt(text, 0);
        outer.ShouldNotBe("base", "Because the valid outer tag must change the font of 'a'");
        FontAt(text, 1).ShouldBe(outer, "Because an unparseable inner value must not undo the enclosing tag");
        FontAt(text, 2).ShouldBe(outer, "Because closing the unparseable inner tag must not close the enclosing tag too");
    }

    [Theory]
    [InlineData("[Alpha=128]a[/Alpha]")]
    [InlineData("[UseFontSmoothing=false]a[/UseFontSmoothing]")]
    [InlineData("[HasDropshadow=false]a[/HasDropshadow]")]
    [InlineData("[Link=shop]a[/Link]")]
    public void Text_KnownTagWithoutInlineStyle_IsRemovedFromDisplayedText(string markup)
    {
        Text text = MakeText(markup);

        text.RawText.ShouldBe("a", "Because every runtime recognizes the same tag names, so a known tag never draws as literal text");
    }

    [Fact]
    public void Text_LineHeightMultiplierTag_IsLiteralText()
    {
        Text text = MakeText("[LineHeightMultiplier=2]a[/LineHeightMultiplier]");

        text.RawText.ShouldBe("[LineHeightMultiplier=2]a[/LineHeightMultiplier]",
            "Because line height applies to whole lines, so no runtime knows it as a tag");
    }

    [Fact]
    public void Text_AlphaTagWithValidValue_AddsAlphaRun()
    {
        Text text = MakeText("[Alpha=128]a[/Alpha]");

        text.InlineVariables.Single(v => v.VariableName == "Alpha").Value.ShouldBe((byte)128);
    }

    [Fact]
    public void Text_UseFontSmoothingTag_ResolvesARunFont()
    {
        Text text = MakeText("[UseFontSmoothing=false]a[/UseFontSmoothing]b");

        text.RawText.ShouldBe("ab");
        FontAt(text, 0).ShouldNotBe("base", "Because an unsmoothed run needs its own font");
        FontAt(text, 0).ShouldNotBe(FontAt(text, 1));
    }

    [Fact]
    public void Text_CustomTagAfterEmoji_ReceivesUtf16IndexWithTagsRemoved()
    {
        Text.Customizations["TagValueIndex"] = (index, block) => new LetterCustomization();
        try
        {
            Text text = MakeText("\U0001F600[Color=Red]a[/Color][Custom=TagValueIndex]bc[/Custom]");

            List<ParameterizedLetterCustomizationCall> calls = text.InlineVariables
                .Select(v => v.Value).OfType<ParameterizedLetterCustomizationCall>().ToList();
            calls.Select(c => c.CharacterIndex).ShouldBe(new[] { 3, 4 });
            calls.ShouldAllBe(c => c.TextBlock == "bc");
        }
        finally
        {
            Text.Customizations.Remove("TagValueIndex");
        }
    }
}
