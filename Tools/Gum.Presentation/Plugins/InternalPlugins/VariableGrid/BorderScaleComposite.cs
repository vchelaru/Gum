namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Composed value for the NineSlice border-scale composite row: the uniform <c>BorderScale</c> plus
/// the four optional per-side overrides (each falling back to <see cref="Uniform"/> when null),
/// mirroring <c>NineSliceRuntime.CustomBorderScaleLeft/Top/Right/Bottom</c>.
/// </summary>
public record struct BorderScaleComposite(
    float Uniform,
    float? Left,
    float? Top,
    float? Right,
    float? Bottom)
{
    /// <summary>True when no side has an override, the common single-value case.</summary>
    public readonly bool IsLinked => Left == null && Top == null && Right == null && Bottom == null;
}
