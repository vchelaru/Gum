namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Composed value for a NineSlice composite row made of a shared value and four optional per-side
/// overrides (each falling back to <see cref="Uniform"/> when null): border scale
/// (<c>BorderScale</c> + <c>CustomBorderScaleLeft/Top/Right/Bottom</c>) and source cut
/// (<c>CustomFrameTextureCoordinateWidth</c> + <c>CustomFrameTextureCoordinateLeft/Top/Right/Bottom</c>).
/// <see cref="Uniform"/> is null only for the source cut, where null cuts a third of the texture.
/// </summary>
public record struct NineSliceSidesComposite(
    float? Uniform,
    float? Left,
    float? Top,
    float? Right,
    float? Bottom)
{
    /// <summary>True when no side has an override, the common single-value case.</summary>
    public readonly bool IsLinked => Left == null && Top == null && Right == null && Bottom == null;
}
