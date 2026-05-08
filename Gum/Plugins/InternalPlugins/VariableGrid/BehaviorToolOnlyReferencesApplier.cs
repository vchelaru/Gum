using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Tool-only apply pass for <see cref="BehaviorSave.ToolOnlyVariableReferences"/>.
/// For the supplied state, walks the element's own behaviors plus the behaviors of
/// each instance's base component and writes the resolved value of every reference
/// line back into the state — qualifying the left-hand side with the instance name
/// when the reference comes from an instance's component.
///
/// Strictly tool-only by design: never invoked from runtime apply paths or generated
/// code. The runtime equivalent for these properties is the Forms control's own
/// setter (e.g. <c>FrameworkElement.IsEnabled</c>'s <c>UpdateState()</c> call), so
/// applying these references at runtime would double-write the visual state.
/// </summary>
public static class BehaviorToolOnlyReferencesApplier
{
    public static void Apply(ElementSave element, StateSave stateSave)
    {
        ApplyBehaviorsOf(element, instance: null, stateSave);

        foreach (InstanceSave instance in element.Instances)
        {
            ElementSave? instanceElement = ObjectFinder.Self.GetElementSave(instance.BaseType);
            if (instanceElement == null)
            {
                continue;
            }

            ApplyBehaviorsOf(instanceElement, instance, stateSave);
        }
    }

    private static void ApplyBehaviorsOf(ElementSave element, InstanceSave? instance, StateSave stateSave)
    {
        if (element is not ComponentSave component)
        {
            return;
        }

        foreach (ElementBehaviorReference reference in component.Behaviors)
        {
            BehaviorSave? behavior = ObjectFinder.Self.GetBehavior(reference);
            if (behavior == null || behavior.ToolOnlyVariableReferences.Count == 0)
            {
                continue;
            }

            foreach (string referenceLine in behavior.ToolOnlyVariableReferences)
            {
                ApplyLine(referenceLine, instance, stateSave);
            }
        }
    }

    private static void ApplyLine(string referenceLine, InstanceSave? instance, StateSave stateSave)
    {
        if (string.IsNullOrWhiteSpace(referenceLine) || referenceLine.TrimStart().StartsWith("//"))
        {
            return;
        }

        int equalsIndex = referenceLine.IndexOf('=');
        if (equalsIndex < 0)
        {
            return;
        }

        string left = referenceLine.Substring(0, equalsIndex).Trim();
        string right = referenceLine.Substring(equalsIndex + 1).Trim();

        if (instance != null)
        {
            right = QualifyBareIdentifiersWithInstance(right, instance.Name);
        }

        string rightAsCSharp = EvaluatedSyntax.ConvertToCSharpSyntax(right);
        ExpressionSyntax rightSyntax = SyntaxFactory.ParseExpression(rightAsCSharp);
        EvaluatedSyntax? evaluated = EvaluatedSyntax.FromSyntaxNode(rightSyntax, stateSave);
        if (evaluated?.Value == null)
        {
            return;
        }

        string effectiveLeft = instance == null ? left : $"{instance.Name}.{left}";
        stateSave.SetValue(effectiveLeft, evaluated.Value, instance);
    }

    /// <summary>
    /// Re-writes the right-hand side of an assignment so every bare identifier becomes
    /// <c>{instanceName}.{identifier}</c>, leaving qualified names and member-access
    /// expressions alone. Mirrors <c>VariableReferenceLogic.QualifyInstanceVariables</c>.
    /// </summary>
    private static string QualifyBareIdentifiersWithInstance(string right, string instanceName)
    {
        SyntaxNode tree = CSharpSyntaxTree.ParseText(right).GetCompilationUnitRoot();

        var bareIdentifiers = tree.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Where(id => id.Parent is not MemberAccessExpressionSyntax
                         && id.Parent is not AliasQualifiedNameSyntax)
            .ToList();

        SyntaxNode rewritten = tree.ReplaceNodes(
            bareIdentifiers,
            (node, _) => SyntaxFactory.IdentifierName($"{instanceName}.{node}"));

        return rewritten.ToString();
    }
}
