using System;
using System.Collections.Generic;
using System.Linq;
using InputLibrary;
using Gum.Managers;
using Gum.ToolStates;
using Gum.DataTypes;
using RenderingLibrary;
using Gum.DataTypes.Variables;
using Gum.Converters;
using Gum.Events;
using Gum.RenderingLibrary;
using Gum.PropertyGridHelpers;
using GumRuntime;
using Gum.Services;

namespace Gum.Wireframe;

public partial class EditingManager
{
    private readonly ISelectedState _selectedState;
    private readonly WireframeObjectManager _wireframeObjectManager;

    public EditingManager(WireframeObjectManager wireframeObjectManager)
    {
        _selectedState = Locator.GetRequiredService<ISelectedState>();
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
