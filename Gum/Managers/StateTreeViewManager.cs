using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonFormsAndControls;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System.Windows.Forms;
using Gum.ToolStates;
using Gum.Wireframe;

namespace Gum.Managers
{
    public partial class StateTreeViewManager
    {
        #region Fields

        static StateTreeViewManager mSelf;

        MultiSelectTreeView mTreeView;

        ElementSave mLastElementRefreshedTo;
        ContextMenuStrip mMenuStrip;

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



        public void Initialize(MultiSelectTreeView treeView, ContextMenuStrip menuStrip)
        {
            mMenuStrip = menuStrip;
            mTreeView = treeView;


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
                    selectedObject = mTreeView.Nodes[0].Tag;
                    mTreeView.SelectedNode = mTreeView.Nodes[0];
                }
            }
            SelectedState.Self.CustomCurrentStateSave = null;
            SelectedState.Self.UpdateToSelectedStateSave();
            // refreshes the yellow highlights
            StateTreeViewManager.Self.RefreshUI(SelectedState.Self.SelectedElement);
        }

        public void Select(StateSave stateSave)
        {
            TreeNode treeNode = GetTreeNodeForTag(stateSave);

            Select(treeNode);

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

        public void RefreshUI(ElementSave element)
        {

            bool changed = element != mLastElementRefreshedTo;

            mLastElementRefreshedTo = element;

            StateSave lastStateSave = SelectedState.Self.SelectedStateSave;
            InstanceSave instance = SelectedState.Self.SelectedInstance;


            if (element != null)
            {

                RemoveUnnecessaryNodes(element);

                AddNeededNodes(element);

                bool wasAnythingSelected = false;

                foreach(var state in element.AllStates)
                {
                    string stateName = state.Name;
                    if (string.IsNullOrEmpty(stateName))
                    {
                        stateName = "Default";
                    }

                    var node = GetTreeNodeForTag(state);

                    if (node.Text != stateName)
                    {
                        node.Text = stateName;
                    }
                    if (node.Tag != state)
                    {
                        node.Tag = state;
                    }

                    node.ImageIndex = ElementTreeViewManager.StateImageIndex;

                    if (state == lastStateSave)
                    {

                        SelectedState.Self.SelectedStateSave = state;

                        wasAnythingSelected = true;
                    }
                    else if (!node.IsSelected && mTreeView.SelectedNode != node)
                    {
                        System.Drawing.Color desiredColor = System.Drawing.Color.White;
                        if (instance != null && state.Variables.Any(item => item.Name.StartsWith(instance.Name + ".")))
                        {
                            desiredColor = System.Drawing.Color.Yellow;
                        }

                        if (node.BackColor != desiredColor)
                        {
                            node.BackColor = desiredColor;
                        }
                    }
                }

                // Victor Chelaru
                // April 26, 2014
                // I think this code would select a 
                // state if the selected node was deleted.
                // But we don't want to do this now since Gum
                // supports state categories.
                //if (wasAnythingSelected == false && element.States != null && 
                //    // The user could be selecting an object that has a missing XML file, so states
                //    // were never loaded
                //    element.States.Count > 0)
                //{
                    
                //    SelectedState.Self.SelectedStateSave = element.States[0];

                //}
            }
            else
            {
                mTreeView.Nodes.Clear();
            }


        }

        private void AddNeededNodes(ElementSave element)
        {
            foreach (var category in element.Categories)
            {
                if (GetTreeNodeForTag(category) == null)
                {
                    var treeNode = mTreeView.Nodes.Add(category.Name);
                    treeNode.Tag = category;
                    treeNode.ImageIndex = ElementTreeViewManager.FolderImageIndex;
                }
            }

            foreach (var state in element.States)
            {
                // uncategorized
                if (GetTreeNodeForTag(state) == null)
                {
                    var treeNode = mTreeView.Nodes.Add(state.Name);
                    treeNode.Tag = state;
                    treeNode.ImageIndex = ElementTreeViewManager.StateImageIndex;
                }
            }

            foreach (var category in element.Categories)
            {
                foreach (var state in category.States)
                {
                    // uncategorized
                    if (GetTreeNodeForTag(state) == null)
                    {
                        var toAddTo = GetTreeNodeForTag(category);

                        var treeNode = toAddTo.Nodes.Add(state.Name);
                        treeNode.ImageIndex = ElementTreeViewManager.StateImageIndex;

                        treeNode.Tag = state;
                    }
                }
            }

        }

        private void RemoveUnnecessaryNodes(ElementSave element)
        {
            var allNodes = mTreeView.Nodes.AllNodes().ToList();

            foreach (var node in allNodes)
            {
                if (node.Tag is StateSave && element.AllStates.Contains(node.Tag as StateSave) == false)
                {
                    if (node.Parent == null)
                    {
                        mTreeView.Nodes.Remove(node);
                    }
                    else
                    {
                        node.Parent.Nodes.Remove(node);
                    }
                }
                else if (node.Tag is StateSaveCategory && element.Categories.Contains(node.Tag as StateSaveCategory) == false)
                {
                    if (node.Parent == null)
                    {
                        mTreeView.Nodes.Remove(node);
                    }
                    else
                    {
                        node.Parent.Nodes.Remove(node);
                    }
                }
            }
        }


        internal void HandleKeyDown(KeyEventArgs e)
        {
            HandleCopyCutPaste(e);
        }

        private void HandleCopyCutPaste(KeyEventArgs e)
        {
            if ((e.Modifiers & Keys.Control) == Keys.Control)
            {
                // copy, ctrl c, ctrl + c
                if (e.KeyCode == Keys.C)
                {
                    EditingManager.Self.OnCopy(CopyType.State);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                // paste, ctrl v, ctrl + v
                else if (e.KeyCode == Keys.V)
                {
                    EditingManager.Self.OnPaste(CopyType.State);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                //// cut, ctrl x, ctrl + x
                //else if (e.KeyCode == Keys.X)
                //{
                //    EditingManager.Self.OnCut(CopyType.Instance);
                //    e.Handled = true;
                //    e.SuppressKeyPress = true;
                //}
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
