using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.ToolStates;

namespace Gum.SelectionHistory;

/// <summary>
/// Browser-style back/forward navigation over element/instance selections. Selections are
/// recorded via <see cref="RecordSelection"/> (wired to the real selection cascade by
/// SelectionHistoryPlugin) and replayed onto <see cref="ISelectedState"/> by NavigateBack/Forward.
/// </summary>
public class SelectionHistoryService : ISelectionHistory
{
    private readonly ISelectedState _selectedState;

    private readonly List<(ElementSave? Element, InstanceSave? Instance)> _entries = new();
    private int _currentIndex = -1;

    // Guards against NavigateBack/Forward's own selection re-entering RecordSelection through the
    // plugin cascade, which would otherwise truncate the very forward/back branch being navigated to.
    private bool _isNavigating;

    // True once the current entry was deleted: the selection then sits just after the entry at
    // _currentIndex rather than on it, so Back returns to that entry itself.
    private bool _isAfterCurrent;

    public SelectionHistoryService(ISelectedState selectedState)
    {
        _selectedState = selectedState;
    }

    public bool CanNavigateBack => _isAfterCurrent ? _currentIndex >= 0 : _currentIndex > 0;
    public bool CanNavigateForward => _currentIndex < _entries.Count - 1;

    public void RecordSelection(ElementSave? element, InstanceSave? instance)
    {
        // Clearing the selection is not a place to come back to.
        if (_isNavigating || (element == null && instance == null))
        {
            return;
        }

        if (_currentIndex >= 0)
        {
            var current = _entries[_currentIndex];
            if (current.Element == element && current.Instance == instance)
            {
                _isAfterCurrent = false;
                return;
            }
        }

        // A new selection made while parked mid-stack discards the forward branch, matching
        // standard browser back/forward semantics.
        if (_currentIndex < _entries.Count - 1)
        {
            _entries.RemoveRange(_currentIndex + 1, _entries.Count - _currentIndex - 1);
        }

        _entries.Add((element, instance));
        _currentIndex = _entries.Count - 1;
        _isAfterCurrent = false;
    }

    public void NavigateBack()
    {
        if (!CanNavigateBack)
        {
            return;
        }

        if (_isAfterCurrent)
        {
            _isAfterCurrent = false;
        }
        else
        {
            _currentIndex--;
        }
        ApplyCurrentEntry();
    }

    public void NavigateForward()
    {
        if (!CanNavigateForward)
        {
            return;
        }

        _isAfterCurrent = false;
        _currentIndex++;
        ApplyCurrentEntry();
    }

    /// <inheritdoc/>
    public void ForgetElement(ElementSave element) =>
        RemoveEntries(entry => entry.Element == element || entry.Instance?.ParentContainer == element);

    /// <inheritdoc/>
    public void ForgetInstance(InstanceSave instance) =>
        RemoveEntries(entry => entry.Instance == instance);

    private void RemoveEntries(Func<(ElementSave? Element, InstanceSave? Instance), bool> isGone)
    {
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            if (isGone(_entries[i]))
            {
                _entries.RemoveAt(i);
                if (i == _currentIndex)
                {
                    _isAfterCurrent = true;
                }
                if (i <= _currentIndex)
                {
                    _currentIndex--;
                }
            }
        }

        // Removing an entry can leave the same selection twice in a row, a step that changes
        // nothing; the earlier of the two stands for both.
        for (int i = _entries.Count - 1; i > 0; i--)
        {
            if (_entries[i] == _entries[i - 1])
            {
                _entries.RemoveAt(i);
                if (i <= _currentIndex)
                {
                    _currentIndex--;
                }
            }
        }
    }

    private void ApplyCurrentEntry()
    {
        var entry = _entries[_currentIndex];

        _isNavigating = true;
        try
        {
            if (entry.Instance != null)
            {
                if (CurrentCopyOf(entry.Instance) is { } instance)
                {
                    _selectedState.SelectedInstance = instance;
                }
                else
                {
                    _selectedState.SelectedElement = entry.Instance.ParentContainer;
                }
            }
            else
            {
                _selectedState.SelectedElement = entry.Element;
            }
        }
        finally
        {
            _isNavigating = false;
        }
    }

    // Undo replaces an element's instances with copies, so a recorded instance may no longer be
    // in its element; the copy with its name stands in its place. Null when the element has none.
    private static InstanceSave? CurrentCopyOf(InstanceSave instance) =>
        instance.ParentContainer is { } owner && !owner.Instances.Contains(instance)
            ? owner.Instances.FirstOrDefault(candidate => candidate.Name == instance.Name)
            : instance;
}
