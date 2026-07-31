namespace Gum.ProjectServices.Screenshot;

/// <summary>
/// Per-element result of a <see cref="ScreenshotDiffService"/> run.
/// </summary>
public class ElementScreenshotDiff
{
    /// <summary>Name of the Screen or Component that was rendered.</summary>
    public required string ElementName { get; init; }

    /// <summary>Whether both backends' renders matched within tolerance.</summary>
    public required bool Matches { get; init; }

    /// <summary>Set when either backend failed to render the element; <see cref="Matches"/> is false.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Path to backend A's rendered PNG, when rendering succeeded.</summary>
    public string? BackendAPath { get; init; }

    /// <summary>Path to backend B's rendered PNG, when rendering succeeded.</summary>
    public string? BackendBPath { get; init; }

    /// <summary>X coordinate of the first out-of-tolerance pixel, when the images differ in content.</summary>
    public int? DiffX { get; init; }

    /// <summary>Y coordinate of the first out-of-tolerance pixel, when the images differ in content.</summary>
    public int? DiffY { get; init; }

    /// <summary>Max per-channel difference at (<see cref="DiffX"/>, <see cref="DiffY"/>).</summary>
    public int? MaxChannelDifference { get; init; }

    /// <summary>Set instead of the pixel-diff fields when the two renders have different dimensions.</summary>
    public string? DimensionMismatchDescription { get; init; }
}
