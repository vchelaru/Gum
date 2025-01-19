using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System.Windows.Forms;
using RenderingLibrary;
using Gum.Wireframe;
using Gum.DataTypes.Behaviors;

namespace Gum.ToolStates
{
    public interface ISelectedState
    {
        ScreenSave SelectedScreen { get; set; }
        ElementSave SelectedElement { get; set; }
        IEnumerable<ElementSave> SelectedElements { get; set; }
        IStateContainer SelectedStateContainer { get; }
        BehaviorSave SelectedBehavior { get; set; }
        ElementBehaviorReference SelectedBehaviorReference { get; set; }
        StateSave CustomCurrentStateSave{ get; set; }
        StateSave SelectedStateSave { get; set; }
        StateSave SelectedStateSaveOrDefault { get;}

        StateSaveCategory SelectedStateCategorySave { get; set; }
        ComponentSave SelectedComponent { get; set; }
        InstanceSave SelectedInstance { get; set; }
        IPositionedSizedObject SelectedIpso { get; set; }
        List<GraphicalUiElement> SelectedIpsos { get; }
        IEnumerable<InstanceSave> SelectedInstances { get; set;  }
        string SelectedVariableName { get; }
        StandardElementSave SelectedStandardElement { get; set; }
        VariableSave SelectedVariableSave { get; set;  }
        VariableSave SelectedBehaviorVariable { get; set; }
        TreeNode SelectedTreeNode { get; }
        IEnumerable<TreeNode> SelectedTreeNodes { get; }
        RecursiveVariableFinder SelectedRecursiveVariableFinder { get; }

        List<ElementWithState> GetTopLevelElementStack();

        //void UpdateToSelectedStateSave();
        //void UpdateToSelectedElement();
        //void UpdateToSelectedInstanceSave();
        //void UpdateToSelectedBehavior();
        //void UpdateToSelectedBehaviorVariable();

    }
}
