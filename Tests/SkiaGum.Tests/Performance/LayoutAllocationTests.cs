using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using MonoGameGum.TestsCommon;
using Shouldly;
using Xunit.Abstractions;

namespace SkiaGum.Tests.Performance;

/// <summary>
/// Per-frame allocation guards for the layout engine on the Skia runtime, ported from
/// MonoGameGum.Tests (issue #1934). Layout is shared code, but Skia hosts it through its own
/// <c>GumService</c> and renderables, so the guarantee is checked here too. The zero-allocation
/// guards use <see cref="AllocationMeasurer.MeasureMinimum"/> so a one-off blip on a slow CI
/// runner does not fail them.
/// </summary>
public class LayoutAllocationTests : BaseTestClass
{
    private readonly ITestOutputHelper _output;
    private readonly SkiaSceneHost _host = new SkiaSceneHost();

    public LayoutAllocationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public override void Dispose()
    {
        _host.Dispose();
        base.Dispose();
    }

    /// <summary>
    /// Builds a content-sized vertical stack of <paramref name="itemCount"/> items, each with a
    /// nested child, and lays it out once. Returns the stack and its first item.
    /// </summary>
    private static (ContainerRuntime Stack, ContainerRuntime FirstItem) BuildContentSizedStack(int itemCount)
    {
        ContainerRuntime stack = new ContainerRuntime();
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        stack.Width = 200;
        stack.WidthUnits = DimensionUnitType.Absolute;
        stack.Height = 0;
        stack.HeightUnits = DimensionUnitType.RelativeToChildren;
        stack.AddToRoot();

        ContainerRuntime firstItem = null!;

        for (int i = 0; i < itemCount; i++)
        {
            ContainerRuntime item = new ContainerRuntime();
            item.Width = 0;
            item.WidthUnits = DimensionUnitType.RelativeToParent;
            item.Height = 30;
            item.HeightUnits = DimensionUnitType.Absolute;
            stack.AddChild(item);

            ContainerRuntime inner = new ContainerRuntime();
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
    public void UpdateLayout_ContentSizedStack_IsZeroAllocation()
    {
        (ContainerRuntime stack, _) = BuildContentSizedStack(itemCount: 20);

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () => stack.UpdateLayout(),
            warmupIterations: 50,
            measuredIterations: 500);

        _output.WriteLine($"UpdateLayout on a content-sized stack: {result.BytesPerIteration:N0} bytes/frame");

        result.TotalBytes.ShouldBe(0);
    }

    [Fact]
    public void PropertyChange_TriggeringRelayout_IsZeroAllocation()
    {
        (_, ContainerRuntime firstItem) = BuildContentSizedStack(itemCount: 20);

        float height = 30;
        const int measuredIterations = 500;
        const int attempts = 3;
        int layoutCallsBefore = GraphicalUiElement.UpdateLayoutCallCount;

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () =>
            {
                height = height == 30 ? 31 : 30;
                firstItem.Height = height;
            },
            attempts: attempts,
            warmupIterations: 50,
            measuredIterations: measuredIterations);

        int layoutCalls = GraphicalUiElement.UpdateLayoutCallCount - layoutCallsBefore;

        _output.WriteLine($"Height change on a content-sized stack: {result.BytesPerIteration:N0} bytes/frame");

        // MeasureMinimum runs the action on every attempt, so the liveness threshold scales with it.
        layoutCalls.ShouldBeGreaterThanOrEqualTo(measuredIterations * attempts);
        result.TotalBytes.ShouldBe(0);
    }

    [Fact]
    public void PropertyChange_TriggeringRelayout_RealisticFormsTree_IsZeroAllocation()
    {
        Gum.Forms.Window window = SkiaSceneHost.BuildRealisticFormsScene();

        float height = 200;
        const int measuredIterations = 500;
        const int attempts = 3;
        int layoutCallsBefore = GraphicalUiElement.UpdateLayoutCallCount;

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () =>
            {
                height = height == 200 ? 201 : 200;
                window.Height = height;
            },
            attempts: attempts,
            warmupIterations: 50,
            measuredIterations: measuredIterations);

        int layoutCalls = GraphicalUiElement.UpdateLayoutCallCount - layoutCallsBefore;

        _output.WriteLine($"Window height change [{SkiaSceneHost.RealisticFormsSceneDescription}]: " +
            $"{result.BytesPerIteration:N0} bytes/frame");

        layoutCalls.ShouldBeGreaterThanOrEqualTo(measuredIterations * attempts);
        result.TotalBytes.ShouldBe(0);
    }

    [Fact]
    public void IdleUpdate_RealisticFormsTree_IsZeroAllocation()
    {
        SkiaSceneHost.BuildRealisticFormsScene();

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () => _host.Update(),
            warmupIterations: 50,
            measuredIterations: 500);

        _output.WriteLine($"Idle Update [{SkiaSceneHost.RealisticFormsSceneDescription}]: " +
            $"{result.BytesPerIteration:N0} bytes/frame");

        result.TotalBytes.ShouldBe(0);
    }
}
