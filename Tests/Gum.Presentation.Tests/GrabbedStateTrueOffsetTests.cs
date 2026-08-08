using Gum.DataTypes;
using Gum.Input;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins GrabbedState's true-offset accumulators, which let grid-snap track where an object would
/// be with no snapping applied, independent of the live (already-snapped) GraphicalUiElement value
/// - see issue #4137 review feedback ("movement fights you").
/// </summary>
public class GrabbedStateTrueOffsetTests
{
    private static GrabbedState CreateSut()
    {
        return new GrabbedState(
            Mock.Of<ISelectedState>(),
            Mock.Of<IWireframeObjectManager>(),
            Mock.Of<IGumCursorState>());
    }

    [Fact]
    public void AccumulateTruePositionOffset_ShouldAccumulateAcrossMultipleCalls_ForComponent()
    {
        GrabbedState sut = CreateSut();

        sut.AccumulateTruePositionOffset(instance: null, deltaX: 1.5f, deltaY: 2f);
        sut.AccumulateTruePositionOffset(instance: null, deltaX: 0.5f, deltaY: -1f);

        sut.GetTruePositionOffset(instance: null).X.ShouldBe(2f);
        sut.GetTruePositionOffset(instance: null).Y.ShouldBe(1f);
    }

    [Fact]
    public void AccumulateTruePositionOffset_ShouldAccumulatePerInstance_Independently()
    {
        GrabbedState sut = CreateSut();
        InstanceSave instanceA = new InstanceSave { Name = "A" };
        InstanceSave instanceB = new InstanceSave { Name = "B" };

        sut.AccumulateTruePositionOffset(instanceA, deltaX: 3f, deltaY: 0f);
        sut.AccumulateTruePositionOffset(instanceB, deltaX: 10f, deltaY: 0f);
        sut.AccumulateTruePositionOffset(instanceA, deltaX: 2f, deltaY: 0f);

        sut.GetTruePositionOffset(instanceA).X.ShouldBe(5f);
        sut.GetTruePositionOffset(instanceB).X.ShouldBe(10f);
    }

    [Fact]
    public void GetTruePositionOffset_ShouldReturnZero_WhenInstanceNeverAccumulated()
    {
        GrabbedState sut = CreateSut();
        InstanceSave instance = new InstanceSave { Name = "Untouched" };

        sut.GetTruePositionOffset(instance).X.ShouldBe(0);
        sut.GetTruePositionOffset(instance).Y.ShouldBe(0);
    }

    [Fact]
    public void HandlePush_ShouldResetAllTrueOffsets()
    {
        GrabbedState sut = CreateSut();
        InstanceSave instance = new InstanceSave { Name = "A" };
        sut.AccumulateTruePositionOffset(null, 5f, 5f);
        sut.AccumulateTruePositionOffset(instance, 5f, 5f);
        sut.AccumulateTrueSizeOffset(null, 5f, 5f);
        sut.AccumulateTrueSizeOffset(instance, 5f, 5f);

        sut.HandlePush();

        sut.GetTruePositionOffset(null).X.ShouldBe(0);
        sut.GetTruePositionOffset(instance).X.ShouldBe(0);
        sut.GetTrueSizeOffset(null).X.ShouldBe(0);
        sut.GetTrueSizeOffset(instance).X.ShouldBe(0);
    }
}
