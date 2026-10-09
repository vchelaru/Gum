using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GumRuntime;

/// <summary>
/// One <c>VariableReferences</c> assignment, such as <c>Y = Math.Sin(Progress)</c>, with the variables
/// its right side reads.
/// </summary>
public sealed class ReferenceRow
{
    internal ReferenceRow(string? sourceObject, InstanceSave? instance, string left, string line,
        string[] reads, bool readsLiveLayout, int authoredIndex)
    {
        SourceObject = sourceObject;
        Instance = instance;
        Left = left;
        WrittenName = string.IsNullOrEmpty(sourceObject) ? left : sourceObject + "." + left;
        Line = line;
        ReadNames = reads;
        ReadsLiveLayout = readsLiveLayout;
        AuthoredIndex = authoredIndex;
    }

    /// <summary>The instance that owns the row, or null for a row on the element itself.</summary>
    public string? SourceObject { get; }

    /// <summary>The instance named by <see cref="SourceObject"/>, or null for a row on the element itself.</summary>
    public InstanceSave? Instance { get; }

    /// <summary>The variable the row writes, relative to its owner, such as <c>Y</c>.</summary>
    public string Left { get; }

    /// <summary>The variable the row writes, relative to the element, such as <c>Instance.Y</c>.</summary>
    public string WrittenName { get; }

    /// <summary>The full assignment, such as <c>Y = Math.Sin(Progress)</c>.</summary>
    public string Line { get; }

    /// <summary>
    /// The variables the right side reads, relative to the element, such as <c>Progress</c> or
    /// <c>Instance.Width</c>. References to other elements are not listed.
    /// </summary>
    public IReadOnlyList<string> Reads => ReadNames;

    /// <summary>True when the right side reads a value that comes from the laid-out visual, such as <c>AbsoluteWidth</c>.</summary>
    public bool ReadsLiveLayout { get; }

    internal string[] ReadNames { get; }

    internal int AuthoredIndex { get; }

    public override string ToString() => Line;
}

/// <summary>
/// The same-component <c>VariableReferences</c> rows of one element, ordered so that a row runs after
/// every row that writes a variable it reads, and the lookup that finds the rows a changed variable
/// affects. See ADR 0022.
/// </summary>
/// <remarks>
/// Derived data: build it from the rows, never edit it. <see cref="GetFor(ElementSave)"/> keeps one per
/// element and rebuilds it when the rows change. Instances are not thread safe because lookups reuse
/// scratch buffers, matching the rest of the runtime.
/// </remarks>
public sealed class VariableReferenceGraph
{
    private static readonly ConditionalWeakTable<ElementSave, VariableReferenceGraph> _cache =
        new ConditionalWeakTable<ElementSave, VariableReferenceGraph>();

    private static readonly string[] OwnerFolders = new[] { "Components/", "Screens/", "Standards/" };

    private static readonly ReferenceRow[] NoRows = new ReferenceRow[0];

    [ThreadStatic]
    private static int _suppressionDepth;

    private readonly ReferenceRow[] _orderedRows;
    private readonly ReferenceRow[] _cyclicRows;
    private readonly ReferenceRow[] _loadOrderRows;
    private readonly bool _hasChains;
    private readonly HashSet<string> _scratch = new HashSet<string>();

    // What the graph was built from, so a changed row is noticed without rebuilding every frame.
    private readonly StateSave[] _sourceStates;
    private readonly List<VariableListSave> _sourceLists = new List<VariableListSave>();
    private readonly List<string?[]> _sourceStrings = new List<string?[]>();
    private readonly int[] _sourceListCounts;
    private readonly string? _baseType;

    private VariableReferenceGraph(ReferenceRow[] ordered, ReferenceRow[] cyclic, StateSave[] sourceStates,
        string? baseType, bool hasChains)
    {
        _hasChains = hasChains;
        _orderedRows = ordered;
        _cyclicRows = cyclic;
        _sourceStates = sourceStates;
        _baseType = baseType;
        _sourceListCounts = new int[sourceStates.Length];

        List<ReferenceRow> loadOrder = new List<ReferenceRow>(ordered);
        loadOrder.AddRange(cyclic);
        _loadOrderRows = loadOrder.ToArray();
    }

    /// <summary>True when the element has at least one reference row.</summary>
    public bool HasRows => _loadOrderRows.Length > 0;

