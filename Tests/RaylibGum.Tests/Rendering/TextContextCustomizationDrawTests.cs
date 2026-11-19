using Gum.GueDeriving;
using Raylib_cs;
using Shouldly;
using static RaylibGum.Tests.Rendering.TextInkRender;
using LetterCustomization = Gum.Renderables.LetterCustomization;
using Text = Gum.Renderables.Text;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// The context a <see cref="Text.ContextCustomizations"/> callback receives during a real draw on raylib: the
/// letter's color as set by the enclosing BBCode tags, and the letter's index in the displayed text.
/// </summary>
public class TextContextCustomizationDrawTests : BaseTestClass
{
    private const string Name = "RecordContext";

    [Theory]
    [InlineData("[Color=Red][Custom=RecordContext]b[/Custom][/Color]", 255, 0, 0)]
    [InlineData("a[Color=Blue]x[Custom=RecordContext]b[/Custom][/Color]", 0, 0, 255)]
    [InlineData("[Red=200][Custom=RecordContext]b[/Custom][/Red]", 200, 255, 255)]
    [InlineData("[Color=Red][Green=128][Custom=RecordContext]b[/Custom][/Green][/Color]", 255, 128, 0)]
    public void ContextCustom_InsideColorTags_ReceivesTheEnclosingColor(string markup, int r, int g, int b)
    {
        List<(int Index, LetterCustomization Context)> calls = DrawAndRecord(markup, configure: null);

        calls.Count.ShouldBeGreaterThan(0);
        System.Drawing.Color? color = calls[0].Context.Color;
        color.ShouldNotBeNull();
        (color!.Value.R, color.Value.G, color.Value.B).ShouldBe(((byte)r, (byte)g, (byte)b));
    }

    [Fact]
    public void ContextCustom_WithNoColorTag_ReceivesTheTextColor()
    {
        List<(int Index, LetterCustomization Context)> calls = DrawAndRecord(
            "a[Custom=RecordContext]b[/Custom]",
            configure: runtime => runtime.Color = new Color(10, 20, 30, 255));

        calls.Count.ShouldBeGreaterThan(0);
        System.Drawing.Color? color = calls[0].Context.Color;
        color.ShouldNotBeNull();
        (color!.Value.R, color.Value.G, color.Value.B).ShouldBe(((byte)10, (byte)20, (byte)30));
    }

    [Theory]
    [InlineData("ab[Color=Red]c[Custom=RecordContext]d[/Custom][/Color]", 3)]
    [InlineData("a[Glyph=0x42][Custom=RecordContext]c[/Custom]", 2)]
    [InlineData("[Color=Red]ab[/Color]\n[Custom=RecordContext]c[/Custom]", 3)]
    public void ContextCustom_AfterTagsAndGlyphs_ReceivesTheDisplayedIndex(string markup, int expectedIndex)
    {
        List<(int Index, LetterCustomization Context)> calls = DrawAndRecord(markup, configure: null);

        calls.Count.ShouldBeGreaterThan(0);
        calls[0].Index.ShouldBe(expectedIndex);
    }

    private static List<(int Index, LetterCustomization Context)> DrawAndRecord(string markup, Action<TextRuntime>? configure)
    {
        List<(int, LetterCustomization)> calls = new();
        Text.ContextCustomizations[Name] = (index, block, context) =>
        {
            calls.Add((index, context));
            return new LetterCustomization();
        };
        try
        {
            Render(markup, left: 10, top: 10, configure);
        }
        finally
        {
            Text.ContextCustomizations.Remove(Name);
        }
        return calls;
    }
}
