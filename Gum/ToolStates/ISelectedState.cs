using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System.Windows.Forms;
using System.Collections.ObjectModel;
using RenderingLibrary;
using Gum.Wireframe;
using Gum.DataTypes.Behaviors;

namespace Gum.ToolStates
{
    public interface ISelectedState
    {
        ScreenSave SelectedScreen { get; set; }
        ElementSave SelectedElement { get; set; }
        IStateContainer SelectedStateContainer { get; }
        BehaviorSave SelectedBehavior { get; set; }
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
        TreeNode SelectedTreeNode { get; }
        RecursiveVariableFinder SelectedRecursiveVariableFinder { get; }
        StateStackingMode StateStackingMode { get; set; }

        void UpdateToSelectedStateSave();
        List<ElementWithState> GetTopLevelElementStack();

        void UpdateToSelectedElement();
        void UpdateToSelectedInstanceSave();
        void UpdateToSelectedBehavior();

    }
}
