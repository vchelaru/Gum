using Apos.Shapes;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace MonoGameAndGum.Renderables;

/// <summary>
/// Draws an SVG document through Apos.Shapes' <see cref="ShapeBatch.DrawSvg(ShapeSvg, Vector2, float, float, Vector2, float)"/>.
/// Every filled element is solved from its own curves in the pixel shader and strokes go through
/// the path renderer, so the drawing stays exact at any size, rotation, and zoom, and it stays
/// inside the batch's single draw call.
/// </summary>
/// <remarks>
/// Derives from <see cref="RenderableShapeBase"/> for its batch participation — the "Apos.Shapes"
/// <c>BatchKey</c>, <c>StartBatch</c>/<c>EndBatch</c>, and blend handling — and because
/// <see cref="ShapeRenderer.EnsureBlend"/> takes a <see cref="RenderableShapeBase"/>. The shape
/// properties that comes with (stroke, corner radii, gradient, drop shadow) do not apply to a
/// document that carries its own paint and are ignored here, the same way <see cref="Line"/>
/// ignores corner radii.
/// </remarks>
internal class Svg : RenderableShapeBase, IAspectRatio
{
    /// <summary>
    /// The loaded document to draw, or <c>null</c> to draw nothing. Obtained from
    /// <see cref="Content.ShapeSvgLoader.Load(string)"/> so it is shared and cached; one document
    /// can back any number of renderables and batches.
    /// </summary>
    public ShapeSvg? Document { get; set; }

    /// <summary>
    /// Width over height of the document's viewBox, or 1 when no document is loaded. Read by
    /// layout for <c>DimensionUnitType.MaintainFileAspectRatio</c>, which is
    /// <see cref="Gum.GueDeriving.SvgRuntime"/>'s default for height.
    /// </summary>
    public float AspectRatio
    {
        get
        {
            if (Document == null || Document.Height == 0)
            {
                return 1;
            }
            return Document.Width / Document.Height;
        }
    }

    public override void Render(ISystemManagers managers)
    {
        if (Document == null)
        {
            return;
        }

        // Issue #2937 - re-open the shared ShapeBatch with this renderable's blend if it differs.
        ShapeRenderer.EnsureBlend(this);

        var sb = ShapeRenderer.ShapeBatch;

        // DrawSvg's `size` is one em in world units, and one em is the viewBox's HEIGHT - so this
        // is a uniform scale driven by Height, and the drawn width follows the file's aspect ratio.
        //
        // Height-dominant is the accepted behavior here, NOT a match for SkiaGum's VectorSprite,
        // which computes scaleX and scaleY independently and squashes the drawing to fill its box.
        // When Width and Height disagree with the file's ratio this draws wider than the Width it
        // reports to layout, so it can overrun a sibling. Tracked in issue #4509 along with the two
        // ways out: scaling the batch through ShapeBatch.Begin's view matrix (Skia parity, at two
        // batch flushes per stretched SVG), or a Vector2-size DrawSvg upstream. The common case is
        // unaffected - SvgRuntime defaults to MaintainFileAspectRatio, where the dimensions already
        // agree and both backends match.
        //
        // `rotation` turns around `position` and `origin` is the pivot measured out from the top
        // left, so passing Vector2.Zero rotates around the top-left corner - Gum's own convention.
        // That is why this needs no AdjustPositionForCenterRotation, unlike the center-rotating
        // DrawRectangle/DrawCircle paths.
        sb.DrawSvg(
            Document,
            new Vector2(this.GetAbsoluteLeft(), this.GetAbsoluteTop()),
            Height,
            rotation: MathHelper.ToRadians(-this.GetAbsoluteRotation()),
            origin: Vector2.Zero,
            aaSize: IsAntialiased ? 1 : 0);
    }
}
