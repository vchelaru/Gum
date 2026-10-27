using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using GumRuntime;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Wireframe;

/// <summary>
/// Finds the default-state reference rows that a running game would have re-driven over the selected
/// state's plain value (#6861). A game applies the state once; when a row's source later changes, the row
/// writes its value again. The canvas is rebuilt from saved data on every refresh, so this remembers each
/// overridden row's evaluated value between refreshes of the same selection: a row whose value changed
/// stays re-driven until the selection changes, which is when a game would apply the state again, or
/// until the state's own value for that variable is edited, which sets it again.
/// Rows that read another element are not tracked: a game applies the state again after those change
/// (<c>RefreshStyles</c>), so the state keeps winning.
/// </summary>
internal class ReDrivenReferenceRowTracker
{
    private readonly SelectedStateReferenceMerger _merger;
    private ElementSave? _element;
    private List<StateSave> _statesOnTop = new List<StateSave>();
    private Dictionary<string, object?> _lastValues = new Dictionary<string, object?>();
    private Dictionary<string, object?> _lastStateValues = new Dictionary<string, object?>();
    private HashSet<string> _reDrivenKeys = new HashSet<string>();

    public ReDrivenReferenceRowTracker(SelectedStateReferenceMerger merger)
    {
        _merger = merger;
    }

    /// <summary>The element whose selection is remembered, or null when nothing is.</summary>
    internal ElementSave? TrackedElement => _element;

    /// <summary>Forgets the selection, so nothing is kept once it is gone.</summary>
    public void Clear()
    {
        _element = null;
        _statesOnTop = new List<StateSave>();
        _lastValues = new Dictionary<string, object?>();
        _lastStateValues = new Dictionary<string, object?>();
        _reDrivenKeys = new HashSet<string>();
    }

    /// <summary>
    /// The default state with <paramref name="statesOnTop"/> merged over it, keeping the overridden rows
    /// whose source changed since the selection was first shown.
    /// </summary>
    public StateSave Merge(ElementSave element, StateSave defaultState, IReadOnlyList<StateSave> statesOnTop)
    {
        if (statesOnTop.Count == 0)
        {
            Clear();
            return defaultState;
        }

        List<string> overriddenKeys = new List<string>();
        StateSave everything = defaultState;
        foreach (StateSave stateOnTop in statesOnTop)
        {
            everything = _merger.Merge(everything, stateOnTop, _ => true, overriddenKeys);
        }

        UpdateReDrivenKeys(element, statesOnTop, everything, new HashSet<string>(overriddenKeys));

        StateSave result = defaultState;
        foreach (StateSave stateOnTop in statesOnTop)
        {
            result = _merger.Merge(result, stateOnTop, _reDrivenKeys.Contains, null);
        }
        return result;
    }

    private void UpdateReDrivenKeys(ElementSave element, IReadOnlyList<StateSave> statesOnTop, StateSave everything,
        HashSet<string> overriddenKeys)
    {
        bool isSameSelection = element == _element && statesOnTop.SequenceEqual(_statesOnTop);
        if (!isSameSelection)
        {
            Reset(element, statesOnTop);
        }

        Dictionary<string, object?> values = new Dictionary<string, object?>();
        Dictionary<string, object?> stateValues = new Dictionary<string, object?>();
        FillOverriddenRowValues(element, everything, statesOnTop, overriddenKeys, values, stateValues);
        if (isSameSelection)
        {
            foreach (KeyValuePair<string, object?> pair in values)
            {
                // Editing the state's own value sets the variable again, which ends a re-drive; a
                // source that changes after that re-drives it again.
                if (_lastStateValues.TryGetValue(pair.Key, out object? lastState) && !Equals(lastState, stateValues[pair.Key]))
                {
                    _reDrivenKeys.Remove(pair.Key);
                }
                else if (_lastValues.TryGetValue(pair.Key, out object? last) && !Equals(last, pair.Value))
                {
                    _reDrivenKeys.Add(pair.Key);
                }
            }
        }
        _lastValues = values;
        _lastStateValues = stateValues;
    }

    private static void FillOverriddenRowValues(ElementSave element, StateSave everything,
        IReadOnlyList<StateSave> statesOnTop, HashSet<string> overriddenKeys,
        Dictionary<string, object?> values, Dictionary<string, object?> stateValues)
    {
        List<StateSave> states = new List<StateSave>();
        List<ElementSave> baseElements = ObjectFinder.Self.GetBaseElements(element);
        for (int i = baseElements.Count - 1; i >= 0; i--)
        {
            if (baseElements[i].DefaultState != null)
            {
                states.Add(baseElements[i].DefaultState!);
            }
        }
        states.Add(everything);

        VariableReferenceGraph graph = VariableReferenceGraph.Create(element, states);
        StateSave evaluationState = everything.Clone();
        foreach (ReferenceRow row in graph.OrderedRows)
        {
            ElementSaveExtensions.VariableReferenceAssignmentResult result =
                ElementSaveExtensions.ApplyVariableReferencesOnSpecificOwner(row.Instance, row.Line, evaluationState);
            string key = SelectedStateReferenceMerger.GetRowKey(row.WrittenName, row.Line);
            if (row.IsLiveEvaluable && row.ElementReads.Count == 0 && overriddenKeys.Contains(key))
            {
                values[key] = result.NewValue;
                stateValues[key] = GetStateValue(statesOnTop, row.WrittenName);
            }
        }
    }

    private static object? GetStateValue(IReadOnlyList<StateSave> statesOnTop, string variableName)
    {
        object? value = null;
        foreach (StateSave stateOnTop in statesOnTop)
        {
            VariableSave? variable = stateOnTop.GetVariableSave(variableName);
            if (variable != null && variable.SetsValue)
            {
                value = variable.Value;
            }
        }
        return value;
    }

    private void Reset(ElementSave element, IReadOnlyList<StateSave> statesOnTop)
    {
        Clear();
        _element = element;
        _statesOnTop = statesOnTop.ToList();
    }
}