    /// <summary>True when some row reads a variable another row writes.</summary>
    public bool HasChains => _hasChains;

    /// <summary>
    /// Every row that can be re-evaluated, in dependency order: a row comes after every row writing a
    /// variable it reads. Rows that are part of a cycle are not included.
    /// </summary>
    public IReadOnlyList<ReferenceRow> OrderedRows => _orderedRows;

    /// <summary>
    /// Rows that depend on themselves, directly or through other rows. A cycle is skipped when
    /// re-evaluating live, since there is no order to evaluate it in.
    /// </summary>
    public IReadOnlyList<ReferenceRow> CyclicRows => _cyclicRows;

    /// <summary>
    /// The order to apply every row when the element is created: dependency order, with cyclic rows
    /// last in the order they were written.
    /// </summary>
    internal IReadOnlyList<ReferenceRow> LoadOrderRows => _loadOrderRows;

    /// <summary>True while rows are being re-evaluated, so the writes they make do not trigger further re-evaluation.</summary>
    internal static bool IsSuppressed => _suppressionDepth > 0;

    /// <summary>
    /// Runs <paramref name="action"/> without live re-evaluation of references, for a caller that has
    /// already evaluated them into the values it is about to apply.
    /// </summary>
    public static void RunWithoutReevaluation(Action action)
    {
        BeginSuppression();
        try
        {
            action();
        }
        finally
        {
            EndSuppression();
        }
    }

    internal static void BeginSuppression() => _suppressionDepth++;

    internal static void EndSuppression() => _suppressionDepth--;

    /// <summary>
    /// The graph for <paramref name="element"/>'s own rows and those it inherits from its base elements,
    /// built from each level's default state. Cached per element and rebuilt when the rows change.
    /// </summary>
    public static VariableReferenceGraph GetFor(ElementSave element)
    {
        if (_cache.TryGetValue(element, out VariableReferenceGraph? cached) && cached.IsCurrent(element))
        {
            return cached;
        }

        List<StateSave> states = new List<StateSave>();
        List<ElementSave> baseElements = ObjectFinder.Self.GetBaseElements(element);
        // Closest base first; the farthest base's rows come first.
        for (int i = baseElements.Count - 1; i >= 0; i--)
        {
            if (baseElements[i].DefaultState != null)
            {
                states.Add(baseElements[i].DefaultState!);
            }
        }
        if (element.DefaultState != null)
        {
            states.Add(element.DefaultState);
        }

        VariableReferenceGraph graph = Create(element, states);
        _cache.Remove(element);
        _cache.Add(element, graph);
        return graph;
    }

    /// <summary>
    /// The graph for the rows on a single <paramref name="state"/> of <paramref name="element"/>, such
    /// as the state an animation preview is applying.
    /// </summary>
    public static VariableReferenceGraph GetForState(ElementSave element, StateSave state)
    {
        return Create(element, new[] { state });
    }

    /// <summary>
    /// Builds a graph from the rows on <paramref name="states"/>, in the order given. Prefer
    /// <see cref="GetFor(ElementSave)"/>, which caches.
    /// </summary>
    public static VariableReferenceGraph Create(ElementSave element, IReadOnlyList<StateSave> states)
    {
        List<ReferenceRow> rows = new List<ReferenceRow>();
        IEnumerable<string> instanceNames = ElementSaveExtensions.GetReferencableInstanceNames(element);

        for (int stateIndex = 0; stateIndex < states.Count; stateIndex++)
        {
            foreach (VariableListSave list in states[stateIndex].VariableLists)
            {
                if (list.GetRootName() != "VariableReferences" || list.ValueAsIList.Count == 0)
                {
                    continue;
                }

                string? sourceObject = list.SourceObject;
                InstanceSave? instance = null;
                if (!string.IsNullOrEmpty(sourceObject))
                {
                    instance = element.GetInstance(sourceObject);
                    if (instance == null)
                    {
                        // The instance is gone, so there is nothing for the row to write to.
                        continue;
                    }
                }

                ElementSave? channelOwner = instance != null ? ObjectFinder.Self.GetElementSave(instance) : element;

                foreach (object? item in list.ValueAsIList)
                {
                    if (item is not string referenceString)
                    {
                        continue;
                    }

                    foreach (string line in ElementSaveExtensions.ExpandCompositeReferenceLine(referenceString, channelOwner))
                    {
                        int equalsIndex = line.IndexOf('=');
                        if (line.StartsWith("//") || equalsIndex < 0)
                        {
                            continue;
                        }

                        string left = line.Substring(0, equalsIndex).Trim();
                        string right = line.Substring(equalsIndex + 1).Trim();
                        // Assigning these through a state renames the element itself, which a live
                        // re-evaluation must never do.
                        if (left.Length == 0 || right.Length == 0 || (instance == null && (left == "Name" || left == "BaseType")))
                        {
                            continue;
                        }

                        right = ElementSaveExtensions.ResolveOwnerPrefix(right, sourceObject, instanceNames);
                        right = StripOwnElementQualifier(right, element);

                        List<string> reads = new List<string>();
                        bool readsLiveLayout = ScanReads(right, reads);

                        rows.Add(new ReferenceRow(sourceObject, instance, left, line, reads.ToArray(),
                            readsLiveLayout, rows.Count));
                    }
                }
            }
        }

        Order(rows, out ReferenceRow[] ordered, out ReferenceRow[] cyclic, out bool hasChains);

        StateSave[] sourceStates = new StateSave[states.Count];
        for (int i = 0; i < states.Count; i++)
        {
            sourceStates[i] = states[i];
        }

        VariableReferenceGraph graph = new VariableReferenceGraph(ordered, cyclic, sourceStates, element.BaseType, hasChains);
        graph.RecordSources();
        return graph;
    }

