using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace GumRuntime;

/// <summary>
/// One <c>VariableReferences</c> assignment, such as <c>Y = Math.Sin(Progress)</c>, with the variables
/// its right side reads.
/// </summary>
public sealed class ReferenceRow
{
    internal ReferenceRow(string? sourceObject, InstanceSave? instance, string left, string line,
        string[] reads, string[] elementReads, bool readsLiveLayout, bool isLiveEvaluable, int authoredIndex)
    {
        SourceObject = sourceObject;
        Instance = instance;
        Left = left;
        WrittenName = string.IsNullOrEmpty(sourceObject) ? left : sourceObject + "." + left;
        Line = line;
        ReadNames = reads;
        ElementReads = elementReads;
        ReadsLiveLayout = readsLiveLayout;
        IsLiveEvaluable = isLiveEvaluable;
        AuthoredIndex = authoredIndex;
    }

    /// <summary>
    /// The elements the right side reads, by qualified name, such as <c>Components/Styles/Colors</c>.
    /// </summary>
    public IReadOnlyList<string> ElementReads { get; }

    /// <summary>
    /// False for a row that assigns the element's own <c>Name</c> or <c>BaseType</c>. Applying it to a
    /// state changes the element itself, so it is applied once with the rest but never re-evaluated live.
    /// </summary>
    public bool IsLiveEvaluable { get; }

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

    /// <summary>
    /// The name a row reading <c>global::Localization.CurrentLanguage</c> lists in <see cref="ReferenceRow.Reads"/>.
    /// Report it as changed to re-evaluate those rows when the language switches.
    /// </summary>
    public const string CurrentLanguageReadName = "global::Localization.CurrentLanguage";

    [ThreadStatic]
    private static int _suppressionDepth;

    private readonly ReferenceRow[] _orderedRows;
    private readonly ReferenceRow[] _cyclicRows;
    private readonly ReferenceRow[] _loadOrderRows;
    private readonly ReferenceRow[] _liveRows;
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

        _loadOrderRows = ordered;

