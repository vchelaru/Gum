using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using MonoGameGum.TestsCommon;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Performance;

/// <summary>
/// Hiding an element and laying out a suspended one record skipped layout; neither allocates once warm (#8218).
/// </summary>
public class VisibilityAllocationTests : BaseTestClass
{
    [Fact]
    public void ResumeLayout_AfterSuspendedChange_IsZeroAllocation()
    {
        ContainerRuntime parent = new() { Width = 400, Height = 300 };
        ContainerRuntime child = new() { Width = 40, Height = 20 };
        parent.AddChild(child);
        parent.UpdateLayout();
        float width = 40;

        AllocationResult result = AllocationMeasurer.Measure(
            () =>
            {
                child.SuspendLayout();
                width = width == 40 ? 41 : 40;
                child.Width = width;
                child.ResumeLayout();
            },
            warmupIterations: 20,
            measuredIterations: 200);

        child.AbsoluteWidth.ShouldBe(width);
        result.TotalBytes.ShouldBe(0);
    }

    [Fact]
    public void Visible_Toggled_IsZeroAllocation()
    {
        ContainerRuntime parent = new() { Width = 400, Height = 300 };
        ContainerRuntime child = new() { Width = 40, Height = 20 };
        RectangleRuntime sibling = new() { Width = 40, Height = 20 };
        parent.AddChild(child);
        parent.AddChild(sibling);
        parent.UpdateLayout();
        bool visible = true;

        AllocationResult result = AllocationMeasurer.Measure(
            () =>
            {
                visible = !visible;
                child.Visible = visible;
            },
            warmupIterations: 20,
            measuredIterations: 200);

        child.Visible.ShouldBe(visible);
        result.TotalBytes.ShouldBe(0);
    }
}
