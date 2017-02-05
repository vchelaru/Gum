using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Gum.ToolStates;
using Gum.DataTypes;
using Gum.Wireframe;

namespace Gum.Managers
{
    public class MenuStripManager
    {
        #region Fields

        ToolStripMenuItem mRemoveElement;
        ToolStripMenuItem mRemoveState;
        ToolStripMenuItem removeVariable;

        static MenuStripManager mSelf;

        #endregion

        #region Properties

        public static MenuStripManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new MenuStripManager();
                }
                return mSelf;
            }
        }

        #endregion


        public void Initialize(ToolStripMenuItem removeElement, ToolStripMenuItem removeState,
            ToolStripMenuItem removeVariable)
        {
            mRemoveState = removeState;
            mRemoveState.Click += RemoveStateOrCategoryClicked;

            mRemoveElement = removeElement;
            mRemoveElement.Click += RemoveElementClicked;

            this.removeVariable = removeVariable;
            this.removeVariable.Click += HanldeRemoveBehaviorVariableClicked;

            RefreshUI();

        }

        private void HanldeRemoveBehaviorVariableClicked(object sender, EventArgs e)
        {
            GumCommands.Self.Edit.RemoveBehaviorVariable(
                SelectedState.Self.SelectedBehavior,
                SelectedState.Self.SelectedBehaviorVariable);
        }

        public void RefreshUI()
        {
            if (SelectedState.Self.SelectedStateSave != null && SelectedState.Self.SelectedStateSave.Name != "Default")
            {
                mRemoveState.Text = "State " + SelectedState.Self.SelectedStateSave.Name;
                mRemoveState.Enabled = true;
            }
            else if (SelectedState.Self.SelectedStateCategorySave != null)
            {
                mRemoveState.Text = "Category " + SelectedState.Self.SelectedStateCategorySave.Name;
                mRemoveState.Enabled = true;
            }
            else
            {
                mRemoveState.Text = "<no state selected>";
                mRemoveState.Enabled = false;
            }

            if (SelectedState.Self.SelectedElement != null && !(SelectedState.Self.SelectedElement is StandardElementSave))
            {
                mRemoveElement.Text = SelectedState.Self.SelectedElement.Name;
                mRemoveElement.Enabled = true;
            }
            else
            {
                mRemoveElement.Text = "<no element selected>";
                mRemoveElement.Enabled = false;
            }

            if(SelectedState.Self.SelectedBehaviorVariable != null)
            {
                removeVariable.Text = SelectedState.Self.SelectedBehaviorVariable.ToString();
                removeVariable.Enabled = true;
            }
            else
            {
                removeVariable.Text = "<no behavior variable selected>";
                removeVariable.Enabled = false;
            }

        }


        private void RemoveElementClicked(object sender, EventArgs e)
        {
            EditingManager.Self.RemoveSelectedElement();
        }

        private void RemoveStateOrCategoryClicked(object sender, EventArgs e)
        {
            if (SelectedState.Self.SelectedStateSave != null)
            {
                GumCommands.Self.Edit.RemoveState(
                    SelectedState.Self.SelectedStateSave, SelectedState.Self.SelectedStateContainer);
            }
            else if (SelectedState.Self.SelectedStateCategorySave != null)
            {
                GumCommands.Self.Edit.RemoveStateCategory(
                    SelectedState.Self.SelectedStateCategorySave, SelectedState.Self.SelectedStateContainer as IStateCategoryListContainer);
            }
        }
    }

}
