using Gum.GueDeriving;

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Forest Glade's signature corner silhouette — sharp top-left and
/// bottom-right, rounded top-right and bottom-left. Applied uniformly to
/// every <see cref="RectangleRuntime"/> across the theme so each
/// control reads as the same leaf shape. Sizes match the CSS
/// <c>--leaf-sm/md/lg/xl</c> tokens.
/// </summary>
internal static class ForestGladeLeaf
{
    /// <summary>CSS <c>--leaf-sm: 2px 8px 2px 8px</c> — checkboxes and small surfaces.</summary>
    public static void ApplySmall(RectangleRuntime r) => Apply(r, 2f, 8f);

    /// <summary>CSS <c>--leaf-md: 2px 12px 2px 12px</c> — inputs, combo boxes.</summary>
    public static void ApplyMedium(RectangleRuntime r) => Apply(r, 2f, 12f);

    /// <summary>CSS <c>--leaf-lg: 4px 18px 4px 18px</c> — buttons, list panels.</summary>
    public static void ApplyLarge(RectangleRuntime r) => Apply(r, 4f, 18f);

    /// <summary>CSS <c>--leaf-xl: 6px 24px 6px 24px</c> — Window chrome.</summary>
    public static void ApplyExtraLarge(RectangleRuntime r) => Apply(r, 6f, 24f);

    private static void Apply(RectangleRuntime r, float sharp, float rounded)
    {
        // Per-corner pattern: TL & BR stay sharp (the "stem ends" of the
        // leaf), TR & BL bulge out. CornerRadius is the fallback for any
        // corner where a Custom* override isn't set — kept at the sharp
        // value so nothing surprising shows up if a future renderable code
        // path consults CornerRadius without checking the overrides.
        r.CornerRadius = sharp;
        r.CustomRadiusTopLeft = sharp;
        r.CustomRadiusTopRight = rounded;
        r.CustomRadiusBottomRight = sharp;
        r.CustomRadiusBottomLeft = rounded;
    }
}
