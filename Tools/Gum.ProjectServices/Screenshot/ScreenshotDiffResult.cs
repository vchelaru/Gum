using System.Collections.Generic;
using System.Linq;

namespace Gum.ProjectServices.Screenshot;

/// <summary>
/// Result of a <see cref="ScreenshotDiffService"/> run across every Screen and Component in a project.
/// </summary>
public class ScreenshotDiffResult
{
    /// <summary>One entry per Screen/Component in the project, in enumeration order.</summary>
    public required IReadOnlyList<ElementScreenshotDiff> ElementDiffs { get; init; }

    /// <summary>
    /// Directory the rendered PNGs were written to (the request's <c>OutputDirectory</c>, or the
    /// temp directory generated when it was omitted).
    /// </summary>
    public required string OutputDirectory { get; init; }

    /// <summary>True when at least one element failed to render or differs beyond tolerance.</summary>
    public bool HasMismatch => ElementDiffs.Any(d => !d.Matches);
}
