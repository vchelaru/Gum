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
/// Per-frame allocation baselines and regression guards for the layout engine
/// (<see cref="Gum.Wireframe.GraphicalUiElement.UpdateLayout()"/>). Part of the runtime
/// allocation pass, issue #1934: the target is zero managed bytes allocated per steady-state
/// layout pass; each guard ratchets down as allocation sources are removed.
/// </summary>
public class LayoutAllocationTests : BaseTestClass
{
    private readonly ITestOutputHelper _output;

    public LayoutAllocationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Builds a content-sized vertical stack of <paramref name="itemCount"/> items, each with a
    /// nested child, and runs one layout pass so it starts in a laid-out state. This exercises the
    /// content-measuring recursion (RelativeToChildren height, parent-relative width, stacking)
    /// that dominates the scroll/layout scenario. Returns the stack and its first item so callers
    /// can mutate a child to trigger a realistic relayout.
    /// </summary>
    private static (ContainerRuntime Stack, ContainerRuntime FirstItem) BuildContentSizedStack(int itemCount)
    {
        ContainerRuntime stack = new();
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        stack.Width = 200;
        stack.WidthUnits = DimensionUnitType.Absolute;
        stack.Height = 0;
        stack.HeightUnits = DimensionUnitType.RelativeToChildren;

        ContainerRuntime firstItem = null!;

        for (int i = 0; i < itemCount; i++)
        {
            ContainerRuntime item = new();
            item.Width = 0;
            item.WidthUnits = DimensionUnitType.RelativeToParent;
            item.Height = 30;
            item.HeightUnits = DimensionUnitType.Absolute;
            stack.AddChild(item);

            ContainerRuntime inner = new();
            inner.Width = 0;
            inner.WidthUnits = DimensionUnitType.RelativeToParent;
            inner.Height = 20;
            inner.HeightUnits = DimensionUnitType.Absolute;
            item.AddChild(inner);

            if (i == 0)
            {
                firstItem = item;
            }
        }

        stack.UpdateLayout();
        return (stack, firstItem);
    }

    [Fact]
    public void PropertyChange_TriggeringRelayout_IsZeroAllocation()
    {
        const int itemCount = 20;

        (ContainerRuntime stack, ContainerRuntime firstItem) = BuildContentSizedStack(itemCount);

        // Alternate the first item's height every frame so the change is real (the setter re-lays-out
        // and, because the stack is content-sized, propagates up to re-measure and re-stack siblings).
        float height = 30;
        const int measuredIterations = 500;

        int layoutCallsBefore = GraphicalUiElement.UpdateLayoutCallCount;

        AllocationResult result = AllocationMeasurer.Measure(
            () =>
            {
                height = height == 30 ? 31 : 30;
                firstItem.Height = height;
            },
            warmupIterations: 50,
            measuredIterations: measuredIterations);

        int layoutCalls = GraphicalUiElement.UpdateLayoutCallCount - layoutCallsBefore;

        _output.WriteLine($"Height change on the first item of a {itemCount}-item content-sized stack: " +
            $"{result.BytesPerIteration:N0} bytes/frame ({result.TotalBytes:N0} bytes over {result.Iterations} frames)");

        // Liveness: prove the scenario actually drove relayout each frame (guards against a silent
        // no-op setter making the zero-allocation result meaningless).
        layoutCalls.ShouldBeGreaterThanOrEqualTo(measuredIterations);
        // A property change that re-lays-out and propagates up a content-sized stack allocates
        // nothing in steady state; this guards against a regression reintroducing allocations (#1934).
        result.TotalBytes.ShouldBe(0);
    }

    [Fact]
    public void UpdateLayout_ContentSizedStack_IsZeroAllocation()
    {
        const int itemCount = 20;

        (ContainerRuntime stack, _) = BuildContentSizedStack(itemCount);

        AllocationResult result = AllocationMeasurer.Measure(
            () => stack.UpdateLayout(),
            warmupIterations: 50,
            measuredIterations: 500);

        _output.WriteLine($"UpdateLayout on a {itemCount}-item content-sized stack: " +
            $"{result.BytesPerIteration:N0} bytes/frame ({result.TotalBytes:N0} bytes over {result.Iterations} frames)");

        // A steady-state layout pass over a container-only tree allocates nothing; this guards
        // against a regression (e.g. a new LINQ call or per-pass list) reintroducing allocations.
        result.TotalBytes.ShouldBe(0);
    }
}
