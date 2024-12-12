using System;
using System.Windows.Forms;
using Gum.Managers;
using Gum.ToolStates;

namespace Gum.Controls
{
    public partial class StateView : UserControl
    {
        public event EventHandler StateStackingModeChange;
        private readonly StateTreeViewRightClickService _stateTreeViewRightClickService;
        private readonly HotkeyManager _hotkeyManager;

        public ContextMenuStrip TreeViewContextMenu => TreeView.ContextMenuStrip;

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

        public StateView(StateTreeViewRightClickService stateTreeViewRightClickService, HotkeyManager hotkeyManager)
        {
            _stateTreeViewRightClickService = stateTreeViewRightClickService;
            _hotkeyManager = hotkeyManager;
            InitializeComponent();

            this.TreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.StateTreeView_AfterSelect);
            this.TreeView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.StateTreeView_KeyDown);
            this.TreeView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.StateTreeView_MouseClick);
            this.TreeView.MouseDown += HandleMouseDown;


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
            bool isHandled = TryHandleCmdKeyStateView(keyData);


            if (isHandled)
            {
                return true;
            }
            else
            { 
                return base.ProcessCmdKey(ref msg, keyData);
            }
        }

        internal bool TryHandleCmdKeyStateView(Keys keyData)
        {
            if (_hotkeyManager.ReorderUp.IsPressed(keyData))
            {
                _stateTreeViewRightClickService.MoveStateInDirection(-1);
                return true;
            }
            else if (_hotkeyManager.ReorderDown.IsPressed(keyData))
            {
                var stateSave = ProjectState.Self.Selected.SelectedStateSave;
                bool isDefault = stateSave != null &&
                    stateSave == ProjectState.Self.Selected.SelectedElement.DefaultState;

                if (!isDefault)
                {
                    _stateTreeViewRightClickService.MoveStateInDirection(1);
                }
                return true;
            }
            else if (_hotkeyManager.Rename.IsPressed(keyData))
            {
                if (SelectedState.Self.SelectedStateSave != null)
                {
                    if (SelectedState.Self.SelectedElement?.DefaultState != SelectedState.Self.SelectedStateSave)
                    {
                        _stateTreeViewRightClickService.RenameStateClick();

                    }
                    return true;
                }
                else if (SelectedState.Self.SelectedStateCategorySave != null)
                {
                    _stateTreeViewRightClickService.RenameCategoryClick();
                    return true;
                }
            }
            else if (_hotkeyManager.Delete.IsPressed(keyData))
            {
                if (SelectedState.Self.SelectedStateSave != null)
                {
                    if (SelectedState.Self.SelectedElement?.DefaultState != SelectedState.Self.SelectedStateSave)
                    {
                        _stateTreeViewRightClickService.DeleteStateClick();

                    }
                    return true;
                }
                else if (SelectedState.Self.SelectedStateCategorySave != null)
                {
                    _stateTreeViewRightClickService.DeleteCategoryClick();
                    return true;
                }
            }
            return false;
        }

        private void StateTreeView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _stateTreeViewRightClickService.PopulateMenuStrip();
            }
        }
        private void HandleMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _stateTreeViewRightClickService.PopulateMenuStrip();
            }
        }


        private void StateTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            _hotkeyManager.HandleKeyDownStateView(e);
        }

    }
}
