using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonFormsAndControls;
using System.Windows.Forms;
using Gum.DataTypes;
using Gum.ToolStates;
using Gum.DataTypes.Variables;
using System.IO;
using Gum.Debug;
using ToolsUtilities;
using Gum.Events;
using Gum.Wireframe;

namespace Gum.Managers
{
    public partial class ElementTreeViewManager
    {
        #region Fields

        const int TransparentImageIndex = 0;
        const int FolderImageIndex = 1;
        const int ComponentImageIndex = 2;
        const int InstanceImageIndex = 3;
        const int ScreenImageIndex = 4;
        const int StandardElementImageIndex = 5;
        const int ExclamationIndex = 6;


        static ElementTreeViewManager mSelf;

        MultiSelectTreeView mTreeView;

        TreeNode mScreensTreeNode;
        TreeNode mComponentsTreeNode;
        TreeNode mStandardElementsTreeNode;

        /// <summary>
        /// Used to store off what was previously selected
        /// when the tree view refreshes itself - so the user
        /// doesn't lose his selection.
        /// </summary>
        object mRecordedSelectedObject;

        #endregion

        #region Properties

        public static ElementTreeViewManager Self
        {
            get 
            {
                if (mSelf == null)
                {
                    mSelf = new ElementTreeViewManager();
                }
                return mSelf; 
            }
        }

        public TreeNode SelectedNode
        {
            get { return mTreeView.SelectedNode; }
            set
            {
                mTreeView.SelectedNode = value;
            }
        }

        public List<TreeNode> SelectedNodes
        {
            get
            {
                return mTreeView.SelectedNodes;
            }
        }
        #endregion


        public void Initialize(MultiSelectTreeView treeView)
        {
            mTreeView = treeView;
            mMenuStrip = mTreeView.ContextMenuStrip;
            RefreshUI();

            InitializeTreeNodes();
        }
        

        public void RefreshUI()
        {
            RecordSelection();
            // brackets are used simply to indicate the recording and selection should
            // go around the rest of the function:
            {
                CreateRootTreeNodesIfNecessary();

                AddAndRemoveFolderNodes();

                AddAndRemoveScreensComponentsAndStandards(null);
            }
            SelectRecordedSelection();

        }

        private void AddAndRemoveFolderNodes()
        {
            if (ObjectFinder.Self.GumProjectSave != null && 
                
                !string.IsNullOrEmpty(ObjectFinder.Self.GumProjectSave.FullFileName ))
            {
                string currentDirectory = FileManager.GetDirectory(ObjectFinder.Self.GumProjectSave.FullFileName);

                // Let's make sure these folders exist, they better!
                Directory.CreateDirectory(this.mStandardElementsTreeNode.GetFullFilePath());
                Directory.CreateDirectory(this.mScreensTreeNode.GetFullFilePath());
                Directory.CreateDirectory(this.mComponentsTreeNode.GetFullFilePath());


                // add folders to the screens, entities, and standard elements
                AddAndRemoveFolderNodes(this.mStandardElementsTreeNode.GetFullFilePath(), this.mStandardElementsTreeNode.Nodes);
                AddAndRemoveFolderNodes(this.mScreensTreeNode.GetFullFilePath(), this.mScreensTreeNode.Nodes);
                AddAndRemoveFolderNodes(this.mComponentsTreeNode.GetFullFilePath(), this.mComponentsTreeNode.Nodes);

                //AddAndRemoveFolderNodes(currentDirectory, this.mTreeView.Nodes);
            }
        }

