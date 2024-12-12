using System;
using System.Collections.Generic;
using System.Linq;
using CommonFormsAndControls;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System.Windows.Forms;
using Gum.ToolStates;
using Gum.Plugins;

namespace Gum.Managers
{
    public partial class StateTreeViewManager
    {
        #region Fields

        static StateTreeViewManager mSelf;

        MultiSelectTreeView mTreeView;

        ContextMenuStrip mMenuStrip;
        StateTreeViewRightClickService _stateTreeViewRightClickService;
        HotkeyManager _hotkeyManager;

        #endregion

        #region Properties

        public static StateTreeViewManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new StateTreeViewManager();
                }
                return mSelf;
            }
        }

        public TreeNode SelectedNode
        {
            get { return mTreeView?.SelectedNode; }
        }

        #endregion

        public TreeNode GetTreeNodeForTag(object tag)
        {
            if (tag == null)
            {
                return null;
            }
            // Will need to expand this when we add categories
            foreach (TreeNode node in mTreeView.Nodes)
            {
                if (node.Tag == tag)
                {
                    return node;
                }

                foreach (TreeNode subnode in node.Nodes)
                {
                    if (subnode.Tag == tag)
                    {
                        return subnode;
                    }
                }
            }
            return null;
        }

        public void Initialize(MultiSelectTreeView treeView, ContextMenuStrip menuStrip, 
            StateTreeViewRightClickService stateTreeViewRightClickService,
            HotkeyManager hotkeyManager)
        {
            _stateTreeViewRightClickService = stateTreeViewRightClickService;
            _hotkeyManager = hotkeyManager;
            if (treeView == null)
            {
                throw new ArgumentNullException(nameof(treeView));
            }
            mMenuStrip = menuStrip;
            mTreeView = treeView;

            InitializeKeyboardShortcuts(treeView);


            mMenuStrip.Items.Clear();

            var tsmi = new ToolStripMenuItem();
            tsmi.Text = "Add State";
            tsmi.Click += ((obj, arg) =>
            {

                GumCommands.Self.Edit.AddState();
            });
            mMenuStrip.Items.Add(tsmi);

            tsmi = new ToolStripMenuItem();
            tsmi.Text = "Add Category";
            tsmi.Click += ((obj, arg) =>
            {

                GumCommands.Self.Edit.AddCategory();
            });
            mMenuStrip.Items.Add(tsmi);
        }

        private void InitializeKeyboardShortcuts(MultiSelectTreeView treeView)
        {
            treeView.KeyDown += HandleKeyDown;
            //treeView.KeyPress += HandleTreeViewKeyPressed;
        }

        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            if(_hotkeyManager.Rename.IsPressed(e))
            {
                if(SelectedState.Self.SelectedStateSave != null )
                {
                    if(SelectedState.Self.SelectedElement?.DefaultState != SelectedState.Self.SelectedStateSave)
                    {
                        _stateTreeViewRightClickService.RenameStateClick();

                    }
                }
                else if(SelectedState.Self.SelectedStateCategorySave != null)
                {
                    _stateTreeViewRightClickService.RenameCategoryClick();
                }
            }
            else if(_hotkeyManager.Delete.IsPressed(e))
            {
                if (SelectedState.Self.SelectedStateSave != null)
                {
                    if (SelectedState.Self.SelectedElement?.DefaultState != SelectedState.Self.SelectedStateSave)
                    {
                        _stateTreeViewRightClickService.DeleteStateClick();

                    }
                }
                else if (SelectedState.Self.SelectedStateCategorySave != null)
                {
                    _stateTreeViewRightClickService.DeleteCategoryClick();
                }
            }
        }

        //private void HandleTreeViewKeyPressed(object sender, KeyPressEventArgs e)
        //{
        //    switch(e.)
        //}

        internal void OnSelect()
        {

            TreeNode treeNode = mTreeView.SelectedNode;

            object selectedObject = null;

            if (treeNode != null)
            {
                selectedObject = treeNode.Tag;
            }

            if (selectedObject == null)
            {
                // What do we do?  This is invalid.  A State should always be selected...
                // What we do is select the first one if it exists
                if (mTreeView.Nodes.Count != 0)
                {
                    var newlySelectedNode = mTreeView.Nodes.FirstOrDefault(item=> 
                        {
                            TreeNode itemAsNode = item as TreeNode;
                            return itemAsNode.Tag is StateSave && (itemAsNode.Tag as StateSave).Name == "Default";
                        }) as TreeNode;

                    selectedObject = newlySelectedNode?.Tag;
                    mTreeView.SelectedNode = newlySelectedNode;
                }
            }
            SelectedState.Self.CustomCurrentStateSave = null;

            var selectedItem = mTreeView.SelectedNode.Tag;
            if(selectedItem is StateSave stateSave)
            {
                SelectedState.Self.SelectedStateSave = stateSave;
            }
            else if(selectedItem is StateSaveCategory category)
            {
                // todo:
                //SelectedState.Self.SelectedStateCategorySave = category;
            }

            SelectedState.Self.UpdateToSelectedStateSave();
            // refreshes the yellow highlights

            //GumCommands.Self.GuiCommands.RefreshStateTreeView();

            PluginManager.Self.StateWindowTreeNodeSelected(treeNode);
        }

        public void Select(StateSave stateSave)
        {
            TreeNode treeNode = GetTreeNodeForTag(stateSave);

            Select(treeNode);

            // Vic says - why is this recording an undo?
            // Shouldn't this only happen whenever you end 
            // some kind of edit?
            // This change causes incorrect undos to register.
            //UndoManager.Self.RecordUndo();
        }

        public void Select(StateSaveCategory stateSaveCategory)
        {
            TreeNode treeNode = GetTreeNodeForTag(stateSaveCategory);

            Select(treeNode);
        }

        public void Select(TreeNode treeNode)
        {
            if (mTreeView.SelectedNode != treeNode)
            {
                mTreeView.SelectedNode = treeNode;

                if (treeNode != null)
                {
                    treeNode.EnsureVisible();
                }

            }
        }
    }


    static class TreeNodeExtensions
    {
        public static IEnumerable<TreeNode> AllNodes(this TreeNodeCollection treeNodes)
        {
            foreach (TreeNode node in treeNodes)
            {
                foreach (var item in node.Nodes.AllNodes())
                {
                    yield return item;
                }

                yield return node;
            }
        }


    }


}
