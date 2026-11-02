using Gum.Converters;
using Gum.DataTypes;

namespace Gum.Renderables;

/// <summary>
/// Converts a raylib shape's gradient endpoint / radius values from their authored units to
/// element-local pixels. Mirrors the unit handling in SkiaGum's <c>RenderableShapeBase</c> and
/// Apos's <c>GetGradient</c> so a gradient authored in percent renders the same on every backend.
/// </summary>
public static class GradientUnitResolver
{
    /// <summary>Resolves a gradient endpoint coordinate against the element's size on that axis.</summary>
    public static float ResolveEndpoint(float value, GeneralUnitType units, float axisSize)
    {
        switch (units)
        {
            case GeneralUnitType.PixelsFromMiddle:
                return value + axisSize / 2.0f;
            case GeneralUnitType.PixelsFromLarge:
                return value + axisSize;
            case GeneralUnitType.Percentage:
                return axisSize * value / 100f;
            default:
                return value;
        }
    }

    /// <summary>
    /// Outer radius, in pixels, a radial gradient uses when the authored one resolves to 0 or less.
    /// Matches Skia's and MonoGame's fallback.
    /// </summary>
    public const float DefaultOuterRadius = 100f;

    /// <summary>
    /// Resolves a radial gradient's outer radius like <see cref="ResolveRadius"/>, falling back to
    /// <see cref="DefaultOuterRadius"/> when the result is 0 or less (as Skia does).
    /// </summary>
    public static float ResolveOuterRadius(float value, DimensionUnitType units, float width)
    {
        float resolved = ResolveRadius(value, units, width);
        return resolved > 0f ? resolved : DefaultOuterRadius;
    }

    /// <summary>Resolves a radial gradient radius against the element's width (as Skia does).</summary>
    public static float ResolveRadius(float value, DimensionUnitType units, float width)
    {
        switch (units)
        {
            case DimensionUnitType.PercentageOfParent:
                return width * value / 100f;
            case DimensionUnitType.RelativeToParent:
                return width + value;
            default:
                return value;
        }
    }
}
