using Shouldly;
using static Gum.Wireframe.Editors.Handlers.ResizeInputHandler;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins ResizeInputHandler.GetDifferenceToGridForResizeAxis, the per-axis math behind resize
/// grid-snap (issue #4137, refined by #4380). It resolves position (X/Y) and size (Width/Height)
/// together from one pair of edges instead of two independent computations: the anchor edge (the
/// one NOT being dragged) is pinned to its position AT GRAB TIME - frame-invariant by construction
/// - and only the dragged edge (identified by sizeMultiplier's sign) is snapped to the nearest
/// world-space grid line. Position is then re-derived from the resolved edges via originRatio,
/// rather than snapped independently.
///
/// An earlier version snapped X/Y and Width/Height independently, each re-deriving the anchor from
/// "live" (already-partially-snapped) state every tick. That let the two computations disagree and
/// drift: dragging the Top handle, the Bottom edge (meant to stay fixed) would visibly "snap around"
/// frame to frame instead of staying put - see the two-frame stability tests below.
/// </summary>
public class ResizeInputHandlerGridSnapTests
{
    [Fact]
    public void GetDifferenceToGridForResizeAxis_ShouldSkipAxis_WhenSizeMultiplierIsZero()
    {
        GetDifferenceToGridForResizeAxis(gridSize: 16,
            grabStartPositionAxis: 5, grabStartSizeAxis: 25, trueSizeOffsetSinceGrabAxis: 0,
            sizeMultiplier: 0, originRatio: 0,
            positionIsPixelBased: true, sizeIsPixelBased: true,
            liveAbsoluteMin: 5, livePositionAxis: 5, liveSizeAxis: 25,
            out float differenceToGridPosition, out float differenceToGridSize);

        differenceToGridPosition.ShouldBe(0);
        differenceToGridSize.ShouldBe(0);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void GetDifferenceToGridForResizeAxis_ShouldSkipAxis_WhenEitherUnitIsNotPixelBased(
        bool positionIsPixelBased, bool sizeIsPixelBased)
    {
        GetDifferenceToGridForResizeAxis(gridSize: 16,
            grabStartPositionAxis: 5, grabStartSizeAxis: 25, trueSizeOffsetSinceGrabAxis: 0,
            sizeMultiplier: 1, originRatio: 0,
            positionIsPixelBased: positionIsPixelBased, sizeIsPixelBased: sizeIsPixelBased,
            liveAbsoluteMin: 5, livePositionAxis: 5, liveSizeAxis: 25,
            out float differenceToGridPosition, out float differenceToGridSize);

        differenceToGridPosition.ShouldBe(0);
        differenceToGridSize.ShouldBe(0);
    }

    [Fact]
    public void GetDifferenceToGridForResizeAxis_ShouldSnapDraggedMaxEdge_WhenAnchorMinIsOffGrid()
    {
        // Left-origin box (originRatio 0), Right handle dragged (sizeMultiplier > 0). Anchor (Left)
        // sits at 5, off the 16px grid. True width (no snap ever applied) is 25, so the true
        // (unsnapped) Right edge is 5+25=30 -> nearest grid line 32, giving a width of 27.
        GetDifferenceToGridForResizeAxis(gridSize: 16,
            grabStartPositionAxis: 5, grabStartSizeAxis: 25, trueSizeOffsetSinceGrabAxis: 0,
            sizeMultiplier: 1, originRatio: 0,
            positionIsPixelBased: true, sizeIsPixelBased: true,
            liveAbsoluteMin: 5, livePositionAxis: 5, liveSizeAxis: 25,
            out float differenceToGridPosition, out float differenceToGridSize);

        differenceToGridPosition.ShouldBe(0); // Left (anchor) never moves
        differenceToGridSize.ShouldBe(2); // 25 -> 27, so Right lands at 5+27=32
    }

    [Fact]
    public void GetDifferenceToGridForResizeAxis_ShouldSnapDraggedMinEdge_WhenAnchorMaxIsOffGrid()
    {
        // Left-origin box (originRatio 0), Left handle dragged (sizeMultiplier < 0). Anchor (Right)
        // sits at 15+25=40, off the 16px grid. True (unsnapped) Left edge is 40-25=15 -> nearest
        // grid line 16, giving a width of 24 and moving Left/X from 15 to 16.
        GetDifferenceToGridForResizeAxis(gridSize: 16,
            grabStartPositionAxis: 15, grabStartSizeAxis: 25, trueSizeOffsetSinceGrabAxis: 0,
            sizeMultiplier: -1, originRatio: 0,
            positionIsPixelBased: true, sizeIsPixelBased: true,
            liveAbsoluteMin: 15, livePositionAxis: 15, liveSizeAxis: 25,
            out float differenceToGridPosition, out float differenceToGridSize);

        differenceToGridPosition.ShouldBe(1); // X: 15 -> 16
        differenceToGridSize.ShouldBe(-1); // Width: 25 -> 24, so Right (anchor) stays at 40
    }

    [Fact]
    public void GetDifferenceToGridForResizeAxis_ShouldResolveBothEdges_ForCenterOrigin()
    {
        // Center-origin box (originRatio .5): X tracks the center, not either edge. Grabbed at
        // Center=15, Width=20 -> Left=5, Right=25. Left handle dragged (sizeMultiplier < 0); Right
        // (anchor) stays fixed at 25. True Left is unchanged (5) -> nearest grid line (16) is 0,
        // giving a new Width of 25, and a new Center of 0 + 25/2 = 12.5.
        GetDifferenceToGridForResizeAxis(gridSize: 16,
            grabStartPositionAxis: 15, grabStartSizeAxis: 20, trueSizeOffsetSinceGrabAxis: 0,
            sizeMultiplier: -1, originRatio: 0.5f,
            positionIsPixelBased: true, sizeIsPixelBased: true,
            liveAbsoluteMin: 5, livePositionAxis: 15, liveSizeAxis: 20,
            out float differenceToGridPosition, out float differenceToGridSize);

        differenceToGridPosition.ShouldBe(-2.5f); // Center: 15 -> 12.5
        differenceToGridSize.ShouldBe(5); // Width: 20 -> 25, so Right (anchor) stays at 25
    }

    [Fact]
    public void GetDifferenceToGridForResizeAxis_ShouldKeepMaxAnchorStable_AcrossConsecutiveDragTicks()
    {
        // Regression for "grab the handle at the top and move it around - the bottom doesn't stay
        // fixed, it snaps around". Top-origin box (originRatio 0) grabbed at Y=5, Height=20 (so
        // Bottom=25). Dragging the Top handle (sizeMultiplier < 0) across two ticks must keep the
        // Bottom anchor at exactly 25 both times, and once frame 1's result is stable, frame 2 (fed
        // frame 1's applied Y/Height as its "live" values) must compute a zero further delta -
        // proving the two ticks don't fight each other.
        const float gridSize = 16;
        const float grabStartY = 5;
        const float grabStartHeight = 20;

        // Frame 1: true (unsnapped) height has shrunk from 20 to 17 (Top dragged down by 3).
        GetDifferenceToGridForResizeAxis(gridSize,
            grabStartPositionAxis: grabStartY, grabStartSizeAxis: grabStartHeight, trueSizeOffsetSinceGrabAxis: -3,
            sizeMultiplier: -1, originRatio: 0,
            positionIsPixelBased: true, sizeIsPixelBased: true,
            liveAbsoluteMin: grabStartY, livePositionAxis: grabStartY, liveSizeAxis: grabStartHeight,
            out float diffPosition1, out float diffSize1);

        float liveY2 = grabStartY + diffPosition1;
        float liveHeight2 = grabStartHeight + diffSize1;

        (liveY2 + liveHeight2).ShouldBe(25); // Bottom after frame 1

        // Frame 2: true height shrinks further to 15, but the snapped target doesn't change.
        GetDifferenceToGridForResizeAxis(gridSize,
            grabStartPositionAxis: grabStartY, grabStartSizeAxis: grabStartHeight, trueSizeOffsetSinceGrabAxis: -5,
            sizeMultiplier: -1, originRatio: 0,
            positionIsPixelBased: true, sizeIsPixelBased: true,
            liveAbsoluteMin: liveY2, livePositionAxis: liveY2, liveSizeAxis: liveHeight2,
            out float diffPosition2, out float diffSize2);

        diffPosition2.ShouldBe(0); // Already stable - no further jitter
        diffSize2.ShouldBe(0);
        (liveY2 + diffPosition2 + liveHeight2 + diffSize2).ShouldBe(25); // Bottom still exactly 25
    }

    [Fact]
    public void GetDifferenceToGridForResizeAxis_ShouldKeepMinAnchorStable_AcrossConsecutiveDragTicks()
    {
        // Mirrors the Top/Bottom regression on the X axis: dragging the Right handle must keep the
        // Left anchor fixed across ticks (the user's "probably same with left vs right").
        const float gridSize = 16;
        const float grabStartX = 5;
        const float grabStartWidth = 20;

        GetDifferenceToGridForResizeAxis(gridSize,
            grabStartPositionAxis: grabStartX, grabStartSizeAxis: grabStartWidth, trueSizeOffsetSinceGrabAxis: 3,
            sizeMultiplier: 1, originRatio: 0,
            positionIsPixelBased: true, sizeIsPixelBased: true,
            liveAbsoluteMin: grabStartX, livePositionAxis: grabStartX, liveSizeAxis: grabStartWidth,
            out float diffPosition1, out float diffSize1);

        diffPosition1.ShouldBe(0); // Left (anchor) never moves

        float liveX2 = grabStartX + diffPosition1;
        float liveWidth2 = grabStartWidth + diffSize1;

        GetDifferenceToGridForResizeAxis(gridSize,
            grabStartPositionAxis: grabStartX, grabStartSizeAxis: grabStartWidth, trueSizeOffsetSinceGrabAxis: 5,
            sizeMultiplier: 1, originRatio: 0,
            positionIsPixelBased: true, sizeIsPixelBased: true,
            liveAbsoluteMin: liveX2, livePositionAxis: liveX2, liveSizeAxis: liveWidth2,
            out float diffPosition2, out float diffSize2);

        diffPosition2.ShouldBe(0); // Still never moves
    }

    [Fact]
    public void GetDifferenceToGridForResizeAxis_ShouldFlipAnchor_WhenDraggedPastAnchor()
    {
        // Left-origin box, Left=5, Width=20 (Right=25). Right handle dragged left by a total of
        // 30 - the dragged (Right) edge's raw target is 25-30=-5, past the Left anchor (5).
        // Un-snapped this flips to Left=-5, Right=5, Width=10 (see the equivalent unsnapped case in
        // ResizeInputHandlerFlipTests); snapping the dragged edge (-5) to the 16px grid rounds it to
        // 0, giving Left=0, Right=5 (anchor), Width=5.
        GetDifferenceToGridForResizeAxis(gridSize: 16,
            grabStartPositionAxis: 5, grabStartSizeAxis: 20, trueSizeOffsetSinceGrabAxis: -30,
            sizeMultiplier: 1, originRatio: 0,
            positionIsPixelBased: true, sizeIsPixelBased: true,
            liveAbsoluteMin: 5, livePositionAxis: 5, liveSizeAxis: 20,
            out float differenceToGridPosition, out float differenceToGridSize);

        differenceToGridPosition.ShouldBe(-5); // X: 5 -> 0
        differenceToGridSize.ShouldBe(-15); // Width: 20 -> 5
    }
}
