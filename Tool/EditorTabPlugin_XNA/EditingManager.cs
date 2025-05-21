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

namespace Gum.Wireframe;

public partial class EditingManager
{
    #region Methods

    public void Initialize(System.Windows.Forms.ContextMenuStrip contextMenuStrip)
    {
        RightClickInitialize(contextMenuStrip);
    }


    public void RefreshPositionsAndScalesForInstance(InstanceSave instance, List<ElementWithState> elementStack)
    {
        GraphicalUiElement ipso = WireframeObjectManager.Self.GetRepresentation(instance, elementStack);
        ipso.UpdateLayout();
    }



    #endregion
}
