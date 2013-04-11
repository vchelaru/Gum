using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonFormsAndControls;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System.Windows.Forms;
using Gum.ToolStates;

namespace Gum.Managers
{
    public partial class StateTreeViewManager
    {
        #region Fields

        static StateTreeViewManager mSelf;

        MultiSelectTreeView mTreeView;

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
            get { return mTreeView.SelectedNode; }
        }

        #endregion

        public TreeNode GetTreeNodeFor(StateSave stateSave)
        {
            if (stateSave == null)
            {
                return null;
            }
            // Will need to expand this when we add categories
            foreach (TreeNode node in mTreeView.Nodes)
            {
                if (node.Tag == stateSave)
                {
                    return node;
                }
            }
            return null;
        }

        public void Initialize(MultiSelectTreeView treeView)
        {
            mTreeView = treeView;
        }

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
            }

            SelectedState.Self.UpdateToSelectedStateSave();

        }

        public void Select(StateSave stateSave)
        {
            TreeNode treeNode = GetTreeNodeFor(stateSave);

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

        public void RefreshUI(ElementSave element)
        {
            mTreeView.Nodes.Clear();

            if (element != null)
            {
                foreach (StateSave state in element.States)
                {
                    TreeNode treeNode = new TreeNode();
                    treeNode.Text = state.Name;
                    treeNode.Tag = state;
                    mTreeView.Nodes.Add(treeNode);
                }

                if (SelectedState.Self.SelectedStateSave == null)
                {
                    SelectedState.Self.SelectedStateSave = element.States[0];

                }
                else // We refreshed the tree node so let's update to selected state
                {
                    SelectedState.Self.SelectedStateSave = SelectedState.Self.SelectedStateSave;
                }
            }


        }


    }
}
