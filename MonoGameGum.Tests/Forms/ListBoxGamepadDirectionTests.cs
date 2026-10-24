using Gum.Forms.Controls;
using MonoGameGum.Tests.Input;
using Shouldly;
using Xunit;
using XnaButtons = Microsoft.Xna.Framework.Input.Buttons;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Pins how ListBox reads the gamepad D-pad and left stick while its items have focus. The reading is
/// shared with Menu through FrameworkElement.GetRepeatedGamepadDirection (#6696).
/// </summary>
public class ListBoxGamepadDirectionTests : BaseTestClass
{
    [Theory]
    [InlineData(XnaButtons.DPadDown, 0f, 0f, 2)]
    [InlineData(XnaButtons.DPadRight, 0f, 0f, 2)]
    [InlineData(XnaButtons.DPadUp, 0f, 0f, 0)]
    [InlineData(XnaButtons.DPadLeft, 0f, 0f, 0)]
    [InlineData((XnaButtons)0, 0f, -1f, 2)]
    [InlineData((XnaButtons)0, 1f, 0f, 2)]
    [InlineData((XnaButtons)0, 0f, 1f, 0)]
    [InlineData((XnaButtons)0, -1f, 0f, 0)]
    public void Direction_WithItemFocused_MovesFocusToNeighborRow(
        XnaButtons buttons, float leftStickX, float leftStickY, int expectedRow)
    {
        ListBox listBox = new ListBox();
        listBox.AddToRoot();
        listBox.Items.Add("a");
        listBox.Items.Add("b");
        listBox.Items.Add("c");
        listBox.IsFocused = true;
        listBox.DoListItemsHaveFocus = true;
        listBox.SelectedIndex = 1;
        listBox.ListBoxItems[1].IsFocused = true;

        Gum.Input.GamePad gamepad = new();
        gamepad.Activity(
            new Microsoft.Xna.Framework.Input.GamePadState(
                new Microsoft.Xna.Framework.Vector2(leftStickX, leftStickY),
                Microsoft.Xna.Framework.Vector2.Zero,
                0, 0,
                buttons),
            0);
        FrameworkElement.GamePadsForUiControl.Add(gamepad);

        listBox.OnFocusUpdate();

        listBox.SelectedIndex.ShouldBe(expectedRow);
        listBox.ListBoxItems[expectedRow].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void LeftAndDownHeldTogether_LeftWins()
    {
        ListBox listBox = new ListBox();
        listBox.AddToRoot();
        listBox.Items.Add("a");
        listBox.Items.Add("b");
        listBox.Items.Add("c");
        listBox.IsFocused = true;
        listBox.DoListItemsHaveFocus = true;
        listBox.SelectedIndex = 1;
        listBox.ListBoxItems[1].IsFocused = true;

        Gum.Input.GamePad gamepad = new();
        gamepad.Activity(
            new Microsoft.Xna.Framework.Input.GamePadState(
                Microsoft.Xna.Framework.Vector2.Zero,
                Microsoft.Xna.Framework.Vector2.Zero,
                0, 0,
                XnaButtons.DPadLeft | XnaButtons.DPadDown),
            0);
        FrameworkElement.GamePadsForUiControl.Add(gamepad);

        listBox.OnFocusUpdate();

        listBox.SelectedIndex.ShouldBe(0);
    }
}