        private void AddAndRemoveFolderNodes(string currentDirectory, TreeNodeCollection nodesToAddTo)
        {

            // todo: removes
            var directories = Directory.EnumerateDirectories(currentDirectory);

            foreach (string directory in directories)
            {
                TreeNode existingTreeNode = GetTreeNodeFor(directory);

                if (existingTreeNode == null)
                {
                    existingTreeNode = nodesToAddTo.Add(FileManager.RemovePath(directory));
                    existingTreeNode.ImageIndex = FolderImageIndex;
                }
                AddAndRemoveFolderNodes(directory, existingTreeNode.Nodes);
            }

            for(int i = nodesToAddTo.Count - 1; i > -1; i--)
            {
                TreeNode node = nodesToAddTo[i];

                bool found = false;

                string nodeText = node.Text.ToLower();

                foreach (string directory in directories)
                {
                    string directoryStripped = FileManager.RemovePath(directory).ToLower();

                    if (directoryStripped == nodeText)
                    {
                        found = true;
                        break;
                    }

                }

                if (!found)
                {
                    nodesToAddTo.RemoveAt(i);

                }               

            }
        }

        private void AddAndRemoveScreensComponentsAndStandards(TreeNode folderTreeNode)
        {
            // Save off old selected stuff
            InstanceSave selectedInstance = SelectedState.Self.SelectedInstance;
            ElementSave selectedElement = SelectedState.Self.SelectedElement;


            ///////////////EARLY OUT///////////////////
            if (ProjectManager.Self.GumProjectSave == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(ProjectManager.Self.GumProjectSave.FullFileName))
            {
                string currentDirectory = FileManager.GetDirectory(ProjectManager.Self.GumProjectSave.FullFileName);

                if (folderTreeNode != null)
                {
                    currentDirectory = folderTreeNode.GetFullFilePath();
                }
            }

            // todo - now use this 


            #region Add nodes that haven't been added yet

            foreach (ScreenSave screenSave in ProjectManager.Self.GumProjectSave.Screens)
            {
                if (GetTreeNodeFor(screenSave) == null)
                {
                    TreeNode treeNode = new TreeNode();
                    if (screenSave.IsSourceFileMissing)
                    {
                        treeNode.ImageIndex = ExclamationIndex;
                    }
                    else
                    {
                        treeNode.ImageIndex = ScreenImageIndex;
                    }
                    treeNode.Tag = screenSave;

                    string fullPath = FileLocations.Self.ScreensFolder + FileManager.GetDirectory(screenSave.Name);

                    TreeNode treeNodeToAddTo = GetTreeNodeFor(fullPath);

                    treeNodeToAddTo.Nodes.Add(treeNode);
                    
                }
            }
            foreach (ComponentSave componentSave in ProjectManager.Self.GumProjectSave.Components)
            {
                if (GetTreeNodeFor(componentSave) == null)
                {
                    TreeNode treeNode = new TreeNode();
                    if (componentSave.IsSourceFileMissing)
                    {
                        treeNode.ImageIndex = ExclamationIndex;
                    }
                    else
                    {
                        treeNode.ImageIndex = ComponentImageIndex;
                    }
                    treeNode.Tag = componentSave;

                    string fullPath = FileLocations.Self.ComponentsFolder + FileManager.GetDirectory(componentSave.Name);

                    TreeNode treeNodeToAddTo = GetTreeNodeFor(fullPath);

                    treeNodeToAddTo.Nodes.Add(treeNode);
                }
            }

            foreach (StandardElementSave standardSave in ProjectManager.Self.GumProjectSave.StandardElements)
            {
                if (standardSave.Name != "Component")
                {
                    if (GetTreeNodeFor(standardSave) == null)
                    {
                        TreeNode treeNode = new TreeNode();
                        if (standardSave.IsSourceFileMissing)
                        {
                            treeNode.ImageIndex = ExclamationIndex;
                        }
                        else
                        {
                            treeNode.ImageIndex = StandardElementImageIndex;
                        }
                        treeNode.Tag = standardSave;

                        mStandardElementsTreeNode.Nodes.Add(treeNode);
                    }
                }
            }

            #endregion

            #region Remove nodes that are no longer needed

            for (int i = mScreensTreeNode.Nodes.Count - 1; i > -1; i--)
            {
                // If the tag is null, that means that it's a folder TreeNode, so we don't want to remove it
                if (mScreensTreeNode.Nodes[i].Tag != null)
                {

                    ScreenSave ss = mScreensTreeNode.Nodes[i].Tag as ScreenSave;

                    if (!ProjectManager.Self.GumProjectSave.Screens.Contains(ss))
                    {
                        mScreensTreeNode.Nodes.RemoveAt(i);
                    }
                }
            }

            for (int i = mComponentsTreeNode.Nodes.Count - 1; i > -1; i--)
            {
                // If the tag is null, that means that it's a folder TreeNode, so we don't want to remove it
                if (mComponentsTreeNode.Nodes[i].Tag != null)
                {
                    ComponentSave cs = mComponentsTreeNode.Nodes[i].Tag as ComponentSave;

                    if (!ProjectManager.Self.GumProjectSave.Components.Contains(cs))
                    {
                        mComponentsTreeNode.Nodes.RemoveAt(i);
                    }
                }
            }

            for (int i = mStandardElementsTreeNode.Nodes.Count - 1; i > -1; i-- )
            {
                // Do we want to support folders here?
                StandardElementSave ses = mStandardElementsTreeNode.Nodes[i].Tag as StandardElementSave;

                if (!ProjectManager.Self.GumProjectSave.StandardElements.Contains(ses))
                {
                    mStandardElementsTreeNode.Nodes.RemoveAt(i);
                }
            }

            #endregion

            #region Update the nodes

            foreach (TreeNode treeNode in mScreensTreeNode.Nodes)
            {
                RefreshUI(treeNode);
            }

            foreach (TreeNode treeNode in mComponentsTreeNode.Nodes)
            {
                RefreshUI(treeNode);
            }

            foreach (TreeNode treeNode in mStandardElementsTreeNode.Nodes)
            {
                RefreshUI(treeNode);
            }

            #endregion

            #region Re-select whatever was selected before

            if (selectedInstance != null)
            {
                SelectedState.Self.SelectedInstance = selectedInstance;
            }



            #endregion


        }

