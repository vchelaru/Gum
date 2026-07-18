using System.Collections.Generic;
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

    public SelectionHistoryService(ISelectedState selectedState)
    {
        _selectedState = selectedState;
    }

    public bool CanNavigateBack => _currentIndex > 0;
    public bool CanNavigateForward => _currentIndex < _entries.Count - 1;

    public void RecordSelection(ElementSave? element, InstanceSave? instance)
    {
        if (_isNavigating)
        {
            return;
        }

        if (_currentIndex >= 0)
        {
            var current = _entries[_currentIndex];
            if (current.Element == element && current.Instance == instance)
            {
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
    }

    public void NavigateBack()
    {
        if (!CanNavigateBack)
        {
            return;
        }

        _currentIndex--;
        ApplyCurrentEntry();
    }

    public void NavigateForward()
    {
        if (!CanNavigateForward)
        {
            return;
        }

        _currentIndex++;
        ApplyCurrentEntry();
    }

    private void ApplyCurrentEntry()
    {
        var entry = _entries[_currentIndex];

        _isNavigating = true;
        try
        {
            if (entry.Instance != null)
            {
                _selectedState.SelectedInstance = entry.Instance;
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
}