        List<ReferenceRow> live = new List<ReferenceRow>(ordered.Length);
        foreach (ReferenceRow row in ordered)
        {
            if (row.IsLiveEvaluable)
            {
                live.Add(row);
            }
        }
        _liveRows = live.ToArray();
    }

    /// <summary>True when the element has at least one reference row.</summary>
    public bool HasRows => _loadOrderRows.Length > 0 || _cyclicRows.Length > 0;

    /// <summary>True when some row reads a variable another row writes.</summary>
    public bool HasChains => _hasChains;

    /// <summary>True when a base component, rather than the element itself, owns at least one row.</summary>
    internal bool HasInheritedRows { get; private set; }

    /// <summary>
    /// Every row in dependency order: a row comes after every row writing a variable it reads. Rows that
    /// are part of a cycle are not included. Live re-evaluation skips the rows with
    /// <see cref="ReferenceRow.IsLiveEvaluable"/> false.
    /// </summary>
    public IReadOnlyList<ReferenceRow> OrderedRows => _orderedRows;

    /// <summary>
    /// Rows that depend on themselves, directly or through other rows. A cycle is never evaluated, at
    /// creation or live, since there is no order to evaluate it in: its rows keep their stored values.
    /// </summary>
    public IReadOnlyList<ReferenceRow> CyclicRows => _cyclicRows;

    /// <summary>
    /// The order to apply the rows when the element is created: dependency order. Cyclic rows are left
    /// out, so they keep their stored values.
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

        // Live evaluation never re-runs a loop through other elements as a unit; the apply paths find it
        // with GetForApply, so a change to another element cannot leave this cached graph stale.
        VariableReferenceGraph graph = Create(element, states, false);
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
    /// The graph to apply the rows of <paramref name="state"/> with: like <see cref="GetFor(ElementSave)"/>
    /// for the default state and <see cref="GetForState"/> otherwise, with rows in a loop through other
    /// elements (<c>Panel.A</c> reads <c>Other.B</c>, which reads <c>Panel.A</c>) in <see cref="CyclicRows"/>.
    /// </summary>
    internal static VariableReferenceGraph GetForApply(ElementSave element, StateSave state)
    {
        if (state != element.DefaultState)
        {
            return GetForState(element, state);
        }

        VariableReferenceGraph cached = GetFor(element);
        if (!cached.HasElementReads)
        {
            return cached;
        }

        HashSet<string>? loopRows = GetCrossLoopRows(element);
        if (loopRows == null || loopRows.Count == 0)
        {
            return cached;
        }

        List<StateSave> states = new List<StateSave>();
        List<ElementSave> baseElements = ObjectFinder.Self.GetBaseElements(element);
        for (int i = baseElements.Count - 1; i >= 0; i--)
        {
            if (baseElements[i].DefaultState != null)
            {
                states.Add(baseElements[i].DefaultState!);
            }
        }
        states.Add(state);
        return Create(element, states, true);
    }

    /// <summary>True when some row reads another element.</summary>
    internal bool HasElementReads { get; private set; }

    /// <summary>
    /// Builds a graph from the rows on <paramref name="states"/>, in the order given. Prefer
    /// <see cref="GetFor(ElementSave)"/>, which caches.
    /// </summary>
    public static VariableReferenceGraph Create(ElementSave element, IReadOnlyList<StateSave> states) =>
        Create(element, states, true);

    internal static VariableReferenceGraph Create(ElementSave element, IReadOnlyList<StateSave> states, bool findCrossElementLoops)
    {
        List<ReferenceRow> rows = new List<ReferenceRow>();
        IEnumerable<string> instanceNames = ElementSaveExtensions.GetReferencableInstanceNames(element);
        bool hasInheritedRows = false;

        for (int stateIndex = 0; stateIndex < states.Count; stateIndex++)
        {
            int rowCountBeforeState = rows.Count;
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
                        if (left.Length == 0 || right.Length == 0)
                        {
                            continue;
                        }

                        right = ElementSaveExtensions.ResolveOwnerPrefix(right, sourceObject, instanceNames);
                        right = StripOwnElementQualifier(right, element);

                        List<string> reads = new List<string>();
                        List<string> elementReads = new List<string>();
                        bool readsLiveLayout = ScanReads(right, reads, elementReads);

                        // Assigning these through a state renames the element itself, which a live
                        // re-evaluation must never do.
                        bool isLiveEvaluable = !(instance == null && (left == "Name" || left == "BaseType"));

                        rows.Add(new ReferenceRow(sourceObject, instance, left, line, reads.ToArray(),
                            elementReads.ToArray(), readsLiveLayout, isLiveEvaluable, rows.Count));
                    }
                }
            }

            // Rows a base component owns were evaluated against that component's values when the base
            // state was applied, so a derived element overriding what they read needs them again.
            // A standard element's rows are styling, which stays explicit.
            if (rows.Count > rowCountBeforeState && stateIndex < states.Count - 1 &&
                states[stateIndex].ParentContainer is not StandardElementSave)
            {
                hasInheritedRows = true;
            }
        }

        Order(rows, out ReferenceRow[] ordered, out ReferenceRow[] cyclic, out bool hasChains);

        bool hasElementReads = rows.Exists(row => row.ElementReads.Count > 0);
        if (findCrossElementLoops && hasElementReads)
        {
            MoveCrossElementLoopRows(element, ref ordered, ref cyclic);
        }

        StateSave[] sourceStates = new StateSave[states.Count];
        for (int i = 0; i < states.Count; i++)
        {
            sourceStates[i] = states[i];
        }

        VariableReferenceGraph graph = new VariableReferenceGraph(ordered, cyclic, sourceStates, element.BaseType, hasChains);
        graph.HasInheritedRows = hasInheritedRows;
        graph.HasElementReads = hasElementReads;
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
        if (_liveRows.Length == 0)
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
        if (_liveRows.Length == 0)
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
        if (_liveRows.Length == 0)
        {
            return;
        }

        _scratch.Clear();
        _scratch.Add(changedName);
        Select(results, false, includeLiveLayoutRows);
    }

    /// <summary>
    /// Evaluates every row in dependency order onto <paramref name="liveRoot"/>, so a row reading another
    /// row's result sees it whatever order the rows are written in. Rows in a cycle are skipped. Rows are
    /// read from the element's authored values. The authored data is not changed.
    /// </summary>
    /// <param name="element">The element the graph was built for.</param>
    /// <param name="liveRoot">The visual for <paramref name="element"/>.</param>
    public void ApplyAll(ElementSave element, GraphicalUiElement liveRoot)
    {
        StateSave evaluationState = new StateSave { Name = "References", ParentContainer = element };
        IReadOnlyList<ReferenceRow> rows = _liveRows;
        if (HasElementReads && element.DefaultState != null)
        {
            // A loop through other elements is not evaluated, like a loop within this one.
            IReadOnlyList<ReferenceRow> crossLoops = GetForApply(element, element.DefaultState).CyclicRows;
            List<ReferenceRow> withoutLoops = new List<ReferenceRow>(_liveRows.Length);
            foreach (ReferenceRow row in _liveRows)
            {
                bool inLoop = false;
                foreach (ReferenceRow loopRow in crossLoops)
                {
                    if (loopRow.WrittenName == row.WrittenName && loopRow.Line == row.Line)
                    {
                        inLoop = true;
                        break;
                    }
                }
                if (!inLoop)
                {
                    withoutLoops.Add(row);
                }
            }
            rows = withoutLoops;
        }
        Evaluate(rows, evaluationState, liveRoot);
    }

    /// <summary>
    /// Every re-evaluable row in evaluation order, for a caller that cannot tell which variables changed.
    /// </summary>
    public void FindAllRows(List<ReferenceRow> results)
    {
        results.Clear();
        results.AddRange(_liveRows);
    }

    /// <summary>
    /// True when changing the variable can affect a row: a row reads it, or reads a laid-out value. A
    /// cheap check that avoids building anything for a change no row cares about.
    /// </summary>
    public bool IsAffectedBy(string variableName)
    {
        for (int i = 0; i < _liveRows.Length; i++)
        {
            ReferenceRow row = _liveRows[i];
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
        for (int i = 0; i < _liveRows.Length; i++)
        {
            ReferenceRow row = _liveRows[i];
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
        for (int i = 0; i < _liveRows.Length; i++)
        {
            ReferenceRow row = _liveRows[i];
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
                        row.Instance, row.Line, evaluationState, liveRoot, propagateNull: true);

                if (liveRoot == null || (result.NewValue == null && !result.AssignsNull) || result.VariableName == null)
                {
                    continue;
                }

                GraphicalUiElement? owner = string.IsNullOrEmpty(row.SourceObject)
                    ? liveRoot
                    : liveRoot.GetGraphicalUiElementByName(row.SourceObject);
                if (owner == null)
                {
                    continue;
                }

                owner.SetProperty(row.Left, result.NewValue);
                owner.RecordReferenceInput(row.Left, result.NewValue);

                // The write above reports to nobody (see BeginSuppression). A child component's own
                // rows that read what this row just wrote still need it; the child's rows cannot lead
                // back here, so this does not loop.
                if (owner != liveRoot)
                {
                    owner.NotifyVariableChanged(row.Left, result.NewValue);
                }
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

    // A variable path read from another element, such as "Components/Other.B.Width": group 1 the element,
    // group 2 the variable path after it.
    private static readonly Regex QualifiedReadRegex = new Regex(
        @"(?<![\w.])(?:global::)?((?:Components|Screens|Standards)/[\w/]+)((?:\.\w+)*)", RegexOptions.Compiled);

    // Rows of a loop through other elements are not ordered by Order, which sees one element. They move
    // from the ordered rows to the cyclic rows, so every apply path leaves them at their stored values.
    private static void MoveCrossElementLoopRows(ElementSave element, ref ReferenceRow[] ordered, ref ReferenceRow[] cyclic)
    {
        HashSet<string>? loopRows = GetCrossLoopRows(element);
        if (loopRows == null || loopRows.Count == 0)
        {
            return;
        }

        List<ReferenceRow> remaining = new List<ReferenceRow>();
        List<ReferenceRow> moved = new List<ReferenceRow>(cyclic);
        bool movedAny = false;
        foreach (ReferenceRow row in ordered)
        {
            if (loopRows.Contains(row.WrittenName))
            {
                moved.Add(row);
                movedAny = true;
            }
            else
            {
                remaining.Add(row);
            }
        }
        if (!movedAny)
        {
            return;
        }
        moved.Sort((first, second) => first.AuthoredIndex.CompareTo(second.AuthoredIndex));
        ordered = remaining.ToArray();
        cyclic = moved.ToArray();
    }

    // Every default-state row of the project that is in a loop through two or more elements, found once
    // with one pass over all rows (Tarjan), and kept until a row anywhere changes.
    private sealed class CrossLoopIndex
    {
        public GumProjectSave Project = null!;
        public ElementSave[] Elements = null!;
        public VariableReferenceGraph[] Graphs = null!;
        public Dictionary<ElementSave, HashSet<string>> LoopRows = null!;
    }

    private static CrossLoopIndex? _crossLoopIndex;
    private static readonly object CrossLoopLock = new object();

    private static HashSet<string>? GetCrossLoopRows(ElementSave element)
    {
        GumProjectSave? project = ObjectFinder.Self.GumProjectSave;
        if (project == null)
        {
            return null;
        }

        lock (CrossLoopLock)
        {
            CrossLoopIndex? index = _crossLoopIndex;
            if (index == null || !IsCurrent(index, project))
            {
                index = BuildCrossLoopIndex(project);
                _crossLoopIndex = index;
            }
            return index.LoopRows.TryGetValue(element, out HashSet<string>? rows) ? rows : null;
        }
    }

    private static int ProjectElementCount(GumProjectSave project) =>
        project.Screens.Count + project.Components.Count + project.StandardElements.Count;

    private static ElementSave ProjectElementAt(GumProjectSave project, int index)
    {
        if (index < project.Screens.Count)
        {
            return project.Screens[index];
        }
        index -= project.Screens.Count;
        if (index < project.Components.Count)
        {
            return project.Components[index];
        }
        return project.StandardElements[index - project.Components.Count];
    }

    private static bool IsCurrent(CrossLoopIndex index, GumProjectSave project)
    {
        if (!ReferenceEquals(index.Project, project) || index.Elements.Length != ProjectElementCount(project))
        {
            return false;
        }
        for (int i = 0; i < index.Elements.Length; i++)
        {
            ElementSave element = ProjectElementAt(project, i);
            if (!ReferenceEquals(index.Elements[i], element) || !ReferenceEquals(GetFor(element), index.Graphs[i]))
            {
                return false;
            }
        }
        return true;
    }

    private static CrossLoopIndex BuildCrossLoopIndex(GumProjectSave project)
    {
        int elementCount = ProjectElementCount(project);
        CrossLoopIndex index = new CrossLoopIndex
        {
            Project = project,
            Elements = new ElementSave[elementCount],
            Graphs = new VariableReferenceGraph[elementCount],
            LoopRows = new Dictionary<ElementSave, HashSet<string>>()
        };

        // One node per (element, written variable).
        Dictionary<string, int> elementByQualifiedName = new Dictionary<string, int>();
        List<Dictionary<string, int>> nodeByName = new List<Dictionary<string, int>>();
        List<int> nodeElement = new List<int>();
        List<string> nodeName = new List<string>();
        List<ReferenceRow> nodeRows = new List<ReferenceRow>();
        List<int> nodeOfRow = new List<int>();
        for (int e = 0; e < elementCount; e++)
        {
            ElementSave element = ProjectElementAt(project, e);
            index.Elements[e] = element;
            index.Graphs[e] = GetFor(element);
            elementByQualifiedName[ElementReference.GetQualifiedName(element, element.Name)] = e;
            Dictionary<string, int> names = new Dictionary<string, int>();
            nodeByName.Add(names);
            foreach (IReadOnlyList<ReferenceRow> rows in new[] { index.Graphs[e].OrderedRows, index.Graphs[e].CyclicRows })
            {
                foreach (ReferenceRow row in rows)
                {
                    if (!names.TryGetValue(row.WrittenName, out int node))
                    {
                        node = nodeElement.Count;
                        names[row.WrittenName] = node;
                        nodeElement.Add(e);
                        nodeName.Add(row.WrittenName);
                    }
                    nodeRows.Add(row);
                    nodeOfRow.Add(node);
                }
            }
        }

        // A row reads the rows writing the variables it names: in its own element, or in another.
        List<HashSet<int>> edges = new List<HashSet<int>>();
        for (int n = 0; n < nodeElement.Count; n++)
        {
            edges.Add(new HashSet<int>());
        }
        bool anyCrossEdge = false;
        for (int r = 0; r < nodeRows.Count; r++)
        {
            ReferenceRow row = nodeRows[r];
            int node = nodeOfRow[r];
            int element = nodeElement[node];
            foreach (string read in row.Reads)
            {
                if (nodeByName[element].TryGetValue(read, out int target))
                {
                    edges[node].Add(target);
                }
            }
            if (row.ElementReads.Count == 0)
            {
                continue;
            }
            int equalsIndex = row.Line.IndexOf('=');
            if (equalsIndex < 0)
            {
                continue;
            }
            foreach (Match match in QualifiedReadRegex.Matches(row.Line.Substring(equalsIndex + 1)))
            {
                string variablePath = match.Groups[2].Value.TrimStart('.');
                if (variablePath.Length > 0 && elementByQualifiedName.TryGetValue(match.Groups[1].Value, out int otherElement)
                    && otherElement != element && nodeByName[otherElement].TryGetValue(variablePath, out int target))
                {
                    edges[node].Add(target);
                    anyCrossEdge = true;
                }
            }
        }

        if (anyCrossEdge)
        {
            MarkLoops(index, edges, nodeElement, nodeName);
        }
        return index;
    }

    // Iterative Tarjan: a chain thousands of elements long must not overflow the stack.
    private static void MarkLoops(CrossLoopIndex index, List<HashSet<int>> edges, List<int> nodeElement, List<string> nodeName)
    {
        int count = edges.Count;
        int[] order = new int[count];
        int[] low = new int[count];
        bool[] onStack = new bool[count];
        int next = 1;
        Stack<int> stack = new Stack<int>();
        Stack<(int Node, IEnumerator<int> Targets)> work = new Stack<(int, IEnumerator<int>)>();

        for (int root = 0; root < count; root++)
        {
            if (order[root] != 0)
            {
                continue;
            }
            order[root] = low[root] = next++;
            stack.Push(root);
            onStack[root] = true;
            work.Push((root, edges[root].GetEnumerator()));

            while (work.Count > 0)
            {
                (int node, IEnumerator<int> targets) = work.Peek();
                if (targets.MoveNext())
                {
                    int target = targets.Current;
                    if (order[target] == 0)
                    {
                        order[target] = low[target] = next++;
                        stack.Push(target);
                        onStack[target] = true;
                        work.Push((target, edges[target].GetEnumerator()));
                    }
                    else if (onStack[target])
                    {
                        low[node] = Math.Min(low[node], order[target]);
                    }
                    continue;
                }

                work.Pop();
                if (work.Count > 0)
                {
                    int parent = work.Peek().Node;
                    low[parent] = Math.Min(low[parent], low[node]);
                }
                if (low[node] != order[node])
                {
                    continue;
                }

                List<int> component = new List<int>();
                int member;
                do
                {
                    member = stack.Pop();
                    onStack[member] = false;
                    component.Add(member);
                }
                while (member != node);

                // A loop within one element is already in that element's cyclic rows.
                bool spansElements = false;
                foreach (int item in component)
                {
                    if (nodeElement[item] != nodeElement[component[0]])
                    {
                        spansElements = true;
                        break;
                    }
                }
                if (!spansElements)
                {
                    continue;
                }
                foreach (int item in component)
                {
                    ElementSave element = index.Elements[nodeElement[item]];
                    if (!index.LoopRows.TryGetValue(element, out HashSet<string>? names))
                    {
                        names = new HashSet<string>();
                        index.LoopRows[element] = names;
                    }
                    names.Add(nodeName[item]);
                }
            }
        }
    }

    /// <summary>
    /// The rows of a loop that leads from <paramref name="start"/> through other elements' rows back to a
    /// row of <paramref name="owner"/> writing the same variable, each with the element that holds it, or
    /// null when there is none. For naming the loop; finding the rows to skip is done once per project.
    /// </summary>
    public static List<KeyValuePair<ElementSave, ReferenceRow>>? FindCrossElementLoop(ElementSave owner, ReferenceRow start)
    {
        Dictionary<ElementSave, ReferenceRow[]> rowsByElement = new Dictionary<ElementSave, ReferenceRow[]>();
        List<KeyValuePair<ElementSave, ReferenceRow>> path = new List<KeyValuePair<ElementSave, ReferenceRow>>();
        HashSet<ReferenceRow> visited = new HashSet<ReferenceRow>();
        return SearchLoop(owner, start, owner, start, path, visited, rowsByElement) ? path : null;
    }

    private static bool SearchLoop(ElementSave startOwner, ReferenceRow start, ElementSave owner, ReferenceRow current,
        List<KeyValuePair<ElementSave, ReferenceRow>> path, HashSet<ReferenceRow> visited,
        Dictionary<ElementSave, ReferenceRow[]> rowsByElement)
    {
        path.Add(new KeyValuePair<ElementSave, ReferenceRow>(owner, current));
        visited.Add(current);

        List<KeyValuePair<ElementSave, string>> reads = new List<KeyValuePair<ElementSave, string>>();
        foreach (string read in current.Reads)
        {
            reads.Add(new KeyValuePair<ElementSave, string>(owner, read));
        }
        int equalsIndex = current.Line.IndexOf('=');
        if (equalsIndex >= 0)
        {
            foreach (Match match in QualifiedReadRegex.Matches(current.Line.Substring(equalsIndex + 1)))
            {
                string qualified = match.Groups[1].Value;
                ElementSave? referenced = ObjectFinder.Self.GetElementSave(qualified.Substring(qualified.IndexOf('/') + 1));
                string variablePath = match.Groups[2].Value.TrimStart('.');
                if (referenced != null && referenced != owner && variablePath.Length > 0)
                {
                    reads.Add(new KeyValuePair<ElementSave, string>(referenced, variablePath));
                }
            }
        }

        foreach (KeyValuePair<ElementSave, string> read in reads)
        {
            if (!rowsByElement.TryGetValue(read.Key, out ReferenceRow[]? rows))
            {
                VariableReferenceGraph graph = GetFor(read.Key);
                List<ReferenceRow> all = new List<ReferenceRow>(graph.OrderedRows);
                all.AddRange(graph.CyclicRows);
                rows = all.ToArray();
                rowsByElement[read.Key] = rows;
            }

            foreach (ReferenceRow next in rows)
            {
                if (next.WrittenName != read.Value)
                {
                    continue;
                }
                if (read.Key == startOwner && next.WrittenName == start.WrittenName)
                {
                    if (path.Exists(step => step.Key != startOwner))
                    {
                        return true;
                    }
                }
                else if (!visited.Contains(next) && SearchLoop(startOwner, start, read.Key, next, path, visited, rowsByElement))
                {
                    return true;
                }
            }
        }

        path.RemoveAt(path.Count - 1);
        return false;
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
    internal static bool ScanReads(string expression, List<string> reads) => ScanReads(expression, reads, null);

    /// <summary>
    /// Lists the variable paths an expression reads, and when <paramref name="elementReads"/> is given,
    /// the qualified names of the other elements it reads, such as <c>Components/Styles/Colors</c> for
    /// <c>Components/Styles/Colors.Red</c> (also written <c>global::Components/Styles/Colors.Red</c>).
    /// </summary>
    internal static bool ScanReads(string expression, List<string> reads, List<string>? elementReads)
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
                    if (IsCurrentLanguagePath(expression, i + 2))
                    {
                        reads.Add(CurrentLanguageReadName);
                        i += 2 + CurrentLanguagePath.Length;
                        continue;
                    }

                    i = ReadElementReference(expression, i + 2, elementReads);
                    continue;
                }

                if (i < expression.Length && expression[i] == '/' && IsElementFolder(path))
                {
                    i = ReadElementReference(expression, start, elementReads);
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

    private const string CurrentLanguagePath = "Localization.CurrentLanguage";

    // The text after "global::" is the reserved language identifier and not a longer name that starts with it.
    private static bool IsCurrentLanguagePath(string expression, int index)
    {
        int end = index + CurrentLanguagePath.Length;
        return string.CompareOrdinal(expression, index, CurrentLanguagePath, 0, CurrentLanguagePath.Length) == 0
            && (end >= expression.Length || !IsIdentifierChar(expression[end]));
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

    // A path into another element, such as "Components/Other.Width". Not a same-component read. The
    // element's name ends at the first '.'; a path with no '.' names no variable and is not a read.
    private static int ReadElementReference(string text, int index, List<string>? elementReads)
    {
        int i = index;
        while (i < text.Length && (IsIdentifierChar(text[i]) || text[i] == '/' || text[i] == '-'))
        {
            i++;
        }

        if (i < text.Length && text[i] == '.' && i + 1 < text.Length && IsIdentifierStart(text[i + 1]))
        {
            elementReads?.Add(text.Substring(index, i - index));
            i = ReadPath(text, i + 1);
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

        // A visual keeps its graph when hot reload gives it a new element of the same name, so the
        // graph must also be for this element's states and not an earlier copy's.
        if (_sourceStates.Length > 0 && element.DefaultState != null && !ReferenceEquals(_sourceStates[_sourceStates.Length - 1], element.DefaultState))
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
