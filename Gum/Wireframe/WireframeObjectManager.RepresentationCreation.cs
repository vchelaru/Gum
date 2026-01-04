using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Commands;
using RenderingLibrary;
using Gum.DataTypes;
using Gum.Managers;
using RenderingLibrary.Graphics;
using Gum.DataTypes.Variables;
using Gum.ToolStates;
using Gum.PropertyGridHelpers.Converters;
using GumRuntime;
using Gum.Plugins;
using Gum.Services;
using Gum.Services.Dialogs;

namespace Gum.Wireframe;

public partial class WireframeObjectManager
{
    #region Fields


    public string[] PositionAndSizeVariables = new string[]{
        "Width",
        "Height",
        "WidthUnits",
        "HeightUnits",
        "XOrigin",
        "YOrigin",
        "X",
        "Y",
        "XUnits",
        "YUnits",
        "Guide",
        "Parent"
    
    };

    public string[] ColorAndAlpha = new string[]{
        "Red",
        "Green",
        "Blue",
        "Alpha"
    };

    public string[] Color = new string[]{
        "Red",
        "Green",
        "Blue"
    };

    #endregion


    private bool GetIfSelectedStateIsSetRecursively()
    {
        var category = _selectedState.SelectedStateCategorySave;
        if(category != null)
        {
            var selectedElement = _selectedState.SelectedElement;
            foreach(var behaviorReference in selectedElement.Behaviors)
            {
                var behavior = ObjectFinder.Self.GetBehavior(behaviorReference);

                if(behavior != null && behavior.Categories.Any(item => item.Name == category.Name))
                {
                    return true;
                }
            }
        }
        return false;
    }

    bool IsRecursive(GraphicalUiElement item, HashSet<GraphicalUiElement> history)
    {
        if (history.Contains(item))
        {
            // recursion found!!!!!!
            return true;
        }
        history.Add(item);
        var parentGue = item.Parent as GraphicalUiElement;
        if (parentGue == null)
        {
            return false;
        }
        else
        {
            return IsRecursive(parentGue, history);
        }
    }

    private int GetDepth(GraphicalUiElement item, HashSet<GraphicalUiElement> history)
    {
        if(history.Contains(item))
        {
            return int.MaxValue / 2;
        }
        history.Add(item);
        var parentGue = item.Parent as GraphicalUiElement;
        if(parentGue == null)
        {
            return 0;
        }
        else
        {
            return 1 + GetDepth(parentGue, history);
        }
    }

    private void SetUpParentRelationship(IEnumerable<GraphicalUiElement> elements, List<ElementWithState> elementStack)
    {
        // Now that we have created all instances, we can establish parent relationships

        HashSet<IRenderable> recursiveHashSet = new HashSet<IRenderable>();

        foreach (GraphicalUiElement contained in elements)
        {
            if (contained.Tag is InstanceSave childInstanceSave)
            {
                RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(elementStack);

                string parentName = rvf.GetValue<string>($"{childInstanceSave.Name}.Parent");

                if (!string.IsNullOrEmpty(parentName) && parentName != StandardElementsManager.ScreenBoundsName)
                {
                    var newParent = elements.FirstOrDefault(item => item.Name == parentName);

                    // This may have bad XML so if it doesn't exist, then let's ignore this:
                    if (newParent != null)
                    {
                        recursiveHashSet.Clear();
                        GetAllParents(newParent, recursiveHashSet);
                        if(recursiveHashSet.Contains(contained))
                        {
                            // RECURSIVE!!!!
                        }
                        else
                        {
                            contained.Parent = newParent;
                        }

                    }
                }

                //var innerChildren = contained.Children.Select(item => item as GraphicalUiElement).ToArray();
                //if(innerChildren.Length > 0)
                //{
                //    SetUpParentRelationship(innerChildren, elementStack);
                //}


            }
        }
    }

    void GetAllParents(IRenderableIpso ipso, HashSet<IRenderable> toFill)
    {
        if(ipso.Parent != null)
        {
            toFill.Add(ipso.Parent);
            GetAllParents(ipso.Parent, toFill);
        }
    }
    
    private IPositionedSizedObject CreateRectangleFor(InstanceSave instance, List<ElementWithState> elementStack, GraphicalUiElement graphicalUiElement)
    {
        ElementSave instanceBase = ObjectFinder.Self.GetElementSave(instance.BaseType);
        graphicalUiElement.CreateGraphicalComponent(instanceBase, null);
        graphicalUiElement.Tag = instance;
        graphicalUiElement.Name = instance.Name;
        graphicalUiElement.Component.Tag = instance;
        
        return graphicalUiElement;
    }

    private void SetGuideParent(GraphicalUiElement parentIpso, GraphicalUiElement ipso, string guideName)
    {
        // I dont't think we want to do this anymore because it should be handled by the GraphicalUiElement
        if (parentIpso != null && (parentIpso.Tag == null || parentIpso.Tag is ScreenSave == false))
        {
            ipso.Parent = parentIpso;
        }
        
        // don't do this because it causes double render.
        //else if (setParentToBoundsIfNoGuide) 
        //{
        //    ipso.Parent = mWireframeControl.ScreenBounds;
        //}
    }
}
