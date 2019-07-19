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
using Gum.DataTypes.Behaviors;
using Gum.Plugins;

namespace Gum.Managers
{
    public partial class ElementTreeViewManager
    {
        #region Fields

        public const int TransparentImageIndex = 0;
        public const int FolderImageIndex = 1;
        public const int ComponentImageIndex = 2;
        public const int InstanceImageIndex = 3;
        public const int ScreenImageIndex = 4;
        public const int StandardElementImageIndex = 5;
        public const int ExclamationIndex = 6;
        public const int StateImageIndex = 7;
        public const int BehaviorImageIndex = 8;

        static ElementTreeViewManager mSelf;

        MultiSelectTreeView mTreeView;

        TreeNode mScreensTreeNode;
        TreeNode mComponentsTreeNode;
        TreeNode mStandardElementsTreeNode;
        TreeNode mBehaviorsTreeNode;

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
            get
            {
                // This could be called before the tree is created:
                if (mTreeView == null)
                {
                    return null;
                }
                else
                {
                    return mTreeView.SelectedNode;
                }
            }
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

        #region Methods


        #region Find/Get
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
            return GetTreeNodeForTag(screenSave, RootScreensTreeNode);
        }

        public TreeNode GetTreeNodeFor(ComponentSave componentSave)
        {
            return GetTreeNodeForTag(componentSave, RootComponentsTreeNode);
        }

        public TreeNode GetTreeNodeFor(StandardElementSave standardElementSave)
        {
            return GetTreeNodeForTag(standardElementSave, RootStandardElementsTreeNode);
        }

        public TreeNode GetTreeNodeFor(InstanceSave instanceSave, TreeNode container)
        {
            foreach (TreeNode node in container.Nodes)
            {
                if (node.Tag == instanceSave)
                {
                    return node;
                }

                TreeNode childNode = GetTreeNodeFor(instanceSave, node);
                if (childNode != null)
                {
                    return childNode;
                }
            }

            return null;
        }

        public TreeNode GetTreeNodeFor(BehaviorSave behavior)
        {
            return GetTreeNodeForTag(behavior, RootBehaviorsTreeNode);
        }

        public TreeNode GetTreeNodeFor(string absoluteDirectory)
        {
            string relative = FileManager.MakeRelative(absoluteDirectory,
                FileManager.GetDirectory(ProjectManager.Self.GumProjectSave.FullFileName));


            relative = FileManager.Standardize(relative);

            if (relative.StartsWith("screens/"))
            {
                string modifiedRelative = relative.Substring("screens/".Length);

                return GetTreeNodeFor(modifiedRelative, mScreensTreeNode);
            }
            else if (relative.StartsWith("components/"))
            {
                string modifiedRelative = relative.Substring("components/".Length);

                return GetTreeNodeFor(modifiedRelative, mComponentsTreeNode);
            }
            else if (relative.StartsWith("standards/"))
            {
                string modifiedRelative = relative.Substring("standards/".Length);

                return GetTreeNodeFor(modifiedRelative, mStandardElementsTreeNode);
            }
            else if(relative.StartsWith("behaviors/"))
            {
                string modifiedRelative = relative.Substring("behaviors/".Length);

                return GetTreeNodeFor(modifiedRelative, mBehaviorsTreeNode);
            }

            return null;

        }

        TreeNode GetTreeNodeFor(string relativeDirectory, TreeNode container)
        {
            if (string.IsNullOrEmpty(relativeDirectory))
            {
                return container;
            }

            int indexOfSlash = relativeDirectory.IndexOf('/');
            string whatToLookFor = relativeDirectory;
            string sub = "";

            if (indexOfSlash != -1)
            {
                whatToLookFor = relativeDirectory.Substring(0, indexOfSlash);
                sub = relativeDirectory.Substring(indexOfSlash + 1, relativeDirectory.Length - (indexOfSlash + 1));
            }

            foreach (TreeNode node in container.Nodes)
            {
                if (node.Text.Equals(whatToLookFor, StringComparison.OrdinalIgnoreCase))
                {
                    return GetTreeNodeFor(sub, node);
                }
            }

            return null;
        }

