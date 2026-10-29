using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// #7106: a tag name is recognized (and stripped) ignoring case, so it must also be applied ignoring case.
// Each row pairs the canonically cased markup with a variant that differs only in the case of the tag
// names; the applied runs must be identical.
public class TextRuntimeBbCodeTagCaseTests : BaseTestClass
{
    private const string FontData =
@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight=20 base=20 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""x.png""
chars count=2
char id=32 x=0 y=0 width=10 height=13 xoffset=0 yoffset=4 xadvance=10 page=0 chnl=15
char id=97 x=0 y=0 width=10 height=13 xoffset=0 yoffset=4 xadvance=10 page=0 chnl=15
";

    private class StubFontCreator : IInMemoryFontCreator
    {
        public BitmapFont? TryCreateFont(BmfcSave bmfcSave)
        {
            BitmapFont font = new BitmapFont((Texture2D)null!, FontData);
            font.SetFontPattern(256, 256);
            return font;
        }
    }

    private static string Describe(string markup)
    {
        IInMemoryFontCreator? previousCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = new StubFontCreator();
        try
        {
            TextRuntime textRuntime = new();
            StateSave state = new() { Name = "Hi" };
            state.Variables.Add(new VariableSave { Name = "Color", Value = System.Drawing.Color.FromArgb(255, 10, 20, 30) });
            textRuntime.AddStates(new List<StateSave> { state });
            Text.Customizations["CaseTest"] = (index, block) => new LetterCustomization();

            textRuntime.Text = markup;

            Text text = (Text)textRuntime.RenderableComponent!;
            IEnumerable<string> runs = text.InlineVariables.Select(v =>
                $"{v.VariableName}|{v.StartIndex}|{v.CharacterCount}|{DescribeValue(v.Value)}");
            return text.RawText + "\n" + string.Join("\n", runs);
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = previousCreator;
        }
    }

    private static string DescribeValue(object? value) => value switch
    {
        BitmapFont => "BitmapFont",
        ParameterizedLetterCustomizationCall call => $"{call.FunctionName}@{call.CharacterIndex}",
        _ => value?.ToString() ?? "null",
    };

    [Theory]
    [InlineData("[Color=Red]a[/Color]", "[color=Red]a[/color]")]
    [InlineData("[Color=Red]a[/Color]", "[COLOR=Red]a[/COLOR]")]
    [InlineData("[Color=Red]a[/Color]", "[color=Red]a[/Color]")]
    [InlineData("[Color=0xFF112233]a[/Color]", "[color=0xFF112233]a[/color]")]
    [InlineData("[Red=255]a[/Red]", "[red=255]a[/red]")]
    [InlineData("[Green=255]a[/Green]", "[green=255]a[/green]")]
    [InlineData("[Blue=255]a[/Blue]", "[blue=255]a[/blue]")]
    [InlineData("[FontScale=2]a[/FontScale]", "[fontscale=2]a[/fontscale]")]
    [InlineData("[FontScale=2]a[/FontScale]", "[FONTSCALE=2]a[/FONTSCALE]")]
    [InlineData("[Font=Foo]a[/Font]", "[font=Foo]a[/font]")]
    [InlineData("[FontSize=40]a[/FontSize]", "[fontsize=40]a[/fontsize]")]
    [InlineData("[IsBold=true]a[/IsBold]", "[isbold=true]a[/isbold]")]
    [InlineData("[IsItalic=true]a[/IsItalic]", "[isitalic=true]a[/isitalic]")]
    [InlineData("[OutlineThickness=2]a[/OutlineThickness]", "[outlinethickness=2]a[/outlinethickness]")]
    [InlineData("[HasDropshadow=true]a[/HasDropshadow]", "[hasdropshadow=true]a[/hasdropshadow]")]
    [InlineData("[Custom=CaseTest]aa[/Custom]", "[custom=CaseTest]aa[/custom]")]
    [InlineData("[State=Hi]a[/State]", "[state=Hi]a[/state]")]
    [InlineData("a[Color=Red]a[IsBold=true]a[/IsBold]a[/Color]", "a[color=Red]a[isbold=true]a[/isbold]a[/color]")]
    public void Text_WithDifferentCaseTagName_AppliesTheSameAsCanonicalCase(string canonical, string variant)
    {
        string expected = Describe(canonical);
        expected.Split('\n').Length.ShouldBeGreaterThan(1, "Because the canonical markup must apply at least one run");

        Describe(variant).ShouldBe(expected);
    }

    [Fact]
    public void Text_WithLowercaseGlyphTag_ReplacesItLikeTheCanonicalCase()
    {
        Describe("[glyph=97]a").ShouldBe(Describe("[Glyph=97]a"));
    }

    [Theory]
    [InlineData("[color=Red]a[/color]", "Color")]
    [InlineData("[red=255]a[/red]", "Red")]
    [InlineData("[fontscale=2]a[/fontscale]", "FontScale")]
    [InlineData("[isbold=true]a[/isbold]", "BitmapFont")]
    [InlineData("[custom=CaseTest]a[/custom]", "Custom")]
    [InlineData("[state=Hi]a[/state]", "Color")]
    public void Text_WithLowercaseTagName_AddsTheRun(string markup, string expectedVariableName)
    {
        Describe(markup).ShouldContain($"\n{expectedVariableName}|");
    }
}