    /// <summary>
    /// Finds the rows affected when the given variables change: rows that read one of them, and rows that
    /// read a variable written by an affected row. Results are in evaluation order. Existing contents of
    /// <paramref name="results"/> are replaced.
    /// </summary>
    /// <param name="changed">The variables that changed. Only their names are used.</param>
    /// <param name="results">Receives the affected rows.</param>
    /// <param name="includeLiveLayoutRows">When true, rows reading a laid-out value such as
    /// <c>AbsoluteWidth</c> are always affected, since any change can move the layout.</param>
    public void FindAffectedRows(IReadOnlyList<VariableSave> changed, List<ReferenceRow> results,
        bool includeLiveLayoutRows = true)
    {
        results.Clear();
        if (_orderedRows.Length == 0)
        {
            return;
        }

        _scratch.Clear();
        for (int i = 0; i < changed.Count; i++)
        {
            _scratch.Add(changed[i].Name);
        }
        Select(results, false, includeLiveLayoutRows);
    }

    /// <inheritdoc cref="FindAffectedRows(IReadOnlyList{VariableSave}, List{ReferenceRow}, bool)"/>
    public void FindAffectedRows(IEnumerable<string> changedNames, List<ReferenceRow> results,
        bool includeLiveLayoutRows = true)
    {
        results.Clear();
        if (_orderedRows.Length == 0)
        {
            return;
        }

        _scratch.Clear();
        foreach (string name in changedNames)
        {
            _scratch.Add(name);
        }
        Select(results, false, includeLiveLayoutRows);
    }

    /// <summary>
    /// Finds the rows affected when a single variable changes. See
    /// <see cref="FindAffectedRows(IReadOnlyList{VariableSave}, List{ReferenceRow}, bool)"/>.
    /// </summary>
    public void FindAffectedRows(string changedName, List<ReferenceRow> results, bool includeLiveLayoutRows = true)
    {
        results.Clear();
        if (_orderedRows.Length == 0)
        {
            return;
        }

        _scratch.Clear();
        _scratch.Add(changedName);
        Select(results, false, includeLiveLayoutRows);
    }

    /// <summary>
    /// Evaluates every row in dependency order onto <paramref name="liveRoot"/>, so a row reading another
    /// row's result sees it whatever order the rows are written in. Rows are read from the element's
    /// authored values. The authored data is not changed.
    /// </summary>
    /// <param name="element">The element the graph was built for.</param>
    /// <param name="liveRoot">The visual for <paramref name="element"/>.</param>
    public void ApplyAll(ElementSave element, GraphicalUiElement liveRoot)
    {
        StateSave evaluationState = new StateSave { Name = "References", ParentContainer = element };
        Evaluate(_orderedRows, evaluationState, liveRoot);
    }

    /// <summary>
    /// Every re-evaluable row in evaluation order, for a caller that cannot tell which variables changed.
    /// </summary>
    public void FindAllRows(List<ReferenceRow> results)
    {
        results.Clear();
        results.AddRange(_orderedRows);
    }

