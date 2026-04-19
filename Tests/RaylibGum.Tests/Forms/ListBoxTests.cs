using Gum.Forms.Controls;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System.Collections.Generic;
using Keys = Gum.Forms.Input.Keys;

namespace RaylibGum.Tests.Forms;

/// <summary>
/// End-to-end Raylib keyboard coverage for <see cref="ListBox"/>. Proves the full
/// chain from a mocked <see cref="IInputReceiverKeyboard"/> through
/// <c>ListBox.OnFocusUpdate</c> → <c>DoTopLevelFocusUpdate</c> →
/// <c>DoListItemFocusUpdate</c> advances <c>SelectedIndex</c> on
/// <c>Keys.Down</c> for Raylib.
/// </summary>
public class ListBoxTests : BaseTestClass
{
    [Fact]
    public void ListBox_DownArrowPressed_MovesSelectionDown()
    {
        ListBox listBox = new ListBox();

        listBox.Visual.ShouldNotBeNull();
        listBox.AddToRoot();

        listBox.Items!.Add("A");
        listBox.Items!.Add("B");
        listBox.Items!.Add("C");

        listBox.SelectedIndex = 0;
        // ListBox routes keyboard arrow navigation through DoListItemFocusUpdate(),
        // which only runs when items (not the list itself) have focus. In a real app,
        // Enter/Space on the top-level focused ListBox flips this to true; here we set
        // it directly so the test isolates arrow-key navigation.
        listBox.DoListItemsHaveFocus = true;

        Mock<IInputReceiverKeyboard> keyboard = new Mock<IInputReceiverKeyboard>();
        keyboard.Setup(k => k.KeyTyped(Keys.Down)).Returns(true);
        keyboard.Setup(k => k.KeysTyped).Returns(new List<int>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        listBox.OnFocusUpdate();

        listBox.SelectedIndex.ShouldBe(1);
    }
}
