using Gum.DataTypes;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.ToolCommands;
using Gum.ToolStates;
using System;
using System.Collections.Generic;

namespace Gum.Wireframe;

/// <summary>
/// The canvas-side editing state the selection manager consults, plus the right-click menu's
/// items (see the partial in <c>EditingManager.RightClick.cs</c>). Framework-neutral: the head
/// renders the items and reports whether its menu is open.
/// </summary>
public partial class EditingManager : IContextMenuState
{
    private readonly ISelectedState _selectedState;
    private readonly IReorderLogic _reorderLogic;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly IAddInstanceLogic _addInstanceLogic;
    private readonly ISetVariableLogic _setVariableLogic;
    private readonly ICircularReferenceManager _circularReferenceManager;
    private readonly IFavoriteComponentManager _favoriteComponentManager;
    private readonly IHotkeyManager _hotkeyManager;

    public EditingManager(
        IWireframeObjectManager wireframeObjectManager,
        IReorderLogic reorderLogic,
        IAddInstanceLogic addInstanceLogic,
        ISetVariableLogic setVariableLogic,
        ISelectedState selectedState,
        ICircularReferenceManager circularReferenceManager,
        IFavoriteComponentManager favoriteComponentManager,
        IHotkeyManager hotkeyManager)
    {
        _selectedState = selectedState;
        _reorderLogic = reorderLogic;
        _wireframeObjectManager = wireframeObjectManager;
        _addInstanceLogic = addInstanceLogic;
        _setVariableLogic = setVariableLogic;
        _circularReferenceManager = circularReferenceManager;
        _favoriteComponentManager = favoriteComponentManager;
        _hotkeyManager = hotkeyManager;
    }

    /// <summary>
    /// Wires the right-click menu. <paramref name="isContextMenuOpen"/> reports whether the head's
    /// menu is showing, which the selection manager uses to ignore clicks that close it.
    /// </summary>
    public void Initialize(Func<bool> isContextMenuOpen)
    {
        RightClickInitialize(isContextMenuOpen);
    }

    public void RefreshPositionsAndScalesForInstance(InstanceSave instance, List<ElementWithState> elementStack)
    {
        GraphicalUiElement? ipso = _wireframeObjectManager.GetRepresentation(instance, elementStack);

        ipso?.UpdateLayout();
    }
}
