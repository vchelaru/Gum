using Gum.DataTypes;
using Gum.Undo;
using System.Collections.Generic;
using Gum.DataTypes.Variables;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>Handles variable reference parsing, validation, and assignment reactions.</summary>
public interface IVariableReferenceLogic
{
    AssignmentExpressionSyntax? GetAssignmentSyntax(string item);

    void DoVariableReferenceReaction(ElementSave parentElement, InstanceSave? leftSideInstance, string unqualifiedMember,
        StateSave stateSave, string qualifiedName, bool trySave, bool isFullCommit = true);

    /// <summary>
    /// Re-evaluates every variable reference in other elements that reads <paramref name="element"/>,
    /// so their stored values follow it, and optionally auto-saves the elements that hold them.
    /// </summary>
    /// <returns>The values that changed, so a caller can refresh what shows them and keep undo snapshots current;
    /// empty when nothing changed.</returns>
    StoredValueChange[] ApplyReferencesToElement(ElementSave element, bool trySave, bool isFullCommit = true);

    /// <summary>
    /// Re-reads every reference row on every state of <paramref name="element"/>, then runs the cascade into the
    /// elements that read it (saving them when Auto Save is on). What an undo or redo runs after restoring a
    /// snapshot, so the result matches typing the restored value. Does not save <paramref name="element"/> itself
    /// unless something else reads it.
    /// </summary>
    /// <returns>The values that changed in other elements.</returns>
    StoredValueChange[] ReapplyReferences(ElementSave element);

    /// <summary>
    /// Re-applies the rows of <paramref name="element"/> that read a sibling-dependent name
    /// (<c>Index</c>), after paste, delete, reorder or a Parent change moved instances around. Does
    /// nothing, and does not touch the wireframe, when no row uses such a name.
    /// </summary>
    void ReapplySiblingDependentReferences(ElementSave element);

    void ReactIfChangedMemberIsVariableReference(InstanceSave? instance, StateSave stateSave, string changedMember, object? oldValue);
}
