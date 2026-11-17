using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using GumRuntime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Gum;

#if !IOS && !ANDROID

/// <summary>
/// Applies a reloaded project's variable references only to the elements whose result can differ from
/// what they hold, with the same result as <see cref="ElementSaveExtensions.ApplyAllVariableReferences"/>.
/// After a pass an element holds what its rows compute from what it reads, unless something it reads was
/// applied after it and changed (the pass orders by the elements rows name, not by base or instance
/// types). So an element that is unchanged since the last pass, was settled by it, and reads only such
/// elements would get back exactly what it holds. Every other element, and everything that reads it,
/// is applied, in the full pass's order.
/// </summary>
internal static class ReloadReferenceScope
{
    // What each project held right after its last pass. A project with none gets the full pass.
    private static readonly ConditionalWeakTable<GumProjectSave, Snapshot> Snapshots = new();

    private sealed class Snapshot
    {
        public Dictionary<string, Shape> ByName = new(StringComparer.OrdinalIgnoreCase);
        public object? Evaluator;
        public ILocalizationService? Localization;
        public int Language;
    }

    private sealed class Shape
    {
        public List<object?> Tokens = null!;
        public HashSet<string> Reads = null!;
        // False when something this element reads was applied after it in the pass and changed.
        public bool Settled;
    }

    /// <summary>
    /// Applies the rows of <paramref name="project"/>, comparing with its own last pass when it had one,
    /// else with <paramref name="previous"/>'s. Returns how many elements were applied.
    /// </summary>
    public static int Apply(GumProjectSave project, GumProjectSave? previous)
    {
        List<ElementSave> all = new List<ElementSave>();
        all.AddRange(project.StandardElements);
        all.AddRange(project.Components);
        all.AddRange(project.Screens);
        List<ElementSave> sorted = ReferenceDependencies.OrderByDependency(all);

        Dictionary<string, ElementSave> byName = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, ElementSave> byQualifiedName = new();
        foreach (ElementSave element in all)
        {
            byName[element.Name] = element;
            byQualifiedName[ElementReference.GetQualifiedName(element, element.Name)] = element;
        }

        Snapshot? baseline;
        if (!Snapshots.TryGetValue(project, out baseline) && previous != null && previous != project)
        {
            Snapshots.TryGetValue(previous, out baseline);
        }
        if (baseline != null && !MatchesEvaluator(baseline))
        {
            baseline = null;
        }

        Dictionary<ElementSave, HashSet<string>> reads = new();
        Dictionary<ElementSave, List<ElementSave>> sources = new();
        Dictionary<ElementSave, List<ElementSave>> readers = new();
        Dictionary<ElementSave, List<object?>> tokens = new();
        HashSet<ElementSave> dirty = new();
        foreach (ElementSave element in all)
        {
            HashSet<string> elementReads = GetReads(element, byQualifiedName);
            reads[element] = elementReads;
            List<ElementSave> elementSources = new List<ElementSave>();
            foreach (string read in elementReads)
            {
                if (byName.TryGetValue(read, out ElementSave? source) && source != element)
                {
                    elementSources.Add(source);
                    if (!readers.TryGetValue(source, out List<ElementSave>? list))
                    {
                        readers[source] = list = new List<ElementSave>();
                    }
                    list.Add(element);
                }
            }
            sources[element] = elementSources;

            List<object?> elementTokens = Capture(element);
            tokens[element] = elementTokens;
            if (baseline == null
                || !baseline.ByName.TryGetValue(element.Name, out Shape? shape)
                || !shape.Settled
                || !shape.Reads.SetEquals(elementReads)
                || !TokensEqual(shape.Tokens, elementTokens))
            {
                dirty.Add(element);
            }
        }

        Queue<ElementSave> queue = new Queue<ElementSave>(dirty);
        while (queue.Count > 0)
        {
            ElementSave changed = queue.Dequeue();
            if (readers.TryGetValue(changed, out List<ElementSave>? list))
            {
                foreach (ElementSave reader in list)
                {
                    if (dirty.Add(reader))
                    {
                        queue.Enqueue(reader);
                    }
                }
            }
        }

        List<ElementSave> toApply = new List<ElementSave>(dirty.Count);
        foreach (ElementSave element in sorted)
        {
            if (dirty.Contains(element))
            {
                toApply.Add(element);
            }
        }
        ElementSaveExtensions.ApplyVariableReferencesInOrder(toApply);

        Dictionary<ElementSave, List<object?>> after = new();
        HashSet<ElementSave> changedByApply = new();
        foreach (ElementSave element in toApply)
        {
            List<object?> captured = Capture(element);
            after[element] = captured;
            if (!TokensEqual(captured, tokens[element]))
            {
                changedByApply.Add(element);
            }
        }

        Dictionary<ElementSave, int> position = new();
        for (int i = 0; i < sorted.Count; i++)
        {
            position[sorted[i]] = i;
        }

        Snapshot snapshot = new Snapshot
        {
            Evaluator = ElementSaveExtensions.CustomEvaluateExpression,
            Localization = LocalizationRuntimeState.Current,
            Language = LocalizationRuntimeState.Current?.CurrentLanguage ?? 0,
        };
        foreach (ElementSave element in all)
        {
            bool applied = after.TryGetValue(element, out List<object?>? captured);
            snapshot.ByName[element.Name] = new Shape
            {
                Tokens = applied ? captured! : tokens[element],
                Reads = reads[element],
                // An element left alone read only settled, unchanged elements, so it stays settled.
                Settled = !applied || changedByApply.Count == 0
                    || !ReadsChangedLater(element, sources, position, changedByApply),
            };
        }
        Snapshots.AddOrUpdate(project, snapshot);
        return toApply.Count;
    }

