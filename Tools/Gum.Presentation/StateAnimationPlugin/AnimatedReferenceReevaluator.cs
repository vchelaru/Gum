using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using GumRuntime;
using Gum.Wireframe;
using System;
using System.Collections.Generic;

namespace StateAnimationPlugin;

/// <summary>
/// Re-evaluates the variable-reference rows an animation preview tick affects, so a variable that
/// references an animated variable follows it in the wireframe (issue #5662).
/// </summary>
/// <remarks>
/// Rows are evaluated against the tick's throwaway state, which is where the animated values live,
/// so the evaluator sees them without any extra plumbing. Only rows that read an animated variable
/// (directly or through another affected row) are evaluated: each evaluation parses an expression
/// with Roslyn, so evaluating every row on every tick would make cost scale with the screen's
/// total reference count instead of with what the animation touches.
/// </remarks>
internal static class AnimatedReferenceReevaluator
{
    private const string ReferencesName = "VariableReferences";

    /// <summary>
    /// The names of every variable the given states set, such as <c>Width</c> or <c>Instance.Width</c>.
    /// </summary>
    public static HashSet<string> GetVariableNames(IEnumerable<StateSave> states)
    {
        HashSet<string> names = new HashSet<string>();
        foreach (StateSave state in states)
        {
            foreach (VariableSave variable in state.Variables)
            {
                names.Add(variable.Name);
            }
        }
        return names;
    }

    /// <summary>
    /// Replaces the reference rows on <paramref name="state"/> with only those affected by
    /// <paramref name="animatedNames"/>, then applies them to <paramref name="state"/>. The state is
    /// modified, so it must be a throwaway such as the one a preview tick produces.
    /// </summary>
    /// <param name="animatesUnknownVariables">True when the animation can set variables that are not in
    /// <paramref name="animatedNames"/> (sub-animations), so every row is treated as affected.</param>
    public static void Apply(ElementSave element, StateSave state, HashSet<string> animatedNames,
        bool animatesUnknownVariables, GraphicalUiElement? liveRoot)
    {
        List<VariableListSave> referenceLists = new List<VariableListSave>();
        foreach (VariableListSave list in state.VariableLists)
        {
            if (list.GetRootName() == ReferencesName && list.ValueAsIList.Count > 0)
            {
                referenceLists.Add(list);
            }
        }

        //////////////////////// EARLY OUT
        if (referenceLists.Count == 0)
        {
            return;
        }
        ////////////////////// END EARLY OUT

        List<VariableListSave<string>> affected = FindAffectedLists(
            element, referenceLists, animatedNames, animatesUnknownVariables);

        state.VariableLists.RemoveAll(list => list.GetRootName() == ReferencesName);
        if (affected.Count == 0)
        {
            return;
        }

        state.VariableLists.AddRange(affected);
        element.ApplyVariableReferences(state, liveRoot, isFullCommit: false, notifyChanges: false);
    }

    // Each pass adds the rows that read a variable written by an earlier pass, and becomes its own
    // list, so applying the lists in order evaluates a chain (A reads B, B reads the animated C)
    // source first.
    private static List<VariableListSave<string>> FindAffectedLists(ElementSave element,
        List<VariableListSave> referenceLists, HashSet<string> animatedNames, bool animatesUnknownVariables)
    {
        List<(string? SourceObject, string Left, string Right, string Line)> remaining = new();
        foreach (VariableListSave list in referenceLists)
        {
            InstanceSave? instance = string.IsNullOrEmpty(list.SourceObject) ? null : element.GetInstance(list.SourceObject);
            ElementSave? channelOwner = instance != null ? ObjectFinder.Self.GetElementSave(instance) : element;
            foreach (string referenceString in list.ValueAsIList)
            {
                foreach (string line in ElementSaveExtensions.ExpandCompositeReferenceLine(referenceString, channelOwner))
                {
                    int equalsIndex = line.IndexOf('=');
                    if (line.StartsWith("//") || equalsIndex < 0)
                    {
                        continue;
                    }
                    string left = line.Substring(0, equalsIndex).Trim();
                    string right = ElementSaveExtensions.ResolveOwnerPrefix(
                        line.Substring(equalsIndex + 1).Trim(), list.SourceObject);
                    right = StripOwnElementQualifier(right, element);
                    remaining.Add((list.SourceObject, left, right, line));
                }
            }
        }

        HashSet<string> writtenNames = new HashSet<string>(animatedNames);
        List<VariableListSave<string>> result = new List<VariableListSave<string>>();

        bool foundAny = true;
        while (foundAny && remaining.Count > 0)
        {
            foundAny = false;
            Dictionary<string, VariableListSave<string>> passLists = new Dictionary<string, VariableListSave<string>>();
            List<string> newNames = new List<string>();

            for (int i = remaining.Count - 1; i >= 0; i--)
            {
                var row = remaining[i];
                if (!animatesUnknownVariables && !ReadsAny(row.Right, writtenNames))
                {
                    continue;
                }

                remaining.RemoveAt(i);
                foundAny = true;

                string listName = string.IsNullOrEmpty(row.SourceObject)
                    ? ReferencesName
                    : row.SourceObject + "." + ReferencesName;
                if (!passLists.TryGetValue(listName, out VariableListSave<string>? passList))
                {
                    passList = new VariableListSave<string> { Name = listName, Type = "string" };
                    passLists[listName] = passList;
                }
                // Walking backwards, so insert at the front to keep authored order.
                passList.Value.Insert(0, row.Line);

                newNames.Add(string.IsNullOrEmpty(row.SourceObject) ? row.Left : row.SourceObject + "." + row.Left);
            }

            result.AddRange(passLists.Values);
            foreach (string name in newNames)
            {
                writtenNames.Add(name);
            }
        }

        return result;
    }

    // The tool writes same-element references qualified with the element's own name, as in
    // "Components/Foo.WaveValue". Dropping that qualifier leaves the plain variable path the animated
    // names are written in.
    private static string StripOwnElementQualifier(string right, ElementSave element)
    {
        foreach (string folder in new[] { "Components/", "Screens/", "Standards/" })
        {
            right = right.Replace(folder + element.Name + ".", string.Empty);
        }
        return right;
    }

    // True when the right side mentions any of the names as a whole variable path. A name preceded by
    // '.' is a member of something else (Width inside Other.Width), so it does not count. Absolute*
    // identifiers are read from the live layout, which an animated size or position changes, so they
    // always count.
    private static bool ReadsAny(string right, HashSet<string> names)
    {
        if (right.IndexOf("Absolute", StringComparison.Ordinal) >= 0)
        {
            return true;
        }

        foreach (string name in names)
        {
            int index = right.IndexOf(name, StringComparison.Ordinal);
            while (index >= 0)
            {
                int end = index + name.Length;
                bool startsClean = index == 0 || !IsPathChar(right[index - 1]);
                bool endsClean = end == right.Length || !IsIdentifierChar(right[end]);
                if (startsClean && endsClean)
                {
                    return true;
                }
                index = right.IndexOf(name, index + 1, StringComparison.Ordinal);
            }
        }
        return false;
    }

    private static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    private static bool IsPathChar(char c) => IsIdentifierChar(c) || c == '.';
}
