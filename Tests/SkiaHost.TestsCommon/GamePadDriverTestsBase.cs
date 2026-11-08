using Gum.Input;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace SkiaHostTests;

/// <summary>
/// A host's gamepad driver wired to a fake device the test drives. A fresh rig is made for each test.
/// </summary>
public interface IHostGamePadRig
{
    /// <summary>The Gum gamepad the driver fills.</summary>
    GamePad GamePad { get; }

    /// <summary>The fake device holds exactly <paramref name="button"/> down and nothing else.</summary>
    void Press(GamepadButton button);

    /// <summary>The fake device's triggers sit at these positions (0 to 1).</summary>
    void SetTriggers(float left, float right);

    /// <summary>Runs the host's driver once, copying the fake device into <see cref="GamePad"/>.</summary>
    void Apply(double time);

    /// <summary>
    /// Makes the host report no device at index 0 for <see cref="IGumService.ApplyGamePadState"/>.
    /// A host whose bootstrap already has none does nothing.
    /// </summary>
    void RemoveDevice();
}

/// <summary>
/// A gamepad rig whose device has thumbsticks. Positions are in Gum's convention (positive Y is up);
/// the rig converts to its device's native convention.
/// </summary>
public interface IHostStickGamePadRig : IHostGamePadRig
{
    /// <summary>The left stick sits at this position.</summary>
    void SetLeftStick(float x, float y);

    /// <summary>The right stick sits at this position.</summary>
    void SetRightStick(float x, float y);
}

/// <summary>
/// What every Skia host's gamepad driver maps from its device into <see cref="GamePad"/>: each button to
/// exactly one Gum button, triggers as buttons at half travel, and the frame committed once per
/// <c>Apply</c>. A host's test project derives from this class and supplies a rig over its own fake
/// device.
/// </summary>
public abstract class GamePadDriverTestsBase
{
    private static readonly GamepadButton[] MappedButtons =
    {
        GamepadButton.DPadUp, GamepadButton.DPadDown, GamepadButton.DPadLeft, GamepadButton.DPadRight,
        GamepadButton.A, GamepadButton.B, GamepadButton.X, GamepadButton.Y,
        GamepadButton.LeftShoulder, GamepadButton.RightShoulder,
        GamepadButton.Start, GamepadButton.Back,
        GamepadButton.LeftStick, GamepadButton.RightStick,
        GamepadButton.LeftTrigger, GamepadButton.RightTrigger,
    };

    /// <summary>A rig with a fresh gamepad and fake device.</summary>
    protected abstract IHostGamePadRig CreateRig();

    [Fact]
    public void Apply_MapsAButtonPressed_ToA()
    {
        IHostGamePadRig rig = CreateRig();
        rig.Press(GamepadButton.A);

        rig.Apply(1);

        rig.GamePad.ButtonDown(GamepadButton.A).ShouldBeTrue();
        rig.GamePad.ButtonDown(GamepadButton.B).ShouldBeFalse();
    }

    [Fact]
    public void Apply_MapsLeftShoulder_NotRightShoulder()
    {
        IHostGamePadRig rig = CreateRig();
        rig.Press(GamepadButton.LeftShoulder);

        rig.Apply(1);

        rig.GamePad.ButtonDown(GamepadButton.LeftShoulder).ShouldBeTrue();
        rig.GamePad.ButtonDown(GamepadButton.RightShoulder).ShouldBeFalse();
    }

    [Fact]
    public void Apply_ThresholdsLeftTrigger_ToLeftTrigger()
    {
        IHostGamePadRig rig = CreateRig();
        rig.SetTriggers(0.75f, 0f);

        rig.Apply(1);

        rig.GamePad.ButtonDown(GamepadButton.LeftTrigger).ShouldBeTrue();
        rig.GamePad.ButtonDown(GamepadButton.RightTrigger).ShouldBeFalse();
    }

