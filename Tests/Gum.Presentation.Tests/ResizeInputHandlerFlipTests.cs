using Shouldly;
using static Gum.Wireframe.Editors.Handlers.ResizeInputHandler;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins ResizeInputHandler.ResolveResizeAxis - the per-axis math backing the "no negative
/// Width/Height, flip the anchor instead" decision on #4385. Width/Height are resolved from a
/// grab-time ANCHOR edge (invariant for the whole gesture) and a DRAGGED edge whose raw, never-
/// clamped position is anchor +/- the true (unclamped) size accumulated since grab. Taking
/// Math.Min/Max of those two edges - rather than assuming the dragged edge is always the min or
/// always the max, as the pre-#4385 code did - makes crossing the anchor "just work": the
/// previously-dragged edge becomes the new anchor-side edge with no special-cased branch.
///
/// Each call resolves ONE tick's delta from the accumulated true size offset immediately before
/// and after that tick (not from a "live" position/size) - see ResolveResizeAxis's own doc comment
/// for why the live representation can't be used as the "before" reference once rotation is
/// involved (ResizeInputHandlerFlipRotationTests covers that composition end to end).
/// </summary>
public class ResizeInputHandlerFlipTests
{
    [Fact]
    public void ResolveResizeAxis_ShouldReturnZeroDeltas_WhenSizeMultiplierIsZero()
    {
        ResolveResizeAxis(
            grabStartPositionAxis: 5, grabStartSizeAxis: 25,
            trueSizeOffsetBeforeAxis: -500, trueSizeOffsetAfterAxis: -999,
            sizeMultiplier: 0, originRatio: 0,
            out float positionDelta, out float sizeDelta);

        positionDelta.ShouldBe(0);
        sizeDelta.ShouldBe(0);
    }

    [Fact]
    public void ResolveResizeAxis_ShouldShrinkNormally_WhenNotCrossingTheAnchor()
    {
        // Left-origin box (originRatio 0), Right handle (sizeMultiplier > 0): Left is the anchor
        // and never moves. Shrinking by 10 (20 -> without crossing) should not touch position.
        ResolveResizeAxis(
            grabStartPositionAxis: 5, grabStartSizeAxis: 25,
            trueSizeOffsetBeforeAxis: 0, trueSizeOffsetAfterAxis: -10,
            sizeMultiplier: 1, originRatio: 0,
            out float positionDelta, out float sizeDelta);

        positionDelta.ShouldBe(0);
        sizeDelta.ShouldBe(-10);
    }

    [Fact]
    public void ResolveResizeAxis_ShouldAllowExactlyZeroSize_WithoutFlipping()
    {
        // Shrinking exactly down to the anchor (Width -> 0) is a legitimate stopping point - e.g.
        // an animation that grows a Width from 0 - not something that should trigger a flip.
        ResolveResizeAxis(
            grabStartPositionAxis: 5, grabStartSizeAxis: 25,
            trueSizeOffsetBeforeAxis: 0, trueSizeOffsetAfterAxis: -25,
            sizeMultiplier: 1, originRatio: 0,
            out float positionDelta, out float sizeDelta);

        positionDelta.ShouldBe(0);
        sizeDelta.ShouldBe(-25); // new size is exactly 0
    }

    [Fact]
    public void ResolveResizeAxis_ShouldFlipAnchor_WhenRightHandleDraggedPastLeftOrigin()
    {
        // Left-origin box, Left=5, Width=20 (Right=25). Right handle dragged left by a total of 30
        // - 10 further than the Left anchor. The dragged (Right) edge's raw target is 25-30=-5,
        // which is left of the anchor (5), so the box should flip: new Left=-5 (the overshot
        // dragged edge), new Right=5 (the old anchor), Width=10.
        ResolveResizeAxis(
            grabStartPositionAxis: 5, grabStartSizeAxis: 20,
            trueSizeOffsetBeforeAxis: 0, trueSizeOffsetAfterAxis: -30,
            sizeMultiplier: 1, originRatio: 0,
            out float positionDelta, out float sizeDelta);

        positionDelta.ShouldBe(-10); // X: 5 -> -5
        sizeDelta.ShouldBe(-10); // Width: 20 -> 10
    }