    // True when anything the element reads, directly or through what it reads, was applied after it and changed.
    private static bool ReadsChangedLater(ElementSave element, Dictionary<ElementSave, List<ElementSave>> sources,
        Dictionary<ElementSave, int> position, HashSet<ElementSave> changedByApply)
    {
        int own = position[element];
        HashSet<ElementSave> seen = new HashSet<ElementSave> { element };
        Stack<ElementSave> pending = new Stack<ElementSave>(sources[element]);
        while (pending.Count > 0)
        {
            ElementSave source = pending.Pop();
            if (!seen.Add(source))
            {
                continue;
            }
            if (position[source] > own && changedByApply.Contains(source))
            {
                return true;
            }
            foreach (ElementSave next in sources[source])
            {
                pending.Push(next);
            }
        }
        return false;
    }

    // A row's result can change with the evaluator (Gum.Expressions initialized later) or the language.
    private static bool MatchesEvaluator(Snapshot snapshot) =>
        ReferenceEquals(snapshot.Evaluator, ElementSaveExtensions.CustomEvaluateExpression)
        && ReferenceEquals(snapshot.Localization, LocalizationRuntimeState.Current)
        && snapshot.Language == (LocalizationRuntimeState.Current?.CurrentLanguage ?? 0);

    // Every element whose values a row of this element can read: the elements its rows name, its base
    // type, and its instances' types (an instance's value falls back to its type's). Names that resolve
    // to no element are kept too, so one appearing or disappearing counts as a change.
    private static HashSet<string> GetReads(ElementSave element, Dictionary<string, ElementSave> byQualifiedName)
    {
        HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string qualified in ReferenceDependencies.GetElementsRead(element))
        {
            result.Add(byQualifiedName.TryGetValue(qualified, out ElementSave? source) ? source.Name : "?" + qualified);
        }
        if (!string.IsNullOrEmpty(element.BaseType))
        {
            result.Add(element.BaseType);
        }
        foreach (InstanceSave instance in element.Instances)
        {
            if (!string.IsNullOrEmpty(instance.BaseType))
            {
                result.Add(instance.BaseType);
            }
        }
        return result;
    }

    // Everything of an element a row reads or writes, flattened in order.
    private static List<object?> Capture(ElementSave element)
    {
        List<object?> tokens = new List<object?> { element.BaseType, element.Instances.Count };
        foreach (InstanceSave instance in element.Instances)
        {
            tokens.Add(instance.Name);
            tokens.Add(instance.BaseType);
            tokens.Add(instance.DefinedByBase);
        }
        List<StateSave> states = element.States ?? new List<StateSave>();
        tokens.Add(states.Count);
        foreach (StateSave state in states)
        {
            CaptureState(state, tokens);
        }
        List<StateSaveCategory> categories = element.Categories ?? new List<StateSaveCategory>();
        tokens.Add(categories.Count);
        foreach (StateSaveCategory category in categories)
        {
            tokens.Add(category.Name);
            tokens.Add(category.States.Count);
            foreach (StateSave state in category.States)
            {
                CaptureState(state, tokens);
            }
        }
        return tokens;
    }

    private static void CaptureState(StateSave state, List<object?> tokens)
    {
        tokens.Add(state.Name);
        tokens.Add(state.Variables.Count);
        foreach (VariableSave variable in state.Variables)
        {
            tokens.Add(variable.Name);
            tokens.Add(variable.Type);
            tokens.Add(variable.SetsValue);
            tokens.Add(variable.ExposedAsName);
            AddValue(variable.Value, tokens);
        }
        tokens.Add(state.VariableLists.Count);
        foreach (VariableListSave list in state.VariableLists)
        {
            tokens.Add(list.Name);
            tokens.Add(list.Type);
            AddValue(list.ValueAsIList, tokens);
        }
    }

    private static readonly object ListMarker = new object();

    // A list is mutable, so its items are copied rather than the list itself.
    private static void AddValue(object? value, List<object?> tokens)
    {
        if (value is IList list && value is not string)
        {
            tokens.Add(ListMarker);
            tokens.Add(list.Count);
            foreach (object? item in list)
            {
                tokens.Add(item);
            }
        }
        else
        {
            tokens.Add(value);
        }
    }

    private static bool TokensEqual(List<object?> a, List<object?> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }
        for (int i = 0; i < a.Count; i++)
        {
            if (!Equals(a[i], b[i]))
            {
                return false;
            }
        }
        return true;
    }
}

#endif
