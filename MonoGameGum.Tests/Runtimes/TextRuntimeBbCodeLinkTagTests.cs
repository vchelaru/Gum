using Gum.GueDeriving;
using Gum.Wireframe;
using MonoGameGum.TestsCommon;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// [Link=id]...[/Link] BBCode tag and TextRuntime.GetTagAtIndex (#7062): the tag is stripped from the
// drawn text like the other non-visual tags and kept as a range that the game can query by tag name,
// in the same index space as GetCharacterIndexAtPosition.
public class TextRuntimeBbCodeLinkTagTests : BaseTestClass
{
    private static TextRuntime Make(string? text, float? width = null)
    {
        TextRuntime textRuntime = new();
        if (width != null)
        {
            textRuntime.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            textRuntime.Width = width.Value;
        }
        textRuntime.Text = text;
        return textRuntime;
    }

    [Fact]
    public void Text_WithLinkTag_StripsTagFromDrawnText()
    {
        TextRuntime textRuntime = Make("Click [Link=shop]here[/Link] now");

        Text text = (Text)textRuntime.RenderableComponent!;
        text.RawText.ShouldBe("Click here now");
        text.InlineVariables.ShouldBeEmpty();
    }

    [Fact]
    public void GetTagAtIndex_InsideLink_ReturnsNameArgumentAndRange()
    {
        TextRuntime textRuntime = Make("Click [Link=shop]here[/Link] now");

        TextTagSpan? span = textRuntime.GetTagAtIndex("Link", 7);

        span.ShouldNotBeNull();
        span.Value.Name.ShouldBe("Link");
        span.Value.Argument.ShouldBe("shop");
        span.Value.StartIndex.ShouldBe(6);
        span.Value.EndIndex.ShouldBe(10);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(5, false)]
    [InlineData(6, true)]
    [InlineData(9, true)]
    [InlineData(10, false)]
    [InlineData(13, false)]
    [InlineData(14, false)]
    [InlineData(1000, false)]
    public void GetTagAtIndex_AtBoundaries_IncludesStartAndExcludesEnd(int index, bool expectedInside)
    {
        // Stripped text "Click here now": the link covers "here" at [6, 10).
        TextRuntime textRuntime = Make("Click [Link=shop]here[/Link] now");

        (textRuntime.GetTagAtIndex("Link", index) != null).ShouldBe(expectedInside);
    }

    [Fact]
    public void GetTagAtIndex_SeveralLinksNestedInOtherTags_ReturnsEachByItsOwnRange()
    {
        TextRuntime textRuntime = Make(
            "[Color=Red][Link=a]one[/Link][/Color] and [Link=b][FontScale=1.5]two[/FontScale][/Link]");

        Text text = (Text)textRuntime.RenderableComponent!;
        text.RawText.ShouldBe("one and two");

        TextTagSpan first = textRuntime.GetTagAtIndex("Link", 1)!.Value;
        first.Argument.ShouldBe("a");
        first.StartIndex.ShouldBe(0);
        first.EndIndex.ShouldBe(3);

        textRuntime.GetTagAtIndex("Link", 4).ShouldBeNull();

        TextTagSpan second = textRuntime.GetTagAtIndex("Link", 9)!.Value;
        second.Argument.ShouldBe("b");
        second.StartIndex.ShouldBe(8);
        second.EndIndex.ShouldBe(11);

        // The other tags still style their runs.
        text.InlineVariables.Single(v => v.VariableName == "Color").StartIndex.ShouldBe(0);
        text.InlineVariables.Single(v => v.VariableName == "FontScale").StartIndex.ShouldBe(8);
    }

    [Theory]
    [InlineData("link")]
    [InlineData("LINK")]
    public void GetTagAtIndex_TagNameCase_IsIgnored(string queryName)
    {
        TextRuntime textRuntime = Make("a [Link=x]b[/Link]");

        textRuntime.GetTagAtIndex(queryName, 2).ShouldNotBeNull();
    }

    [Fact]
    public void GetTagAtIndex_WithDifferentTagName_ReturnsThatTagOrNull()
    {
        TextRuntime textRuntime = Make("a [Link=x][Color=Red]b[/Color][/Link]");

        textRuntime.GetTagAtIndex("Custom", 2).ShouldBeNull();

        // The query is generic by name, not specific to Link.
        TextTagSpan color = textRuntime.GetTagAtIndex("Color", 2)!.Value;
        color.Argument.ShouldBe("Red");
        color.StartIndex.ShouldBe(2);
        color.EndIndex.ShouldBe(3);
    }

    [Fact]
    public void GetTagAtIndex_NestedLinks_ReturnsInnermost()
    {
        TextRuntime textRuntime = Make("[Link=outer]ab[Link=inner]cd[/Link]ef[/Link]");

        textRuntime.GetTagAtIndex("Link", 0)!.Value.Argument.ShouldBe("outer");
        textRuntime.GetTagAtIndex("Link", 2)!.Value.Argument.ShouldBe("inner");
        textRuntime.GetTagAtIndex("Link", 4)!.Value.Argument.ShouldBe("outer");
    }

