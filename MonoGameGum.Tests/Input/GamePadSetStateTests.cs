using Gum.Input;
using Shouldly;
using Xunit;
using GamePad = Gum.Input.GamePad;
using GamepadButton = Gum.Input.GamepadButton;

namespace MonoGameGum.Tests.Input;

public class GamePadSetStateTests
{
    private static readonly GamepadButton[] DigitalButtons =
    {
        GamepadButton.DPadUp, GamepadButton.DPadDown, GamepadButton.DPadLeft, GamepadButton.DPadRight,
        GamepadButton.A, GamepadButton.B, GamepadButton.X, GamepadButton.Y,
        GamepadButton.LeftShoulder, GamepadButton.RightShoulder,
        GamepadButton.Start, GamepadButton.Back,
        GamepadButton.LeftStick, GamepadButton.RightStick,
        GamepadButton.LeftTrigger, GamepadButton.RightTrigger,
    };

    public static IEnumerable<object[]> EachButtonCase()
    {
        yield return new object[] { new GamePadSnapshot { DPadUp = true }, GamepadButton.DPadUp };
        yield return new object[] { new GamePadSnapshot { DPadDown = true }, GamepadButton.DPadDown };
        yield return new object[] { new GamePadSnapshot { DPadLeft = true }, GamepadButton.DPadLeft };
        yield return new object[] { new GamePadSnapshot { DPadRight = true }, GamepadButton.DPadRight };
        yield return new object[] { new GamePadSnapshot { A = true }, GamepadButton.A };
        yield return new object[] { new GamePadSnapshot { B = true }, GamepadButton.B };
        yield return new object[] { new GamePadSnapshot { X = true }, GamepadButton.X };
        yield return new object[] { new GamePadSnapshot { Y = true }, GamepadButton.Y };
        yield return new object[] { new GamePadSnapshot { LeftShoulder = true }, GamepadButton.LeftShoulder };
        yield return new object[] { new GamePadSnapshot { RightShoulder = true }, GamepadButton.RightShoulder };
        yield return new object[] { new GamePadSnapshot { Start = true }, GamepadButton.Start };
        yield return new object[] { new GamePadSnapshot { Back = true }, GamepadButton.Back };
        yield return new object[] { new GamePadSnapshot { LeftStick = true }, GamepadButton.LeftStick };
        yield return new object[] { new GamePadSnapshot { RightStick = true }, GamepadButton.RightStick };
        yield return new object[] { new GamePadSnapshot { LeftTrigger = 1f }, GamepadButton.LeftTrigger };
        yield return new object[] { new GamePadSnapshot { RightTrigger = 1f }, GamepadButton.RightTrigger };
    }

    [Theory]
    [MemberData(nameof(EachButtonCase))]
    public void SetState_MapsEachSnapshotField_ToExactlyOneButton(GamePadSnapshot snapshot, GamepadButton expected)
    {
        GamePad sut = new GamePad();

        sut.SetState(snapshot);
        sut.Activity(1);

        foreach (GamepadButton button in DigitalButtons)
        {
            sut.ButtonDown(button).ShouldBe(button == expected, button.ToString());
        }
    }

    [Theory]
    [InlineData(0f, false)]
    [InlineData(0.49f, false)]
    [InlineData(0.5f, true)]
    [InlineData(1f, true)]
    public void SetState_ThresholdsTriggers_AtHalf(float value, bool expected)
    {
        GamePad sut = new GamePad();

        sut.SetState(new GamePadSnapshot { LeftTrigger = value, RightTrigger = value });
        sut.Activity(1);

        sut.ButtonDown(GamepadButton.LeftTrigger).ShouldBe(expected);
        sut.ButtonDown(GamepadButton.RightTrigger).ShouldBe(expected);
    }

    [Fact]
    public void SetState_PassesSticksThrough_WithYPositiveUp()
    {
        GamePad sut = new GamePad();

        sut.SetState(new GamePadSnapshot { LeftStickX = -0.9f, LeftStickY = 0.9f, RightStickX = 0.9f, RightStickY = -0.9f });
        sut.Activity(1);

        sut.LeftStick.X.ShouldBeLessThan(-0.5f);
        sut.LeftStick.Y.ShouldBeGreaterThan(0.5f);
        sut.RightStick.X.ShouldBeGreaterThan(0.5f);
        sut.RightStick.Y.ShouldBeLessThan(-0.5f);
    }

    [Fact]
    public void SetState_SetsConnectedFromSnapshot()
    {
        GamePad sut = new GamePad();

        sut.SetState(new GamePadSnapshot { IsConnected = true });
        sut.Activity(1);
        sut.IsConnected.ShouldBeTrue();

        sut.SetState(new GamePadSnapshot { IsConnected = false });
        sut.Activity(2);
        sut.IsConnected.ShouldBeFalse();
    }

    [Fact]
    public void SetState_DoesNotCommitTheFrame_UntilActivity()
    {
        GamePad sut = new GamePad();

        sut.SetState(new GamePadSnapshot { A = true });

        sut.ButtonDown(GamepadButton.A).ShouldBeFalse();

        sut.Activity(1);

        sut.ButtonPushed(GamepadButton.A).ShouldBeTrue();
    }

    [Fact]
    public void SetState_DefaultSnapshot_ReleasesEverythingPreviouslyHeld()
    {
        GamePad sut = new GamePad();
        sut.SetState(new GamePadSnapshot { A = true, DPadDown = true, LeftTrigger = 1f, LeftStickX = 1f });
        sut.Activity(1);

        sut.SetState(default);
        sut.Activity(2);

        sut.ButtonDown(GamepadButton.A).ShouldBeFalse();
        sut.ButtonDown(GamepadButton.DPadDown).ShouldBeFalse();
        sut.ButtonDown(GamepadButton.LeftTrigger).ShouldBeFalse();
        sut.LeftStick.X.ShouldBe(0f);
    }
}
