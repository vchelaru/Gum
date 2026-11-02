using System.Collections.Generic;
using Gum.Forms.Controls;
using Shouldly;
using Xunit;
using GumKeys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A TextBox typed into the way a first user does: click, type, backspace, select all and
/// overwrite, hit MaxLength, type into a disabled box, set Text to null.
/// </summary>
public class TextBoxFirstHourTests : BeginnerDriverTestBase
{
    private TextBox CreateFocused()
    {
        TextBox textBox = AddToRoot(new TextBox { Width = 200 }, "Tb");
        Driver.Click("Tb");
        return textBox;
    }

    [Fact]
    public void Text_ShouldBeTyped_WhenTextBoxIsClickedAndTyped()
    {
        TextBox textBox = CreateFocused();

        Driver.Type("hello");

        textBox.Text.ShouldBe("hello");
        textBox.CaretIndex.ShouldBe(5);
    }

    [Fact]
    public void TextChanged_ShouldRaiseOncePerCharacter_WhenTypingOneCharacterPerFrame()
    {
        TextBox textBox = CreateFocused();
        int changes = 0;
        textBox.TextChanged += (_, _) => changes++;

        Driver.Type("a");
        Driver.Type("b");
        Driver.Type("c");

        changes.ShouldBe(3);
    }

    [Fact]
    public void Text_ShouldDropLastCharacter_WhenBackspaceIsPressed()
    {
        TextBox textBox = CreateFocused();
        Driver.Type("abc");

        Driver.PressKey(GumKeys.Back);

        textBox.Text.ShouldBe("ab");
    }

    [Fact]
    public void Backspace_ShouldNotThrow_WhenTextBoxIsEmpty()
    {
        TextBox textBox = CreateFocused();

        Should.NotThrow(() => Driver.PressKey(GumKeys.Back));
        Should.NotThrow(() => Driver.PressKey(GumKeys.Delete));

        textBox.Text.ShouldBeNullOrEmpty();
    }

    [Fact]
    public void Text_ShouldBeReplaced_WhenAllIsSelectedAndUserTypes()
    {
        TextBox textBox = CreateFocused();
        Driver.Type("old text");
        textBox.SelectAll();

        Driver.Type("new");

        textBox.Text.ShouldBe("new");
    }

    [Fact]
    public void Text_ShouldStopGrowing_WhenMaxLengthIsReached()
    {
        TextBox textBox = CreateFocused();
        textBox.MaxLength = 5;

        Driver.Type("abcdefghij");

        textBox.Text.ShouldBe("abcde");
    }

    [Fact]
    public void Text_ShouldStayEmpty_WhenTextBoxIsDisabledAndTyped()
    {
        TextBox textBox = AddToRoot(new TextBox { Width = 200 }, "Tb");
        textBox.IsEnabled = false;

        Driver.Click("Tb");
        Driver.Type("abc");

        textBox.Text.ShouldBeNullOrEmpty();
    }

    [Fact]
    public void Text_ShouldNotThrow_WhenSetToNullAfterTyping()
    {
        TextBox textBox = CreateFocused();
        Driver.Type("abc");

        Should.NotThrow(() => textBox.Text = null!);

        textBox.Text.ShouldBeNullOrEmpty();
        textBox.CaretIndex.ShouldBe(0);
    }

    [Fact]
    public void CaretIndex_ShouldBeClamped_WhenTextIsShortenedFromCode()
    {
        TextBox textBox = CreateFocused();
        Driver.Type("a long sentence");

        textBox.Text = "hi";

        textBox.CaretIndex.ShouldBeLessThanOrEqualTo(2);
        Should.NotThrow(() => Driver.Type("!"));
    }

    [Fact]
    public void TextChanged_ShouldRaiseOnce_WhenTextIsAssignedFromCode()
    {
        TextBox textBox = AddToRoot(new TextBox(), "Tb");
        List<string?> seen = new();
        textBox.TextChanged += (_, _) => seen.Add(textBox.Text);

        textBox.Text = "Hero";
        textBox.Text = "Hero";

        seen.ShouldBe(new List<string?> { "Hero" });
    }

    [Fact]
    public void Focus_ShouldMoveToSecondTextBox_WhenSecondIsClicked()
    {
        TextBox first = AddToRoot(new TextBox { Width = 200 }, "First");
        TextBox second = AddToRoot(new TextBox { Width = 200, Y = 40 }, "Second");
        Driver.Click("First");

        Driver.Click("Second");
        Driver.Type("x");

        first.Text.ShouldBeNullOrEmpty();
        second.Text.ShouldBe("x");
    }
}
