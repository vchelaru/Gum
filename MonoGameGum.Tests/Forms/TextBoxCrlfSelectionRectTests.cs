using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Forms.Input;
using Gum.Wireframe;
using Shouldly;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Selection highlight rectangles (#6999): the same selection must draw the same rectangles whether the
/// text uses "\n" or "\r\n", and however the selection was reached.
/// </summary>
public class TextBoxCrlfSelectionRectTests : BaseTestClass
{
    [Theory]
    [InlineData("ecuf gh", "\n")]
    [InlineData("ecuf g", "\r\n")]
    public void ShiftDown_CrossingAnchorKeepingLength_ShouldDrawRectanglesOfTheSelection(string firstLine, string newline)
    {
        string text = firstLine + newline + "o iqb";
        TextBox box = CreateTextBox(text);
        box.CaretIndex = 4;
        for (int i = 0; i < 4; i++)
        {
            box.HandleKeyDown(Keys.Left, true, false, false);
        }
        box.SelectionLength.ShouldBe(4);

        box.HandleKeyDown(Keys.Down, true, false, false);

        box.SelectionStart.ShouldBe(4);
        TextBox expected = CreateTextBox(text);
        expected.SelectionStart = 4;
        expected.SelectionLength = box.SelectionLength;
        Describe(box).ShouldBe(Describe(expected));
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void Selection_EndingAtStartOfNextLine_ShouldNotDrawRectangleOnThatLine(string newline)
    {
        TextBox box = CreateTextBox("ecuf g" + newline + "o iqb");
        box.SelectionStart = 4;
        box.SelectionLength = newline.Length + 2;

        GetVisibleSelections(box).Length.ShouldBe(1);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void Selection_EndingAtStartOfBlankLine_ShouldNotDrawRectangleOnThatLine(string newline)
    {
        TextBox box = CreateTextBox("ab" + newline + newline + "cd");
        box.SelectionStart = 0;
        box.SelectionLength = 2 + newline.Length;

        GetVisibleSelections(box).Length.ShouldBe(1);
    }

    static string Describe(TextBox box) =>
        string.Join(",", GetVisibleSelections(box).Select(r => $"{r.AbsoluteTop:0.##}:{r.AbsoluteLeft:0.##}+{r.GetAbsoluteWidth():0.##}"));

    static TextBox CreateTextBox(string text)
    {
        TextBox textBox = new();
        textBox.TextWrapping = Gum.Forms.TextWrapping.Wrap;
        textBox.AcceptsReturn = true;
        textBox.Height = 200;
        textBox.Width = 300;
        textBox.Visual.X = 100;
        textBox.Visual.Y = 80;
        textBox.Text = text;
        textBox.IsFocused = true;
        return textBox;
    }

    static GraphicalUiElement[] GetVisibleSelections(TextBox textBox) =>
        ((TextBoxBaseVisual)textBox.Visual).ClipContainer.Children
            .OfType<GraphicalUiElement>()
            .Where(child => child.Name == "SelectionInstance" && child.Visible)
            .ToArray();
}
