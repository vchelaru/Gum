using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System.Collections.Generic;

namespace GumRuntime;

/// <summary>
/// Which elements' <c>VariableReferences</c> rows read which other elements, and the order elements
/// have to be applied in so a row sees the value it reads. Shares its scanner with
/// <see cref="VariableReferenceGraph"/>, so every caller agrees on what a row reads.
/// </summary>
public static class ReferenceDependencies
{
    /// <summary>
    /// True when the right side of a reference line, such as <c>Red = 1 + Components/Styles/Colors.Red</c>,
    /// reads the element anywhere in the expression.
    /// </summary>
    /// <param name="referenceLine">The whole row.</param>
    /// <param name="elementQualifiedName">The element's qualified name, such as <c>Components/Styles/Colors</c>.</param>
    public static bool RowReadsElement(string referenceLine, string elementQualifiedName)
    {
        List<string>? elementReads = ScanElementReads(referenceLine);
        return elementReads != null && elementReads.Contains(elementQualifiedName);
    }

    /// <summary>
    /// True when any row on <paramref name="state"/> reads one of the elements.
    /// </summary>
    /// <param name="state">The state whose rows to check.</param>
    /// <param name="elementQualifiedNames">Qualified names, such as <c>Components/Styles/Colors</c>.</param>
    public static bool StateReadsAny(StateSave state, ICollection<string> elementQualifiedNames)
    {
        foreach (VariableListSave list in state.VariableLists)
        {
            if (list.GetRootName() != "VariableReferences")
            {
                continue;
            }

            foreach (object? item in list.ValueAsIList)
            {
                if (item is not string line)
                {
                    continue;
                }

                List<string>? elementReads = ScanElementReads(line);
                if (elementReads == null)
                {
                    continue;
                }
                foreach (string name in elementReads)
                {
                    if (elementQualifiedNames.Contains(name))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// The qualified names of the other elements the rows on any state of <paramref name="element"/> read.
    /// </summary>
    public static HashSet<string> GetElementsRead(ElementSave element)
    {
        HashSet<string> names = new HashSet<string>();
        foreach (StateSave state in element.AllStates)
        {
            foreach (VariableListSave list in state.VariableLists)
            {
                if (list.GetRootName() != "VariableReferences")
                {
                    continue;
                }

                foreach (object? item in list.ValueAsIList)
                {
                    List<string>? elementReads = item is string line ? ScanElementReads(line) : null;
                    if (elementReads != null)
                    {
                        names.UnionWith(elementReads);
                    }
                }
            }
        }
        names.Remove(GetQualifiedName(element));
        return names;
    }

    /// <summary>
    /// Orders elements so each comes after every element it reads. Elements that read each other, and
    /// those that read them, keep their order in <paramref name="elements"/> and come last.
    /// </summary>
    public static List<ElementSave> OrderByDependency(IReadOnlyList<ElementSave> elements)
    {
        int count = elements.Count;
        Dictionary<string, int> indexByName = new Dictionary<string, int>();
        for (int i = 0; i < count; i++)
        {
            indexByName[GetQualifiedName(elements[i])] = i;
        }

        // sources[i] = elements that element i reads; dependents[s] = elements that read s
        List<int>[] dependents = new List<int>[count];
        int[] waitingOn = new int[count];
        for (int i = 0; i < count; i++)
        {
            dependents[i] = new List<int>();
        }
        for (int reader = 0; reader < count; reader++)
        {
            foreach (string name in GetElementsRead(elements[reader]))
            {
                if (indexByName.TryGetValue(name, out int source) && source != reader)
                {
                    dependents[source].Add(reader);
                    waitingOn[reader]++;
                }
            }
        }

        bool[] done = new bool[count];
        List<ElementSave> ordered = new List<ElementSave>(count);
        while (true)
        {
            int next = -1;
            for (int i = 0; i < count; i++)
            {
                if (!done[i] && waitingOn[i] == 0)
                {
                    next = i;
                    break;
                }
            }
            if (next < 0)
            {
                break;
            }

            done[next] = true;
            ordered.Add(elements[next]);
            foreach (int dependent in dependents[next])
            {
                waitingOn[dependent]--;
            }
        }

        for (int i = 0; i < count; i++)
        {
            if (!done[i])
            {
                ordered.Add(elements[i]);
            }
        }
        return ordered;
    }

    /// <summary>
    /// The elements that read <paramref name="changed"/>, directly or through other elements, ordered so
    /// each comes after every element it reads. <paramref name="changed"/> itself is not included.
    /// </summary>
    public static List<ElementSave> GetDependentsInOrder(IReadOnlyList<ElementSave> allElements, ElementSave changed)
    {
        Dictionary<ElementSave, HashSet<string>> readsByElement = new Dictionary<ElementSave, HashSet<string>>();
        foreach (ElementSave element in allElements)
        {
            readsByElement[element] = GetElementsRead(element);
        }

        HashSet<string> affected = new HashSet<string> { GetQualifiedName(changed) };
        List<ElementSave> dependents = new List<ElementSave>();
        bool foundMore = true;
        while (foundMore)
        {
            foundMore = false;
            foreach (ElementSave element in allElements)
            {
                if (element == changed || dependents.Contains(element))
                {
                    continue;
                }

                if (readsByElement[element].Overlaps(affected))
                {
                    dependents.Add(element);
                    affected.Add(GetQualifiedName(element));
                    foundMore = true;
                }
            }
        }

        // Keep project order among the dependents, then sort them by what they read.
        List<ElementSave> inProjectOrder = new List<ElementSave>();
        foreach (ElementSave element in allElements)
        {
            if (dependents.Contains(element))
            {
                inProjectOrder.Add(element);
            }
        }
        return OrderByDependency(inProjectOrder);
    }

    private static string GetQualifiedName(ElementSave element) => ElementReference.GetQualifiedName(element, element.Name);

    private static List<string>? ScanElementReads(string referenceLine)
    {
        int equalsIndex = referenceLine.IndexOf('=');
        if (equalsIndex < 0 || referenceLine.StartsWith("//"))
        {
            return null;
        }

        string right = referenceLine.Substring(equalsIndex + 1);
        // Every element path has a folder prefix, so this skips the common row cheaply.
        if (right.IndexOf('/') < 0)
        {
            return null;
        }

        List<string> elementReads = new List<string>();
        VariableReferenceGraph.ScanReads(right, new List<string>(), elementReads);
        return elementReads;
    }
}
