using Gum.DataTypes;
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
    void ApplyReferencesToElement(ElementSave element, bool trySave, bool isFullCommit = true);

    void ReactIfChangedMemberIsVariableReference(InstanceSave? instance, StateSave stateSave, string changedMember, object? oldValue);
}
