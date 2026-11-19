using Gum.Forms.Controls;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// TextBox holding text that looks like BBCode. A TextBox edits characters, so its Text round-trips exactly and
/// its caret moves over every typed character. Linked into SkiaGum.Tests so both runtimes run the same cases.
/// </summary>
// Red until #8329.
public class TextBoxBbCodeTests : BaseTestClass
{
    private static IFormsText CoreText(TextBox textBox) =>
        (IFormsText)textBox.Visual.GetGraphicalUiElementByName("TextInstance")!.RenderableComponent;

    [Theory]
    [InlineData("a[Color=Red]b[/Color]c")]
    [InlineData("[FontScale=2]big[/FontScale]")]
    [InlineData("[Glyph=0x42]")]
    [InlineData("x[color=red]y[/COLOR]")]
    public void Text_SetToTaggedText_RoundTripsExactly(string markup)
    {
        TextBox textBox = new();

        textBox.Text = markup;

        textBox.Text.ShouldBe(markup);
    }

    [Theory]
    [InlineData("[b]")]
    [InlineData("[Color=Red]x[/Color]")]
    public void HandleCharEntered_TypingTagText_KeepsEveryCharacter(string typed)
    {
        TextBox textBox = new();
        textBox.IsFocused = true;

        foreach (char c in typed)
        {
            textBox.HandleCharEntered(c);
        }

        textBox.Text.ShouldBe(typed);
        textBox.CaretIndex.ShouldBe(typed.Length);
    }

    [Theory]
    [InlineData("a[Color=Red]b[/Color]c")]
    [InlineData("[FontScale=2]big[/FontScale]")]
    public void Text_SetToTaggedText_InnerTextMatchesOnEveryRuntime(string markup)
    {
        TextBox textBox = new();

        textBox.Text = markup;

        // Whatever the inner text displays, the caret indexes into it, so it must hold every character of Text.
        string.Concat(CoreText(textBox).WrappedText).ShouldBe(markup);
    }

    [Fact]
    public void Text_MultiLineTaggedText_InnerTextHoldsEveryLine()
    {
        TextBox textBox = new();
        textBox.TextWrapping = Gum.Forms.TextWrapping.Wrap;
        textBox.AcceptsReturn = true;

        textBox.Text = "a[Color=Red]b\nc[/Color]d";

        textBox.Text.ShouldBe("a[Color=Red]b\nc[/Color]d");
        IFormsText text = CoreText(textBox);
        text.WrappedText.Count.ShouldBe(2);
        string.Concat(text.WrappedText).ShouldBe("a[Color=Red]b\nc[/Color]d");
    }

    [Fact]
    public void HandleBackspace_SelectionOverTagText_RemovesExactlyTheSelectedCharacters()
    {
        TextBox textBox = new();
        textBox.IsFocused = true;
        textBox.Text = "a[Color=Red]b[/Color]c";

        textBox.SelectionStart = 1;
        textBox.SelectionLength = 11;
        textBox.HandleBackspace();

        textBox.Text.ShouldBe("ab[/Color]c");
        textBox.CaretIndex.ShouldBe(1);
    }

    [Fact]
    public void Text_TaggedTextOnVisualBeforeWrapping_IsKeptLiteral()
    {
        Gum.Forms.DefaultVisuals.V3.TextBoxVisual visual = new(tryCreateFormsObject: false);
        visual.TextInstance.Text = "a[Color=Red]b[/Color]c";

        TextBox textBox = new(visual);

        textBox.Text.ShouldBe("a[Color=Red]b[/Color]c");
        string.Concat(CoreText(textBox).WrappedText).ShouldBe("a[Color=Red]b[/Color]c");
    }

    [Fact]
    public void TextRuntime_TaggedTextOutsideATextBox_StillParsesTags()
    {
        TextBox textBox = new();
        textBox.Text = "a[Color=Red]b[/Color]c";
        TextRuntime label = new();

        label.Text = "a[Color=Red]b[/Color]c";

        string.Concat(((IFormsText)label.RenderableComponent).WrappedText).ShouldBe("abc");
    }

    private const string Markup = "a[Color=Red]b[/Color]c";

    private static TextRuntime CreateSizedToText(bool isBbCodeEnabled)
    {
        TextRuntime text = new();
        text.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        text.Width = 0;
        text.IsBbCodeEnabled = isBbCodeEnabled;
        text.Text = Markup;
        return text;
    }

    [Fact]
    public void IsBbCodeEnabled_TurnedOff_ShowsMarkupLiterallyAndRelaysOut()
    {
        TextRuntime literal = CreateSizedToText(isBbCodeEnabled: false);
        TextRuntime text = CreateSizedToText(isBbCodeEnabled: true);
        float parsedWidth = text.GetAbsoluteWidth();

        text.IsBbCodeEnabled = false;

        string.Concat(((IFormsText)text.RenderableComponent).WrappedText).ShouldBe(Markup);
        text.Text.ShouldBe(Markup);
        text.GetAbsoluteWidth().ShouldBe(literal.GetAbsoluteWidth());
        text.GetAbsoluteWidth().ShouldBeGreaterThan(parsedWidth);
    }

    [Fact]
    public void IsBbCodeEnabled_TurnedBackOn_ReparsesAndRelaysOut()
    {
        TextRuntime parsed = CreateSizedToText(isBbCodeEnabled: true);
        TextRuntime text = CreateSizedToText(isBbCodeEnabled: false);
        float literalWidth = text.GetAbsoluteWidth();

        text.IsBbCodeEnabled = true;

        string.Concat(((IFormsText)text.RenderableComponent).WrappedText).ShouldBe("abc");
        text.GetAbsoluteWidth().ShouldBe(parsed.GetAbsoluteWidth());
        text.GetAbsoluteWidth().ShouldBeLessThan(literalWidth);
    }
}
