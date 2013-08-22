using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System.Windows.Forms;
using System.Collections.ObjectModel;

namespace Gum.ToolStates
{
    public interface ISelectedState
    {
        ScreenSave SelectedScreen { get; set; }
        ElementSave SelectedElement { get; set; }
        StateSave CustomCurrentStateSave{ get; set; }
        StateSave SelectedStateSave { get; set; }
        ComponentSave SelectedComponent { get; set; }
        InstanceSave SelectedInstance { get; set; }
        IEnumerable<InstanceSave> SelectedInstances { get; set;  }
        string SelectedVariableName { get; }
        StandardElementSave SelectedStandardElement { get; set; }
        VariableSave SelectedVariableSave { get; }
        TreeNode SelectedTreeNode { get; }
        RecursiveVariableFinder SelectedRecursiveVariableFinder { get; }
        void UpdateToSelectedStateSave();

        void UpdateToSelectedElement();
        void UpdateToSelectedInstanceSave();

    }
}
