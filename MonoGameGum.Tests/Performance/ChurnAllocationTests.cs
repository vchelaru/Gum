using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using MonoGameGum.TestsCommon;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace MonoGameGum.Tests.Performance;

/// <summary>
/// Allocation baselines and regression guards for runtime add/remove-child churn on
/// <see cref="GraphicalUiElement"/> (issue #1934). UIs that spawn/despawn elements at runtime
/// (damage numbers, tooltips, streaming list items) repeatedly add and remove children; unlike the
/// steady-state layout pass, these paths mutate the child collection, reparent, and fire
/// notifications, so they are the ones warmed under churn. Each guard measures managed bytes per
/// add+remove cycle and ratchets down as avoidable allocation sources are removed.
/// </summary>
public class ChurnAllocationTests : BaseTestClass
{
    private readonly ITestOutputHelper _output;

    public ChurnAllocationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Builds a content-sized vertical stack and a reusable child, then lays the stack out so it
    /// starts laid-out. The returned child is not attached; callers add and remove it to exercise
    /// the churn path without paying for element construction inside the measured window.
    /// </summary>
    private static (ContainerRuntime Stack, ContainerRuntime Child) BuildStackAndChild()
    {
        ContainerRuntime stack = new();
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        stack.Width = 200;
        stack.WidthUnits = DimensionUnitType.Absolute;
        stack.Height = 0;
        stack.HeightUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime child = new();
        child.Width = 0;
        child.WidthUnits = DimensionUnitType.RelativeToParent;
        child.Height = 30;
        child.HeightUnits = DimensionUnitType.Absolute;

        stack.UpdateLayout();
        return (stack, child);
    }

    [Fact]
    public void AddRemoveChild_Churn_AllocatesMinimalBytes()
    {
        (ContainerRuntime stack, ContainerRuntime child) = BuildStackAndChild();

        const int measuredIterations = 500;
        const int attempts = 3;

        int addRemoveCount = 0;

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () =>
            {
                stack.AddChild(child);
                stack.RemoveChild(child);
                addRemoveCount += 2;
            },
            attempts: attempts,
            warmupIterations: 50,
            measuredIterations: measuredIterations);

        _output.WriteLine($"Add+remove child churn on a content-sized stack: " +
            $"{result.BytesPerIteration:N0} bytes/cycle ({result.TotalBytes:N0} bytes over {result.Iterations} cycles)");

        // Liveness: prove the add/remove actually ran on every measured iteration of every attempt
        // (a silently no-op mutation would make the byte count meaningless).
        addRemoveCount.ShouldBeGreaterThanOrEqualTo(measuredIterations * attempts * 2);
        // Ratchet. Suppressing the inner collection's redundant notifications and de-boxing the
        // HandleCollectionChanged enumerators took this from ~560 to ~336 B/cycle. The residual is
        // the outer wrapper's own ObservableCollection event args (one NotifyCollectionChangedEventArgs
        // + single-item list per add and per remove), which HandleCollectionChanged legitimately
        // consumes to reparent — removing it needs a notification-design change, out of scope here.
        result.BytesPerIteration.ShouldBeLessThanOrEqualTo(400);
    }
}
