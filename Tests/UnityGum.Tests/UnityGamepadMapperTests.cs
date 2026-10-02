using Gum;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Unity;
using Shouldly;

namespace UnityGum.Tests;

/// <summary>
/// Unity gamepad state (a fake device list standing in for Gamepad.all) mapped into Gum's slots and
/// driven through the real Forms focus and click logic. Covers what the manual Windows checklist
/// in #5533 covers, except the Unity-side reads themselves.
/// </summary>
public class UnityGamepadMapperTests : BaseTestClass
{
    private static GamePad[] Slots => GumService.Default.Gamepads;

    private static void Frame(double time, params UnityGamepadState[] pads)
    {
        UnityGamepadMapper.Push(Slots, pads);
        GumService.Default.Update(time);
    }

    [Fact]
    public void Push_ConnectedPads_FillSlotsInOrder()
    {
        Frame(0, new UnityGamepadState(), new UnityGamepadState());

        Slots[0].IsConnected.ShouldBeTrue();
        Slots[1].IsConnected.ShouldBeTrue();
        Slots[2].IsConnected.ShouldBeFalse();
    }

    [Fact]
    public void Push_UnplugAndReplug_DisconnectsThenReconnectsWithoutError()
    {
        Frame(0, new UnityGamepadState());
        Slots[0].IsConnected.ShouldBeTrue();

        Frame(0.016);
        Slots[0].IsConnected.ShouldBeFalse();

        Frame(0.032, new UnityGamepadState { ButtonSouth = true });
        Slots[0].IsConnected.ShouldBeTrue();
        Slots[0].ButtonDown(GamepadButton.A).ShouldBeTrue();
    }

    [Fact]
    public void Push_FirstPadUnplugged_SecondPadShiftsIntoFirstSlot()
    {
        Frame(0, new UnityGamepadState(), new UnityGamepadState { ButtonNorth = true });
        Slots[1].ButtonDown(GamepadButton.Y).ShouldBeTrue();

        Frame(0.016, new UnityGamepadState { ButtonNorth = true });

        Slots[0].ButtonDown(GamepadButton.Y).ShouldBeTrue();
        Slots[1].IsConnected.ShouldBeFalse();
    }

    [Fact]
    public void Push_FaceButtons_MapPositionToXboxNames()
    {
        Frame(0, new UnityGamepadState { ButtonSouth = true, ButtonEast = true, ButtonWest = true, ButtonNorth = true });

        Slots[0].ButtonDown(GamepadButton.A).ShouldBeTrue();
        Slots[0].ButtonDown(GamepadButton.B).ShouldBeTrue();
        Slots[0].ButtonDown(GamepadButton.X).ShouldBeTrue();
        Slots[0].ButtonDown(GamepadButton.Y).ShouldBeTrue();

        Frame(0.016, new UnityGamepadState { ButtonEast = true });

        Slots[0].ButtonDown(GamepadButton.A).ShouldBeFalse();
        Slots[0].ButtonDown(GamepadButton.B).ShouldBeTrue();
        Slots[0].ButtonDown(GamepadButton.X).ShouldBeFalse();
    }

    [Fact]
    public void Push_OtherButtons_Map()
    {
        Frame(0, new UnityGamepadState
        {
            LeftShoulder = true, RightShoulder = true, Start = true, Select = true,
            LeftStickButton = true, RightStickButton = true, DPadUp = true,
        });

        Slots[0].ButtonDown(GamepadButton.LeftShoulder).ShouldBeTrue();
        Slots[0].ButtonDown(GamepadButton.RightShoulder).ShouldBeTrue();
        Slots[0].ButtonDown(GamepadButton.Start).ShouldBeTrue();
        Slots[0].ButtonDown(GamepadButton.Back).ShouldBeTrue();
        Slots[0].ButtonDown(GamepadButton.LeftStick).ShouldBeTrue();
        Slots[0].ButtonDown(GamepadButton.RightStick).ShouldBeTrue();
        Slots[0].ButtonDown(GamepadButton.DPadUp).ShouldBeTrue();
        Slots[0].ButtonDown(GamepadButton.DPadDown).ShouldBeFalse();
    }

    [Theory]
    [InlineData(0.49f, false)]
    [InlineData(0.5f, true)]
    public void Push_Triggers_PressAtThreshold(float value, bool expected)
    {
        Frame(0, new UnityGamepadState { LeftTrigger = value, RightTrigger = value });

        Slots[0].ButtonDown(GamepadButton.LeftTrigger).ShouldBe(expected);
        Slots[0].ButtonDown(GamepadButton.RightTrigger).ShouldBe(expected);
    }

