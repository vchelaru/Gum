using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
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
        // Tool-only references exist solely to drive the element's resting design-time
        // wireframe preview from FormsProperty values, and that resting preview is the
        // default (uncategorized) state. Materializing into a categorized state bakes a
        // category selector into a state it doesn't belong to - including the selector's
        // own category, which produces a self-referential/circular state that re-drives
        // the category back to its FormsProperty value on preview, clobbering the state's
        // authored values (issue #3055). Since the references evaluate from single-valued
        // FormsProperties, they resolve identically in every state anyway, so restricting
        // to the default state loses nothing.
        if (element.DefaultState == null || stateSave != element.DefaultState)
        {
            return;
        }

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

    /// <summary>
    /// Returns the underlying (visual) variable names that the relevant element's behavior
    /// <see cref="BehaviorSave.ToolOnlyVariableReferences"/> drive from <paramref name="changedMember"/> -
    /// i.e. the left-hand sides of reference lines whose right-hand side reads
    /// <paramref name="changedMember"/>. For a StackPanel, "Spacing" yields ["StackSpacing"] (from
    /// <c>StackSpacing = Spacing</c>) and "Orientation" yields ["ChildrenLayout"] (from
    /// <c>ChildrenLayout = Orientation == ...</c>).
    ///
    /// Forms-promotion aliases (Spacing, Orientation) are not themselves visual properties, so the
    /// wireframe preview cannot push them onto a live <c>GraphicalUiElement</c>. This lets the editor
    /// resolve the underlying visual variable(s) - which the state-level <see cref="Apply"/> has already
    /// materialized - so scrubbing an alias updates the preview incrementally instead of forcing a full
    /// rebuild (issue #3191). Returns an empty list when the changed member drives no reference.
    /// </summary>
    /// <param name="element">The element being edited (the container of <paramref name="instance"/>, or
    /// the behavior-carrying component itself when <paramref name="instance"/> is null).</param>
    /// <param name="instance">The instance whose variable changed, or null for an element-level edit.</param>
    /// <param name="changedMember">The unqualified name of the variable that changed (e.g. "Spacing").</param>
    public static IReadOnlyList<string> GetUnderlyingMembersDrivenBy(ElementSave element, InstanceSave? instance, string changedMember)
    {
        if (string.IsNullOrEmpty(changedMember))
        {
            return Array.Empty<string>();
        }

        ComponentSave? component = instance != null
            ? ObjectFinder.Self.GetElementSave(instance.BaseType) as ComponentSave
            : element as ComponentSave;

        if (component == null)
        {
            return Array.Empty<string>();
        }

        List<string>? driven = null;

        foreach (ElementBehaviorReference reference in component.Behaviors)
        {
            BehaviorSave? behavior = ObjectFinder.Self.GetBehavior(reference);
            if (behavior == null || behavior.ToolOnlyVariableReferences.Count == 0)
            {
                continue;
            }

            foreach (string referenceLine in behavior.ToolOnlyVariableReferences)
            {
                if (string.IsNullOrWhiteSpace(referenceLine) || referenceLine.TrimStart().StartsWith("//"))
                {
                    continue;
                }

                int equalsIndex = referenceLine.IndexOf('=');
                if (equalsIndex < 0)
                {
                    continue;
                }

                string left = referenceLine.Substring(0, equalsIndex).Trim();
                string right = referenceLine.Substring(equalsIndex + 1).Trim();

                if (left.Length == 0 || !RightSideReadsIdentifier(right, changedMember))
                {
                    continue;
                }

                driven ??= new List<string>();
                if (!driven.Contains(left))
                {
                    driven.Add(left);
                }
            }
        }

        if (driven == null)
        {
            return Array.Empty<string>();
        }

        return driven;
    }

    /// <summary>
    /// Returns true if <paramref name="right"/> (the right-hand side of a reference assignment) reads
    /// <paramref name="identifier"/> as a bare identifier. Parsed with Roslyn so an identifier that only
    /// appears inside a string literal (e.g. <c>Foo == "Spacing"</c>) does not count as a read.
    /// </summary>
    private static bool RightSideReadsIdentifier(string right, string identifier)
    {
        SyntaxNode tree = CSharpSyntaxTree.ParseText(right).GetCompilationUnitRoot();
        return tree.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Any(id => id.Identifier.ValueText == identifier);
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

        // Skip materializing a value identical to the resting wireframe — the value the
        // reference produces when nothing is authored and every identifier resolves to its
        // FormsProperty default. When the authored result matches that resting value, the
        // assignment carries no information beyond the resting preview, so writing it only
        // spams the state — e.g. every Forms-control instance's
        // "<Instance>.<X>CategoryState = Enabled" in a screen that overrode nothing
        // (issue #3080). Only authored deviations from rest (IsEnabled = false, a checked
        // CheckBox, etc.) are worth materializing. The throwaway-element ceremony required to
        // evaluate "with nothing authored" lives in FromSyntaxNodeUsingDefaultsOnly (issue #3082).
        string effectiveLeft = instance == null ? left : $"{instance.Name}.{left}";

        EvaluatedSyntax? resting = EvaluatedSyntax.FromSyntaxNodeUsingDefaultsOnly(rightSyntax, fallback);
        if (resting != null && Equals(resting.Value, evaluated.Value))
        {
            // The reference resolves to its resting (default) value, so the target belongs at its
            // default. For an instance - where the reference owns the (hidden) variable - remove any
            // value materialized while the driver was non-default so it resets to default instead of
            // persisting stale after the driver returns to default (e.g. ChildrenLayout =
            // LeftToRightStack left behind when Orientation goes Horizontal -> Vertical). The element's
            // own default state (instance == null) holds the authored baseline, so leave it alone -
            // issue #3080 only suppressed the write; it never cleaned up a prior one.
            if (instance != null)
            {
                VariableSave? stale = stateSave.Variables.FirstOrDefault(v => v.Name == effectiveLeft);
                if (stale != null)
                {
                    stateSave.Variables.Remove(stale);
                }
            }
            return;
        }

        // Coerce the evaluated value to the left-hand variable's declared type before
        // writing it - the same step the state-level VariableReferences apply path performs
        // via EvaluatedSyntax.CastTo (see ApplyVariableReferencesOnSpecificOwner). The
        // tool-only applier evaluates the RHS directly, so without this an enum-typed target
        // like ChildrenLayout would store the raw string a ternary produces instead of the
        // boxed enum (which the typed wireframe setter and int-on-disk serializer require).
        string? leftSideType = ResolveLeftSideType(effectiveLeft, instance, stateSave);
        if (leftSideType != null)
        {
            evaluated.CastTo(leftSideType);
        }

        stateSave.SetValue(effectiveLeft, evaluated.Value, instance);
    }

    private static string? ResolveLeftSideType(string qualifiedLeftName, InstanceSave? instance, StateSave stateSave)
    {
        VariableSave? variableOnState = stateSave.Variables.FirstOrDefault(item => item.Name == qualifiedLeftName);
        if (variableOnState?.Type != null)
        {
            return variableOnState.Type;
        }

        ElementSave? elementOwningInstance = instance?.ParentContainer ?? stateSave.ParentContainer;
        return ObjectFinder.Self.GetRootVariable(qualifiedLeftName, elementOwningInstance)?.Type;
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
