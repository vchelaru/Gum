using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using Text = Gum.Renderables.Text;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// Verifies that the Raylib <see cref="Text"/> renderable stores BBCode / markup inline
/// styling (Color / FontScale runs) when text is assigned through the property pipeline,
/// mirroring the MonoGame runtime's behavior. Issue #3471.
/// </summary>
public class TextMarkupTests
{
    [Fact]
    public void Text_WithColorMarkup_PopulatesInlineVariablesAndStripsTags()
    {
        TextRuntime textRuntime = new();
        textRuntime.Text = "[Color=Green]Hello[/Color] World";

        Text internalText = (Text)textRuntime.RenderableComponent;

        internalText.RawText.ShouldBe("Hello World");
        internalText.StoredMarkupText.ShouldBe("[Color=Green]Hello[/Color] World");
        internalText.InlineVariables.Count.ShouldBe(1);
        internalText.InlineVariables[0].VariableName.ShouldBe("Color");
        internalText.InlineVariables[0].StartIndex.ShouldBe(0);
        internalText.InlineVariables[0].CharacterCount.ShouldBe(5);
    }

    [Fact]
    public void Text_WithFontScaleMarkup_PopulatesInlineVariable()
    {
        TextRuntime textRuntime = new();
        textRuntime.Text = "Big [FontScale=2]text[/FontScale]";

        Text internalText = (Text)textRuntime.RenderableComponent;

        internalText.RawText.ShouldBe("Big text");
        internalText.InlineVariables.Count.ShouldBe(1);
        internalText.InlineVariables[0].VariableName.ShouldBe("FontScale");
        internalText.InlineVariables[0].Value.ShouldBe(2f);
        internalText.InlineVariables[0].StartIndex.ShouldBe(4);
        internalText.InlineVariables[0].CharacterCount.ShouldBe(4);
    }

    [Fact]
    public void Text_ChangingFromMarkupToPlain_ClearsInlineVariablesAndStoredMarkup()
    {
        TextRuntime textRuntime = new();
        textRuntime.Text = "[Color=Green]Hello[/Color] World";

        Text internalText = (Text)textRuntime.RenderableComponent;
        internalText.InlineVariables.Count.ShouldBe(1);

        textRuntime.Text = "Plain text";

        internalText.RawText.ShouldBe("Plain text");
        internalText.StoredMarkupText.ShouldBeNull();
        internalText.InlineVariables.ShouldBeEmpty();
    }
}