    /// <summary>
    /// True when changing the variable can affect a row: a row reads it, or reads a laid-out value. A
    /// cheap check that avoids building anything for a change no row cares about.
    /// </summary>
    public bool IsAffectedBy(string variableName)
    {
        for (int i = 0; i < _orderedRows.Length; i++)
        {
            ReferenceRow row = _orderedRows[i];
            if (row.ReadsLiveLayout)
            {
                return true;
            }
            string[] reads = row.ReadNames;
            for (int j = 0; j < reads.Length; j++)
            {
                if (string.Equals(reads[j], variableName, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// <see cref="IsAffectedBy(string)"/> for the variable <c>instanceName.memberName</c>, without building the name.
    /// </summary>
    public bool IsAffectedBy(string instanceName, string memberName)
    {
        for (int i = 0; i < _orderedRows.Length; i++)
        {
            ReferenceRow row = _orderedRows[i];
            if (row.ReadsLiveLayout)
            {
                return true;
            }
            string[] reads = row.ReadNames;
            for (int j = 0; j < reads.Length; j++)
            {
                string read = reads[j];
                if (read.Length == instanceName.Length + 1 + memberName.Length
                    && read[instanceName.Length] == '.'
                    && string.CompareOrdinal(read, 0, instanceName, 0, instanceName.Length) == 0
                    && string.CompareOrdinal(read, instanceName.Length + 1, memberName, 0, memberName.Length) == 0)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void Select(List<ReferenceRow> results, bool all, bool includeLiveLayoutRows)
    {
        for (int i = 0; i < _orderedRows.Length; i++)
        {
            ReferenceRow row = _orderedRows[i];
            if (all || (includeLiveLayoutRows && row.ReadsLiveLayout) || ReadsAny(row, _scratch))
            {
                results.Add(row);
                _scratch.Add(row.WrittenName);
            }
        }
    }

    private static bool ReadsAny(ReferenceRow row, HashSet<string> names)
    {
        string[] reads = row.ReadNames;
        for (int i = 0; i < reads.Length; i++)
        {
            if (names.Contains(reads[i]))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Evaluates each row against <paramref name="evaluationState"/>, in order, writing every result back
    /// into that state so later rows read it, and, when <paramref name="liveRoot"/> is given, onto the
    /// live visual.
    /// </summary>
    /// <param name="rows">The rows to evaluate, in dependency order, such as from <see cref="FindAffectedRows(string, List{ReferenceRow}, bool)"/>.</param>
    /// <param name="evaluationState">Holds the values the rows should read in place of the authored ones.
    /// Its <c>ParentContainer</c> must be the element the rows belong to. It is modified.</param>
    /// <param name="liveRoot">The visual for the element, or null to update only the state.</param>
    public static void Evaluate(IReadOnlyList<ReferenceRow> rows, StateSave evaluationState, GraphicalUiElement? liveRoot)
    {
        BeginSuppression();
        try
        {
            for (int i = 0; i < rows.Count; i++)
            {
                ReferenceRow row = rows[i];
                ElementSaveExtensions.VariableReferenceAssignmentResult result =
                    ElementSaveExtensions.ApplyVariableReferencesOnSpecificOwner(
                        row.Instance, row.Line, evaluationState, liveRoot);

                if (liveRoot == null || result.NewValue == null || result.VariableName == null)
                {
                    continue;
                }

                GraphicalUiElement? owner = string.IsNullOrEmpty(row.SourceObject)
                    ? liveRoot
                    : liveRoot.GetGraphicalUiElementByName(row.SourceObject);
                owner?.SetProperty(row.Left, result.NewValue);
            }
        }
        finally
        {
            EndSuppression();
        }
    }

    // Kahn's algorithm, always taking the earliest-written ready row, so rows with no dependency between
    // them keep their written order and an already-ordered list comes out unchanged.
    private static void Order(List<ReferenceRow> rows, out ReferenceRow[] ordered, out ReferenceRow[] cyclic,
        out bool hasChains)
    {
        hasChains = false;
        int count = rows.Count;
        if (count == 0)
        {
            ordered = NoRows;
            cyclic = NoRows;
            return;
        }

        Dictionary<string, List<int>> writersByName = new Dictionary<string, List<int>>();
        for (int i = 0; i < count; i++)
        {
            if (!writersByName.TryGetValue(rows[i].WrittenName, out List<int>? writers))
            {
                writers = new List<int>();
                writersByName[rows[i].WrittenName] = writers;
            }
            writers.Add(i);
        }

        // dependents[w] = rows that must run after row w
        List<int>[] dependents = new List<int>[count];
        int[] waitingOn = new int[count];
        for (int i = 0; i < count; i++)
        {
            dependents[i] = new List<int>();
        }
        for (int reader = 0; reader < count; reader++)
        {
            HashSet<int> writersOfReader = new HashSet<int>();
            foreach (string read in rows[reader].ReadNames)
            {
                if (writersByName.TryGetValue(read, out List<int>? writers))
                {
                    foreach (int writer in writers)
                    {
                        writersOfReader.Add(writer);
                    }
                }
            }
            foreach (int writer in writersOfReader)
            {
                dependents[writer].Add(reader);
                waitingOn[reader]++;
                hasChains = true;
            }
        }

        bool[] done = new bool[count];
        List<ReferenceRow> orderedList = new List<ReferenceRow>(count);
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
            orderedList.Add(rows[next]);
            foreach (int dependent in dependents[next])
            {
                waitingOn[dependent]--;
            }
        }

        // What is left is on a cycle or downstream of one. Rows downstream of a cycle (nothing in the
        // remainder reads them) can still be ordered, last to first, once the cycle is set aside.
        List<ReferenceRow> downstream = new List<ReferenceRow>();
        bool peeled = true;
        while (peeled)
        {
            peeled = false;
            for (int i = count - 1; i >= 0; i--)
            {
                if (done[i])
                {
                    continue;
                }
                bool hasRemainingDependent = false;
                foreach (int dependent in dependents[i])
                {
                    if (!done[dependent] && dependent != i)
                    {
                        hasRemainingDependent = true;
                        break;
                    }
                }
                if (!hasRemainingDependent && !dependents[i].Contains(i))
                {
                    done[i] = true;
                    downstream.Add(rows[i]);
                    peeled = true;
                }
            }
        }
        downstream.Reverse();
        orderedList.AddRange(downstream);

        List<ReferenceRow> cyclicList = new List<ReferenceRow>();
        for (int i = 0; i < count; i++)
        {
            if (!done[i])
            {
                cyclicList.Add(rows[i]);
            }
        }

        ordered = orderedList.ToArray();
        cyclic = cyclicList.ToArray();
    }

    // The tool writes same-element references qualified with the element's own name, as in
    // "Components/Foo.WaveValue". Dropping that qualifier leaves the plain variable path the changed
    // names are written in.
    private static string StripOwnElementQualifier(string right, ElementSave element)
    {
        foreach (string folder in OwnerFolders)
        {
            right = right.Replace("global::" + folder + element.Name + ".", string.Empty);
            right = right.Replace(folder + element.Name + ".", string.Empty);
        }
        return right;
    }

    /// <summary>
    /// Lists the variable paths an expression reads. Function names, literals and references to other
    /// elements are skipped. Returns true when a path ends in a laid-out value such as
    /// <c>AbsoluteWidth</c>.
    /// </summary>
    /// <remarks>
    /// This is a scan rather than a parse so it works without the optional Roslyn package. It may list
    /// a path that is not a variable, which only costs an unneeded re-evaluation, and never misses one.
    /// </remarks>
    internal static bool ScanReads(string expression, List<string> reads)
    {
        bool readsLiveLayout = false;
        int i = 0;
        while (i < expression.Length)
        {
            char c = expression[i];
            if (c == '"' || c == '\'')
            {
                i = SkipQuoted(expression, i);
            }
            else if (char.IsDigit(c))
            {
                i = SkipNumber(expression, i);
            }
            else if (IsIdentifierStart(c))
            {
                int start = i;
                i = ReadPath(expression, i);
                string path = expression.Substring(start, i - start);

                if (path == "global" && i + 1 < expression.Length && expression[i] == ':' && expression[i + 1] == ':')
                {
                    i = SkipElementPath(expression, i + 2);
                    continue;
                }

                if (i < expression.Length && expression[i] == '/' && IsElementFolder(path))
                {
                    i = SkipElementPath(expression, i);
                    continue;
                }

                int next = i;
                while (next < expression.Length && expression[next] == ' ')
                {
                    next++;
                }
                bool isCall = next < expression.Length && expression[next] == '(';
                if (isCall || path == "true" || path == "false" || path == "null")
                {
                    continue;
                }

                string decoded = ElementSaveExtensions.DecodeOwnerName(path);
                reads.Add(decoded);
                if (IsLiveLayoutPath(decoded))
                {
                    readsLiveLayout = true;
                }
            }
            else
            {
                i++;
            }
        }
        return readsLiveLayout;
    }

    private static bool IsElementFolder(string path)
    {
        return path == "Components" || path == "Screens" || path == "Standards";
    }

    private static bool IsLiveLayoutPath(string path)
    {
        int lastDot = path.LastIndexOf('.');
        string last = lastDot < 0 ? path : path.Substring(lastDot + 1);
        switch (last)
        {
            case "AbsoluteX":
            case "AbsoluteY":
            case "AbsoluteLeft":
            case "AbsoluteTop":
            case "AbsoluteRight":
            case "AbsoluteBottom":
            case "AbsoluteWidth":
            case "AbsoluteHeight":
                return true;
            default:
                return false;
        }
    }

    private static bool IsIdentifierStart(char c) => char.IsLetter(c) || c == '_';

    private static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    // Reads identifier segments joined by '.', such as "Instance.Width".
    private static int ReadPath(string text, int index)
    {
        int i = index;
        while (i < text.Length)
        {
            if (IsIdentifierChar(text[i]))
            {
                i++;
            }
            else if (text[i] == '.' && i + 1 < text.Length && IsIdentifierStart(text[i + 1]))
            {
                i++;
            }
            else
            {
                break;
            }
        }
        return i;
    }

    // A path into another element, such as "Components/Other.Width". Not a same-component read.
    private static int SkipElementPath(string text, int index)
    {
        int i = index;
        while (i < text.Length && (IsIdentifierChar(text[i]) || text[i] == '/' || text[i] == '.'))
        {
            i++;
        }
        return i;
    }

    private static int SkipNumber(string text, int index)
    {
        int i = index;
        while (i < text.Length && (IsIdentifierChar(text[i]) || text[i] == '.'))
        {
            i++;
        }
        return i;
    }

    private static int SkipQuoted(string text, int index)
    {
        char quote = text[index];
        int i = index + 1;
        while (i < text.Length && text[i] != quote)
        {
            i += text[i] == '\\' ? 2 : 1;
        }
        return Math.Min(i + 1, text.Length);
    }

    private void RecordSources()
    {
        for (int stateIndex = 0; stateIndex < _sourceStates.Length; stateIndex++)
        {
            StateSave state = _sourceStates[stateIndex];
            _sourceListCounts[stateIndex] = state.VariableLists.Count;
            foreach (VariableListSave list in state.VariableLists)
            {
                if (list.GetRootName() != "VariableReferences")
                {
                    continue;
                }

                _sourceLists.Add(list);
                string?[] strings = new string?[list.ValueAsIList.Count];
                for (int i = 0; i < strings.Length; i++)
                {
                    strings[i] = list.ValueAsIList[i] as string;
                }
                _sourceStrings.Add(strings);
            }
        }
    }

    // True when the rows this graph was built from are unchanged. Compares references and strings
    // only, so it is cheap enough to run before every use.
    internal bool IsCurrent(ElementSave element)
    {
        if (_baseType != element.BaseType)
        {
            return false;
        }

        int listIndex = 0;
        for (int stateIndex = 0; stateIndex < _sourceStates.Length; stateIndex++)
        {
            List<VariableListSave> lists = _sourceStates[stateIndex].VariableLists;
            if (lists.Count != _sourceListCounts[stateIndex])
            {
                return false;
            }

            for (int i = 0; i < lists.Count; i++)
            {
                if (listIndex < _sourceLists.Count && ReferenceEquals(lists[i], _sourceLists[listIndex]))
                {
                    System.Collections.IList values = lists[i].ValueAsIList;
                    string?[] recorded = _sourceStrings[listIndex];
                    if (values.Count != recorded.Length)
                    {
                        return false;
                    }
                    for (int j = 0; j < recorded.Length; j++)
                    {
                        if (!string.Equals(values[j] as string, recorded[j], StringComparison.Ordinal))
                        {
                            return false;
                        }
                    }
                    listIndex++;
                }
            }
        }

        return listIndex == _sourceLists.Count;
    }
}
