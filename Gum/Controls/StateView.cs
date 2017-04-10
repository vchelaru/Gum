using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gum.Managers;
using Gum.ToolStates;

namespace Gum.Controls
{
    public partial class StateView : UserControl
    {
        public event EventHandler StateStackingModeChange;

        public StateStackingMode StateStackingMode
        {
            get
            {
                if (SingleStateRadio.Checked)
                {
                    return StateStackingMode.SingleState;
                }
                else
                {
                    return StateStackingMode.CombineStates;
                }
            }
            set
            {
                if (value == StateStackingMode.SingleState)
                {
                    SingleStateRadio.Checked = true;
                }
                else
                {
                    StackStateRadio.Checked = true;
                }
            }
        }

        public StateView()
        {
            InitializeComponent();

            this.TreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.StateTreeView_AfterSelect);
            this.TreeView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.StateTreeView_KeyDown);
            this.TreeView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.StateTreeView_MouseClick);

        }

        private void SingleStateRadio_CheckedChanged(object sender, EventArgs e)
        {
            StateStackingModeChange?.Invoke(this, null);
        }

        private void StackStateRadio_CheckedChanged(object sender, EventArgs e)
        {
            StateStackingModeChange?.Invoke(this, null);
        }

        private void StateTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            StateTreeViewManager.Self.OnSelect();
        }

        // We have to use ProcessCmdKey to intercept the key when moving objects
        // up and down, otherwise the built-in functionality of moving up/down kicks
        // in and selects the node above/below the selected node
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            bool isHandled = StateTreeViewManager.Self.TryHandleCmdKey(keyData);


            if (isHandled)
            {
                return true;
            }
            else
            { 
                return base.ProcessCmdKey(ref msg, keyData);
            }
        }

        private void StateTreeView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                StateTreeViewManager.Self.PopulateMenuStrip();
            }
        }



        private void StateTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            StateTreeViewManager.Self.HandleKeyDown(e);
        }

    }
}
