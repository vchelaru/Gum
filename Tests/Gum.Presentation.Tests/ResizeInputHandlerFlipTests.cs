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

/// <summary>
/// Pins ResizeInputHandler.ResolveResizeAxisFromCenter - the Resize From Center counterpart to
/// <see cref="ResizeInputHandlerFlipTests"/> (#4390). Resize From Center grows/shrinks
/// symmetrically around a fixed CENTER point rather than a fixed edge, so there is no "other" edge
/// to flip against the way the plain edge-anchored resize does. Instead: Width/Height is always the
/// absolute value of the true (signed, never-clamped) size accumulated since grab, and position is
/// re-derived each tick from the grab-time center - which itself never moves - and the new size.
/// </summary>
public class ResizeInputHandlerFlipFromCenterTests
{
    [Fact]
    public void ResolveResizeAxisFromCenter_ShouldGrowSymmetrically_WhenNotCrossingZero()
    {
        // Left-origin box (originRatio 0): Left(X)=10, Width=20 -> Center=20. A single tick that
        // grows the true size offset by 10 (Width: 20 -> 30) should keep Center fixed at 20, moving
        // Left from 10 to 5 (half the growth on each side).
        ResolveResizeAxisFromCenter(
            grabStartPositionAxis: 10, grabStartSizeAxis: 20,
            trueSizeOffsetBeforeAxis: 0, trueSizeOffsetAfterAxis: 10,
            originRatio: 0,
            out float positionDelta, out float sizeDelta);

        positionDelta.ShouldBe(-5); // X: 10 -> 5
        sizeDelta.ShouldBe(10); // Width: 20 -> 30
    }

    [Fact]
    public void ResolveResizeAxisFromCenter_ShouldAllowExactlyZeroSize_WithoutFlipping()
    {
        // Shrinking exactly down to the center (Width -> 0) is a legitimate stopping point, not a
        // flip trigger.
        ResolveResizeAxisFromCenter(
            grabStartPositionAxis: 10, grabStartSizeAxis: 20,
            trueSizeOffsetBeforeAxis: 0, trueSizeOffsetAfterAxis: -20,
            originRatio: 0,
            out float positionDelta, out float sizeDelta);

        positionDelta.ShouldBe(10); // X: 10 -> 20 (Center stays at 20, Width is 0)
        sizeDelta.ShouldBe(-20); // Width: 20 -> 0
    }

    [Fact]
    public void ResolveResizeAxisFromCenter_ShouldFlip_WhenShrinkingPastZero()
    {
        // Left-origin box: Left(X)=10, Width=20 -> Center=20. Shrinking the true size offset past
        // -20 (the point Width hits 0) continues symmetric growth in the "flipped" sense - Width is
        // the magnitude of the true (signed) size - while Center stays fixed at 20.
        ResolveResizeAxisFromCenter(
            grabStartPositionAxis: 10, grabStartSizeAxis: 20,
            trueSizeOffsetBeforeAxis: 0, trueSizeOffsetAfterAxis: -60,
            originRatio: 0,
            out float positionDelta, out float sizeDelta);

        positionDelta.ShouldBe(-10); // X: 10 -> 0 (Left=0, Right=40, Center=20)
        sizeDelta.ShouldBe(20); // Width: 20 -> 40
    }

    [Fact]
    public void ResolveResizeAxisFromCenter_ShouldKeepCenterFixed_ForCenterOrigin()
    {
        // Center-origin box (originRatio .5): X already tracks the center directly, so it must
        // never move regardless of how far the size offset crosses zero.
        ResolveResizeAxisFromCenter(
            grabStartPositionAxis: 20, grabStartSizeAxis: 20,
            trueSizeOffsetBeforeAxis: 0, trueSizeOffsetAfterAxis: -60,
            originRatio: 0.5f,
            out float positionDelta, out float sizeDelta);

        positionDelta.ShouldBe(0); // Center-origin X is already the center - stays at 20
        sizeDelta.ShouldBe(20); // Width: 20 -> 40
    }

    [Fact]
    public void ResolveResizeAxisFromCenter_ShouldSumToSameResult_WhenSplitAcrossConsecutiveTicks()
    {
        // Same per-tick-from-grab-state invariant as ResolveResizeAxis: two ticks that together
        // cross zero (0 -> -30, then -30 -> -60) must sum to the same delta as one tick covering the
        // same total range (0 -> -60).
        ResolveResizeAxisFromCenter(
            grabStartPositionAxis: 10, grabStartSizeAxis: 20,
            trueSizeOffsetBeforeAxis: 0, trueSizeOffsetAfterAxis: -30,
            originRatio: 0,
            out float positionDelta1, out float sizeDelta1);

        ResolveResizeAxisFromCenter(
            grabStartPositionAxis: 10, grabStartSizeAxis: 20,
            trueSizeOffsetBeforeAxis: -30, trueSizeOffsetAfterAxis: -60,
            originRatio: 0,
            out float positionDelta2, out float sizeDelta2);

        ResolveResizeAxisFromCenter(
            grabStartPositionAxis: 10, grabStartSizeAxis: 20,
            trueSizeOffsetBeforeAxis: 0, trueSizeOffsetAfterAxis: -60,
            originRatio: 0,
            out float positionDeltaOneTick, out float sizeDeltaOneTick);

        (positionDelta1 + positionDelta2).ShouldBe(positionDeltaOneTick);
        (sizeDelta1 + sizeDelta2).ShouldBe(sizeDeltaOneTick);
    }

    [Fact]
    public void ResolveResizeAxisFromCenter_ShouldReturnZeroDeltas_WhenOffsetUnchangedSinceLastTick()
    {
        ResolveResizeAxisFromCenter(
            grabStartPositionAxis: 10, grabStartSizeAxis: 20,
            trueSizeOffsetBeforeAxis: -60, trueSizeOffsetAfterAxis: -60,
            originRatio: 0,
            out float positionDelta, out float sizeDelta);

        positionDelta.ShouldBe(0);
        sizeDelta.ShouldBe(0);
    }
}
