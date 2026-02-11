using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.RenderingLibrary;
using Gum.Services;
using Gum.ToolCommands;
using Gum.ToolStates;
using GumRuntime;
using InputLibrary;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Wireframe;

public partial class EditingManager : IEditingManager
{
    private readonly ISelectedState _selectedState;
    private readonly IReorderLogic _reorderLogic;
    private readonly WireframeObjectManager _wireframeObjectManager;
    private readonly IElementCommands _elementCommands;
    private readonly INameVerifier _nameVerifier;
    private readonly ISetVariableLogic _setVariableLogic;

    public EditingManager(
        WireframeObjectManager wireframeObjectManager,
        IReorderLogic reorderLogic,
        IElementCommands elementCommands,
        INameVerifier nameVerifier,
        ISetVariableLogic setVariableLogic)
    {
        _selectedState = Locator.GetRequiredService<ISelectedState>();
        _reorderLogic = reorderLogic;
        _wireframeObjectManager = wireframeObjectManager;
        _elementCommands = elementCommands;
        _nameVerifier = nameVerifier;
        _setVariableLogic = setVariableLogic;
    }
    #region Methods

    public void Initialize(System.Windows.Forms.ContextMenuStrip contextMenuStrip)
    {
        RightClickInitialize(contextMenuStrip);
    }


    public void RefreshPositionsAndScalesForInstance(InstanceSave instance, List<ElementWithState> elementStack)
    {
        GraphicalUiElement? ipso = _wireframeObjectManager.GetRepresentation(instance, elementStack);
        ipso?.UpdateLayout();
    }



    #endregion
}
