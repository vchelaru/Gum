using System;
using System.Collections.Generic;
using System.Linq;
using WpfDataUi.DataTypes;

namespace WpfDataUi;

/// <summary>
/// Narrows a grid's categories to the members matching a predicate, and puts them back when the
/// predicate is dropped. Backs the Variables tab's filter box (#4631).
/// </summary>
/// <remarks>
/// Hiding a member means removing it from its category's <c>Members</c>, matching how
/// <c>DataUiGrid</c> already hides delegate-driven members. That keeps row striping correct and reuses
/// <see cref="MemberCategory.Visibility"/>, which collapses a category once it holds nothing. Removal
/// loses the member's position though, so the pre-filter list is snapshotted and every narrowing is
/// derived from it rather than from the currently shown rows.
/// </remarks>
public class MemberCategoryFilter
{
    private readonly Dictionary<MemberCategory, CategorySnapshot> _snapshots;

    /// <summary>Whether a predicate is currently narrowing the categories.</summary>
    public bool IsFiltering { get; private set; }

    public MemberCategoryFilter()
    {
        _snapshots = new Dictionary<MemberCategory, CategorySnapshot>();
        IsFiltering = false;
    }

    /// <summary>
    /// Shows only the members <paramref name="isMatch"/> accepts, expanding the categories that hold
    /// them. Passing null clears the filter, restoring every category's original members, their order,
    /// and the expansion state the user had chosen.
    /// </summary>
    public void Apply(IList<MemberCategory> categories, Func<InstanceMember, bool>? isMatch)
    {
        if (isMatch == null)
        {
            Clear(categories);
            return;
        }

        IsFiltering = true;

        foreach (MemberCategory category in categories)
        {
            // Snapshot per category on first sight rather than all at once: the grid also swaps
            // categories in one at a time, and one arriving mid-filter carries its full member list
            // and would otherwise never be narrowed.
            if (!_snapshots.TryGetValue(category, out CategorySnapshot? snapshot))
            {
                snapshot = new CategorySnapshot(category.Members.ToList(), category.IsExpanded);
                _snapshots[category] = snapshot;
            }

            List<InstanceMember> matches = snapshot.Members.Where(isMatch).ToList();
            ApplyMembers(category, matches);

            // A match inside a collapsed section is not a result the user can see.
            category.IsExpanded = matches.Count > 0 || snapshot.IsExpanded;
        }
    }

    /// <summary>
    /// The expansion state <paramref name="category"/> had before the filter forced it open, or its
    /// current state when no filter is active. <c>DataUiGrid</c> persists expansion across selection
    /// changes and must persist the user's choice rather than the filter's.
    /// </summary>
    public bool GetPreFilterIsExpanded(MemberCategory category)
    {
        if (IsFiltering && _snapshots.TryGetValue(category, out CategorySnapshot? snapshot))
        {
            return snapshot.IsExpanded;
        }

        return category.IsExpanded;
    }

    /// <summary>
    /// Forgets the snapshot without restoring it, for when the grid replaces its categories outright
    /// and the remembered rows no longer belong to anything on screen.
    /// </summary>
    public void Invalidate()
    {
        _snapshots.Clear();
        IsFiltering = false;
    }

    private static void ApplyMembers(MemberCategory category, List<InstanceMember> desired)
    {
        // Mutate toward the desired list rather than clearing and refilling: rows that survive the
        // keystroke keep their control, so only the rows actually appearing or leaving cost anything.
        for (int i = category.Members.Count - 1; i >= 0; i--)
        {
            if (!desired.Contains(category.Members[i]))
            {
                category.Members.RemoveAt(i);
            }
        }

        for (int i = 0; i < desired.Count; i++)
        {
            if (i >= category.Members.Count)
            {
                category.Members.Add(desired[i]);
            }
            else if (category.Members[i] != desired[i])
            {
                category.Members.Insert(i, desired[i]);
            }
        }
    }

    private void Clear(IList<MemberCategory> categories)
    {
        if (!IsFiltering)
        {
            return;
        }

        foreach (MemberCategory category in categories)
        {
            if (!_snapshots.TryGetValue(category, out CategorySnapshot? snapshot))
            {
                continue;
            }

            ApplyMembers(category, snapshot.Members);
            category.IsExpanded = snapshot.IsExpanded;
        }

        Invalidate();
    }

    private class CategorySnapshot
    {
        public List<InstanceMember> Members { get; }

        public bool IsExpanded { get; }

        public CategorySnapshot(List<InstanceMember> members, bool isExpanded)
        {
            Members = members;
            IsExpanded = isExpanded;
        }
    }
}
