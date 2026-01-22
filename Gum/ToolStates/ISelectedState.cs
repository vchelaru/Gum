using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System.Windows.Forms;
using RenderingLibrary;
using Gum.Wireframe;
using Gum.DataTypes.Behaviors;
using Gum.Managers;

namespace Gum.ToolStates;

public interface ISelectedState
{
    ScreenSave? SelectedScreen { get; set; }
    ElementSave? SelectedElement { get; set; }
    IEnumerable<ElementSave> SelectedElements { get; set; }
    IStateContainer? SelectedStateContainer { get; }
    public IInstanceContainer? SelectedInstanceContainer =>
        (IInstanceContainer?)SelectedComponent ??
        SelectedScreen ??
        // December 3, 2025:
        // Technically this cannot contain instances, but based on its type
        // it is an InstanceContainer so, let's return it unless it causes problems?
        (IInstanceContainer?)SelectedStandardElement ??
        SelectedBehavior;

    BehaviorSave? SelectedBehavior { get; set; }
    IEnumerable<BehaviorSave> SelectedBehaviors { get; set; }
    ElementBehaviorReference? SelectedBehaviorReference { get; set; }
    StateSave? CustomCurrentStateSave{ get; set; }
    StateSave? SelectedStateSave { get; set; }
    StateSave SelectedStateSaveOrDefault { get;}

    StateSaveCategory? SelectedStateCategorySave { get; set; }
    ComponentSave? SelectedComponent { get; set; }
    InstanceSave? SelectedInstance { get; set; }

    // January 22, 2026
    // This should be converted
    // to a GraphicalUiElement at
    // some point in the future.
    IPositionedSizedObject? SelectedIpso { get; }

    IEnumerable<InstanceSave> SelectedInstances { get; set;  }
    string? SelectedVariableName { get; }
    StandardElementSave? SelectedStandardElement { get; set; }
    VariableSave? SelectedVariableSave { get; set;  }
    VariableSave? SelectedBehaviorVariable { get; set; }
    ITreeNode? SelectedTreeNode { get; }
    IEnumerable<ITreeNode> SelectedTreeNodes { get; }
    public RecursiveVariableFinder SelectedRecursiveVariableFinder
    {
        get
        {
            if (SelectedInstance != null)
            {
                return new RecursiveVariableFinder(SelectedInstance, SelectedElement);
            }
            else
            {
                return new RecursiveVariableFinder(SelectedStateSave);
            }
        }
    }

    public List<ElementWithState> GetTopLevelElementStack()
    {
        List<ElementWithState> toReturn = new List<ElementWithState>();

        if (SelectedElement != null)
        {
            ElementWithState item = new ElementWithState(SelectedElement);
            if (this.SelectedStateSave != null)
            {
                item.StateName = this.SelectedStateSave.Name;
            }
            toReturn.Add(item);


        }

        return toReturn;
    }

    //void UpdateToSelectedStateSave();
    //void UpdateToSelectedElement();
    //void UpdateToSelectedInstanceSave();
    //void UpdateToSelectedBehavior();
    //void UpdateToSelectedBehaviorVariable();

}
