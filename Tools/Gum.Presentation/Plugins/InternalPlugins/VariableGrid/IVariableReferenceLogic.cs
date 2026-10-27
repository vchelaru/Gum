using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
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

    /// <summary>
    /// After <paramref name="behavior"/> was reloaded from disk, re-applies its tool-only references to the
    /// components that implement it and to every element holding an instance of one, the same pass an edit
    /// of those elements runs, then updates the elements whose variable references read what changed.
    /// </summary>
    /// <param name="behavior">The behavior as it is now loaded.</param>
    /// <param name="previous">The copy the reload replaced. A variable only its lines wrote, and no remaining line of
    /// the component writes, is removed; null when unknown.</param>
    /// <param name="trySave">True to auto-save each element whose stored values changed.</param>
    /// <returns>Every stored value that changed, in the re-applied elements and in the elements that read them
    /// (a value a reference removed is reported with a null value); empty when nothing changed.</returns>
    StoredValueChange[] ReapplyToolOnlyReferences(BehaviorSave behavior, BehaviorSave? previous, bool trySave);

    /// <summary>
    /// After <paramref name="element"/> was reloaded from disk, re-applies the tool-only references of its
    /// behaviors (and of its instances' components) to it and to every element holding an instance of it,
    /// because an outside edit may have changed a Forms value they read or the behaviors it implements.
    /// </summary>
    /// <param name="element">The element as it is now loaded.</param>
    /// <param name="previous">The copy the reload replaced. A variable a behavior it no longer implements wrote is
    /// removed from it and from instances of it; null when unknown.</param>
    /// <param name="trySave">True to auto-save each element whose stored values changed.</param>
    /// <returns>See <see cref="ReapplyToolOnlyReferences(BehaviorSave, BehaviorSave?, bool)"/>.</returns>
    StoredValueChange[] ReapplyToolOnlyReferences(ElementSave element, ElementSave? previous, bool trySave);

    void ReactIfChangedMemberIsVariableReference(InstanceSave? instance, StateSave stateSave, string changedMember, object? oldValue);
}
