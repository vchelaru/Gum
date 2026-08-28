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

    /// <summary>
    /// Half a world unit of disagreement between the drawn width and the element's Width is
    /// treated as none, so float noise doesn't cost two batch flushes. Not scaled by camera zoom -
    /// a zoomed-in camera can magnify a skipped mismatch to a visible pixel or two.
    /// </summary>
    internal const float NonUniformScaleTolerance = 0.5f;

    /// <summary>
    /// Whether the width Apos.Shapes draws from the height differs visibly from the width the
    /// element reports to layout. False for the <c>MaintainFileAspectRatio</c> default and for
    /// degenerate dimensions, so the stretch path costs nothing in the common case.
    /// </summary>
    internal static bool RequiresNonUniformScale(float width, float height, float documentAspectRatio)
    {
        if (width <= 0 || height <= 0 || documentAspectRatio <= 0)
        {
            return false;
        }

        float drawnWidth = height * documentAspectRatio;

        return System.Math.Abs(width - drawnWidth) > NonUniformScaleTolerance;
    }

    /// <summary>
    /// The transform that turns Apos.Shapes' height-driven uniform scale into the non-uniform one
    /// SkiaGum's <c>VectorSprite</c> produces, for use as a pre-multiplied view matrix.
    /// </summary>
    /// <remarks>
    /// Apos maps a point of the drawing to <c>T(topLeft) * R(rotation) * S(height, height)</c>;
    /// Skia composes <c>T * R * S(scaleX, scaleY)</c>. The heights already agree, so only x needs
    /// correcting, and conjugating the x scale by the element's own rotation and position stretches
    /// along the element's local axis rather than shearing a rotated drawing.
    /// </remarks>
    internal static Matrix CreateNonUniformScaleMatrix(float width, float height,
        float documentAspectRatio, Vector2 topLeft, float rotationRadians)
    {
        float horizontalScale = width / (height * documentAspectRatio);

        return Matrix.CreateTranslation(-topLeft.X, -topLeft.Y, 0)
            * Matrix.CreateRotationZ(-rotationRadians)
            * Matrix.CreateScale(horizontalScale, 1, 1)
            * Matrix.CreateRotationZ(rotationRadians)
            * Matrix.CreateTranslation(topLeft.X, topLeft.Y, 0);
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

        var topLeft = new Vector2(this.GetAbsoluteLeft(), this.GetAbsoluteTop());
        var rotationRadians = MathHelper.ToRadians(-this.GetAbsoluteRotation());

        // Issue #4509 - DrawSvg's `size` is one em in world units, and one em is the viewBox's
        // HEIGHT, so the drawn width always follows the file's aspect ratio. When Width disagrees,
        // the batch is re-opened with a view matrix that stretches x, matching SkiaGum's
        // VectorSprite (which scales x and y independently) including its effect on strokes, whose
        // pen becomes elliptical. That costs two batch flushes, so it only happens on a real
        // mismatch - the MaintainFileAspectRatio default never pays for it.
        var requiresNonUniformScale = RequiresNonUniformScale(Width, Height, AspectRatio);

        if (requiresNonUniformScale)
        {
            ShapeRenderer.PushView(
                CreateNonUniformScaleMatrix(Width, Height, AspectRatio, topLeft, rotationRadians));
        }

        // `rotation` turns around `position` and `origin` is the pivot measured out from the top
        // left, so passing Vector2.Zero rotates around the top-left corner - Gum's own convention.
        // That is why this needs no AdjustPositionForCenterRotation, unlike the center-rotating
        // DrawRectangle/DrawCircle paths.
        sb.DrawSvg(
            Document,
            topLeft,
            Height,
            rotation: rotationRadians,
            origin: Vector2.Zero,
            aaSize: IsAntialiased ? 1 : 0);

        if (requiresNonUniformScale)
        {
            ShapeRenderer.PopView();
        }
    }
}
