using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Gum.ToolStates;
using Gum.DataTypes;

namespace Gum.Managers
{
    public class MenuStripManager
    {
        #region Fields

        ToolStripMenuItem mRemoveElement;
        ToolStripMenuItem mRemoveState;

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


        public void Initialize(ToolStripMenuItem removeElement, ToolStripMenuItem removeState)
        {
            mRemoveState = removeState;
            mRemoveElement = removeElement;

            RefreshUI();

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

        }
    }
}