    [Theory]
    [InlineData(GamepadButton.DPadUp)]
    [InlineData(GamepadButton.DPadDown)]
    [InlineData(GamepadButton.DPadLeft)]
    [InlineData(GamepadButton.DPadRight)]
    [InlineData(GamepadButton.A)]
    [InlineData(GamepadButton.B)]
    [InlineData(GamepadButton.X)]
    [InlineData(GamepadButton.Y)]
    [InlineData(GamepadButton.LeftShoulder)]
    [InlineData(GamepadButton.RightShoulder)]
    [InlineData(GamepadButton.Start)]
    [InlineData(GamepadButton.Back)]
    [InlineData(GamepadButton.LeftStick)]
    [InlineData(GamepadButton.RightStick)]
    public void Apply_MapsEachDeviceButton_ToExactlyOneGumButton(GamepadButton expected)
    {
        IHostGamePadRig rig = CreateRig();
        rig.Press(expected);

        rig.Apply(1);

        foreach (GamepadButton button in MappedButtons)
        {
            rig.GamePad.ButtonDown(button).ShouldBe(button == expected, button.ToString());
        }
    }

    [Theory]
    [InlineData(0.49f, false)]
    [InlineData(0.5f, true)]
    public void Apply_ThresholdsBothTriggers_AtHalf(float position, bool expected)
    {
        IHostGamePadRig rig = CreateRig();
        rig.SetTriggers(position, position);

        rig.Apply(1);

        rig.GamePad.ButtonDown(GamepadButton.LeftTrigger).ShouldBe(expected);
        rig.GamePad.ButtonDown(GamepadButton.RightTrigger).ShouldBe(expected);
    }

    [Fact]
    public void Apply_CommitsTheFrame_SoPushIsReportedOnlyOnce()
    {
        IHostGamePadRig rig = CreateRig();
        rig.Press(GamepadButton.A);

        rig.Apply(1);
        rig.GamePad.ButtonPushed(GamepadButton.A).ShouldBeTrue();

        rig.Apply(2);
        rig.GamePad.ButtonPushed(GamepadButton.A).ShouldBeFalse();
        rig.GamePad.ButtonDown(GamepadButton.A).ShouldBeTrue();
    }
}

/// <summary>
/// The gamepad cases for a polled device with thumbsticks (a pad that disappears keeps its pushed buttons): positive Y reaches <see cref="GamePad"/> as up
/// whatever the device's native convention, and X passes through. A host derives from this class instead
/// of <see cref="GamePadDriverTestsBase"/>.
/// </summary>
public abstract class GamePadDriverStickTestsBase : GamePadDriverTestsBase
{
    /// <summary>A rig with a fresh gamepad and a fake device that has thumbsticks.</summary>
    protected abstract IHostStickGamePadRig CreateStickRig();

    /// <inheritdoc/>
    protected sealed override IHostGamePadRig CreateRig() => CreateStickRig();

    [Fact]
    public void Apply_MapsLeftStickUp_ToPositiveY()
    {
        IHostStickGamePadRig rig = CreateStickRig();
        rig.SetLeftStick(0f, 1f);

        rig.Apply(1);

        rig.GamePad.LeftStick.AsDPadDown(DPadDirection.Up).ShouldBeTrue();
    }

    [Fact]
    public void Apply_MapsRightStickUp_ToPositiveY_AndPassesXThrough()
    {
        IHostStickGamePadRig rig = CreateStickRig();
        rig.SetRightStick(0.9f, 0.9f);

        rig.Apply(1);

        rig.GamePad.RightStick.X.ShouldBeGreaterThan(0.5f);
        rig.GamePad.RightStick.Y.ShouldBeGreaterThan(0.5f);
    }

    [Fact]
    public void ApplyGamePadState_WithNoDeviceAtIndex_DisconnectsButKeepsLastPushedButtons()
    {
        GamePad gamePad = new GamePad();
        gamePad.SetConnected(true);
        gamePad.SetButtonState(GamepadButton.A, isDown: true);
        gamePad.Activity(1);
        gamePad.IsConnected.ShouldBeTrue();
        CreateStickRig().RemoveDevice();

        IGumService.Default!.ApplyGamePadState(gamePad, 0, 2);

        gamePad.IsConnected.ShouldBeFalse();
        // Nothing releases the buttons on disconnect; they keep their last pushed state.
        gamePad.ButtonDown(GamepadButton.A).ShouldBeTrue();
    }

    [Fact]
    public void Apply_PassesLeftStickXThrough()
    {
        IHostStickGamePadRig rig = CreateStickRig();
        rig.SetLeftStick(-0.9f, 0f);

        rig.Apply(1);

        rig.GamePad.LeftStick.X.ShouldBeLessThan(-0.5f);
    }
}
