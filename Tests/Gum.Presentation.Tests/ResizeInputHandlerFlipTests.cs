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
/// </summary>
public class ResizeInputHandlerFlipTests
{
    [Fact]
    public void ResolveResizeAxis_ShouldReturnZeroDeltas_WhenSizeMultiplierIsZero()
    {
        ResolveResizeAxis(
            grabStartPositionAxis: 5, grabStartSizeAxis: 25, trueSizeOffsetSinceGrabAxis: -999,
            sizeMultiplier: 0, originRatio: 0,
            livePositionAxis: 5, liveSizeAxis: 25,
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
            grabStartPositionAxis: 5, grabStartSizeAxis: 25, trueSizeOffsetSinceGrabAxis: -10,
            sizeMultiplier: 1, originRatio: 0,
            livePositionAxis: 5, liveSizeAxis: 25,
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
            grabStartPositionAxis: 5, grabStartSizeAxis: 25, trueSizeOffsetSinceGrabAxis: -25,
            sizeMultiplier: 1, originRatio: 0,
            livePositionAxis: 5, liveSizeAxis: 25,
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
            grabStartPositionAxis: 5, grabStartSizeAxis: 20, trueSizeOffsetSinceGrabAxis: -30,
            sizeMultiplier: 1, originRatio: 0,
            livePositionAxis: 5, liveSizeAxis: 20,
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
            grabStartPositionAxis: 5, grabStartSizeAxis: 20, trueSizeOffsetSinceGrabAxis: -30,
            sizeMultiplier: -1, originRatio: 0,
            livePositionAxis: 5, liveSizeAxis: 20,
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
            grabStartPositionAxis: 15, grabStartSizeAxis: 20, trueSizeOffsetSinceGrabAxis: -30,
            sizeMultiplier: 1, originRatio: 0.5f,
            livePositionAxis: 15, liveSizeAxis: 20,
            out float positionDelta, out float sizeDelta);

        positionDelta.ShouldBe(-15); // Center: 15 -> 0
        sizeDelta.ShouldBe(-10); // Width: 20 -> 10
    }

    [Fact]
    public void ResolveResizeAxis_ShouldStayIdempotent_OnceFlippedAndStable_AcrossConsecutiveTicks()
    {
        // Regression shape for the grid-snap "keep anchor stable across ticks" tests: once a flip
        // has resolved, feeding the SAME grab-time values and the SAME accumulated true offset (no
        // further cursor movement) back in as the next tick's live values must produce zero further
        // delta - proving the flip doesn't jitter/re-trigger every frame.
        const float grabStartX = 5;
        const float grabStartWidth = 20;
        const float trueSizeOffsetSinceGrab = -25; // crosses the anchor by 5

        ResolveResizeAxis(
            grabStartPositionAxis: grabStartX, grabStartSizeAxis: grabStartWidth,
            trueSizeOffsetSinceGrabAxis: trueSizeOffsetSinceGrab,
            sizeMultiplier: 1, originRatio: 0,
            livePositionAxis: grabStartX, liveSizeAxis: grabStartWidth,
            out float positionDelta1, out float sizeDelta1);

        float liveX2 = grabStartX + positionDelta1;
        float liveWidth2 = grabStartWidth + sizeDelta1;

        ResolveResizeAxis(
            grabStartPositionAxis: grabStartX, grabStartSizeAxis: grabStartWidth,
            trueSizeOffsetSinceGrabAxis: trueSizeOffsetSinceGrab, // no further cursor movement
            sizeMultiplier: 1, originRatio: 0,
            livePositionAxis: liveX2, liveSizeAxis: liveWidth2,
            out float positionDelta2, out float sizeDelta2);

        positionDelta2.ShouldBe(0);
        sizeDelta2.ShouldBe(0);
    }
}
