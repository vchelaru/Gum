using System.Collections.Generic;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using MonoGameGum.TestsCommon;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace MonoGameGum.Tests.Performance;

/// <summary>
/// A playing state animation builds the state it applies from objects kept between ticks instead of a new
/// <see cref="StateSave"/> with new <see cref="VariableSave"/>s every tick (#7873). ApplyState is replaced by a
/// no-op here so the reading is the interpolation alone; what applying the state costs is not part of this.
/// </summary>
public class AnimationTickAllocationTests : BaseTestClass
{
    private readonly ITestOutputHelper _output;

    public AnimationTickAllocationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private sealed class StateDiscardingContainer : ContainerRuntime
    {
        public int StatesApplied;

        public override void ApplyState(StateSave state)
        {
            StatesApplied++;
        }
    }

    private static (StateDiscardingContainer Element, AnimationRuntime Animation) Build(params string[] variables)
    {
        StateDiscardingContainer element = new();
        StateSaveCategory category = new() { Name = "Category1" };
        element.Categories[category.Name] = category;
        StateSave start = new() { Name = "Start" };
        StateSave end = new() { Name = "End" };
        foreach (string variable in variables)
        {
            start.Variables.Add(new VariableSave { Name = variable, Value = 10f });
            end.Variables.Add(new VariableSave { Name = variable, Value = 110f });
        }
        category.States.Add(start);
        category.States.Add(end);

        AnimationRuntime animation = new() { Name = "Animation1", Loops = true };
        animation.Keyframes.Add(new KeyframeRuntime { Time = 0, StateName = "Category1/Start", InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear });
        animation.Keyframes.Add(new KeyframeRuntime { Time = 1, StateName = "Category1/End", InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear });
        animation.RefreshCumulativeStates(element);
        return (element, animation);
    }

    private double Measure(string label, StateDiscardingContainer element, AnimationRuntime animation)
    {
        double time = 0;
        int appliedBefore = element.StatesApplied;

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () =>
            {
                time = (time + 0.05) % 1;
                animation.ApplyAtTimeTo(time, element);
            },
            attempts: 3,
            warmupIterations: 50,
            measuredIterations: 900);

        // Liveness: every measured tick built and handed over a state.
        (element.StatesApplied - appliedBefore).ShouldBeGreaterThanOrEqualTo(900);

        _output.WriteLine($"{label}: {result.BytesPerIteration:N0} bytes/frame");
        return result.BytesPerIteration;
    }

    [Fact]
    public void ApplyAtTimeTo_OneInterpolatedVariable_AllocatesOnlyTheBoxedValues()
    {
        (StateDiscardingContainer element, AnimationRuntime animation) = Build("X");

        // Measured (Debug, macOS): 1,888 bytes/frame when every tick built a new state, 280 with the state
        // reused. What is left is the boxed floats of the two merges, the substrings of the two categorized
        // state names, and the interpolation function lookup.
        Measure("one variable", element, animation).ShouldBeLessThanOrEqualTo(340);
    }

    [Fact]
    public void ApplyAtTimeTo_FourInterpolatedVariables_DoesNotAllocateAStateOrVariablesPerTick()
    {
        (StateDiscardingContainer element, AnimationRuntime animation) = Build("X", "Y", "Width", "Height");

        // Measured: 3,472 before, 640 now. The rest is boxed values; it grows with the variable count, a state does not.
        Measure("four variables", element, animation).ShouldBeLessThanOrEqualTo(760);
    }

    [Fact]
    public void ApplyAtTimeTo_InstanceVariables_DoNotSplitTheirNamesEveryTick()
    {
        (StateDiscardingContainer element, AnimationRuntime animation) = Build("Box.X", "Box.Width");

        // Measured: 2,512 before, 496 now.
        Measure("two instance variables", element, animation).ShouldBeLessThanOrEqualTo(600);
    }
}
