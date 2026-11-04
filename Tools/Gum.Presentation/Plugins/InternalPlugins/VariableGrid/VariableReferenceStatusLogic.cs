using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using GumRuntime;
using System.Collections;
using System.Linq;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Tells whether a variable that a reference drives is following it right now (#7512). A right side such as
/// <c>5 / Y</c> is valid, but while <c>Y</c> is 0 it has no value, the variable keeps its last value, and the
/// Variables tab says why on that row. Computed from current values, so it is right for any element on open.
/// </summary>
public interface IVariableReferenceStatusLogic
{
    /// <summary>
    /// Returns the note for the row <paramref name="variableName"/> (such as <c>Not updating: Y is 0, so the reference can't divide by it.</c>), or null when its reference evaluates or it has none.
    /// </summary>
    /// <param name="variableName">The unqualified variable the row shows.</param>
    /// <param name="instance">The instance the row belongs to; null on an element-level row.</param>
    /// <param name="owner">The element that contains the reference list.</param>
    /// <param name="state">The state whose reference list is shown.</param>
    string? GetNote(string variableName, InstanceSave? instance, ElementSave owner, StateSave state);
}

/// <inheritdoc/>
public class VariableReferenceStatusLogic : IVariableReferenceStatusLogic
{
    private const string Prefix = "Not updating: ";

    /// <inheritdoc/>
    public string? GetNote(string variableName, InstanceSave? instance, ElementSave owner, StateSave state)
    {
        string listName = instance == null ? "VariableReferences" : instance.Name + ".VariableReferences";
        if (state.GetValueRecursive(listName) is not IList lines)
        {
            return null;
        }

        ElementSave? lineOwner = instance == null ? owner : ObjectFinder.Self.GetElementSave(instance);
        foreach (string? line in lines.OfType<string>())
        {
            if (line.TrimStart().StartsWith("//"))
            {
                continue;
            }

            foreach (string expanded in ElementSaveExtensions.ExpandCompositeReferenceLine(line, lineOwner))
            {
                int equalsIndex = expanded.IndexOf('=');
                if (equalsIndex <= 0 || expanded.Substring(0, equalsIndex).Trim() != variableName)
                {
                    continue;
                }

                string resolved = ElementSaveExtensions.ResolveOwnerPrefix(expanded, instance?.Name,
                    ElementSaveExtensions.GetReferencableInstanceNames(owner));
                var assignment = VariableReferenceLogic.ParseAssignment(resolved);
                if (assignment == null || EvaluatedSyntax.GetFunctionCallProblem(assignment.Right) != null)
                {
                    continue;
                }

                // A value computed from the live layout (Index, Absolute*) cannot be known without the canvas.
                if (assignment.Right.DescendantNodesAndSelf().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax>()
                    .Any(identifier => EvaluatedSyntax.IsLiveLayoutName(identifier.Identifier.Text)))
                {
                    continue;
                }

                EvaluatedSyntax? evaluated = EvaluatedSyntax.FromSyntaxNode(assignment.Right, state);
                if (evaluated != null && evaluated.EvaluatedType == null)
                {
                    return Prefix + VariableReferenceLogic.DescribeRightSideFailure(assignment.Right, state, null).NoteReason + ".";
                }
            }
        }

        return null;
    }
}
