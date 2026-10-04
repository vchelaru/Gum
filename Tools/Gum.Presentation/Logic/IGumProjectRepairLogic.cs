using Gum.DataTypes;

namespace Gum.Logic;

/// <summary>
/// Load-time normalization/repair passes run against a freshly-loaded <see cref="GumProjectSave"/>.
/// Each pass fixes up data left in an inconsistent state by an older version of Gum (stray spaces or
/// backslashes in names, duplicate variables, recursive base-type assignments) and reports whether it
/// changed anything, so the caller can decide whether the project needs to be re-saved.
/// </summary>
public interface IGumProjectRepairLogic
{
    /// <summary>
    /// Strips spaces from variable names that historically contained them (e.g. "Base Type" ->
    /// "BaseType"), so old projects match the current variable-naming convention.
    /// </summary>
    bool RemoveSpacesInVariables(GumProjectSave gumProjectSave);

    /// <summary>
    /// Finds instances whose base type recursively references the containing element, and forces
    /// them to <c>Container</c> to break the cycle.
    /// </summary>
    bool FixRecursiveAssignments(GumProjectSave gumProjectSave);

    /// <summary>
    /// Replaces backslashes with forward slashes in element/instance/behavior names and references,
    /// so names are consistent regardless of which OS created the project.
    /// </summary>
    bool FixSlashesInNames(GumProjectSave gumProjectSave);

    /// <summary>
    /// Removes variables with duplicate names from every state in the project, keeping the first
    /// occurrence of each name.
    /// </summary>
    bool RemoveDuplicateVariables(GumProjectSave gumProjectSave);

    /// <summary>
    /// Rewrites an absolute font-file path (a <c>.ttf</c>/<c>.otf</c> in <c>Font</c> or
    /// <c>CustomFontFile</c>) to a project-relative one, when it points at an existing file inside
    /// <paramref name="projectDirectory"/>. A path that does not resolve on this machine, or resolves
    /// outside the project, is left alone: guessing where it moved to is not safe unprompted.
    /// </summary>
    bool MakeFontFilePathsRelative(GumProjectSave gumProjectSave, string projectDirectory);
}