    [Fact]
    public void ResolveResizeAxis_ShouldFlipAnchor_WhenLeftHandleDraggedPastRightAnchor()
    {
        // Mirror of the above: Left-origin box, Left(X)=5, Width=20 (Right anchor=25). Left handle
        // (sizeMultiplier < 0) dragged right by a total of 30 - past the Right anchor. The dragged
        // (Left) edge's raw target is 5+30=35, right of the anchor (25), so the box flips: new
        // Left=25 (the old anchor), new Right=35 (the overshot dragged edge), Width=10.
        ResolveResizeAxis(
            grabStartPositionAxis: 5, grabStartSizeAxis: 20,
            trueSizeOffsetBeforeAxis: 0, trueSizeOffsetAfterAxis: -30,
            sizeMultiplier: -1, originRatio: 0,
            out float positionDelta, out float sizeDelta);

        positionDelta.ShouldBe(20); // X: 5 -> 25
        sizeDelta.ShouldBe(-10); // Width: 20 -> 10
    }

    [Fact]
    public void ResolveResizeAxis_ShouldFlipAnchor_ForCenterOrigin()
    {
        // Center-origin box (originRatio .5): X tracks the center. Grabbed at Center(X)=15,
        // Width=20 -> Left=5, Right=25. Right handle dragged left by a total of 30, overshooting
        // the Left anchor (5) down to a raw target of 25-30=-5. Flips to Left=-5, Right=5,
        // Width=10, new Center=(−5+5)/2=0.
        ResolveResizeAxis(
            grabStartPositionAxis: 15, grabStartSizeAxis: 20,
            trueSizeOffsetBeforeAxis: 0, trueSizeOffsetAfterAxis: -30,
            sizeMultiplier: 1, originRatio: 0.5f,
            out float positionDelta, out float sizeDelta);

        positionDelta.ShouldBe(-15); // Center: 15 -> 0
        sizeDelta.ShouldBe(-10); // Width: 20 -> 10
    }

    [Fact]
    public void ResolveResizeAxis_ShouldSumToSameResult_WhenSplitAcrossConsecutiveTicks()
    {
        // The delta for tick N is resolved from grab-time state alone (before/after that tick's own
        // accumulated offset) rather than by diffing against a live position/size. Two ticks that
        // together cross the anchor (0 -> -15, then -15 -> -30) must sum to exactly the same
        // position/size delta as a single tick covering the same total range (0 -> -30) - this is
        // what keeps per-tick deltas correct once they get rotated and applied to a rotated object's
        // X/Y (ResizeInputHandlerFlipRotationTests), where the live X/Y can no longer serve as that
        // reference after the first tick.
        ResolveResizeAxis(
            grabStartPositionAxis: 5, grabStartSizeAxis: 20,
            trueSizeOffsetBeforeAxis: 0, trueSizeOffsetAfterAxis: -15,
            sizeMultiplier: 1, originRatio: 0,
            out float positionDelta1, out float sizeDelta1);

        ResolveResizeAxis(
            grabStartPositionAxis: 5, grabStartSizeAxis: 20,
            trueSizeOffsetBeforeAxis: -15, trueSizeOffsetAfterAxis: -30,
            sizeMultiplier: 1, originRatio: 0,
            out float positionDelta2, out float sizeDelta2);

        ResolveResizeAxis(
            grabStartPositionAxis: 5, grabStartSizeAxis: 20,
            trueSizeOffsetBeforeAxis: 0, trueSizeOffsetAfterAxis: -30,
            sizeMultiplier: 1, originRatio: 0,
            out float positionDeltaOneTick, out float sizeDeltaOneTick);

        (positionDelta1 + positionDelta2).ShouldBe(positionDeltaOneTick);
        (sizeDelta1 + sizeDelta2).ShouldBe(sizeDeltaOneTick);
    }

    [Fact]
    public void ResolveResizeAxis_ShouldReturnZeroDeltas_WhenOffsetUnchangedSinceLastTick()
    {
        // No further cursor movement this tick (before == after) must be a true no-op, even once
        // already flipped past the anchor.
        ResolveResizeAxis(
            grabStartPositionAxis: 5, grabStartSizeAxis: 20,
            trueSizeOffsetBeforeAxis: -25, trueSizeOffsetAfterAxis: -25,
            sizeMultiplier: 1, originRatio: 0,
            out float positionDelta, out float sizeDelta);

        positionDelta.ShouldBe(0);
        sizeDelta.ShouldBe(0);
    }
}
