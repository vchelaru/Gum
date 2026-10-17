using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using WpfDataUi.DataTypes;

namespace AvaloniaDataUi;

/// <summary>Two members shown side by side in one row.</summary>
internal sealed record PairedRow(InstanceMember First, InstanceMember Second);

/// <summary>
/// The rows a category shows: its members, with each <see cref="RowPair"/> whose two members are both
/// present folded into one <see cref="PairedRow"/> at the first member's place. It follows the member
/// list, changing only the rows that appear or leave, so a filter keystroke keeps the editors of the rows
/// that stay.
/// </summary>
internal sealed class RowProjection : ObservableCollection<object>
{
    private readonly ObservableCollection<InstanceMember> _members;
    private readonly IReadOnlyList<RowPair> _pairs;
    private readonly Dictionary<(InstanceMember, InstanceMember), PairedRow> _pairedRows;

    public RowProjection(ObservableCollection<InstanceMember> members, IReadOnlyList<RowPair> pairs)
    {
        _members = members;
        _pairs = pairs;
        _pairedRows = new Dictionary<(InstanceMember, InstanceMember), PairedRow>();
        _members.CollectionChanged += HandleMembersChanged;
        Sync();
    }

    private void HandleMembersChanged(object? sender, NotifyCollectionChangedEventArgs e) => Sync();

    private static bool IsNamed(InstanceMember member, string name) =>
        member.Name == name || member.Name.EndsWith("." + name, StringComparison.Ordinal);

    private List<object> Build()
    {
        Dictionary<InstanceMember, InstanceMember> secondByFirst = new Dictionary<InstanceMember, InstanceMember>();
        HashSet<InstanceMember> seconds = new HashSet<InstanceMember>();
        foreach (RowPair pair in _pairs)
        {
            InstanceMember? first = _members.FirstOrDefault(member => IsNamed(member, pair.First));
            InstanceMember? second = _members.FirstOrDefault(member => IsNamed(member, pair.Second));
            if (first != null && second != null && !seconds.Contains(first) && !secondByFirst.ContainsKey(first)
                && !secondByFirst.ContainsKey(second) && !seconds.Contains(second))
            {
                secondByFirst[first] = second;
                seconds.Add(second);
            }
        }

        List<object> rows = new List<object>();
        foreach (InstanceMember member in _members)
        {
            if (seconds.Contains(member))
            {
                continue;
            }

            if (secondByFirst.TryGetValue(member, out InstanceMember? partner))
            {
                if (!_pairedRows.TryGetValue((member, partner), out PairedRow? pairedRow))
                {
                    pairedRow = new PairedRow(member, partner);
                    _pairedRows[(member, partner)] = pairedRow;
                }
                rows.Add(pairedRow);
            }
            else
            {
                rows.Add(member);
            }
        }

        foreach ((InstanceMember, InstanceMember) key in _pairedRows.Keys.ToList())
        {
            if (!rows.Contains(_pairedRows[key]))
            {
                _pairedRows.Remove(key);
            }
        }
        return rows;
    }

    private void Sync()
    {
        List<object> desired = Build();

        for (int i = Count - 1; i >= 0; i--)
        {
            if (!desired.Contains(this[i]))
            {
                RemoveAt(i);
            }
        }

        for (int i = 0; i < desired.Count; i++)
        {
            if (i >= Count)
            {
                Add(desired[i]);
            }
            else if (!ReferenceEquals(this[i], desired[i]))
            {
                int existing = IndexOf(desired[i]);
                if (existing > i)
                {
                    Move(existing, i);
                }
                else
                {
                    Insert(i, desired[i]);
                }
            }
        }
    }
}
