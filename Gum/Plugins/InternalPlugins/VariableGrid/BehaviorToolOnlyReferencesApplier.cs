using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
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
        if (element is ComponentSave component)
        {
            ApplyBehaviorsOf(component, instance: null, stateSave);
        }

        foreach (InstanceSave instance in element.Instances)
        {
            if (ObjectFinder.Self.GetElementSave(instance.BaseType) is ComponentSave instanceComponent)
            {
                ApplyBehaviorsOf(instanceComponent, instance, stateSave);
            }
        }
    }

    private static void ApplyBehaviorsOf(ComponentSave component, InstanceSave? instance, StateSave stateSave)
    {
        Func<string, object?> fallback = BuildFormsPropertyDefaultsFallback(component, instance);

        foreach (ElementBehaviorReference reference in component.Behaviors)
        {
            BehaviorSave? behavior = ObjectFinder.Self.GetBehavior(reference);
            if (behavior == null || behavior.ToolOnlyVariableReferences.Count == 0)
            {
                continue;
            }

            foreach (string referenceLine in behavior.ToolOnlyVariableReferences)
            {
                ApplyLine(referenceLine, instance, stateSave, fallback);
            }
        }
    }

    private static void ApplyLine(string referenceLine, InstanceSave? instance, StateSave stateSave, Func<string, object?> fallback)
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
        EvaluatedSyntax? evaluated = EvaluatedSyntax.FromSyntaxNode(rightSyntax, stateSave, fallback);
        if (evaluated?.Value == null)
        {
            return;
        }

        string effectiveLeft = instance == null ? left : $"{instance.Name}.{left}";
        stateSave.SetValue(effectiveLeft, evaluated.Value, instance);
    }

    /// <summary>
    /// Builds a name-resolver that looks up bare or instance-qualified identifiers
    /// against every linked behavior's <c>FormsProperty</c> declarations on
    /// <paramref name="component"/>, returning the declared <c>Value</c>. Used by
    /// <see cref="EvaluatedSyntax.FromSyntaxNode"/> as a fallback when the state
    /// has no authored value — so a behavior-declared default (e.g. IsEnabled = true)
    /// flows through to the wireframe preview without polluting the saved state.
    /// </summary>
    private static Func<string, object?> BuildFormsPropertyDefaultsFallback(ComponentSave component, InstanceSave? instance)
    {
        string? instancePrefix = instance != null ? instance.Name + "." : null;
        return name =>
        {
            string lookupName = instancePrefix != null && name.StartsWith(instancePrefix)
                ? name.Substring(instancePrefix.Length)
                : name;

            foreach (ElementBehaviorReference reference in component.Behaviors)
            {
                BehaviorSave? behavior = ObjectFinder.Self.GetBehavior(reference);
                if (behavior == null)
                {
                    continue;
                }

                foreach (VariableSave declaration in behavior.FormsProperties)
                {
                    if (declaration.Name == lookupName)
                    {
                        return declaration.Value;
                    }
                }
            }

            return null;
        };
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
