using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

// Issue found via manual test (FontResizeDemo sample): two adjacent, identical glyphs (e.g. "))")
// rendered with visibly different edges under font oversampling/window-zoom. Root cause:
// SpriteRenderer.Draw only pixel-snaps a sprite's DESTINATION ORIGIN when the effective scale
// (CurrentZoom * scale) is near a whole integer (isIntegerScale) -- a gate that exists to avoid
// DISTORTING non-integer-scaled sprite DIMENSIONS (see the large comment above SpriteRenderer.Draw).
// But origin-snapping doesn't need that guard for FONT glyphs (DimensionSnapping.DimensionSnapping):
// rounding a position to the nearest device pixel is safe there even when width/height stay
// unsnapped, since glyph cells don't need edge-to-edge continuity with unrelated neighbors.
//
// Regression found immediately after the first version of this fix: NineSlice pieces (and any
// other stacked/adjacent sprites) draw with DimensionSnapping.SideSnapping, which relies on BOTH
// position AND dimensions being snapped TOGETHER -- each piece's shared edge is computed by
// rounding the SAME underlying world coordinate on both sides, which only lines up when neither
// side is independently perturbed. Snapping position alone (leaving width/height raw) at
// non-integer scale broke that coincidence and opened gaps between NineSlice segments. The fix is
// scoped to DimensionSnapping.DimensionSnapping (font glyph draws) only -- SideSnapping draws keep
// the original coupled behavior (position snap requires isIntegerScale, same as before).
public class SpriteRendererPixelSnappingTests
{
    [Fact]
    public void ShouldSnapPosition_ForFontGlyphAtNonIntegerScale_ReturnsTrue()
    {
        bool saved = SpriteRenderer.SnapPositionEvenWhenScaled;
        try
        {
            SpriteRenderer.SnapPositionEvenWhenScaled = true;

            bool result = SpriteRenderer.ShouldSnapPosition(
                isRotationNearZero: true, offsetPixel: true, isIntegerScale: false,
                dimensionSnapping: DimensionSnapping.DimensionSnapping);

            result.ShouldBeTrue("because font glyph origins must snap to the pixel grid even at a non-integer effective scale -- that's the whole point of the fix");
        }
        finally
        {
            SpriteRenderer.SnapPositionEvenWhenScaled = saved;
        }
    }

    [Fact]
    public void ShouldSnapPosition_ForSideSnappingAtNonIntegerScale_ReturnsFalse()
    {
        bool saved = SpriteRenderer.SnapPositionEvenWhenScaled;
        try
        {
            SpriteRenderer.SnapPositionEvenWhenScaled = true;

            bool result = SpriteRenderer.ShouldSnapPosition(
                isRotationNearZero: true, offsetPixel: true, isIntegerScale: false,
                dimensionSnapping: DimensionSnapping.SideSnapping);

            result.ShouldBeFalse("because SideSnapping draws (NineSlice pieces, generic stacked sprites) rely on position and dimensions staying coupled -- snapping position alone at non-integer scale opens gaps between adjacent pieces");
        }
        finally
        {
            SpriteRenderer.SnapPositionEvenWhenScaled = saved;
        }
    }

    [Fact]
    public void ShouldSnapPosition_ForSideSnappingAtIntegerScale_ReturnsTrue()
    {
        bool saved = SpriteRenderer.SnapPositionEvenWhenScaled;
        try
        {
            SpriteRenderer.SnapPositionEvenWhenScaled = true;

            bool result = SpriteRenderer.ShouldSnapPosition(
                isRotationNearZero: true, offsetPixel: true, isIntegerScale: true,
                dimensionSnapping: DimensionSnapping.SideSnapping);

            result.ShouldBeTrue("because at integer scale, snapping both position and dimensions together is safe and unchanged from before the fix");
        }
        finally
        {
            SpriteRenderer.SnapPositionEvenWhenScaled = saved;
        }
    }

    [Fact]
    public void ShouldSnapPosition_WhenRotated_ReturnsFalse()
    {
        bool saved = SpriteRenderer.SnapPositionEvenWhenScaled;
        try
        {
            SpriteRenderer.SnapPositionEvenWhenScaled = true;

            bool result = SpriteRenderer.ShouldSnapPosition(
                isRotationNearZero: false, offsetPixel: true, isIntegerScale: true,
                dimensionSnapping: DimensionSnapping.DimensionSnapping);

            result.ShouldBeFalse("because a rotated sprite's screen-space axes no longer align with device pixels, so origin snapping does not apply");
        }
        finally
        {
            SpriteRenderer.SnapPositionEvenWhenScaled = saved;
        }
    }

    [Fact]
    public void ShouldSnapPosition_WhenOffsetPixelFalse_ReturnsFalse()
    {
        bool saved = SpriteRenderer.SnapPositionEvenWhenScaled;
        try
        {
            SpriteRenderer.SnapPositionEvenWhenScaled = true;

            bool result = SpriteRenderer.ShouldSnapPosition(
                isRotationNearZero: true, offsetPixel: false, isIntegerScale: true,
                dimensionSnapping: DimensionSnapping.DimensionSnapping);

            result.ShouldBeFalse("because the caller explicitly opted out of pixel offsetting for this draw call");
        }
        finally
        {
            SpriteRenderer.SnapPositionEvenWhenScaled = saved;
        }
    }

    [Fact]
    public void ShouldSnapPosition_ForFontGlyphAtNonIntegerScale_WhenToggleDisabled_ReturnsFalse()
    {
        bool saved = SpriteRenderer.SnapPositionEvenWhenScaled;
        try
        {
            SpriteRenderer.SnapPositionEvenWhenScaled = false;

            bool result = SpriteRenderer.ShouldSnapPosition(
                isRotationNearZero: true, offsetPixel: true, isIntegerScale: false,
                dimensionSnapping: DimensionSnapping.DimensionSnapping);

            result.ShouldBeFalse("because the static toggle must let callers reproduce the pre-fix (unsnapped) behavior for A/B comparison");
        }
        finally
        {
            SpriteRenderer.SnapPositionEvenWhenScaled = saved;
        }
    }
}
