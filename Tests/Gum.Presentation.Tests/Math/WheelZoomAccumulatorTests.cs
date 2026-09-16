using RenderingLibrary;
using Shouldly;

namespace Gum.Presentation.Tests.Math;

public class WheelZoomAccumulatorTests
{
    [Fact]
    public void Consume_WithOneFullNotch_ReturnsOneStepImmediately()
    {
        var accumulator = new WheelZoomAccumulator();

        accumulator.Consume(120).ShouldBe(1);
    }

    [Fact]
    public void Consume_WithOneFullNotchNegative_ReturnsNegativeOneStepImmediately()
    {
        var accumulator = new WheelZoomAccumulator();

        accumulator.Consume(-120).ShouldBe(-1);
    }

    [Fact]
    public void Consume_WithManySmallDeltasInOneDirection_OnlyStepsOnceTheyReachAFullNotch()
    {
        // A trackpad's two-finger scroll reports many small deltas per second rather than one
        // 120-unit notch per event; this must throttle to the same one-step-per-notch cadence
        // as a real mouse wheel instead of zooming on every event.
        var accumulator = new WheelZoomAccumulator();

        var stepsBeforeNotch = 0;
        for (int i = 0; i < 9; i++)
        {
            stepsBeforeNotch += accumulator.Consume(12);
        }

        stepsBeforeNotch.ShouldBe(0);
        accumulator.Consume(12).ShouldBe(1);
    }

    [Fact]
    public void Consume_ReversingDirectionBeforeANotch_CancelsOutAndDoesNotStep()
    {
        var accumulator = new WheelZoomAccumulator();

        accumulator.Consume(60).ShouldBe(0);
        accumulator.Consume(-60).ShouldBe(0);
        accumulator.Consume(59).ShouldBe(0);
    }

    [Fact]
    public void Consume_WithDeltaLargerThanOneNotch_StepsOnceAndCarriesTheRemainderToTheNextCall()
    {
        var accumulator = new WheelZoomAccumulator();

        accumulator.Consume(200).ShouldBe(1);
        // 80 leftover from the first call, plus 40 here, reaches the next full notch.
        accumulator.Consume(40).ShouldBe(1);
    }
}
