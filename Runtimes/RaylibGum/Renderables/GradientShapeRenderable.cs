using Gum.Converters;
using Gum.DataTypes;
using RenderingLibrary.Graphics;

namespace Gum.Renderables;

/// <summary>
/// Shared gradient geometry for the raylib shape renderables (<see cref="LineArc"/>,
/// <see cref="LineCircle"/>, <see cref="LineRectangle"/>): the endpoint / radius values, their
/// authored units, the unit-resolved <c>EffectiveGradient*</c> values the draw code reads, and the
/// rotated-endpoint helper. Units resolve like Skia does (see <see cref="GradientUnitResolver"/>),
/// so a gradient authored in percent renders the same on every backend (#7306, #7312).
/// </summary>
public abstract class GradientShapeRenderable : InvisibleRenderable
{
    /// <summary>
    /// X coordinate of the gradient axis start (linear) or radial gradient center, relative to the
    /// shape's top-left, in <see cref="GradientX1Units"/>. Default 0.
    /// </summary>
    public float GradientX1 { get; set; }

    /// <summary>Y coordinate of <see cref="GradientX1"/>, same coord space.</summary>
    public float GradientY1 { get; set; }

    /// <summary>X coordinate of the gradient axis end (linear only; ignored for radial).</summary>
    public float GradientX2 { get; set; }

    /// <summary>Y coordinate of <see cref="GradientX2"/>.</summary>
    public float GradientY2 { get; set; }

    /// <summary>
    /// Inner radius for a radial gradient: at this radius the color is the gradient's start color.
    /// Default 0 (solid inner color at the gradient center).
    /// </summary>
    public float GradientInnerRadius { get; set; }

    /// <summary>
    /// Outer radius for a radial gradient: at this radius the color is the gradient's end color.
    /// When 0 (the default) the shape's own extent is used so a default radial covers the whole shape.
    /// </summary>
    public float GradientOuterRadius { get; set; }

    /// <summary>Unit of <see cref="GradientX1"/>, resolved against the shape's width. Matches Skia.</summary>
    public GeneralUnitType GradientX1Units { get; set; }

    /// <summary>Unit of <see cref="GradientY1"/>, resolved against the shape's height.</summary>
    public GeneralUnitType GradientY1Units { get; set; }

    /// <summary>Unit of <see cref="GradientX2"/>, resolved against the shape's width.</summary>
    public GeneralUnitType GradientX2Units { get; set; }

    /// <summary>Unit of <see cref="GradientY2"/>, resolved against the shape's height.</summary>
    public GeneralUnitType GradientY2Units { get; set; }

    /// <summary>Unit of <see cref="GradientInnerRadius"/>, resolved against the shape's width.</summary>
    public DimensionUnitType GradientInnerRadiusUnits { get; set; }

    /// <summary>Unit of <see cref="GradientOuterRadius"/>, resolved against the shape's width.</summary>
    public DimensionUnitType GradientOuterRadiusUnits { get; set; }

    /// <summary><see cref="GradientX1"/> in bbox-local pixels after applying <see cref="GradientX1Units"/>.</summary>
    public float EffectiveGradientX1 => GradientUnitResolver.ResolveEndpoint(GradientX1, GradientX1Units, Width);

    /// <summary><see cref="GradientY1"/> in bbox-local pixels after applying <see cref="GradientY1Units"/>.</summary>
    public float EffectiveGradientY1 => GradientUnitResolver.ResolveEndpoint(GradientY1, GradientY1Units, Height);

    /// <summary><see cref="GradientX2"/> in bbox-local pixels after applying <see cref="GradientX2Units"/>.</summary>
    public float EffectiveGradientX2 => GradientUnitResolver.ResolveEndpoint(GradientX2, GradientX2Units, Width);

    /// <summary><see cref="GradientY2"/> in bbox-local pixels after applying <see cref="GradientY2Units"/>.</summary>
    public float EffectiveGradientY2 => GradientUnitResolver.ResolveEndpoint(GradientY2, GradientY2Units, Height);

    /// <summary><see cref="GradientInnerRadius"/> in pixels after applying <see cref="GradientInnerRadiusUnits"/>.</summary>
    public float EffectiveGradientInnerRadius => GradientUnitResolver.ResolveRadius(GradientInnerRadius, GradientInnerRadiusUnits, Width);

    /// <summary><see cref="GradientOuterRadius"/> in pixels after applying <see cref="GradientOuterRadiusUnits"/>.</summary>
    public float EffectiveGradientOuterRadius => GradientUnitResolver.ResolveRadius(GradientOuterRadius, GradientOuterRadiusUnits, Width);

    /// <summary>
    /// Returns the unit-resolved gradient axis endpoints rotated around the bbox center by
    /// <paramref name="rotationDegrees"/>, so the gradient axis tracks the shape under self-rotation
    /// (#2934 / #2956; same approach as Apos's <c>RenderableShapeBase.GetGradient</c>). Rotation
    /// convention is visual CCW on screen (screen Y points down): <c>[cos sin; -sin cos]</c>.
    /// </summary>
    public (float x1, float y1, float x2, float y2) GetRotatedGradientEndpoints(float rotationDegrees)
    {
        float x1 = EffectiveGradientX1;
        float y1 = EffectiveGradientY1;
        float x2 = EffectiveGradientX2;
        float y2 = EffectiveGradientY2;
        if (rotationDegrees == 0f)
        {
            return (x1, y1, x2, y2);
        }
        float rotRad = rotationDegrees * System.MathF.PI / 180f;
        float cos = System.MathF.Cos(rotRad);
        float sin = System.MathF.Sin(rotRad);
        float pivotX = Width * 0.5f;
        float pivotY = Height * 0.5f;
        float dx1 = x1 - pivotX;
        float dy1 = y1 - pivotY;
        float dx2 = x2 - pivotX;
        float dy2 = y2 - pivotY;
        return (
            pivotX + dx1 * cos + dy1 * sin,
            pivotY - dx1 * sin + dy1 * cos,
            pivotX + dx2 * cos + dy2 * sin,
            pivotY - dx2 * sin + dy2 * cos);
    }
}
