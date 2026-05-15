using System.Collections.Generic;
using Gum.DataTypes;

namespace Gum.ProjectServices;

/// <summary>
/// Compares two <see cref="StandardElementSave"/>s — typically a project's loaded
/// Standard against a fresh reference built from <c>StandardElementsManager</c> — and
/// reports whether they differ, plus a per-variable breakdown of what changed.
/// </summary>
/// <remarks>
/// This is the single source of truth for "do these Standards match." The Gum tool's
/// import dialog and <c>gumcli diff-standards</c> both call into this so they can never
/// disagree. The bool-equivalent behavior matches the tool's historic
/// <c>StandardsDiffer</c> exactly: Categories by name set, then DefaultState
/// <c>FileManager.XmlSerialize</c> string compare on cloned + variable-sorted states.
/// </remarks>
public interface IStandardComparer
{
    /// <summary>
    /// Compares <paramref name="source"/> against <paramref name="destination"/>.
    /// </summary>
    StandardComparisonResult Compare(StandardElementSave source, StandardElementSave destination);
}

/// <summary>Result of a single Standard-vs-Standard comparison.</summary>
public class StandardComparisonResult
{
    /// <summary>
    /// Mirrors the bool the tool's <c>StandardsDiffer</c> used to return. True when
    /// either the Category name set or the DefaultState XML differs.
    /// </summary>
    public bool HasDifferences { get; set; }

    /// <summary>True when the sorted Category-name sets differ.</summary>
    public bool CategoryNamesDiffer { get; set; }

    /// <summary>True when the cloned + sorted DefaultState XML strings differ.</summary>
    public bool DefaultStateXmlDiffers { get; set; }

    /// <summary>Category names present on the source but absent on the destination.</summary>
    public List<string> CategoryNamesOnlyInSource { get; } = new List<string>();

    /// <summary>Category names present on the destination but absent on the source.</summary>
    public List<string> CategoryNamesOnlyInDestination { get; } = new List<string>();

    /// <summary>
    /// Per-variable diffs in the DefaultState. Populated when
    /// <see cref="DefaultStateXmlDiffers"/> is true.
    /// </summary>
    public List<StandardVariableDiff> VariableDifferences { get; } = new List<StandardVariableDiff>();
}
