using System.Collections;
using System.Collections.Generic;

#if FRB
namespace FlatRedBall.Forms.Controls.Primitives;
#else
namespace Gum.Forms.Controls.Primitives;
#endif

/// <summary>
/// A row whose selection <see cref="ItemSelection{TRow}"/> tracks. Rows implement it explicitly so it
/// adds nothing to their public surface.
/// </summary>
internal interface ISelectableRow
{
    bool IsSelected { get; set; }
    bool IsEnabled { get; }
    bool IsFocused { get; }
    object? DataObject { get; }
}

/// <summary>
/// The selection state an items control keeps for its rows: the selected rows, tracked by row identity
/// so two rows sharing a data object can be selected independently (#3509), plus the row/data mapping,
/// IsSelected sync, removal reporting and enabled-row stepping built on it. The owning control keeps its
/// selection modes, events and navigation.
/// </summary>
internal sealed class ItemSelection<TRow> where TRow : class, ISelectableRow
{
    private readonly List<TRow> _selectedRows = new List<TRow>();

    // Data of selected rows removed during the current removal, reported together once it finishes.
    private readonly List<object?> _removedSelectedData = new List<object?>();

    public IReadOnlyList<TRow> SelectedRows => _selectedRows;

    public int Count => _selectedRows.Count;

    public bool Contains(TRow row) => _selectedRows.Contains(row);

    /// <summary>Adds the row unless it is already selected. Returns whether it was added.</summary>
    public bool Add(TRow row)
    {
        if (_selectedRows.Contains(row))
        {
            return false;
        }
        _selectedRows.Add(row);
        return true;
    }

    /// <summary>Removes the row. Returns whether it was selected.</summary>
    public bool Remove(TRow row) => _selectedRows.Remove(row);

    /// <summary>Flips the row's selection. Returns whether it is selected afterward.</summary>
    public bool Toggle(TRow row)
    {
        if (_selectedRows.Remove(row))
        {
            return false;
        }
        _selectedRows.Add(row);
        return true;
    }

    public void Clear() => _selectedRows.Clear();

    /// <summary>Makes <paramref name="row"/> the only selected row.</summary>
    public void SelectOnly(TRow row)
    {
        _selectedRows.Clear();
        _selectedRows.Add(row);
    }

    public void Replace(IEnumerable<TRow> rows)
    {
        _selectedRows.Clear();
        _selectedRows.AddRange(rows);
    }

    /// <summary>
    /// Refills <paramref name="target"/> with the selected rows' data objects in selection order, one entry
    /// per distinct value. A value shared by two selected rows appears once, since the data list is a set
    /// of data objects (#3509).
    /// </summary>
    public void ProjectSelectedData(IList target)
    {
        target.Clear();
        List<object?> seenValues = new List<object?>();
        foreach (TRow row in _selectedRows)
        {
            object? value = row.DataObject;
            if (!seenValues.Contains(value))
            {
                seenValues.Add(value);
                target.Add(value);
            }
        }
    }

    /// <summary>
    /// Sets each row's IsSelected from the selected rows, adding the data object of every row that
    /// changed to <paramref name="addedItems"/> or <paramref name="removedItems"/>.
    /// </summary>
    public void SyncIsSelected(IReadOnlyList<TRow> rows, IList addedItems, IList removedItems)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            TRow row = rows[i];
            bool shouldBeSelected = _selectedRows.Contains(row);

            if (row.IsSelected != shouldBeSelected)
            {
                row.IsSelected = shouldBeSelected;

                if (shouldBeSelected)
                {
                    addedItems.Add(row.DataObject);
                }
                else
                {
                    removedItems.Add(row.DataObject);
                }
            }
        }
    }

    /// <summary>
    /// The index of the first row whose data object equals <paramref name="data"/>, or -1. A value can't
    /// tell duplicate-valued rows apart, so this always resolves to the first one.
    /// </summary>
    public static int IndexOfData(IReadOnlyList<TRow> rows, object? data)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            if (Equals(rows[i].DataObject, data))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Drops a row the control just removed. A selected one is remembered for
    /// <see cref="TakeRemovedSelection"/>.
    /// </summary>
    public void HandleRowRemoved(TRow row)
    {
        if (_selectedRows.Remove(row))
        {
            _removedSelectedData.Add(row.DataObject);
        }
    }

    /// <summary>Whether a selected row was removed since the last <see cref="TakeRemovedSelection"/>.</summary>
    public bool HasRemovedSelection => _removedSelectedData.Count > 0;

    /// <summary>
    /// Moves the data of the selected rows removed since the last call into <paramref name="removedItems"/>.
    /// </summary>
    public void TakeRemovedSelection(IList removedItems)
    {
        foreach (object? data in _removedSelectedData)
        {
            removedItems.Add(data);
        }
        _removedSelectedData.Clear();
    }

    /// <summary>
    /// The first enabled row starting at <paramref name="startRow"/> and moving by <paramref name="step"/>
    /// (+1 or -1), or -1 when there is none. A disabled row is not selectable by gamepad or keyboard, the
    /// same as it is not selectable by mouse.
    /// </summary>
    public static int FindEnabledRow(IReadOnlyList<TRow> rows, int startRow, int step)
    {
        for (int row = startRow; row >= 0 && row < rows.Count; row += step)
        {
            if (rows[row].IsEnabled)
            {
                return row;
            }
        }
        return -1;
    }

    /// <summary>
    /// The row the gamepad or keyboard steps from: the focused row, otherwise the first selected row (a
    /// selected row refuses focus when it is disabled), otherwise -1.
    /// </summary>
    public int GetNavigationRow(IReadOnlyList<TRow> rows)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].IsFocused)
            {
                return i;
            }
        }
        if (_selectedRows.Count > 0)
        {
            TRow selected = _selectedRows[0];
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] == selected)
                {
                    return i;
                }
            }
        }
        return -1;
    }

    /// <summary>
    /// The row one enabled step forward or backward from the navigation row, or -1 when there is none.
    /// Stays on the current row at either end when it is enabled; with no current row, the first enabled
    /// row.
    /// </summary>
    public int GetStepTarget(IReadOnlyList<TRow> rows, bool forward)
    {
        int step = forward ? 1 : -1;
        int currentRow = GetNavigationRow(rows);
        if (currentRow < 0)
        {
            return FindEnabledRow(rows, 0, 1);
        }

        int targetRow = FindEnabledRow(rows, currentRow + step, step);
        if (targetRow < 0 && rows[currentRow].IsEnabled)
        {
            targetRow = currentRow;
        }
        return targetRow;
    }
}
