using Gum.Managers;
using Gum.ViewModels;
using System;
using System.Collections.Generic;

namespace Gum.Wireframe;

public partial class EditingManager
{
    private RightClickViewModel? _viewModel;
    private Func<bool>? _isContextMenuOpen;

    /// <summary>The items the canvas's right-click menu shows, as of the last refresh.</summary>
    public IReadOnlyList<ContextMenuItemViewModel> ContextMenuItems { get; private set; } = Array.Empty<ContextMenuItemViewModel>();

    /// <summary>Raised after <see cref="ContextMenuItems"/> is rebuilt.</summary>
    public event Action? ContextMenuChanged;

    /// <inheritdoc/>
    public bool IsContextMenuOpen => _isContextMenuOpen?.Invoke() == true;

    private void RightClickInitialize(Func<bool> isContextMenuOpen)
    {
        _viewModel = new RightClickViewModel(
            _selectedState,
            _reorderLogic,
            ObjectFinder.Self,
            _elementCommands,
            _nameVerifier,
            _setVariableLogic,
            _circularReferenceManager,
            _favoriteComponentManager);
        _isContextMenuOpen = isContextMenuOpen;
    }

    public void OnRightClick()
    {
        RefreshContextMenu();
    }

    /// <summary>Rebuilds <see cref="ContextMenuItems"/> for the current selection.</summary>
    public void RefreshContextMenu()
    {
        if (_viewModel == null)
        {
            return;
        }

        ContextMenuItems = _selectedState.SelectedInstance != null
            ? _viewModel.GetMenuItems()
            : Array.Empty<ContextMenuItemViewModel>();
        ContextMenuChanged?.Invoke();
    }
}
