using Gum.Input;
using Shouldly;
using GumGamepadButton = Gum.Input.GamepadButton;
using RaylibGamepadButton = Raylib_cs.GamepadButton;
using RaylibGamepadAxis = Raylib_cs.GamepadAxis;

namespace RaylibGum.Tests.Inputs;

/// <summary>
/// Pins the button/axis mapping of <see cref="GamePadDriver"/> — the driver extracted
/// (issue #3552) from the <c>#elif RAYLIB</c> branch of <c>FormsUtilities.UpdateGamepads</c> to
/// mirror the MonoGame <c>GamePadDriver</c>. Every platform now exposes a same-named
/// <c>GamePadDriver</c> class in its own namespace (issue #3559), so this Raylib driver's class
/// name is no longer platform-prefixed. Raylib's real button/axis reads are static functions
/// that hit live hardware, so these tests exercise the driver's internal testing-seam overload
/// with fake readers instead.
/// </summary>
public class GamePadDriverTests : BaseTestClass
{
    [Fact]
    public void Apply_MapsRightFaceDown_ToA()
    {
        GamePad sut = new GamePad();

        GamePadDriver.Apply(
            sut,
            index: 0,
            time: 1,
            isButtonDown: (i, button) => button == RaylibGamepadButton.RightFaceDown,
            getAxisMovement: (i, axis) => 0f);

        sut.ButtonDown(GumGamepadButton.A).ShouldBeTrue();
    }

    [Fact]
    public void Apply_MapsLeftTrigger2_ToLeftTrigger_NotLeftShoulder()
    {
        GamePad sut = new GamePad();

        GamePadDriver.Apply(
            sut,
            index: 0,
            time: 1,
            isButtonDown: (i, button) => button == RaylibGamepadButton.LeftTrigger2,
            getAxisMovement: (i, axis) => 0f);

        sut.ButtonDown(GumGamepadButton.LeftTrigger).ShouldBeTrue();
        sut.ButtonDown(GumGamepadButton.LeftShoulder).ShouldBeFalse();
    }

    [Fact]
    public void Apply_FlipsLeftStickY_ToXnaGumConvention()
    {
        GamePad sut = new GamePad();

        // Raylib reports stick Y as positive-down; Gum/XNA convention is positive-up.
        GamePadDriver.Apply(
            sut,
            index: 0,
            time: 1,
            isButtonDown: (i, button) => false,
            getAxisMovement: (i, axis) => axis == RaylibGamepadAxis.LeftY ? 1f : 0f);

        sut.LeftStick.AsDPadDown(DPadDirection.Down).ShouldBeTrue();
    }

    [Fact]
    public void Apply_ReadsGivenGamepadIndex()
    {
        GamePad sutIndex0 = new GamePad();
        GamePad sutIndex1 = new GamePad();

        bool IsButtonDown(int i, RaylibGamepadButton button) =>
            i == 0 && button == RaylibGamepadButton.RightFaceDown;

        GamePadDriver.Apply(sutIndex0, index: 0, time: 1, IsButtonDown, (i, axis) => 0f);
        GamePadDriver.Apply(sutIndex1, index: 1, time: 1, IsButtonDown, (i, axis) => 0f);

        sutIndex0.ButtonDown(GumGamepadButton.A).ShouldBeTrue();
        sutIndex1.ButtonDown(GumGamepadButton.A).ShouldBeFalse();
    }
}