        private void CreateRootTreeNodesIfNecessary()
        {
            if (mScreensTreeNode == null)
            {
                mScreensTreeNode = new TreeNode("Screens");
                mScreensTreeNode.ImageIndex = FolderImageIndex;
                mTreeView.Nodes.Add(mScreensTreeNode);

                mComponentsTreeNode = new TreeNode("Components");
                mComponentsTreeNode.ImageIndex = FolderImageIndex;
                mTreeView.Nodes.Add(mComponentsTreeNode);

                mStandardElementsTreeNode = new TreeNode("Standard");
                mStandardElementsTreeNode.ImageIndex = FolderImageIndex;
                mTreeView.Nodes.Add(mStandardElementsTreeNode);

            }
        }

        public TreeNode GetTreeNodeFor(ElementSave elementSave)
        {
            if (elementSave == null)
            {
                return null;
            }
            else if (elementSave is ScreenSave)
            {
                return GetTreeNodeFor(elementSave as ScreenSave);
            }
            else if (elementSave is ComponentSave)
            {
                return GetTreeNodeFor(elementSave as ComponentSave);
            }
            else if (elementSave is StandardElementSave)
            {
                return GetTreeNodeFor(elementSave as StandardElementSave);
            }

            return null;
        }




        public TreeNode GetTreeNodeFor(ScreenSave screenSave)
        {
            return GetTreeNodeFor(screenSave.Name, mScreensTreeNode);
            
            //foreach (TreeNode treeNode in mScreensTreeNode.Nodes)
            //{
            //    if (treeNode.Tag == screenSave)
            //    {
            //        return treeNode;
            //    }
            //}

            //return null;
        }

        public TreeNode GetTreeNodeFor(ComponentSave componentSave)
        {
            foreach (TreeNode treeNode in this.mComponentsTreeNode.Nodes)
            {
                if (treeNode.Tag == componentSave)
                {
                    return treeNode;
                }
            }

            return null;
        }

        public TreeNode GetTreeNodeFor(StandardElementSave standardElementSave)
        {
            foreach (TreeNode treeNode in this.mStandardElementsTreeNode.Nodes)
            {
                if (treeNode.Tag == standardElementSave)
                {
                    return treeNode;
                }
            }

            return null;
        }