        TreeNode GetTreeNodeForTag(object tag, TreeNode container = null)
        {
            if (container == null)
            {
                if (tag is ScreenSave)
                {
                    container = RootScreensTreeNode;
                }
                else if (tag is ComponentSave)
                {
                    container = RootComponentsTreeNode;
                }
                else if (tag is StandardElementSave)
                {
                    container = RootStandardElementsTreeNode;
                }
                else if(tag is BehaviorSave)
                {
                    container = RootBehaviorsTreeNode;
                }
            }

            foreach (TreeNode treeNode in container.Nodes)
            {
                if (treeNode.Tag == tag)
                {
                    return treeNode;
                }

                var found = GetTreeNodeForTag(tag, treeNode);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        public TreeNode GetTreeNodeOver()
        {
            System.Drawing.Point point = mTreeView.PointToClient(Cursor.Position);

            return mTreeView.GetNodeAt(point);
        }

        #endregion




        public TreeNode RootScreensTreeNode
        {
            get
            {
                return mScreensTreeNode;
            }
        }

        public TreeNode RootComponentsTreeNode
        {
            get
            {
                return mComponentsTreeNode;
            }
        }

        public TreeNode RootStandardElementsTreeNode
        {
            get
            {
                return mStandardElementsTreeNode;
            }
        }

        public TreeNode RootBehaviorsTreeNode
        {
            get
            {
                return mBehaviorsTreeNode;
            }
        }

        public void Initialize(MultiSelectTreeView treeView)
        {
            mTreeView = treeView;
            mMenuStrip = mTreeView.ContextMenuStrip;
            GumCommands.Self.GuiCommands.RefreshElementTreeView();

            mTreeView.DragDrop += HandleDragDropEvent;

            mTreeView.ItemDrag += (sender, e) =>
            {
                TreeNode node = e.Item as TreeNode;

                mTreeView.DoDragDrop(node, DragDropEffects.Move | DragDropEffects.Copy);

            };

            mTreeView.DragEnter += (sender, e) =>
            {
                e.Effect = DragDropEffects.All;

            };

            mTreeView.DragOver += (sender, e) =>
            {
                e.Effect = DragDropEffects.Move;
            };

            InitializeMenuItems();
        }

        private void HandleDragDropEvent(object sender, DragEventArgs e)
        {
            DragDropManager.Self.HandleDragDropEvent(sender, e);
        }

        private void AddAndRemoveFolderNodes()
        {
            if (ObjectFinder.Self.GumProjectSave != null && 
                
                !string.IsNullOrEmpty(ObjectFinder.Self.GumProjectSave.FullFileName))
            {
                string currentDirectory = FileManager.GetDirectory(ObjectFinder.Self.GumProjectSave.FullFileName);

                // Let's make sure these folders exist, they better!
                Directory.CreateDirectory(mStandardElementsTreeNode.GetFullFilePath());
                Directory.CreateDirectory(mScreensTreeNode.GetFullFilePath());
                Directory.CreateDirectory(mComponentsTreeNode.GetFullFilePath());
                Directory.CreateDirectory(mBehaviorsTreeNode.GetFullFilePath());


                // add folders to the screens, entities, and standard elements
                AddAndRemoveFolderNodes(mStandardElementsTreeNode.GetFullFilePath(), mStandardElementsTreeNode.Nodes);
                AddAndRemoveFolderNodes(mScreensTreeNode.GetFullFilePath(), mScreensTreeNode.Nodes);
                AddAndRemoveFolderNodes(mComponentsTreeNode.GetFullFilePath(), mComponentsTreeNode.Nodes);
                AddAndRemoveFolderNodes(mBehaviorsTreeNode.GetFullFilePath(), mBehaviorsTreeNode.Nodes);

                //AddAndRemoveFolderNodes(currentDirectory, this.mTreeView.Nodes);
            }
            else
            {
                RootComponentsTreeNode.Nodes.Clear();
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

                foreach (string directory in directories)
                {
                    string directoryStripped = FileManager.RemovePath(directory);

                    if (directoryStripped.Equals(node.Text, StringComparison.OrdinalIgnoreCase))
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
            if (ProjectManager.Self.GumProjectSave == null)
                return;

            // Save off old selected stuff
            InstanceSave selectedInstance = SelectedState.Self.SelectedInstance;
            ElementSave selectedElement = SelectedState.Self.SelectedElement;
            BehaviorSave selectedBehavior = SelectedState.Self.SelectedBehavior;


            if (!string.IsNullOrEmpty(ProjectManager.Self.GumProjectSave.FullFileName))
            {
                string currentDirectory = FileManager.GetDirectory(ProjectManager.Self.GumProjectSave.FullFileName);

                if (folderTreeNode != null)
                {
                    currentDirectory = folderTreeNode.GetFullFilePath();
                }
            }
            


            #region Add nodes that haven't been added yet

            foreach (ScreenSave screenSave in ProjectManager.Self.GumProjectSave.Screens)
            {
                if (GetTreeNodeFor(screenSave) == null)
                {
                    string fullPath = FileLocations.Self.ScreensFolder + FileManager.GetDirectory(screenSave.Name);
                    TreeNode parentNode = GetTreeNodeFor(fullPath);

                    AddTreeNodeForElement(screenSave, parentNode, ScreenImageIndex);
                }
            }

            foreach (ComponentSave componentSave in ProjectManager.Self.GumProjectSave.Components)
            {
                if (GetTreeNodeFor(componentSave) == null)
                {
                    string fullPath = FileLocations.Self.ComponentsFolder + FileManager.GetDirectory(componentSave.Name);
                    TreeNode parentNode = GetTreeNodeFor(fullPath);

                    if(parentNode == null)
                    {
                        throw new Exception($"Error trying to get parent node for component {fullPath}");
                    }

                    AddTreeNodeForElement(componentSave, parentNode, ComponentImageIndex);
                }
            }

            foreach (StandardElementSave standardSave in ProjectManager.Self.GumProjectSave.StandardElements)
            {
                if (standardSave.Name != "Component")
                {
                    if (GetTreeNodeFor(standardSave) == null)
                    {
                        AddTreeNodeForElement(standardSave, mStandardElementsTreeNode, StandardElementImageIndex);
                    }
                }
            }

            foreach(BehaviorSave behaviorSave in ProjectManager.Self.GumProjectSave.Behaviors)
            {
                if(GetTreeNodeFor(behaviorSave) == null)
                {
                    string fullPath = FileLocations.Self.BehaviorsFolder + FileManager.GetDirectory(behaviorSave.Name);
                    TreeNode parentNode = GetTreeNodeFor(fullPath);

                    AddTreeNodeForBehavior(behaviorSave, parentNode, BehaviorImageIndex);
                }
            }

            #endregion

            #region Remove nodes that are no longer needed

            for (int i = mScreensTreeNode.Nodes.Count - 1; i > -1; i--)
            {
                ScreenSave screen = mScreensTreeNode.Nodes[i].Tag as ScreenSave;

                // If the screen is null, that means that it's a folder TreeNode, so we don't want to remove it
                if (screen != null)
                {
                    if (!ProjectManager.Self.GumProjectSave.Screens.Contains(screen))
                    {
                        mScreensTreeNode.Nodes.RemoveAt(i);
                    }
                }
            }

            for (int i = mComponentsTreeNode.Nodes.Count - 1; i > -1; i--)
            {
                ComponentSave component = mComponentsTreeNode.Nodes[i].Tag as ComponentSave;

                // If the component is null, that means that it's a folder TreeNode, so we don't want to remove it
                if (component != null)
                {
                    if (!ProjectManager.Self.GumProjectSave.Components.Contains(component))
                    {
                        mComponentsTreeNode.Nodes.RemoveAt(i);
                    }
                }
            }

            for (int i = mStandardElementsTreeNode.Nodes.Count - 1; i > -1; i-- )
            {
                // Do we want to support folders here?
                StandardElementSave standardElement = mStandardElementsTreeNode.Nodes[i].Tag as StandardElementSave;

                if (!ProjectManager.Self.GumProjectSave.StandardElements.Contains(standardElement))
                {
                    mStandardElementsTreeNode.Nodes.RemoveAt(i);
                }
            }

            for(int i = mBehaviorsTreeNode.Nodes.Count - 1; i > -1; i--)
            {
                BehaviorSave behavior = mBehaviorsTreeNode.Nodes[i].Tag as BehaviorSave;

                if(behavior != null)
                {
                    if(!ProjectManager.Self.GumProjectSave.Behaviors.Contains(behavior))
                    {
                        mBehaviorsTreeNode.Nodes.RemoveAt(i);
                    }
                }
            }

            #endregion

            #region Update the nodes

            foreach (TreeNode treeNode in mScreensTreeNode.Nodes)
            {
                RefreshUi(treeNode);
            }

            foreach (TreeNode treeNode in mComponentsTreeNode.Nodes)
            {
                RefreshUi(treeNode);
            }

            foreach (TreeNode treeNode in mStandardElementsTreeNode.Nodes)
            {
                RefreshUi(treeNode);
            }

            foreach(TreeNode treeNode in mBehaviorsTreeNode.Nodes)
            {
                RefreshUi(treeNode);
            }

            #endregion

            #region Sort everything

            mScreensTreeNode.Nodes.SortByName(recursive:true);

            mComponentsTreeNode.Nodes.SortByName(recursive: true);

            mStandardElementsTreeNode.Nodes.SortByName(recursive: true);

            mBehaviorsTreeNode.Nodes.SortByName(recursive: true);

            #endregion

            #region Re-select whatever was selected before

            if (selectedInstance != null)
            {
                SelectedState.Self.SelectedInstance = selectedInstance;
            }
            if(selectedBehavior != null)
            {
                SelectedState.Self.SelectedBehavior = selectedBehavior;
            }
            #endregion
        }

        private static void AddTreeNodeForElement(ElementSave element, TreeNode parentNode, int defaultImageIndex)
        {
            if (parentNode == null)
            {
                throw new NullReferenceException($"{nameof(parentNode)} cannot be null");
            }
            TreeNode treeNode = new TreeNode();

            if (element.IsSourceFileMissing)
                treeNode.ImageIndex = ExclamationIndex;
            else
                treeNode.ImageIndex = defaultImageIndex;

            treeNode.Tag = element;
            
            parentNode.Nodes.Add(treeNode);
        }

        private static void AddTreeNodeForBehavior(BehaviorSave behavior, TreeNode parentNode, int defaultImageIndex)
        {
            TreeNode treeNode = new TreeNode();

            if (behavior.IsSourceFileMissing)
                treeNode.ImageIndex = ExclamationIndex;
            else
                treeNode.ImageIndex = defaultImageIndex;

            treeNode.Tag = behavior;

            parentNode.Nodes.Add(treeNode);
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

                mBehaviorsTreeNode = new TreeNode("Behaviors");
                mBehaviorsTreeNode.ImageIndex = FolderImageIndex;
                mTreeView.Nodes.Add(mBehaviorsTreeNode);
            }
        }


        public void RecordSelection()
        {
            mRecordedSelectedObject = SelectedState.Self.SelectedInstance;

            if (mRecordedSelectedObject == null)
            {
                mRecordedSelectedObject = SelectedState.Self.SelectedElement;
            }

            if(mRecordedSelectedObject == null)
            {
                mRecordedSelectedObject = SelectedState.Self.SelectedBehavior;
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
                else if(mRecordedSelectedObject is BehaviorSave)
                {
                    SelectedState.Self.SelectedBehavior = mRecordedSelectedObject as BehaviorSave;
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

                // This could be null if the user started a new project or loaded a different project.
                if (parentTreeNode != null)
                {
                    Select(GetTreeNodeFor(instanceSave, parentTreeNode));
                }
            }
            else
            {
                Select((TreeNode)null);
            }
        }

        public void Select(BehaviorSave behavior)
        {
            if(behavior != null)
            {
                var treeNode = GetTreeNodeFor(behavior);

                Select(treeNode);
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
                Select(GetTreeNodeFor(elementSave));
            }
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
        }

        private void Select(List<TreeNode> treeNodes)
        {
            mTreeView.SelectedNodes = treeNodes;

            if (treeNodes.Count != 0)
            {
                treeNodes[0].EnsureVisible();
                mTreeView.CallAfterClickSelect(null, new TreeViewEventArgs(treeNodes[0]));
            }
        }

        public void RefreshUi()
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

        public void RefreshUi(ElementSave elementSave)
        {
            TreeNode foundNode = GetTreeNodeFor(elementSave);

            if (foundNode != null)
            {
                RecordSelection();
                RefreshUi(foundNode);
                SelectRecordedSelection();
            }
        }

        public void RefreshUi(IStateContainer stateContainer)
        {
            var foundNode = GetTreeNodeForTag(stateContainer);

            if(foundNode != null)
            {
                RecordSelection();
                RefreshUi(foundNode);
                SelectRecordedSelection();
            }
        }

        void RefreshUi(TreeNode node)
        {
            if (node.Tag is ElementSave)
            {
                ElementSave elementSave = node.Tag as ElementSave;

                RefreshElementTreeNode(node, elementSave);
            }
            else if (node.Tag is InstanceSave)
            {
                InstanceSave instanceSave = node.Tag as InstanceSave;
                node.Text = instanceSave.Name;
            }
            else if(node.Tag is BehaviorSave)
            {
                var behavior = node.Tag as BehaviorSave;
                node.Text = behavior.Name;
            }

            foreach (TreeNode treeNode in node.Nodes)
            {
                RefreshUi(treeNode);
            }
        }

        private void RefreshElementTreeNode(TreeNode node, ElementSave elementSave)
        {
            List<InstanceSave> expandedInstances = new List<InstanceSave>();
            List<InstanceSave> allInstances = elementSave.Instances;

            foreach (InstanceSave instance in allInstances)
            {
                var treeNode = GetTreeNodeFor(instance, node);

                if (treeNode?.Nodes.Count > 0 && treeNode?.IsExpanded == true)
                {
                    expandedInstances.Add(instance);
                }
            }

            node.Text = FileManager.RemovePath(elementSave.Name);
            node.Nodes.Clear();

            foreach (InstanceSave instance in allInstances)
            {
                TreeNode nodeForInstance = GetTreeNodeFor(instance, node);

                if (nodeForInstance == null)
                {
                    nodeForInstance = AddTreeNodeForInstance(instance, node);
                }

                if (expandedInstances.Contains(instance))
                {
                    nodeForInstance.Expand();
                }

                var siblingInstances = instance.GetSiblingsIncludingThis();
                var desiredIndex = siblingInstances.IndexOf(instance);

                var nodeParent = nodeForInstance.Parent;
                if (desiredIndex != nodeParent.Nodes.IndexOf(nodeForInstance))
                {
                    nodeParent.Nodes.Remove(nodeForInstance);
                    nodeParent.Nodes.Insert(desiredIndex, nodeForInstance);
                }
            }
        }

        private TreeNode AddTreeNodeForInstance(InstanceSave instance, TreeNode parentContainerNode, HashSet<InstanceSave> pendingAdditions = null)
        {
            TreeNode treeNode = new TreeNode();

            bool validBaseType = ObjectFinder.Self.GetElementSave(instance.BaseType) != null;

            if (validBaseType)
                treeNode.ImageIndex = InstanceImageIndex;
            else
                treeNode.ImageIndex = ExclamationIndex;

            treeNode.Tag = instance;

            TreeNode parentNode = parentContainerNode;
            InstanceSave parentInstance = FindParentInstance(instance);

            if (parentInstance != null)
            {
                TreeNode parentInstanceNode = GetTreeNodeFor(parentInstance, parentContainerNode);

                // Make sure we are not already trying to add the parent (protects against stack overflow with invalid data)
                if (parentInstanceNode == null && (pendingAdditions == null || !pendingAdditions.Contains(parentInstance)))
                {
                    if (pendingAdditions == null)
                    {
                        pendingAdditions = new HashSet<InstanceSave>();
                    }

                    pendingAdditions.Add(parentInstance);
                    parentInstanceNode = AddTreeNodeForInstance(parentInstance, parentContainerNode, pendingAdditions);
                }

                if (parentInstanceNode != null)
                {
                    parentNode = parentInstanceNode;
                }
            }

            parentNode.Nodes.Add(treeNode);

            return treeNode;
        }

        private InstanceSave FindParentInstance(InstanceSave instance)
        {
            ElementSave element = instance.ParentContainer;

            string name = instance.Name + ".Parent";
            VariableSave variable = element.DefaultState.Variables.FirstOrDefault(v => v.Name == name);

            if (variable != null && variable.SetsValue && variable.Value != null)
            {
                string parentName = (string) variable.Value;
                return element.GetInstance(parentName);
            }

            return null;
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
            else if(selectedObject is BehaviorSave)
            {
                SelectedState.Self.UpdateToSelectedBehavior();
            }

            PluginManager.Self.TreeNodeSelected(selectedTreeNode);
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
            HotkeyManager.Self.HandleKeyDownElementTreeView(e);
        }

        #endregion



        internal void HandleMouseOver(int x, int y)
        {
            var objectOver = this.mTreeView.GetNodeAt(x, y);

            ElementSave element = null;
            InstanceSave instance = null;

            if(objectOver != null && objectOver.Tag != null)
            {
                if(objectOver.Tag is ElementSave)
                {
                    element = objectOver.Tag as ElementSave;
                }
                else if(objectOver.Tag is InstanceSave)
                {
                    instance = objectOver.Tag as InstanceSave;
                }
            }

            GraphicalUiElement whatToHighlight = null;

            if(element != null)
            {
                whatToHighlight = WireframeObjectManager.Self.GetRepresentation(element);
            }
            else if(instance != null)
            {
                whatToHighlight = WireframeObjectManager.Self.GetRepresentation(instance, null);
            }

            SelectionManager.Self.HighlightedIpso = whatToHighlight;
        }
    }


    #region TreeNodeExtensionMethods

    public static class TreeNodeExtensionMethods
    {
        public static bool IsScreenTreeNode(this TreeNode treeNode)
        {
            return treeNode.Tag is ScreenSave;
        }

        public static bool IsComponentTreeNode(this TreeNode treeNode)
        {
            return treeNode.Tag is ComponentSave;
        }

        public static bool IsBehaviorTreeNode(this TreeNode treeNode)
        {
            return treeNode.Tag is BehaviorSave;
        }

        public static bool IsStandardElementTreeNode(this TreeNode treeNode)
        {
            return treeNode.Tag is StandardElementSave;
        }

        public static bool IsInstanceTreeNode(this TreeNode treeNode)
        {
            return treeNode.Tag is InstanceSave;
        }

        public static bool IsStateSaveTreeNode(this TreeNode treeNode)
        {
            return treeNode.Tag is StateSave;
        }

        public static bool IsTopElementContainerTreeNode(this TreeNode treeNode)
        {
            return treeNode.Tag == null;
        }

        public static bool IsTopScreenContainerTreeNode(this TreeNode treeNode)
        {
            return treeNode.Parent == null && treeNode.Text == "Screens";
        }

        public static bool IsTopBehaviorTreeNode(this TreeNode treeNode)
        {
            return treeNode.Parent == null && treeNode.Text == "Behaviors";
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
                treeNode.IsTopScreenContainerTreeNode() ||
                treeNode.IsTopBehaviorTreeNode()
                )
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
                    else if(treeNode.IsTopBehaviorTreeNode())
                    {
                        return projectDirectory + BehaviorReference.Subfolder + "\\";
                    }
                    throw new InvalidOperationException();
                }
            }
            else if (treeNode.IsStandardElementTreeNode() ||
                treeNode.IsComponentTreeNode() ||
                treeNode.IsScreenTreeNode())
            {
                ElementSave element = treeNode.Tag as ElementSave;
                return treeNode.Parent.GetFullFilePath() + treeNode.Text + "." + element.FileExtension;
            }
            else if(treeNode.IsBehaviorTreeNode())
            {
                var behavior = treeNode.Tag as BehaviorSave;
                return treeNode.Parent.GetFullFilePath() + treeNode.Text + "." + BehaviorReference.Extension;
            }
            else
            {
                var toReturn = treeNode.Parent.GetFullFilePath() + treeNode.Text + "\\";
                return toReturn;
            }
        }

        public static bool IsScreensFolderTreeNode(this TreeNode treeNode)
        {
            return treeNode.Tag == null &&
                treeNode.Parent != null &&
                (treeNode.Parent.IsScreensFolderTreeNode() || treeNode.Parent.IsTopScreenContainerTreeNode());
        }



        public static bool IsPartOfScreensFolderStructure(this TreeNode treeNode)
        {
            if (treeNode == ElementTreeViewManager.Self.RootScreensTreeNode)
                return true;

            if (treeNode.Parent == null)
                return false;

            return treeNode.Parent.IsPartOfScreensFolderStructure();
        }

        public static bool IsPartOfComponentsFolderStructure(this TreeNode treeNode)
        {
            if (treeNode == ElementTreeViewManager.Self.RootComponentsTreeNode)
                return true;

            if (treeNode.Parent == null)
                return false;

            return treeNode.Parent.IsPartOfComponentsFolderStructure();
        }

        public static bool IsPartOfStandardElementsFolderStructure(this TreeNode treeNode)
        {
            if (treeNode == ElementTreeViewManager.Self.RootStandardElementsTreeNode)
                return true;

            if (treeNode.Parent == null)
                return false;

            return treeNode.Parent.IsPartOfStandardElementsFolderStructure();
        }


        public static bool IsComponentsFolderTreeNode(this TreeNode treeNode)
        {
            return treeNode.Tag == null &&
                treeNode.Parent != null &&
                (treeNode.Parent.IsComponentsFolderTreeNode() || treeNode.Parent.IsTopComponentContainerTreeNode());
        }

        public static void SortByName(this TreeNodeCollection treeNodeCollection, bool recursive = false)
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

            if(recursive)
            {
                foreach(var node in treeNodeCollection)
                {
                    var asTreeNode = node as TreeNode;
                    if(asTreeNode != null)
                    {
                        var sortInner = asTreeNode.IsScreenTreeNode() == false &&
                            asTreeNode.IsComponentTreeNode() == false &&
                            asTreeNode.IsStandardElementTreeNode() == false &&
                            asTreeNode.IsBehaviorTreeNode() == false;

                        if(sortInner)
                        {
                            asTreeNode.Nodes.SortByName(recursive);
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
