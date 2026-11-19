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

    [Theory]
    [InlineData("[Color=red]a[/Color]", 255, 255, 0, 0)]
    [InlineData("[Color=CornflowerBlue]a[/Color]", 255, 100, 149, 237)]
    [InlineData("[Color=0xFF00FF00]a[/Color]", 255, 0, 255, 0)]
    [InlineData("[Color=0x8000FF00]a[/Color]", 128, 0, 255, 0)]
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
    [InlineData("[Alpha=128]a[/Alpha]")]
    [InlineData("[LineHeightMultiplier=2]a[/LineHeightMultiplier]")]
    [InlineData("[UseFontSmoothing=false]a[/UseFontSmoothing]")]
    [InlineData("[HasDropshadow=false]a[/HasDropshadow]")]
    [InlineData("[Link=shop]a[/Link]")]
    public void Text_KnownTagWithoutInlineStyle_IsRemovedFromDisplayedText(string markup)
    {
        Text text = MakeText(markup);

        text.RawText.ShouldBe("a", "Because every runtime recognizes the same tag names, so a known tag never draws as literal text");
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