        public TreeNode GetTreeNodeFor(InstanceSave instanceSave, TreeNode container)
        {
            foreach (TreeNode node in container.Nodes)
            {
                if (node.Tag == instanceSave)
                {
                    return node;
                }
            }

            return null;
        }

        public TreeNode GetTreeNodeFor(string absoluteDirectory)
        {
            string relative = FileManager.MakeRelative(absoluteDirectory,
                FileManager.GetDirectory(ProjectManager.Self.GumProjectSave.FullFileName));


            relative = FileManager.Standardize(relative);

            if (relative.StartsWith("screens\\"))
            {
                string modifiedRelative = relative.Substring("screens\\".Length);

                return GetTreeNodeFor(modifiedRelative, mScreensTreeNode);
            }
            else if (relative.StartsWith("components\\"))
            {
                string modifiedRelative = relative.Substring("components\\".Length);

                return GetTreeNodeFor(modifiedRelative, mComponentsTreeNode);
            }
            else if (relative.StartsWith("standards\\"))
            {
                string modifiedRelative = relative.Substring("standards\\".Length);

                return GetTreeNodeFor(modifiedRelative, mStandardElementsTreeNode);
            }

            return null;

        }

        TreeNode GetTreeNodeFor(string relativeDirectory, TreeNode container)
        {
            string whatToLookFor = relativeDirectory;
            string sub = "";
            if (string.IsNullOrEmpty(relativeDirectory))
            {
                return container;
            }
            else if (relativeDirectory.Contains("\\"))
            {
                int indexOfSlashes = relativeDirectory.IndexOf('\\');
                whatToLookFor = relativeDirectory.Substring(0, indexOfSlashes);

                sub = relativeDirectory.Substring(indexOfSlashes + 1, relativeDirectory.Length - (indexOfSlashes + 1));


            }

            foreach (TreeNode node in container.Nodes)
            {
                if (node.Text.ToLower() == whatToLookFor.ToLower())
                {
                    return GetTreeNodeFor(sub, node);

                }
            }

            return null;

        }

        public TreeNode GetTreeNodeOver()
        {
            System.Drawing.Point point = 
                mTreeView.PointToClient(Cursor.Position);


            return mTreeView.GetNodeAt(point);

        }

        public void RecordSelection()
        {
            mRecordedSelectedObject = SelectedState.Self.SelectedInstance;

            if (mRecordedSelectedObject == null)
            {
                mRecordedSelectedObject = SelectedState.Self.SelectedElement;
            }
        }

        public void SelectRecordedSelection()
        {
            if (mRecordedSelectedObject != null)
            {
                if (mRecordedSelectedObject is InstanceSave)
                {
                    SelectedState.Self.SelectedInstance = mRecordedSelectedObject as InstanceSave;
                }
                else if (mRecordedSelectedObject is ElementSave)
                {
                    SelectedState.Self.SelectedElement = mRecordedSelectedObject as ElementSave;
                }
            }
        }

        // Discussion about Selection
        // Selection is a rather complicated
        // system in Gum because tree nodes can
        // be selected in a number of ways:
        // 1.  The user can push/release (click)
        // 2.  The user can select an item in the
        //     wireframe window which in turn selects
        //     the appropriate tree node.
        // 3.  The user pushes on a tree node, but then
        //     drags off of it to do a drag+drop somewhere
        //     else.
        // We want the app to refresh what it is displaying
        // in scenario 1 and 2, but not in 3.  Therefore the
        // MultiSelectTreeView class has an event called AfterClickSelect
        // which only fires when the user actually clicks on an item (1) so
        // that #3 doesn't fire off an event.  However, this means that #2 will
        // no longer fire off the event either.  We need to then make sure that #2
        // does still fire off an event, so we'll do this by manually raising the event
        // in the Select methods where a Save object is selected.
        public void Select(InstanceSave instanceSave, ElementSave parent)
        {
            if (instanceSave != null)
            {
                TreeNode parentTreeNode = GetTreeNodeFor(parent);

                if (parentTreeNode == null)
                {
                    throw new NullReferenceException("Could not find a tree node for " + parent);
                }

                TreeNode treeNode = GetTreeNodeFor(instanceSave, parentTreeNode);

                Select(treeNode);

                
            }
            else
            {
                Select((TreeNode)null);
            }
        }

