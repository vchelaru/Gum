using System.Collections.Generic;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Where <see cref="ListMover"/> puts an item.
/// </summary>
public enum ReorderPlacement
{
    /// <summary>The first position.</summary>
    ToStart,
    /// <summary>The last position.</summary>
    ToEnd,
    /// <summary>Directly before an anchor item.</summary>
    Before,
    /// <summary>Directly after an anchor item.</summary>
    After,
}

/// <summary>
/// The list change behind the tool's reordering of instances and states, and moving a state between categories:
/// remove the item, then insert it at its new place. Instance order is draw order and the order code is generated
/// in; state order is the order the tool shows.
/// </summary>
public static class ListMover
{
    /// <summary>
    /// Moves <paramref name="item"/> within <paramref name="list"/>. <paramref name="anchor"/> is required for
    /// <see cref="ReorderPlacement.Before"/> and <see cref="ReorderPlacement.After"/> and must be another item of the
    /// list; when it is null the item goes to the start, as the tool does for a missing sibling.
    /// </summary>
    public static void Move<T>(IList<T> list, T item, ReorderPlacement placement, T? anchor = default) where T : class
    {
        list.Remove(item);
        list.Insert(GetIndex(list, placement, anchor), item);
    }

    /// <summary>
    /// Takes <paramref name="item"/> out of <paramref name="from"/> and inserts it into <paramref name="to"/>.
    /// </summary>
    public static void MoveBetween<T>(IList<T> from, IList<T> to, T item, ReorderPlacement placement = ReorderPlacement.ToEnd, T? anchor = default) where T : class
    {
        from.Remove(item);
        to.Insert(GetIndex(to, placement, anchor), item);
    }

    // The anchor's index is read after the item was removed, so its own removal cannot shift the result.
    private static int GetIndex<T>(IList<T> list, ReorderPlacement placement, T? anchor) where T : class
    {
        int anchorIndex = anchor == null ? -1 : list.IndexOf(anchor);
        return placement switch
        {
            ReorderPlacement.ToStart => 0,
            ReorderPlacement.ToEnd => list.Count,
            ReorderPlacement.Before => System.Math.Max(anchorIndex, 0),
            _ => anchorIndex + 1,
        };
    }
}
