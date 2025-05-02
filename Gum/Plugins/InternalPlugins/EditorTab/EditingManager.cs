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


    //public void UpdateSelectedObjectsPositionAndDimensions()
    //{
    //    var elementStack = SelectedState.Self.GetTopLevelElementStack();
    //    if (SelectedState.Self.SelectedInstances.GetCount() != 0)
    //    {
    //        //// Can we just update layout it?
    //        //foreach (var instance in SelectedState.Self.SelectedInstances)
    //        //{
    //        //    RefreshPositionsAndScalesForInstance(instance, elementStack);
    //        //}

    //        foreach (var ipso in SelectedState.Self.SelectedIpsos)
    //        {
    //            GraphicalUiElement asGue = ipso as GraphicalUiElement;
    //            if (asGue != null)
    //            {
    //                asGue.UpdateLayout();
    //                //RecursiveVariableFinder rvf = new RecursiveVariableFinder(asGue.Tag as InstanceSave, SelectedState.Self.SelectedElement);
    //                //asGue.SetGueValues(rvf);
    //            }
    //        }
    //    }
    //    else
    //    {
    //        GraphicalUiElement ipso = WireframeObjectManager.Self.GetSelectedRepresentation();

    //        if (ipso != null)
    //        {
    //            ElementSave elementSave = SelectedState.Self.SelectedElement;

    //            var state = elementSave.DefaultState;
    //            if(SelectedState.Self.SelectedStateSave != null)
    //            {
    //                state = SelectedState.Self.SelectedStateSave;
    //            }
    //            RecursiveVariableFinder rvf = new RecursiveVariableFinder(state);
    //            (ipso as GraphicalUiElement).SetGueValues(rvf);
    //        }
    //        else if(SelectedState.Self.SelectedElement != null)
    //        {
    //            foreach (var instance in SelectedState.Self.SelectedElement.Instances)
    //            {
    //                RefreshPositionsAndScalesForInstance(instance, elementStack);
    //            }
    //        }
    //    }

    //    GuiCommands.Self.RefreshWireframe();
    //}

    public void RefreshPositionsAndScalesForInstance(InstanceSave instance, List<ElementWithState> elementStack)
    {
        GraphicalUiElement ipso = WireframeObjectManager.Self.GetRepresentation(instance, elementStack);
        ipso.UpdateLayout();
    }



    #endregion
}