        public void Select(IEnumerable<InstanceSave> list)
        {
            if (list.Count() != 0)
            {
                TreeNode parentContainer = GetTreeNodeFor(list.First().ParentContainer);

                List<TreeNode> treeNodeList = new List<TreeNode>();

                foreach (var item in list)
                {
                    TreeNode itemTreeNode = GetTreeNodeFor(item, parentContainer);
                    treeNodeList.Add(itemTreeNode);

                }

                Select(treeNodeList);

            }
            else
            {
                Select((TreeNode)null);
            }

        }


        public void Select(ElementSave elementSave)
        {
            if (elementSave == null)
            {
                if (mTreeView.SelectedNode != null && mTreeView.SelectedNode.Tag != null && mTreeView.SelectedNode.Tag is ElementSave)
                {
                    mTreeView.SelectedNode = null;
                }
            }
            else
            {
                if (elementSave is ScreenSave)
                {
                    Select(elementSave as ScreenSave);
                }
                else if(elementSave is ComponentSave)
                {
                    Select(elementSave as ComponentSave);
                }
                else if (elementSave is StandardElementSave)
                {
                    Select(elementSave as StandardElementSave);
                }
            }

        }

        void Select(ScreenSave screenSave)
        {
            TreeNode treeNode = GetTreeNodeFor(screenSave);

            Select(treeNode);
        }

        void Select(ComponentSave componentSave)
        {
            TreeNode treeNode = GetTreeNodeFor(componentSave);

            Select(treeNode);
        }

        void Select(StandardElementSave standardElementSave)
        {
            TreeNode treeNode = GetTreeNodeFor(standardElementSave);

            Select(treeNode);
        }


        private void Select(TreeNode treeNode)
        {
            if (mTreeView.SelectedNode != treeNode)
            {
                // See comment above about why we have to manually raise the AfterClick



                mTreeView.SelectedNode = treeNode;

                if (treeNode != null)
                {
                    treeNode.EnsureVisible();
                }

                mTreeView.CallAfterClickSelect(null, new TreeViewEventArgs(treeNode));


            }
            ProjectVerifier.Self.AssertSelectedIpsosArePartOfRenderer();
        }

        private void Select(List<TreeNode> treeNodes)
        {
            mTreeView.SelectedNodes = treeNodes;

            if (treeNodes.Count != 0)
            {
                treeNodes[0].EnsureVisible();
                mTreeView.CallAfterClickSelect(null, new TreeViewEventArgs(treeNodes[0]));
            }
            ProjectVerifier.Self.AssertSelectedIpsosArePartOfRenderer();
        }

        public void RefreshUI(ElementSave elementSave)
        {
            RecordSelection();

            RefreshUIInternal(elementSave);

            SelectRecordedSelection();
        }

        void RefreshUIInternal(ElementSave elementSave)
        {
            var foundNode = GetTreeNodeFor(elementSave);

            if (foundNode != null)
            {
                RefreshUI(foundNode);
            }
        }

        void RefreshUI(TreeNode node)
        {

            if (node.Tag is ElementSave)
            {
                ElementSave elementSave = node.Tag as ElementSave;

                node.Text = FileManager.RemovePath(elementSave.Name);

                node.Nodes.Clear();


                foreach (InstanceSave instance in elementSave.Instances)
                {
                    TreeNode nodeForInstance = GetTreeNodeFor(instance, node);

                    if (nodeForInstance == null)
                    {
                        TreeNode treeNode = new TreeNode();
                        treeNode.ImageIndex = InstanceImageIndex;
                        treeNode.Tag = instance;
                        node.Nodes.Add(treeNode);

                    }
                }
            }
            else if (node.Tag is InstanceSave)
            {
                InstanceSave instanceSave = node.Tag as InstanceSave;

                node.Text = instanceSave.Name;
            }

            foreach (TreeNode treeNode in node.Nodes)
            {
                RefreshUI(treeNode);

            }

            // Why do we do this?  Shouldn't we do it by order in the list?
            //node.Nodes.SortByName();
        }
        
