using Gum.Forms.Controls;
using MonoGameGum.Tests.Input;
using Shouldly;
using Xunit;
#if SKIA_LINKED_TESTS
using XnaButtons = SkiaGum.Tests.Forms.Linked.PadButtons;
#else
using XnaButtons = Microsoft.Xna.Framework.Input.Buttons;
#endif

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Pins how ListBox reads the gamepad D-pad and left stick while its items have focus. The reading is
/// shared with Menu through FrameworkElement.GetRepeatedGamepadDirection (#6696). Left and right do not step
/// a vertical list (#6938, see ListBoxPerpendicularExitTests).
/// </summary>
public class ListBoxGamepadDirectionTests : BaseTestClass
{
    [Theory]
    [InlineData(XnaButtons.DPadDown, 0f, 0f, 2)]
    [InlineData(XnaButtons.DPadUp, 0f, 0f, 0)]
    [InlineData((XnaButtons)0, 0f, -1f, 2)]
    [InlineData((XnaButtons)0, 0f, 1f, 0)]
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
#if SKIA_LINKED_TESTS
        SkiaGum.Tests.Forms.Linked.SkiaPadHelper.Apply(gamepad, buttons, leftStickX, leftStickY);
#else
        gamepad.Activity(
            new Microsoft.Xna.Framework.Input.GamePadState(
                new Microsoft.Xna.Framework.Vector2(leftStickX, leftStickY),
                Microsoft.Xna.Framework.Vector2.Zero,
                0, 0,
                buttons),
            0);
#endif
        FrameworkElement.GamePadsForUiControl.Add(gamepad);

        listBox.OnFocusUpdate();

        listBox.SelectedIndex.ShouldBe(expectedRow);
        listBox.ListBoxItems[expectedRow].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void LeftAndDownHeldTogether_DownWins()
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
#if SKIA_LINKED_TESTS
        SkiaGum.Tests.Forms.Linked.SkiaPadHelper.Apply(gamepad, XnaButtons.DPadLeft | XnaButtons.DPadDown, 0, 0);
#else
        gamepad.Activity(
            new Microsoft.Xna.Framework.Input.GamePadState(
                Microsoft.Xna.Framework.Vector2.Zero,
                Microsoft.Xna.Framework.Vector2.Zero,
                0, 0,
                XnaButtons.DPadLeft | XnaButtons.DPadDown),
            0);
#endif
        FrameworkElement.GamePadsForUiControl.Add(gamepad);

        listBox.OnFocusUpdate();

        // Left is the axis the list does not use (#6938), so the held Down steps.
        listBox.SelectedIndex.ShouldBe(2);
    }
}
