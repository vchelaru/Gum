using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.ProjectServices;
using System.Collections.Generic;

namespace ImportFromGumxPlugin.Services;

/// <summary>
/// The result of a transitive dependency computation.
/// </summary>
public class DependencySet
{
    /// <summary>
    /// Components that are transitively required by the selected items but not directly selected.
    /// These are ordered so that leaves of the dependency graph come first (safe import order).
    /// </summary>
    public List<ComponentSave> TransitiveComponents { get; } = new List<ComponentSave>();

    /// <summary>
    /// Behaviors required by any selected or transitive component.
    /// </summary>
    public List<BehaviorSave> Behaviors { get; } = new List<BehaviorSave>();

    /// <summary>
    /// Standards referenced by any selected/transitive component that differ
    /// from the corresponding standard in the destination project.
    /// </summary>
    public List<StandardElementSave> DifferingStandards { get; } = new List<StandardElementSave>();

    /// <summary>
    /// Per-standard <see cref="StandardComparisonResult"/> for every entry in
    /// <see cref="DifferingStandards"/>. The import dialog uses this to render
    /// a variable-level diff under each flagged standard row (#2779).
    /// </summary>
    /// <remarks>
    /// Standards that are wholesale-new in the source (absent from the destination
    /// project) still get an entry whose <see cref="StandardComparisonResult.HasDifferences"/>
    /// is true and whose <see cref="StandardComparisonResult.VariableDifferences"/> is empty —
    /// there is no destination state to diff against per-variable.
    /// </remarks>
    public Dictionary<StandardElementSave, StandardComparisonResult> DifferingStandardDiffs { get; }
        = new Dictionary<StandardElementSave, StandardComparisonResult>();
}

/// <summary>
/// Computes the transitive closure of components, behaviors, and differing standards that a
/// set of directly-selected elements pulls in from a source project into a destination project.
/// </summary>
public interface IGumxDependencyResolver
{
    /// <summary>
    /// Computes the full transitive dependency closure for the given directly-selected elements.
    /// Items already present in the destination project are excluded from the transitive component list.
    /// Standards are shown regardless of whether they exist in the destination — only differing ones appear.
    /// </summary>
    DependencySet ComputeTransitive(
        IList<ElementSave> directSelected,
        GumProjectSave source,
        GumProjectSave destination);
}