        internal void OnSelect(TreeNode selectedTreeNode)
        {
            TreeNode treeNode = mTreeView.SelectedNode;

            object selectedObject = null;

            if (treeNode != null)
            {
                selectedObject = treeNode.Tag;
            }

            if (selectedObject == null)
            {

                SelectedState.Self.UpdateToSelectedElement();
                // do nothing
            }
            else if(selectedObject is ElementSave)
            {
                SelectedState.Self.UpdateToSelectedElement();
            }
            else if (selectedObject is InstanceSave)
            {
                SelectedState.Self.UpdateToSelectedInstanceSave();


                GumEvents.Self.CallInstanceSelected();
            }
            

        }



        public void VerifyComponentsAreInTreeView(GumProjectSave gumProject)
        {
            foreach (ComponentSave component in gumProject.Components)
            {
                if (GetTreeNodeFor(component) == null)
                {
                    throw new Exception();
                }

            }

        }





        internal void HandleKeyDown(KeyEventArgs e)
        {

            HandleCopyCutPaste(e);

            HandleDelete(e);

            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
            {
                ElementTreeViewManager.Self.OnSelect(ElementTreeViewManager.Self.SelectedNode);
            }
        }

        public void HandleDelete(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                EditingManager.Self.HandleDelete();

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        public void HandleCopyCutPaste(KeyEventArgs e)
        {
            if ((e.Modifiers & Keys.Control) == Keys.Control)
            {
                // copy, ctrl c, ctrl + c
                if (e.KeyCode == Keys.C)
                {
                    EditingManager.Self.OnCopy(CopyType.Instance);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                // paste, ctrl v, ctrl + v
                else if (e.KeyCode == Keys.V)
                {
                    EditingManager.Self.OnPaste(CopyType.Instance);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                // cut, ctrl x, ctrl + x
                else if (e.KeyCode == Keys.X)
                {
                    EditingManager.Self.OnCut(CopyType.Instance);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
        }
    }


    #region TreeNodeExtensionMethods

    public static class TreeNodeExtensionMethods
    {
        public static bool IsScreenTreeNode(this TreeNode treeNode)
        {
            return treeNode.Tag != null && treeNode.Tag is ScreenSave;
        }

        public static bool IsComponentTreeNode(this TreeNode treeNode)
        {
            return treeNode.Tag != null && treeNode.Tag is ComponentSave;
        }

        public static bool IsStandardElementTreeNode(this TreeNode treeNode)
        {
            return treeNode.Tag != null && treeNode.Tag is StandardElementSave;
        }

        public static bool IsInstanceTreeNode(this TreeNode treeNode)
        {
            return treeNode.Tag != null && treeNode.Tag is InstanceSave;
        }

        public static bool IsStateSaveTreeNode(this TreeNode treeNode)
        {
            return treeNode.Tag != null && treeNode.Tag is StateSave;
        }

        public static bool IsTopElementContainerTreeNode(this TreeNode treeNode)
        {
            return treeNode.Tag == null;
        }

        public static bool IsTopScreenContainerTreeNode(this TreeNode treeNode)
        {
            return treeNode.Parent == null && treeNode.Text == "Screens";
        }

        public static bool IsTopComponentContainerTreeNode(this TreeNode treeNode)
        {
            return treeNode.Parent == null && treeNode.Text == "Components";
        }

        public static bool IsTopStandardElementTreeNode(this TreeNode treeNode)
        {
            return treeNode.Parent == null && treeNode.Text == "Standard";
        }

        public static string GetFullFilePath(this TreeNode treeNode)
        {
            if (treeNode.IsTopComponentContainerTreeNode() ||
                treeNode.IsTopStandardElementTreeNode() ||
                treeNode.IsTopScreenContainerTreeNode())
            {
                if (ProjectManager.Self.GumProjectSave == null ||
                    string.IsNullOrEmpty(ProjectManager.Self.GumProjectSave.FullFileName))
                {
                    MessageBox.Show("Project isn't saved yet so the root of the project isn't known");
                    return null;
                }
                else
                {
                    string projectDirectory = FileManager.GetDirectory(ProjectManager.Self.GumProjectSave.FullFileName);

                    if (treeNode.IsTopComponentContainerTreeNode())
                    {
                        return projectDirectory + ElementReference.ComponentSubfolder + "\\";
                    }
                    else if (treeNode.IsTopStandardElementTreeNode())
                    {
                        return projectDirectory + ElementReference.StandardSubfolder + "\\";
                    }
                    else if (treeNode.IsTopScreenContainerTreeNode())
                    {
                        return projectDirectory + ElementReference.ScreenSubfolder + "\\";
                    }
                    throw new InvalidOperationException();
                }
            }
            else if (treeNode.IsStandardElementTreeNode() ||
                treeNode.IsComponentTreeNode() ||
                treeNode.IsScreenTreeNode())
            {
                ElementSave element = treeNode.Tag as ElementSave;


                string toReturn = treeNode.Parent.GetFullFilePath() + treeNode.Text + "." + element.FileExtension;

                return toReturn;
            }
            else
            {
                string toReturn = treeNode.Parent.GetFullFilePath() + treeNode.Text + "\\";

                return toReturn;
            }
        }

        public static bool IsScreensFolderTreeNode(this TreeNode treeNode)
        {
            return treeNode.Tag == null &&
                treeNode.Parent != null &&
                (treeNode.Parent.IsScreensFolderTreeNode() || treeNode.Parent.IsTopScreenContainerTreeNode());
        }

        public static bool IsComponentsFolderTreeNode(this TreeNode treeNode)
        {
            return treeNode.Tag == null &&
                treeNode.Parent != null &&
                (treeNode.Parent.IsComponentsFolderTreeNode() || treeNode.Parent.IsTopComponentContainerTreeNode());
        }

        public static void SortByName(this TreeNodeCollection treeNodeCollection)
        {
            int lastObjectExclusive = treeNodeCollection.Count;
            int whereObjectBelongs;
            for (int i = 0 + 1; i < lastObjectExclusive; i++)
            {
                TreeNode first = treeNodeCollection[i];
                TreeNode second = treeNodeCollection[i - 1];
                if (FirstComesBeforeSecond(first, second))
                {
                    if (i == 1)
                    {
                        TreeNode treeNode = treeNodeCollection[i];
                        treeNodeCollection.RemoveAt(i);

                        treeNodeCollection.Insert(0, treeNode);
                        continue;
                    }

                    for (whereObjectBelongs = i - 2; whereObjectBelongs > -1; whereObjectBelongs--)
                    {
                        second = treeNodeCollection[whereObjectBelongs];
                        if (!FirstComesBeforeSecond(treeNodeCollection[i], second))
                        {
                            TreeNode treeNode = treeNodeCollection[i];

                            treeNodeCollection.RemoveAt(i);
                            treeNodeCollection.Insert(whereObjectBelongs + 1, treeNode);
                            break;
                        }
                        else if (whereObjectBelongs == 0 && FirstComesBeforeSecond(treeNodeCollection[i], treeNodeCollection[0]))
                        {
                            TreeNode treeNode = treeNodeCollection[i];
                            treeNodeCollection.RemoveAt(i);
                            treeNodeCollection.Insert(0, treeNode);
                            break;
                        }
                    }
                }
            }

        }

        private static bool FirstComesBeforeSecond(TreeNode first, TreeNode second)
        {
            bool isFirstDirectory = first.IsComponentsFolderTreeNode() || first.IsScreensFolderTreeNode();
            bool isSecondDirectory = second.IsComponentsFolderTreeNode() || second.IsScreensFolderTreeNode();

            if (isFirstDirectory && !isSecondDirectory)
            {
                return true;
            }
            else if (!isFirstDirectory && isSecondDirectory)
            {
                return false;
            }
            else
            {
                return first.Text.CompareTo(second.Text) < 0;
            }
        }

    }

#endregion
}
