namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Composed value for the Rectangle corner-radius composite row: the uniform <c>CornerRadius</c>
/// plus the four optional per-corner overrides (each falling back to <see cref="Uniform"/> when
/// null), mirroring <c>RectangleRuntime.CustomRadiusTopLeft/TopRight/BottomLeft/BottomRight</c>.
/// </summary>
public record struct CornerRadiusComposite(
    float Uniform,
    float? TopLeft,
    float? TopRight,
    float? BottomLeft,
    float? BottomRight)
{
    /// <summary>True when none of the four corners has an override — the common, single-value case.</summary>
    public readonly bool IsLinked => TopLeft == null && TopRight == null && BottomLeft == null && BottomRight == null;
}