    [Theory]
    [InlineData("[OK] press [Sawmill] to continue")]
    [InlineData("[Linked=x]a[/Linked]")]
    [InlineData("[Links]a[/Links]")]
    public void Text_WithUnknownTagNames_KeepsThemAsLiteralText(string input)
    {
        TextRuntime textRuntime = Make(input);

        Text text = (Text)textRuntime.RenderableComponent!;
        text.RawText.ShouldBe(input);
        textRuntime.GetTagAtIndex("OK", 1).ShouldBeNull();
        textRuntime.GetTagAtIndex("Linked", 1).ShouldBeNull();
        textRuntime.GetTagAtIndex("Link", 1).ShouldBeNull();
    }

    [Fact]
    public void GetTagAtIndex_WrappedText_ReturnsRangeInWrappedTextIndexSpace()
    {
        TextRuntime textRuntime = Make(
            "The quick brown fox jumps over the [Link=dog]lazy sleeping dog[/Link] today",
            width: 90);

        string[] lines = textRuntime.WrappedText.ToArray();
        lines.Length.ShouldBeGreaterThan(2, "because the width should force the text onto several lines");
        string wrapped = string.Concat(lines);

        TextTagSpan span = textRuntime.GetTagAtIndex("Link", wrapped.IndexOf("sleeping"))!.Value;

        span.Argument.ShouldBe("dog");
        wrapped.Substring(span.StartIndex, span.EndIndex - span.StartIndex).ShouldBe("lazy sleeping dog");
    }

    [Fact]
    public void GetTagAtIndex_Crlf_CountsLineBreakAsOneCharacter()
    {
        TextRuntime textRuntime = Make("line1\r\nline2 [Link=x]go[/Link]");

        string wrapped = string.Concat(textRuntime.WrappedText);
        wrapped.ShouldBe("line1\nline2 go");

        TextTagSpan span = textRuntime.GetTagAtIndex("Link", wrapped.IndexOf("go"))!.Value;
        span.StartIndex.ShouldBe(wrapped.IndexOf("go"));
        span.EndIndex.ShouldBe(wrapped.Length);
    }

    [Fact]
    public void GetTagAtIndex_AfterGlyphTag_CountsGlyphAsOneCharacter()
    {
        TextRuntime textRuntime = Make("[Glyph=0xE000][Link=x]a[/Link]");

        TextTagSpan span = textRuntime.GetTagAtIndex("Link", 1)!.Value;
        span.StartIndex.ShouldBe(1);
        span.EndIndex.ShouldBe(2);
    }

    [Fact]
    public void GetTagAtIndex_AfterTextChanges_ReflectsTheNewText()
    {
        TextRuntime textRuntime = Make("[Link=first]aaa[/Link]");
        textRuntime.GetTagAtIndex("Link", 1)!.Value.Argument.ShouldBe("first");

        textRuntime.Text = "bb [Link=second]ccc[/Link]";

        textRuntime.GetTagAtIndex("Link", 1).ShouldBeNull();
        TextTagSpan span = textRuntime.GetTagAtIndex("Link", 4)!.Value;
        span.Argument.ShouldBe("second");
        span.StartIndex.ShouldBe(3);

        textRuntime.Text = "plain text";
        textRuntime.GetTagAtIndex("Link", 4).ShouldBeNull();

        textRuntime.Text = "[Link=third]d[/Link]";
        textRuntime.GetTagAtIndex("Link", 0)!.Value.Argument.ShouldBe("third");

        textRuntime.SetTextNoTranslate("plain again");
        textRuntime.GetTagAtIndex("Link", 0).ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("no tags here")]
    public void GetTagAtIndex_WithoutTags_ReturnsNull(string? input)
    {
        TextRuntime textRuntime = Make(input);

        textRuntime.GetTagAtIndex("Link", 0).ShouldBeNull();
    }

    [Theory]
    [InlineData("[Link=x]never closed", "[Link=x]never closed")]
    [InlineData("orphan[/Link] close", "orphan[/Link] close")]
    public void Text_WithUnmatchedLinkTag_KeepsTagAsLiteralTextAndFindsNoLink(string input, string expectedDrawn)
    {
        TextRuntime textRuntime = Make(input);

        Text text = (Text)textRuntime.RenderableComponent!;
        text.RawText.ShouldBe(expectedDrawn);
        for (int i = 0; i < expectedDrawn.Length; i++)
        {
            textRuntime.GetTagAtIndex("Link", i).ShouldBeNull();
        }
    }

    [Fact]
    public void GetTagAtIndex_EmptyLink_NeverMatches()
    {
        TextRuntime textRuntime = Make("[Link=x][/Link]ab");

        Text text = (Text)textRuntime.RenderableComponent!;
        text.RawText.ShouldBe("ab");
        textRuntime.GetTagAtIndex("Link", 0).ShouldBeNull();
    }

