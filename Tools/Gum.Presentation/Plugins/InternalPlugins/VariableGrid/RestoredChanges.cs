using Gum.DataTypes;
using Gum.DataTypes.Variables;
using GumRuntime;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// What an undo or redo changed in an element: the variables whose stored value differs from before the
/// restore, and the reference rows the restore added. Those are the inputs a typed edit would have changed.
/// </summary>
internal sealed class RestoredChanges
{
    /// <summary>Variable names relative to the element, such as <c>Source.Width</c>, changed in any state.</summary>
    public HashSet<string> VariableNames { get; } = new HashSet<string>();

    /// <summary>Keys (see <see cref="RowKey"/>) of rows present now that were not before.</summary>
    public HashSet<string> AddedRows { get; } = new HashSet<string>();

    public bool IsEmpty => VariableNames.Count == 0 && AddedRows.Count == 0;

    public static string RowKey(string? owner, string line) => (owner ?? string.Empty) + "|" + line;

    public static RestoredChanges Find(ElementSave after, ElementSave before)
    {
        RestoredChanges result = new RestoredChanges();
        foreach (StateSave state in after.AllStates)
        {
            StateSave? old = FindState(before, state, after);
            Dictionary<string, object?> oldValues = old?.Variables.ToDictionary(item => item.Name, item => item.Value)
                ?? new Dictionary<string, object?>();
            foreach (VariableSave variable in state.Variables)
            {
                if (!oldValues.TryGetValue(variable.Name, out object? oldValue) || !ElementSaveExtensions.ValueEquality(oldValue, variable.Value))
                {
                    result.VariableNames.Add(variable.Name);
                }
                oldValues.Remove(variable.Name);
            }
            // A variable the restore removed.
            result.VariableNames.UnionWith(oldValues.Keys);

            HashSet<string> oldRows = old == null ? new HashSet<string>() : RowKeys(old);
            result.AddedRows.UnionWith(RowKeys(state).Where(key => !oldRows.Contains(key)));
        }
        return result;
    }

    private static HashSet<string> RowKeys(StateSave state) => state.VariableLists
        .Where(list => list.GetRootName() == "VariableReferences")
        .SelectMany(list => list.ValueAsIList.OfType<string>().Select(line => RowKey(list.SourceObject, line)))
        .ToHashSet();

    private static StateSave? FindState(ElementSave element, StateSave state, ElementSave afterElement)
    {
        StateSaveCategory? category = afterElement.Categories.FirstOrDefault(item => item.States.Contains(state));
        IEnumerable<StateSave> states = category == null
            ? element.States
            : element.Categories.FirstOrDefault(item => item.Name == category.Name)?.States ?? Enumerable.Empty<StateSave>();
        return states.FirstOrDefault(item => item.Name == state.Name);
    }
}
