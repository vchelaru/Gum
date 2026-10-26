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
    /// After an undo or redo restored <paramref name="element"/>, does what typing the restored values would
    /// have done: the rows that read a variable the restore changed (and rows it added) are re-read in
    /// dependency order, then the cascade runs into the elements that read <paramref name="element"/>. A
    /// restored variable's own row is left alone, so a value the user typed over a stale reference stays.
    /// </summary>
    /// <param name="element">The element after the restore.</param>
    /// <param name="before">A copy of the element taken just before the restore.</param>
    /// <param name="otherSide">The snapshot of the same action that was not applied (the redo snapshot of an
    /// undo, the undo snapshot of a redo). A fed variable the action itself did not change keeps the value it
    /// held before the restore, because a removed or null source never resets a value; one the action did
    /// change takes the restored value. Null counts as no change.</param>
    /// <returns>The values that changed in other elements.</returns>
    StoredValueChange[] ReapplyReferences(ElementSave element, ElementSave before, ElementSave? otherSide);

    /// <summary>
    /// Re-applies the rows of <paramref name="element"/> that read a sibling-dependent name
    /// (<c>Index</c>), after paste, delete, reorder or a Parent change moved instances around. Does
    /// nothing, and does not touch the wireframe, when no row uses such a name.
    /// </summary>
    void ReapplySiblingDependentReferences(ElementSave element);

    void ReactIfChangedMemberIsVariableReference(InstanceSave? instance, StateSave stateSave, string changedMember, object? oldValue);
}