    [Theory]
    [InlineData("my id 1")]
    [InlineData("shop/item-12?x&y")]
    [InlineData("Ünïcode: ok!")]
    [InlineData("a,b;c.d")]
    public void GetTagAtIndex_ArgumentWithSpacesAndPunctuation_IsReturnedWhole(string id)
    {
        TextRuntime textRuntime = Make($"go [Link={id}]there[/Link]");

        textRuntime.GetTagAtIndex("Link", 4)!.Value.Argument.ShouldBe(id);
    }

    [Fact]
    public void GetTagAtIndex_LinkWithoutArgument_ReturnsNullArgument()
    {
        TextRuntime textRuntime = Make("go [Link]there[/Link]");

        TextTagSpan span = textRuntime.GetTagAtIndex("Link", 4)!.Value;
        span.Argument.ShouldBeNull();
        span.StartIndex.ShouldBe(3);
    }

    [Fact]
    public void GetTagAtIndex_CombinesWithGetCharacterIndexAtPosition()
    {
        TextRuntime textRuntime = Make("ab [Link=x]cd[/Link]");
        Text text = (Text)textRuntime.RenderableComponent!;
        float left = text.GetAbsoluteLeft();
        float top = text.GetAbsoluteTop();
        float lineHeight = text.LineHeightInPixels * text.LineHeightMultiplier;

        // Left half of the 'c' (index 3 of "ab cd").
        float x = left + text.MeasureString("ab c") - text.MeasureString("c") * 0.75f;
        int index = textRuntime.GetCharacterIndexAtPosition(x, top + lineHeight / 2);

        index.ShouldBe(3);
        textRuntime.GetTagAtIndex("Link", index)!.Value.Argument.ShouldBe("x");
    }

    [Fact]
    public void GetTagAtIndex_AfterRawTextAssignedDirectly_NoLongerFindsTheOldRange()
    {
        TextRuntime textRuntime = Make("[Link=x]abc[/Link]");
        textRuntime.GetTagAtIndex("Link", 1).ShouldNotBeNull();

        // Code such as ListBoxItem and ComboBox writes the renderable's RawText directly.
        Text text = (Text)textRuntime.RenderableComponent!;
        text.RawText = "xyz";

        for (int i = 0; i < 3; i++)
        {
            textRuntime.GetTagAtIndex("Link", i).ShouldBeNull();
        }
    }

    // #7085: an opening tag with no closing tag stays as literal text, the same as a lone opening
    // tag does today and as an orphan closing tag does. Tags that do close still apply.
    [Theory]
    [InlineData("[Color=Red]a[Color=Blue]b[/Color]", "[Color=Red]ab")]
    [InlineData("[Link=x]a[Link=y]b[/Link]", "[Link=x]ab")]
    [InlineData("[FontScale=2]a[FontScale=3]b[/FontScale]", "[FontScale=2]ab")]
    [InlineData("[Color=Red]a[Color=Blue]b[/Color][/Color]", "ab")]
    [InlineData("x[/Color]", "x[/Color]")]
    [InlineData("[Color=Red]a", "[Color=Red]a")]
    public void Text_WithUnclosedOpenTag_KeepsItAsLiteralText(string markup, string expected)
    {
        TextRuntime textRuntime = Make(markup);

        ((Text)textRuntime.RenderableComponent!).RawText.ShouldBe(expected);
    }

    [Fact]
    public void GetTagAtIndex_WithUnclosedOuterLink_FindsOnlyTheClosedInnerLink()
    {
        TextRuntime textRuntime = Make("[Link=x]a[Link=y]b[/Link]");

        // Displayed as "[Link=x]ab": the unclosed tag is literal, so "b" is the last character.
        textRuntime.GetTagAtIndex("Link", 8).ShouldBeNull();
        TextTagSpan span = textRuntime.GetTagAtIndex("Link", 9)!.Value;
        span.Argument.ShouldBe("y");
        span.StartIndex.ShouldBe(9);
        span.EndIndex.ShouldBe(10);
    }

    // #7094: a closing tag pairs with its opener ignoring case, the way tag names are recognized.
    [Theory]
    [InlineData("[Color=Red]a[/color]b", "ab")]
    [InlineData("[Color=Red]a[/COLOR]b", "ab")]
    [InlineData("[link=x]a[/LINK]", "a")]
    [InlineData("[Color=Red]a[Link=x]b[/LINK]c[/color]", "abc")]
    public void Text_WithDifferentCaseCloseTag_PairsAndStripsBothTags(string markup, string expected)
    {
        TextRuntime textRuntime = Make(markup);

        ((Text)textRuntime.RenderableComponent!).RawText.ShouldBe(expected);
    }

    [Fact]
    public void GetTagAtIndex_WithDifferentCaseCloseTag_FindsTheLink()
    {
        TextRuntime textRuntime = Make("a [Link=x]bc[/LINK] d");

        TextTagSpan span = textRuntime.GetTagAtIndex("Link", 2)!.Value;
        span.Argument.ShouldBe("x");
        span.StartIndex.ShouldBe(2);
        span.EndIndex.ShouldBe(4);
    }
}
