using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Forms.Input;
using Gum.Wireframe;
using MonoGameGum.Tests.Runtimes;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Linq;
using Xunit;
using Text = RenderingLibrary.Graphics.Text;

namespace MonoGameGum.Tests.Forms;

public class TextBoxCtrlArrowWhitespaceTests : BaseTestClass
{
    [Fact]
    public void CtrlRight_OverRepeatedSpaces_LandsOnTheNextWord()
    {
        TextBox textBox = new();
        textBox.Width = 300;
        textBox.Text = "foo   bar";
        textBox.IsFocused = true;
        textBox.CaretIndex = 0;
        textBox.HandleKeyDown(Keys.Right, false, false, true);
        textBox.CaretIndex.ShouldBe(6);
    }
}

public class TextBoxCaretIndexRangeTests : BaseTestClass
{
    static TextBox Create(int caret)
    {
        TextBox textBox = new();
        textBox.Width = 300;
        textBox.Text = "abc";
        textBox.IsFocused = true;
        textBox.CaretIndex = caret;
        return textBox;
    }

    [Fact]
    public void CaretIndex_PastTheEnd_IsClamped()
    {
        TextBox textBox = Create(0);
        textBox.CaretIndex = 50;
        textBox.CaretIndex.ShouldBe(3);
    }

    [Fact]
    public void CaretIndex_Negative_IsClampedAndDoesNotThrow()
    {
        TextBox textBox = Create(2);
        textBox.CaretIndex = -4;
        textBox.CaretIndex.ShouldBe(0);
    }
}

public class TextBoxMultiLineScrollBackTests : BaseTestClass
{
    [Fact]
    public void MultiLine_UpFromTheLastLine_ScrollsBackToTheTop()
    {
        string text = string.Join("\n", Enumerable.Range(1, 12).Select(i => "line" + i));
        TextBox textBox = new();
        textBox.TextWrapping = global::Gum.Forms.TextWrapping.Wrap;
        textBox.AcceptsReturn = true;
        textBox.Width = 200;
        textBox.Height = 60;
        textBox.Text = text;
        textBox.IsFocused = true;
        TextBoxBaseVisual visual = (TextBoxBaseVisual)textBox.Visual;
        float restingY = visual.TextInstance.Y;
        textBox.CaretIndex = text.Length;
        visual.TextInstance.Y.ShouldBeLessThan(restingY);

        for (int i = 0; i < 12; i++)
        {
            textBox.HandleKeyDown(Keys.Up, false, false, false);
        }

        textBox.CaretIndex.ShouldBe(0);
        visual.TextInstance.Y.ShouldBe(restingY, tolerance: 0.01f);
    }
}

public class TextBoxOversampledAlignedPositionTests : BaseTestClass
{
    [Theory]
    [InlineData(HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right)]
    public void Selection_WhileOversampledAndAligned_StartsAtTheDrawnPenPosition(HorizontalAlignment alignment)
    {
        (TextBox textBox, Text contained) = TextBoxMixedGlyphPositionTests.Create(true, alignment);
        float drawnLeft = contained.UpdateIpsoForRenderingForTests().X;
        textBox.SelectionStart = 0;
        textBox.SelectionLength = 3;
        GraphicalUiElement selection = ((TextBoxBaseVisual)textBox.Visual).ClipContainer.Children
            .OfType<GraphicalUiElement>()
            .Single(child => child.Name == "SelectionInstance" && child.Visible);

        (selection.GetAbsoluteLeft() - drawnLeft).ShouldBe(0f, tolerance: 0.01f);
    }

    [Theory]
    [InlineData(HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right)]
    public void Click_WhileOversampledAndAligned_ReturnsTheCharacterDrawnAtThePoint(HorizontalAlignment alignment)
    {
        (TextBox textBox, Text contained) = TextBoxMixedGlyphPositionTests.Create(true, alignment);
        float drawnLeft = contained.UpdateIpsoForRenderingForTests().X;
        float y = contained.GetAbsoluteTop() + 1;
        string content = TextBoxMixedGlyphPositionTests.Content;

        for (int character = 0; character < content.Length; character++)
        {
            float start = drawnLeft + TextBoxMixedGlyphPositionTests.Pen(content, character, true);
            float advance = TextBoxMixedGlyphPositionTests.Pen(content, character + 1, true) - TextBoxMixedGlyphPositionTests.Pen(content, character, true);
            textBox.GetCaretIndexAtPosition(start + advance * 0.25f, y).ShouldBe(character, "left quarter of character " + character);
            textBox.GetCaretIndexAtPosition(start + advance * 0.75f, y).ShouldBe(character + 1, "right quarter of character " + character);
        }
    }
}
