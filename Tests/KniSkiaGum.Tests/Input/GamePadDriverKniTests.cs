using Gum.Input;
using Shouldly;
using XnaButtons = Microsoft.Xna.Framework.Input.Buttons;
using XnaButtonState = Microsoft.Xna.Framework.Input.ButtonState;
using XnaGamePadButtons = Microsoft.Xna.Framework.Input.GamePadButtons;
using XnaGamePadDPad = Microsoft.Xna.Framework.Input.GamePadDPad;
using XnaGamePadState = Microsoft.Xna.Framework.Input.GamePadState;
using XnaGamePadThumbSticks = Microsoft.Xna.Framework.Input.GamePadThumbSticks;
using XnaGamePadTriggers = Microsoft.Xna.Framework.Input.GamePadTriggers;

namespace KniSkiaGum.Tests;

/// <summary>
/// Tests for the KNI gamepad driver: it translates a <c>GamePadState</c> into the shared snapshot,
/// so trigger thresholding is the shared one. The sticks are not covered: building a
/// <c>GamePadThumbSticks</c> with values needs a KNI platform package, which a test process lacks.
/// They pass straight through.
/// </summary>
public class GamePadDriverKniTests : IDisposable
{
    private readonly Func<int, XnaGamePadState> _originalReadState = GamePadDriver.ReadState;

    public void Dispose() => GamePadDriver.ReadState = _originalReadState;

    private static XnaGamePadState State(XnaButtons buttons = 0, float leftTrigger = 0, float rightTrigger = 0) =>
        new XnaGamePadState(
            default(XnaGamePadThumbSticks),
            new XnaGamePadTriggers(leftTrigger, rightTrigger),
            new XnaGamePadButtons(buttons),
            new XnaGamePadDPad(
                buttons.HasFlag(XnaButtons.DPadUp) ? XnaButtonState.Pressed : XnaButtonState.Released,
                buttons.HasFlag(XnaButtons.DPadDown) ? XnaButtonState.Pressed : XnaButtonState.Released,
                buttons.HasFlag(XnaButtons.DPadLeft) ? XnaButtonState.Pressed : XnaButtonState.Released,
                buttons.HasFlag(XnaButtons.DPadRight) ? XnaButtonState.Pressed : XnaButtonState.Released));

    [Fact]
    public void Apply_MapsFaceButtonsAndDPad()
    {
        GamePad gamepad = new GamePad();

        GamePadDriver.Apply(gamepad, State(XnaButtons.A | XnaButtons.DPadLeft), 0);

        gamepad.ButtonDown(GamepadButton.A).ShouldBeTrue();
        gamepad.ButtonDown(GamepadButton.DPadLeft).ShouldBeTrue();
        gamepad.ButtonDown(GamepadButton.B).ShouldBeFalse();
    }

    [Fact]
    public void Apply_TriggersCountAsButtonsAtTheSharedThreshold()
    {
        GamePad gamepad = new GamePad();

        GamePadDriver.Apply(gamepad, State(leftTrigger: 0.5f, rightTrigger: 0.49f), 0);

        gamepad.ButtonDown(GamepadButton.LeftTrigger).ShouldBeTrue();
        gamepad.ButtonDown(GamepadButton.RightTrigger).ShouldBeFalse();
    }

    [Fact]
    public void Apply_ByIndex_ReadsThatPadsState()
    {
        int requestedIndex = -1;
        GamePadDriver.ReadState = index =>
        {
            requestedIndex = index;
            return State(XnaButtons.B);
        };
        GamePad gamepad = new GamePad();

        GamePadDriver.Apply(gamepad, 2, 0);

        requestedIndex.ShouldBe(2);
        gamepad.ButtonDown(GamepadButton.B).ShouldBeTrue();
    }
}
