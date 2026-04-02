namespace Gum.DataTypes;

/// <summary>
/// Identifies which font generation backend to use when creating bitmap font files.
/// </summary>
public enum FontGeneratorType
{
    /// <summary>
    /// Uses the embedded bmfont.exe tool (Windows-only).
    /// </summary>
    BmFont = 0,

    /// <summary>
    /// Uses the KernSmith library for cross-platform font generation.
    /// </summary>
    KernSmith = 1
}
