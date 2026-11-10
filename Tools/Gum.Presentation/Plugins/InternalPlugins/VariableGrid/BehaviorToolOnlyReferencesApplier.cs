using Gum.Logic;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.Wireframe;
using GumRuntime;
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
    /// Applies only the lines the edit of <paramref name="changedMember"/> on <paramref name="instance"/> (null for
    /// the element itself) can change, so a drag tick costs what the one edited instance costs, not every
    /// behavior instance of the element. Same writes as <see cref="Apply"/> for that instance.
    /// </summary>
    public static void ApplyDrivenBy(ElementSave element, InstanceSave? instance, string changedMember, StateSave stateSave)
    {
        if (element.DefaultState == null || stateSave != element.DefaultState ||
            GetUnderlyingMembersDrivenBy(element, instance, changedMember).Count == 0)
        {
            return;
        }

        ComponentSave? component = instance != null
            ? ObjectFinder.Self.GetElementSave(instance.BaseType) as ComponentSave
            : element as ComponentSave;
        if (component != null)
        {
            ApplyBehaviorsOf(component, instance, stateSave);
        }
    }

    /// <summary>
    /// Values a Forms control instance's behavior derives that a non-default <paramref name="selectedState"/> of
    /// <paramref name="element"/> would otherwise override: the selected state sets the derived variable itself
    /// (<c>Play.ButtonCategoryState</c>) or a Forms property it reads (<c>Play.IsEnabled</c>). An instance's
    /// authored Forms values win over visual state values, so the canvas applies these after the selected state. They are
    /// evaluated against the default state with the selected state on top, so the project data is not touched.
    /// The element's own derived values are not included: viewed on its own, the clicked state wins (#6851).
    /// </summary>
    public static StateSave GetInstanceValuesToReassert(ElementSave element, StateSave selectedState, ICollection<string> variablesSetBySelected)
    {
        StateSave toReassert = new StateSave { Name = "FormsDerived", ParentContainer = element };
        StateSave? defaultState = element.DefaultState;
        if (defaultState == null || selectedState == defaultState)
        {
            return toReassert;
        }

        StateSave effective = defaultState.Clone();
        effective.ParentContainer = element;
        effective.MergeIntoThis(selectedState);

        foreach (InstanceSave instance in element.Instances)
        {
            if (ObjectFinder.Self.GetElementSave(instance.BaseType) is not ComponentSave instanceComponent)
            {
                continue;
            }

            Func<string, object?> fallback = BuildFormsPropertyDefaultsFallback(instanceComponent, instance);
            foreach ((string left, string right) in GetReferenceLines(instanceComponent))
            {
                // Only a Forms value the author set counts: with every Forms property at its default, a state
                // that sets the category state is the author choosing that look.
                IReadOnlyList<string> readNames = ReadIdentifiers(right).Select(identifier => $"{instance.Name}.{identifier}").ToList();
                bool selectedTouchesIt = variablesSetBySelected.Contains($"{instance.Name}.{left}") || readNames.Any(variablesSetBySelected.Contains);
                bool formsValueAuthored = readNames.Any(name => effective.Variables.Any(item => item.Name == name && item.SetsValue && item.Value != null));
                if (!selectedTouchesIt || !formsValueAuthored)
                {
                    continue;
                }

                EvaluatedLine? line = EvaluateLine(left, right, instance, effective, fallback);
                if (line != null)
                {
                    toReassert.SetValue(line.EffectiveLeft, line.Value, instance);
                }
            }
        }

        return toReassert;
    }

    /// <summary>
    /// What the behaviors of buttons (and other Forms controls) nested inside <paramref name="element"/>'s component
    /// instances derive from a Forms value the element's instance sets through an exposed variable (#7641):
    /// <c>OptionsRow1.IsEnabled = false</c>, with OptionsRow exposing its button's IsEnabled, gives
    /// <c>OptionsRow1.ButtonInstance1.ButtonCategoryState = Disabled</c>. Follows the exposure at every level of
    /// nesting. The result is for the in-memory canvas only; it is never merged into saved data. The values are read
    /// from the element's Default state with <paramref name="selectedState"/> on top.
    /// </summary>
    public static StateSave GetValuesThroughExposedVariables(ElementSave element, StateSave? selectedState)
    {
        StateSave result = new StateSave { Name = "FormsExposed", ParentContainer = element };
        StateSave? defaultState = element.DefaultState;
        if (defaultState == null)
        {
            return result;
        }

        StateSave effective = defaultState.Clone();
        effective.ParentContainer = element;
        if (selectedState != null && selectedState != defaultState)
        {
            effective.MergeIntoThis(selectedState);
        }

        foreach (InstanceSave instance in element.Instances)
        {
            if (ObjectFinder.Self.GetElementSave(instance.BaseType) is ComponentSave component)
            {
                CollectValuesThroughExposedVariables(effective, instance.Name, component, instance.Name, result, depth: 0);
            }
        }

        return result;
    }

    /// <param name="suppliedValues">Holds the values the owner of <paramref name="component"/>'s instance sets, by exposed name behind <paramref name="suppliedPrefix"/>.</param>
    private static void CollectValuesThroughExposedVariables(StateSave suppliedValues, string suppliedPrefix, ComponentSave component,
        string outputPrefix, StateSave result, int depth)
    {
        StateSave? componentDefault = component.DefaultState;
        if (componentDefault == null || depth > 16)
        {
            return;
        }

        // An exposure a base component declares is visible through every component derived from it, and the
        // derived component's own declaration of the same variable wins (derived first, first name kept).
        List<VariableSave> exposures = new List<VariableSave>();
        foreach (StateSave? chainState in new[] { componentDefault }.Concat(ObjectFinder.Self.GetBaseElements(component).Select(item => item.DefaultState)))
        {
            foreach (VariableSave candidate in chainState?.Variables.Where(item => !string.IsNullOrEmpty(item.ExposedAsName)) ?? Enumerable.Empty<VariableSave>())
            {
                if (!exposures.Any(item => item.Name == candidate.Name))
                {
                    exposures.Add(candidate);
                }
            }
        }

        foreach (InstanceSave inner in component.Instances)
        {
            if (ObjectFinder.Self.GetElementSave(inner.BaseType) is not ComponentSave innerComponent)
            {
                continue;
            }

            StateSave innerState = new StateSave { Name = "Exposed", ParentContainer = innerComponent };
            foreach (VariableSave exposure in exposures.Where(item => item.SourceObject == inner.Name))
            {
                string suppliedName = suppliedPrefix.Length == 0 ? exposure.ExposedAsName! : $"{suppliedPrefix}.{exposure.ExposedAsName}";
                VariableSave? supplied = suppliedValues.Variables.FirstOrDefault(item => item.Name == suppliedName && item.SetsValue && item.Value != null);
                if (supplied != null)
                {
                    innerState.Variables.Add(new VariableSave { Name = exposure.GetRootName(), Type = supplied.Type, Value = supplied.Value, SetsValue = true });
                }
            }

            if (innerState.Variables.Count == 0)
            {
                continue;
            }

            Func<string, object?> fallback = BuildFormsPropertyDefaultsFallback(innerComponent, instance: null);
            foreach ((string left, string right) in GetReferenceLines(innerComponent))
            {
                if (!ReadIdentifiers(right).Any(identifier => innerState.Variables.Any(item => item.Name == identifier)))
                {
                    continue;
                }

                EvaluatedLine? line = EvaluateLine(left, right, instance: null, innerState, fallback);
                if (line != null)
                {
                    result.Variables.Add(new VariableSave
                    {
                        Name = $"{outputPrefix}.{inner.Name}.{left}",
                        Type = ResolveLeftSideType(left, instance: null, innerState) ?? "string",
                        Value = line.Value,
                        SetsValue = true
                    });
                }
            }

            CollectValuesThroughExposedVariables(innerState, "", innerComponent, $"{outputPrefix}.{inner.Name}", result, depth + 1);
        }
    }

    /// <summary>
    /// The category states a running game has applied on top of <paramref name="element"/>'s Default values:
    /// each category-state reference line of its behaviors (<c>ButtonCategoryState = IsEnabled ? ...</c>)
    /// evaluated against the Default state, with unset Forms values at their declared defaults. The
    /// canvas layers these on a Default selection (#6857); the project data is not touched.
    /// </summary>
    public static IReadOnlyList<StateSave> GetActiveCategoryStates(ElementSave element)
    {
        List<StateSave> active = new List<StateSave>();
        StateSave? defaultState = element.DefaultState;
        if (element is not ComponentSave component || defaultState == null)
        {
            return active;
        }

        // A value the Forms control assigns (a reference line that is not a category state) belongs to
        // Default, so a category state does not override it, whether it sets it by value or by a reference row (#6908).
        HashSet<string> formsAssigned = new HashSet<string>();
        Func<string, object?> fallback = BuildFormsPropertyDefaultsFallback(component, instance: null);
        foreach ((string left, string right) in GetReferenceLines(component))
        {
            string? categoryName = ResolveLeftSideType(left, instance: null, defaultState);
            IEnumerable<StateSave> categoryStates = categoryName == null ? Enumerable.Empty<StateSave>() : GetCategoryStates(element, categoryName);
            if (!categoryStates.Any())
            {
                formsAssigned.Add(left);
                continue;
            }

            if (EvaluateLine(left, right, instance: null, defaultState, fallback)?.Value is string stateName &&
                categoryStates.FirstOrDefault(item => item.Name == stateName) is { } state)
            {
                active.Add(state);
            }
        }

        for (int i = 0; i < active.Count; i++)
        {
            StateSave withoutFormsValues = active[i].Clone();
            withoutFormsValues.ParentContainer = element;
            SelectedStateReferenceMerger.RemoveSettersOf(withoutFormsValues, formsAssigned);
            active[i] = withoutFormsValues;
        }

        return active;
    }

    private static IEnumerable<StateSave> GetCategoryStates(ElementSave element, string categoryName)
    {
        foreach (StateSaveCategory category in InheritedCategories.GetCategories(element))
        {
            if (category.Name == categoryName)
            {
                return category.States;
            }
        }

        return Enumerable.Empty<StateSave>();
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

        foreach ((string left, string right) in GetReferenceLines(component))
        {
            if (!RightSideReadsIdentifier(right, changedMember))
            {
                continue;
            }

            driven ??= new List<string>();
            if (!driven.Contains(left))
            {
                driven.Add(left);
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
    private static bool RightSideReadsIdentifier(string right, string identifier) =>
        ReadIdentifiers(right).Contains(identifier);

    /// <summary>
    /// The bare identifiers <paramref name="right"/> reads, parsed with Roslyn so a name that only appears
    /// inside a string literal (e.g. <c>Foo == "Spacing"</c>) does not count as a read.
    /// </summary>
    private static IEnumerable<string> ReadIdentifiers(string right)
    {
        SyntaxNode tree = CSharpSyntaxTree.ParseText(right).GetCompilationUnitRoot();
        return tree.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Where(id => !EvaluatedSyntax.IsFunctionName(id))
            .Select(id => id.Identifier.ValueText)
            .ToList();
    }

    /// <summary>
    /// The left and right side of every active line of every linked behavior's
    /// <see cref="BehaviorSave.ToolOnlyVariableReferences"/> (blank, commented and assignment-less lines skipped).
    /// </summary>
    private static IEnumerable<(string Left, string Right)> GetReferenceLines(ComponentSave component)
    {
        // A component derived from a Forms control behaves as that control, so the base chain's behaviors count.
        foreach (BehaviorSave behavior in FormsBehaviorChain.GetBehaviors(component))
        {
            foreach ((string left, string right) in GetReferenceLines(behavior))
            {
                yield return (left, right);
            }
        }
    }

    private static IEnumerable<(string Left, string Right)> GetReferenceLines(BehaviorSave behavior)
    {
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
            if (left.Length > 0)
            {
                yield return (left, referenceLine.Substring(equalsIndex + 1).Trim());
            }
        }
    }

    /// <summary>SPIKE #6408: true when a behavior line of any instance in <paramref name="element"/> matches <paramref name="pattern"/>.</summary>
    public static bool AnyInstanceLineMatches(ElementSave element, System.Text.RegularExpressions.Regex pattern)
    {
        foreach (InstanceSave instance in element.Instances)
        {
            if (ObjectFinder.Self.GetElementSave(instance.BaseType) is ComponentSave component &&
                GetReferenceLines(component).Any(line => pattern.IsMatch(line.Right)))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>The (unqualified) variables the active reference lines of <paramref name="behavior"/> write.</summary>
    public static ISet<string> GetDrivenVariableNames(BehaviorSave behavior) =>
        new HashSet<string>(GetReferenceLines(behavior).Select(line => line.Left));

    /// <summary>The (unqualified) variables the active reference lines of <paramref name="component"/>'s behaviors write.</summary>
    public static ISet<string> GetDrivenVariableNames(ComponentSave component) =>
        new HashSet<string>(GetReferenceLines(component).Select(line => line.Left));

    private static void ApplyBehaviorsOf(ComponentSave component, InstanceSave? instance, StateSave stateSave)
    {
        Func<string, object?> fallback = BuildFormsPropertyDefaultsFallback(component, instance);

        // Two behaviors can write the same variable; a resting line must not undo what an earlier line
        // of this pass wrote, whichever behavior comes first.
        HashSet<string> writtenThisPass = new HashSet<string>();

        foreach ((string left, string right) in GetReferenceLines(component))
        {
            EvaluatedLine? line = EvaluateLine(left, right, instance, stateSave, fallback);
            if (line == null)
            {
                continue;
            }

            // Skip materializing a value identical to the resting wireframe — the value the
            // reference produces when nothing is authored and every identifier resolves to its
            // FormsProperty default. When the authored result matches that resting value, the
            // assignment carries no information beyond the resting preview, so writing it only
            // spams the state — e.g. every Forms-control instance's
            // "<Instance>.<X>CategoryState = Enabled" in a screen that overrode nothing
            // (issue #3080). Only authored deviations from rest (IsEnabled = false, a checked
            // CheckBox, etc.) are worth materializing.
            if (line.IsResting && writtenThisPass.Contains(line.EffectiveLeft))
            {
                continue;
            }

            if (line.IsResting)
            {
                // The reference resolves to its resting (default) value, so the target belongs at its
                // default. For an instance - where the reference owns the (hidden) variable - remove any
                // value materialized while the driver was non-default so it resets to default instead of
                // persisting stale after the driver returns to default (e.g. ChildrenLayout =
                // LeftToRightStack left behind when Orientation goes Horizontal -> Vertical). The element's
                // own default state (instance == null) holds the authored baseline, so leave it alone -
                // issue #3080 only suppressed the write; it never cleaned up a prior one.
                VariableSave? stale = stateSave.Variables.FirstOrDefault(v => v.Name == line.EffectiveLeft);
                if (instance != null)
                {
                    if (stale != null)
                    {
                        stateSave.Variables.Remove(stale);
                    }
                }
                else if (stale != null && !Equals(stale.Value, line.Value))
                {
                    // Only a value the reference wrote can be here (the variable is derived, never authored
                    // apart from it), written while the driver was off its default; once the driver is back
                    // the baseline is the resting value, which the instances without an override read.
                    stale.Value = line.Value;
                }
                continue;
            }

            stateSave.SetValue(line.EffectiveLeft, line.Value, instance);
            writtenThisPass.Add(line.EffectiveLeft);
        }
    }

    /// <summary>One reference line evaluated against a state: where it writes, what it resolves to, and whether that is the resting value.</summary>
    private sealed class EvaluatedLine
    {
        public string EffectiveLeft { get; init; } = "";
        public object Value { get; init; } = null!;
        public bool IsResting { get; init; }
    }

    /// <summary>
    /// Evaluates one reference line against <paramref name="stateSave"/>, coerced to the left-hand variable's
    /// declared type. The "resting" value is what the line resolves to when nothing is authored and every
    /// identifier resolves to its FormsProperty default; the throwaway-element ceremony required to evaluate
    /// "with nothing authored" lives in FromSyntaxNodeUsingDefaultsOnly (issue #3082).
    /// </summary>
    private static EvaluatedLine? EvaluateLine(string left, string right, InstanceSave? instance, StateSave stateSave, Func<string, object?> fallback)
    {
        if (instance != null)
        {
            // SPIKE #6408: FindAncestor<BehaviorName>() -> name of the nearest ancestor instance with that behavior.
            string? withAncestors = ResolveFindAncestor(right, instance, stateSave);
            if (withAncestors == null)
            {
                return null;
            }
            right = ElementSaveExtensions.ResolveOwnerPrefix(
                withAncestors, instance.Name, ElementSaveExtensions.GetReferencableInstanceNames(instance.ParentContainer));
            right = QualifyBareIdentifiersWithInstance(right, instance.Name);
        }

        string rightAsCSharp = EvaluatedSyntax.ConvertToCSharpSyntax(right);
        ExpressionSyntax rightSyntax = SyntaxFactory.ParseExpression(rightAsCSharp);
        EvaluatedSyntax? evaluated = EvaluatedSyntax.FromSyntaxNode(rightSyntax, stateSave, fallback);
        if (evaluated?.Value == null)
        {
            return null;
        }

        string effectiveLeft = instance == null ? left : $"{instance.Name}.{left}";

        EvaluatedSyntax? resting = EvaluatedSyntax.FromSyntaxNodeUsingDefaultsOnly(rightSyntax, fallback);
        bool isResting = resting != null && Equals(resting.Value, evaluated.Value);

        // Coerce the evaluated value to the left-hand variable's declared type - the same step the
        // state-level VariableReferences apply path performs via EvaluatedSyntax.CastTo (see
        // ApplyVariableReferencesOnSpecificOwner). The tool-only applier evaluates the RHS directly, so
        // without this an enum-typed target like ChildrenLayout would store the raw string a ternary
        // produces instead of the boxed enum (which the typed wireframe setter and int-on-disk serializer require).
        string? leftSideType = ResolveLeftSideType(effectiveLeft, instance, stateSave);
        if (leftSideType != null)
        {
            evaluated.CastTo(leftSideType);
        }

        return new EvaluatedLine { EffectiveLeft = effectiveLeft, Value = evaluated.Value!, IsResting = isResting };
    }

    // Group 1: a string literal (left untouched). Group 2: the behavior name of a FindAncestor<Name>() call, with
    // or without the owner prefix. A behavior name must be an identifier; anything else never matches, so the
    // line is left as written and evaluates to nothing.
    private static readonly System.Text.RegularExpressions.Regex FindAncestorCall =
        new System.Text.RegularExpressions.Regex(@"(""(?:[^""\\]|\\.)*"")|@?FindAncestor\s*<\s*(\w+)\s*>\s*\(\s*\)");

    /// <summary>
    /// SPIKE #6408. Replaces each <c>FindAncestor&lt;Behavior&gt;()</c> with the name of the nearest ancestor
    /// instance (walking <c>Parent</c> strings) whose component, or a base component, has that behavior.
    /// Null when any call finds no ancestor.
    /// </summary>
    private static string? ResolveFindAncestor(string right, InstanceSave instance, StateSave stateSave)
    {
        if (!FindAncestorCall.IsMatch(right))
        {
            return right;
        }

        bool allFound = true;
        string result = FindAncestorCall.Replace(right, match =>
        {
            if (match.Groups[1].Success)
            {
                return match.Value; // a string literal
            }

            string? name = FindAncestorWithBehavior(instance, stateSave, match.Groups[2].Value);
            if (name == null)
            {
                allFound = false;
                return match.Value;
            }
            return name;
        });
        return allFound ? result : null;
    }

    private static string? FindAncestorWithBehavior(InstanceSave instance, StateSave stateSave, string behaviorName)
    {
        ElementSave? container = instance.ParentContainer ?? stateSave.ParentContainer;
        if (container == null)
        {
            return null;
        }

        RecursiveVariableFinder finder = new RecursiveVariableFinder(stateSave);
        InstanceSave current = instance;
        HashSet<string> visited = new HashSet<string>();
        while (visited.Add(current.Name))
        {
            string? parent = finder.GetValue(current.Name + ".Parent") as string;
            if (string.IsNullOrEmpty(parent))
            {
                return null;
            }

            // "TabControl1.TabStripInstance": the first segment is the instance in this element.
            string ownerName = parent.Contains('.') ? parent.Substring(0, parent.IndexOf('.')) : parent;
            // The owner may be declared by a base element of the container (a derived component or screen).
            InstanceSave? owner = null;
            foreach (ElementSave element in ObjectFinder.Self.GetBaseElements(container).Prepend(container))
            {
                owner = element.Instances.FirstOrDefault(item => item.Name == ownerName);
                if (owner != null)
                {
                    break;
                }
            }
            if (owner == null)
            {
                return null;
            }

            ElementSave? ownerElement = ObjectFinder.Self.GetElementSave(owner.BaseType);
            if (ownerElement != null)
            {
                List<ElementSave> chain = new List<ElementSave>(ObjectFinder.Self.GetBaseElements(ownerElement)) { ownerElement };
                if (chain.Any(item => item.Behaviors.Any(reference => reference.BehaviorName == behaviorName)))
                {
                    return owner.Name;
                }
            }

            current = owner;
        }

        return null;
    }

    private static string? ResolveLeftSideType(string qualifiedLeftName, InstanceSave? instance, StateSave stateSave)
    {
        VariableSave? variableOnState = stateSave.Variables.FirstOrDefault(item => item.Name == qualifiedLeftName);
        if (variableOnState?.Type != null)
        {
            return variableOnState.Type;
        }

        ElementSave? elementOwningInstance = instance?.ParentContainer ?? stateSave.ParentContainer;
        return elementOwningInstance == null
            ? null
            : ObjectFinder.Self.GetRootVariable(qualifiedLeftName, elementOwningInstance)?.Type;
    }

    /// <summary>
    /// Builds a name-resolver that looks up bare or instance-qualified identifiers first
    /// against <paramref name="component"/>'s own default state (its authored baseline) and
    /// then against every linked behavior's <c>FormsProperty</c> declarations, returning the
    /// declared <c>Value</c>. Used by <see cref="EvaluatedSyntax.FromSyntaxNode"/> as a
    /// fallback when the state (the instance's state, when <paramref name="instance"/> is
    /// non-null) has no authored value — mirroring the three-tier default resolution the rest
    /// of the Forms-property feature uses (instance override → component default → behavior
    /// declaration, see the <c>gum-forms-behaviors</c> skill). Consulting only the static
    /// declaration here — skipping the component-default tier — was issue #4904: once a
    /// component's own default diverged from the behavior's original declared default (e.g.
    /// Orientation edited from the declared "Vertical" to "Horizontal"), an instance whose
    /// value still equalled that stale declared default was wrongly resolved (here, and via
    /// the "resting" comparison in <see cref="ApplyLine"/> that reuses this fallback) as if
    /// nothing had changed, instead of inheriting the component's current default.
    /// </summary>
    private static Func<string, object?> BuildFormsPropertyDefaultsFallback(ComponentSave component, InstanceSave? instance)
    {
        string? instancePrefix = instance != null ? instance.Name + "." : null;
        StateSave? componentDefaultState = instance != null ? component.DefaultState : null;
        ElementSave? container = instance?.ParentContainer;
        return name =>
        {
            // SPIKE #6408: "OtherInstance.Prop" - the declared default of ANOTHER instance's Forms property.
            if (container != null && instancePrefix != null && !name.StartsWith(instancePrefix) && name.Contains('.'))
            {
                string otherName = name.Substring(0, name.IndexOf('.'));
                string otherProp = name.Substring(name.IndexOf('.') + 1);
                InstanceSave? other = container.Instances.FirstOrDefault(item => item.Name == otherName);
                if (ObjectFinder.Self.GetElementSave(other?.BaseType) is ComponentSave otherComponent)
                {
                    return BuildFormsPropertyDefaultsFallback(otherComponent, other)(otherName + "." + otherProp);
                }
            }

            string lookupName = instancePrefix != null && name.StartsWith(instancePrefix)
                ? name.Substring(instancePrefix.Length)
                : name;

            object? componentValue = componentDefaultState?.GetValue(lookupName);
            if (componentValue != null)
            {
                return componentValue;
            }

            foreach (VariableSave declaration in FormsBehaviorChain.GetFormsPropertyDeclarations(component))
            {
                if (declaration.Name == lookupName)
                {
                    return declaration.Value;
                }
            }

            return null;
        };
    }

    /// <summary>
    /// Re-writes the right-hand side of an assignment so every bare identifier becomes
    /// <c>{instanceName}.{identifier}</c>, leaving qualified names and member-access
    /// expressions alone. Behavior references name the instance's own Forms properties, so they are
    /// qualified here; the Variable References grid leaves bare names as typed.
    /// </summary>
    private static string QualifyBareIdentifiersWithInstance(string right, string instanceName)
    {
        SyntaxNode tree = CSharpSyntaxTree.ParseText(right).GetCompilationUnitRoot();

        var bareIdentifiers = tree.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Where(id => !EvaluatedSyntax.IsFunctionName(id)
                         && id.Parent is not MemberAccessExpressionSyntax
                         && id.Parent is not AliasQualifiedNameSyntax)
            .ToList();

        SyntaxNode rewritten = tree.ReplaceNodes(
            bareIdentifiers,
            (node, _) => SyntaxFactory.IdentifierName($"{instanceName}.{node}"));

        return rewritten.ToString();
    }
}