    [Fact]
    public void Push_StickInsideDeadzone_ReadsZero()
    {
        Frame(0, new UnityGamepadState { LeftStickX = 0.05f, LeftStickY = -0.05f });

        Slots[0].LeftStick.X.ShouldBe(0);
        Slots[0].LeftStick.Y.ShouldBe(0);
        Slots[0].ButtonDown(GamepadButton.LeftThumbstickLeft).ShouldBeFalse();
    }

    [Fact]
    public void Push_StickPastDeadzone_ReadsPositiveUpAndReportsDirection()
    {
        Frame(0, new UnityGamepadState { LeftStickX = 0.9f, LeftStickY = 0.9f, RightStickX = -0.8f });

        Slots[0].LeftStick.X.ShouldBeGreaterThan(0.5f);
        Slots[0].LeftStick.Y.ShouldBeGreaterThan(0.5f);
        Slots[0].RightStick.X.ShouldBeLessThan(-0.5f);
        Slots[0].ButtonDown(GamepadButton.LeftThumbstickUp).ShouldBeTrue();
        Slots[0].ButtonDown(GamepadButton.LeftThumbstickDown).ShouldBeFalse();
    }

    [Fact]
    public void Update_ButtonA_IncrementsClickCount()
    {
        GumService.Default.UseGamepadDefaults();
        Button button = new Button();
        button.AddToRoot();
        button.IsFocused = true;
        int clickCount = 0;
        button.Click += (_, _) => clickCount++;

        Frame(0, new UnityGamepadState());
        Frame(0.016, new UnityGamepadState { ButtonSouth = true });
        Frame(0.032, new UnityGamepadState());

        clickCount.ShouldBe(1);
    }

    [Fact]
    public void Update_DPadDown_MovesFocusToNextControl()
    {
        (Button first, Button second) = CreateStackedButtons();

        Frame(0, new UnityGamepadState());
        Frame(0.016, new UnityGamepadState { DPadDown = true });

        first.IsFocused.ShouldBeFalse();
        second.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void Update_LeftThumbstickDown_MovesFocusToNextControl()
    {
        (Button first, Button second) = CreateStackedButtons();

        Frame(0, new UnityGamepadState());
        Frame(0.016, new UnityGamepadState { LeftStickY = -1f });

        first.IsFocused.ShouldBeFalse();
        second.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void Update_StickJitterInsideDeadzone_DoesNotMoveFocus()
    {
        (Button first, Button second) = CreateStackedButtons();

        Frame(0, new UnityGamepadState());
        Frame(0.016, new UnityGamepadState { LeftStickY = -0.05f });

        first.IsFocused.ShouldBeTrue();
        second.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void Push_UnplugWhileHeld_ReleasesButtonsAndSticks()
    {
        Frame(0, new UnityGamepadState { ButtonSouth = true, DPadDown = true, LeftStickX = 1f });

        Frame(0.016);

        Slots[0].IsConnected.ShouldBeFalse();
        Slots[0].ButtonDown(GamepadButton.A).ShouldBeFalse();
        Slots[0].ButtonDown(GamepadButton.DPadDown).ShouldBeFalse();
        Slots[0].LeftStick.X.ShouldBe(0);
    }

    [Fact]
    public void Update_UnplugWhileAHeld_ReleasesAndReplugClicksAgain()
    {
        GumService.Default.UseGamepadDefaults();
        Button button = new Button();
        button.AddToRoot();
        button.IsFocused = true;
        int clickCount = 0;
        button.Click += (_, _) => clickCount++;

        Frame(0, new UnityGamepadState());
        Frame(0.016, new UnityGamepadState { ButtonSouth = true });
        Frame(0.032);
        Frame(0.048);

        // Unplugging releases the held A, which completes the click.
        clickCount.ShouldBe(1);

        Frame(0.064, new UnityGamepadState { ButtonSouth = true });
        Frame(0.080, new UnityGamepadState());

        clickCount.ShouldBe(2);
    }

    private static (Button first, Button second) CreateStackedButtons()
    {
        GumService.Default.UseGamepadDefaults();
        StackPanel panel = new StackPanel();
        panel.AddToRoot();
        Button first = new Button();
        Button second = new Button();
        panel.AddChild(first);
        panel.AddChild(second);
        first.IsFocused = true;
        return (first, second);
    }
}
