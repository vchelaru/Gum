using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Renderables;
using RaylibGum.Renderables;
using RenderingLibrary.Graphics.Fonts;
using Gum.GueDeriving;
using Gum.Wireframe;
using KernSmith.Gum;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Text = Gum.Renderables.Text;

namespace RaylibGum.Tests.Renderables;

// #7106: a tag name is recognized (and stripped) ignoring case, so it must also be applied ignoring case.
// Each row pairs the canonically cased markup with a variant that differs only in the case of the tag
// names; the applied runs must be identical.
public class TextBbCodeTagCaseTests : BaseTestClass
{
    private static string Describe(string markup)
    {
        IRaylibFontCreator? previousCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();
        try
        {
            TextRuntime textRuntime = new();
            textRuntime.Font = "Arial";
            textRuntime.FontSize = 21;
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
        Raylib_cs.Font font => $"Font{font.BaseSize}",
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
    [InlineData("[Font=Arial]a[/Font]", "[font=Arial]a[/font]")]
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
