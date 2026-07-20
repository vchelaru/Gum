using Gum.Input;
using Shouldly;

namespace RaylibGum.Tests.Inputs;

/// <summary>
/// Parity tests for the deadzone/interpolation configuration ported onto the
/// platform-neutral <see cref="AnalogStick"/> in GumCommon (issue #3137, Phase 1).
/// Most of the deadzone math predates <see cref="AnalogStick.X"/>/<see cref="AnalogStick.Y"/>
/// (issue #3839) and is still exercised indirectly, through whether the (post-deadzone)
/// position crosses the DPad on/off thresholds (0.55 / 0.45) surfaced by
/// <see cref="AnalogStick.AsDPadDown"/>.
/// </summary>
public class AnalogStickTests : BaseTestClass
{
    [Fact]
    public void XY_ShouldReturnZero_WhenStickWithinDeadzone()
    {
        AnalogStick sut = new AnalogStick { Deadzone = 0.2f };
        sut.Update(0.1f, -0.1f, 1);

        sut.X.ShouldBe(0f);
        sut.Y.ShouldBe(0f);
    }

    [Fact]
    public void XY_ShouldReturnPostDeadzonePosition_WhenStickBeyondDeadzone()
    {
        AnalogStick sut = new AnalogStick { Deadzone = 0f };
        sut.Update(0.3f, -0.6f, 1);

        sut.X.ShouldBe(0.3f);
        sut.Y.ShouldBe(-0.6f);
    }

    [Fact]
    public void AsDPadDown_Right_False_WhenStickWithinRadialDeadzone()
    {
        AnalogStick sut = new AnalogStick { Deadzone = 0.8f };
        // Magnitude 0.7 < 0.8 deadzone -> zeroed -> never crosses the on-threshold.
        sut.Update(0.7f, 0f, 1);

        sut.AsDPadDown(DPadDirection.Right).ShouldBeFalse();
    }

    [Fact]
    public void AsDPadDown_Right_True_WhenStickBeyondRadialDeadzone()
    {
        AnalogStick sut = new AnalogStick { Deadzone = 0.6f };
        // Magnitude 0.7 > 0.6 deadzone, Instant interpolation keeps it at 0.7 > 0.55 on-threshold.
        sut.Update(0.7f, 0f, 1);

        sut.AsDPadDown(DPadDirection.Right).ShouldBeTrue();
    }

    [Fact]
    public void AsDPadDown_Right_True_WithInstantInterpolation_NearThreshold()
    {
        AnalogStick sut = new AnalogStick
        {
            Deadzone = 0.1f,
            DeadzoneInterpolation = DeadzoneInterpolationType.Instant,
        };
        // Instant leaves 0.58 unchanged: 0.58 > 0.55 on-threshold -> down.
        sut.Update(0.58f, 0f, 1);

        sut.AsDPadDown(DPadDirection.Right).ShouldBeTrue();
    }

    [Fact]
    public void AsDPadDown_Right_False_WithLinearInterpolation_NearThreshold()
    {
        AnalogStick sut = new AnalogStick
        {
            Deadzone = 0.1f,
            DeadzoneInterpolation = DeadzoneInterpolationType.Linear,
        };
        // Linear maps 0.58 -> (0.58 - 0.1) / (1 - 0.1) = 0.533 < 0.55 on-threshold -> not down.
        sut.Update(0.58f, 0f, 1);

        sut.AsDPadDown(DPadDirection.Right).ShouldBeFalse();
    }
}
