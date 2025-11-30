using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Events;
using Gum.Logic;
using Gum.Managers;
using Gum.PropertyGridHelpers;
using Gum.RenderingLibrary;
using Gum.Services;
using Gum.ToolStates;
using GumRuntime;
using InputLibrary;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Wireframe;

public partial class EditingManager
{
    private readonly ISelectedState _selectedState;
    private readonly ReorderLogic _reorderLogic;
    private readonly WireframeObjectManager _wireframeObjectManager;

    public EditingManager(WireframeObjectManager wireframeObjectManager,
        ReorderLogic reorderLogic)
    {
        _selectedState = Locator.GetRequiredService<ISelectedState>();
        _reorderLogic = reorderLogic;
        _wireframeObjectManager = wireframeObjectManager;
    }
    #region Methods

    public void Initialize(System.Windows.Forms.ContextMenuStrip contextMenuStrip)
    {
        RightClickInitialize(contextMenuStrip);
    }


    public void RefreshPositionsAndScalesForInstance(InstanceSave instance, List<ElementWithState> elementStack)
    {
        GraphicalUiElement ipso = _wireframeObjectManager.GetRepresentation(instance, elementStack);
        ipso.UpdateLayout();
    }



    #endregion
}
