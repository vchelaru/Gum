using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.ToolStates;
using System.Windows.Forms;
using Gum.DataTypes;
using System.Collections.ObjectModel;

namespace GumUnitTests.ToolStates
{
    class SelectedStateForTests : ISelectedState
    {
        public Gum.DataTypes.ScreenSave SelectedScreen
        {
            get;
            set;
        }

        public Gum.DataTypes.ElementSave SelectedElement
        {

            get;
            set;
        }

        public Gum.DataTypes.Variables.StateSave CustomCurrentStateSave
        {

            get;
            set;
        }

        public Gum.DataTypes.Variables.StateSave SelectedStateSave
        {

            get;
            set;
        }

        public Gum.DataTypes.ComponentSave SelectedComponent
        {

            get;
            set;
        }

        public Gum.DataTypes.InstanceSave SelectedInstance
        {

            get;
            set;
        }

        public IEnumerable<InstanceSave> SelectedInstances
        {
            get;
            set;
        }

        public Gum.DataTypes.StandardElementSave SelectedStandardElement
        {

            get;
            set;
        }

        public Gum.DataTypes.Variables.VariableSave SelectedVariableSave
        {
            get;
            set;
        }

        public TreeNode SelectedTreeNode
        {
            get;
            set;
        }

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

        public void UpdateToSelectedStateSave()
        {

        }

        public void UpdateToSelectedElement()
        {

        }

        public void UpdateToSelectedInstanceSave()
        {

        }
    }
}
