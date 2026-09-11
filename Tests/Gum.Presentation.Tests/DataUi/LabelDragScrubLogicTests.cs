using Shouldly;
using WpfDataUi.Controls;

namespace Gum.Presentation.Tests.DataUi;

public class LabelDragScrubLogicTests
{
    [Fact]
    public void ApplyDelta_AccumulatesSnapsAndClamps()
    {
        LabelDragScrubLogic logic = new LabelDragScrubLogic();
        logic.Begin(10f, typeof(float));

        // 1px resolution: fractional deltas accumulate and snap to whole numbers.
        logic.ApplyDelta(0.4, changeMultiplier: 1m, rounding: 1m, min: null, max: null).ShouldBe(10);
        logic.ApplyDelta(0.4, changeMultiplier: 1m, rounding: 1m, min: null, max: null).ShouldBe(11);

        // The floor holds while dragging further down.
        logic.ApplyDelta(-50, changeMultiplier: 1m, rounding: 1m, min: 0m, max: null).ShouldBe(0);
    }

    [Fact]
    public void Begin_NullValue_StartsAtZero_AndMultiplierScalesDeltas()
    {
        LabelDragScrubLogic logic = new LabelDragScrubLogic();
        logic.Begin(null, typeof(float));

        logic.ApplyDelta(3, changeMultiplier: .02m, rounding: .01m, min: null, max: null).ShouldBe(0.06, 0.0001);
    }
}
