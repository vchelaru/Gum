using Gum.ProjectServices;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Resolves a GUM0002 row: replaces a variable's stale stored value with the value its
/// VariableReference evaluates to. The reference itself stays.
/// </summary>
public interface IVariableReferenceConflictFixer
{
    /// <summary>
    /// Asks the user to confirm "old becomes new", then replaces the stale value through the tool's
    /// undo, save and plugin notification path. Does nothing when the user declines or when the
    /// conflict no longer exists in the project (the state was replaced by an undo, for example).
    /// </summary>
    void Fix(VariableReferenceConflict conflict);
}
