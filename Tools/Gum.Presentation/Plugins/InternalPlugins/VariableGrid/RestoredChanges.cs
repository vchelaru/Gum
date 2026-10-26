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

    /// <summary>
    /// The variables fed by rows <paramref name="old"/> had and <paramref name="now"/> no longer has. Their
    /// values stay what they were: a removed reference means the value cannot be set that way, not that it
    /// resets.
    /// </summary>
    public static IEnumerable<string> RemovedWrittenNames(StateSave now, StateSave? old)
    {
        if (old == null)
        {
            yield break;
        }
        HashSet<string> remaining = RowKeys(now);
        foreach (string key in RowKeys(old).Where(item => !remaining.Contains(item)))
        {
            int bar = key.IndexOf('|');
            string owner = key.Substring(0, bar);
            string line = key.Substring(bar + 1);
            int equals = line.IndexOf('=');
            if (equals <= 0 || line.StartsWith("//"))
            {
                continue;
            }
            string left = line.Substring(0, equals).Trim();
            yield return owner.Length == 0 ? left : owner + "." + left;
        }
    }

    private static HashSet<string> RowKeys(StateSave state) => state.VariableLists
        .Where(list => list.GetRootName() == "VariableReferences")
        .SelectMany(list => list.ValueAsIList.OfType<string>().Select(line => RowKey(list.SourceObject, line)))
        .ToHashSet();

    public static StateSave? FindState(ElementSave? element, StateSave state, ElementSave afterElement)
    {
        // A snapshot leaves States and Categories null when its action did not change them.
        StateSaveCategory? category = afterElement.Categories.FirstOrDefault(item => item.States.Contains(state));
        IEnumerable<StateSave>? states = category == null
            ? element?.States
            : element?.Categories?.FirstOrDefault(item => item.Name == category.Name)?.States;
        if (states == null)
        {
            return null;
        }
        return states.FirstOrDefault(item => item.Name == state.Name);
    }
}
