using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Shouldly;
using Xunit;
using GumKeys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Clicking and dragging in a TextBox places the caret and the selection where the cursor is, driven by a
/// scripted cursor through GumService.Update.
/// </summary>
public class TextBoxMouseSelectionTests : BeginnerDriverTestBase
{
    const string Content = "hello world again";

    TextBox CreateBox(string text = Content)
    {
        TextBox textBox = new();
        textBox.Width = 400;
        textBox.Visual.X = 20;
        textBox.Visual.Y = 30;
        AddToRoot(textBox, "Box");
        textBox.Text = text;
        Driver.Step(2);
        return textBox;
    }

    // The screen x of the gap before the character at index, read off the caret.
    static int XOf(TextBox textBox, int index)
    {
        int saved = textBox.CaretIndex;
        textBox.CaretIndex = index;
        int x = (int)((TextBoxBaseVisual)textBox.Visual).CaretInstance.AbsoluteLeft;
        textBox.CaretIndex = saved;
        return x;
    }

    static int MidY(TextBox textBox) => (int)(textBox.Visual.AbsoluteTop + textBox.Visual.AbsoluteHeight / 2);

    // Presses at one gap and releases at another, with moves in between so a drag registers.
    void Drag(TextBox textBox, int fromX, int toX)
    {
        int y = MidY(textBox);
        Driver.MoveTo(fromX, y);
        Driver.SetButton(Gum.Automation.MouseButton.Primary, held: true);
        Driver.MoveTo((fromX + toX) / 2, y);
        Driver.MoveTo(toX, y);
        Driver.MoveTo(toX, y);
        Driver.SetButton(Gum.Automation.MouseButton.Primary, held: false);
    }

    [Fact]
    public void Click_InTheMiddleOfText_PlacesTheCaretAtTheNearestGap()
    {
        TextBox textBox = CreateBox();
        int target = 6;

        Driver.Click(XOf(textBox, target) + 1, MidY(textBox));

        textBox.IsFocused.ShouldBeTrue();
        textBox.CaretIndex.ShouldBe(target);
        textBox.SelectionLength.ShouldBe(0);
    }

    [Fact]
    public void Click_PastTheEndOfText_PlacesTheCaretAtTheEnd()
    {
        TextBox textBox = CreateBox();

        Driver.Click((int)(textBox.Visual.AbsoluteLeft + textBox.Visual.AbsoluteWidth - 5), MidY(textBox));

        textBox.CaretIndex.ShouldBe(Content.Length);
    }

    [Fact]
    public void Click_AtTheLeftEdge_PlacesTheCaretAtTheStart()
    {
        TextBox textBox = CreateBox();
        textBox.CaretIndex = 8;

        Driver.Click((int)textBox.Visual.AbsoluteLeft + 1, MidY(textBox));

        textBox.CaretIndex.ShouldBe(0);
    }

    [Fact]
    public void Click_OnEmptyText_KeepsTheCaretAtZero()
    {
        TextBox textBox = CreateBox("");

        Driver.Click((int)textBox.Visual.AbsoluteLeft + 50, MidY(textBox));

        textBox.IsFocused.ShouldBeTrue();
        textBox.CaretIndex.ShouldBe(0);
    }

    [Fact]
    public void Click_WithASelection_ClearsTheSelection()
    {
        TextBox textBox = CreateBox();
        textBox.IsFocused = true;
        textBox.SelectAll();

        Driver.Click(XOf(textBox, 3) + 1, MidY(textBox));

        textBox.SelectionLength.ShouldBe(0);
        textBox.CaretIndex.ShouldBe(3);
    }

    [Fact]
    public void Drag_LeftToRight_SelectsBetweenThePressAndReleaseGaps()
    {
        TextBox textBox = CreateBox();

        Drag(textBox, XOf(textBox, 2) + 1, XOf(textBox, 7) + 1);

        textBox.SelectionStart.ShouldBe(2);
        textBox.SelectionLength.ShouldBe(5);
    }

    [Fact]
    public void Drag_RightToLeft_SelectsBetweenTheReleaseAndPressGaps()
    {
        TextBox textBox = CreateBox();

        Drag(textBox, XOf(textBox, 9) + 1, XOf(textBox, 4) + 1);

        textBox.SelectionStart.ShouldBe(4);
        textBox.SelectionLength.ShouldBe(5);
    }

    [Fact]
    public void Drag_PastTheRightEdgeOfTheText_SelectsToTheEnd()
    {
        TextBox textBox = CreateBox();

        Drag(textBox, XOf(textBox, 6) + 1, (int)(textBox.Visual.AbsoluteLeft + textBox.Visual.AbsoluteWidth - 3));

        textBox.SelectionStart.ShouldBe(6);
        textBox.SelectionLength.ShouldBe(Content.Length - 6);
    }

    [Fact]
    public void Drag_BackToThePressGap_SelectsNothing()
    {
        TextBox textBox = CreateBox();
        int y = MidY(textBox);
        int pressX = XOf(textBox, 5) + 1;
        Driver.MoveTo(pressX, y);
        Driver.SetButton(Gum.Automation.MouseButton.Primary, held: true);
        Driver.MoveTo(XOf(textBox, 9) + 1, y);
        Driver.MoveTo(XOf(textBox, 9) + 2, y);
        textBox.SelectionLength.ShouldBeGreaterThan(0);
        Driver.MoveTo(pressX, y);
        Driver.MoveTo(pressX, y);
        Driver.SetButton(Gum.Automation.MouseButton.Primary, held: false);

        textBox.SelectionLength.ShouldBe(0);
    }

    [Fact]
    public void DoubleClick_SelectsTheWholeText()
    {
        TextBox textBox = CreateBox();
        int x = XOf(textBox, 4) + 1;

        Driver.Click(x, MidY(textBox));
        Driver.Click(x, MidY(textBox));

        textBox.SelectionStart.ShouldBe(0);
        textBox.SelectionLength.ShouldBe(Content.Length);
    }

    [Fact]
    public void ClickOutside_LosesFocus()
    {
        TextBox textBox = CreateBox();
        Driver.Click(XOf(textBox, 2), MidY(textBox));
        textBox.IsFocused.ShouldBeTrue();

        Driver.Click(5, 5);
        Driver.Step(2);

        textBox.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void ReadOnly_Drag_StillSelectsText()
    {
        TextBox textBox = CreateBox();
        textBox.IsReadOnly = true;

        Drag(textBox, XOf(textBox, 1) + 1, XOf(textBox, 4) + 1);

        textBox.SelectionStart.ShouldBe(1);
        textBox.SelectionLength.ShouldBe(3);
    }

    [Fact]
    public void Drag_ThenTyping_ReplacesTheDraggedSelection()
    {
        TextBox textBox = CreateBox();

        Drag(textBox, XOf(textBox, 0) + 1, XOf(textBox, 5) + 1);
        Driver.Type("X");

        textBox.Text.ShouldBe("X world again");
    }

    [Fact]
    public void Drag_ThenCtrlShiftLeft_DoesNotThrow()
    {
        TextBox textBox = CreateBox();
        Drag(textBox, XOf(textBox, 6) + 1, XOf(textBox, 12) + 1);

        textBox.HandleKeyDown(Gum.Forms.Input.Keys.Left, true, false, true);

        (textBox.SelectionStart + textBox.SelectionLength).ShouldBeLessThanOrEqualTo(Content.Length);
    }
}
