using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonFormsAndControls;
using Gum.ToolCommands;
using Gum.DataTypes;
using Gum.ToolStates;
using System.Windows.Forms;
using Gum.Gui.Forms;
using Gum.Plugins;
using System.Diagnostics;
using System.IO;
using ToolsUtilities;
using Gum.Wireframe;
using Gum.Logic;
using Gum.DataTypes.Behaviors;

namespace Gum.Managers
{
    public partial class ElementTreeViewManager
    {

        #region Fields



        ToolStripMenuItem mAddScreen;
        ToolStripMenuItem mImportScreen;

        ToolStripMenuItem mAddComponent;
        ToolStripMenuItem mImportComponent;
        ToolStripMenuItem mAddLinkedComponent;

        ToolStripMenuItem mAddInstance;
        ToolStripMenuItem mSaveObject;
        ToolStripMenuItem mGoToDefinition;
        ToolStripMenuItem mDeleteObject;

        ToolStripMenuItem mAddFolder;

        ToolStripMenuItem duplicateElement;

        

        #endregion

        #region Initialize and event handlers

        private void InitializeMenuItems()
        {
            mAddScreen = new ToolStripMenuItem();
            mAddScreen.Text = "Add Screen";
            mAddScreen.Click += AddScreenClick;

            mImportScreen = new ToolStripMenuItem();
            mImportScreen.Text = "Import Screen";
            mImportScreen.Click += ImportScreenClick;

            mAddComponent = new ToolStripMenuItem();
            mAddComponent.Text = "Add Component";
            mAddComponent.Click += AddComponentClick;

            mImportComponent = new ToolStripMenuItem();
            mImportComponent.Text = "Import Components";
            mImportComponent.Click += ImportComponentsClick;

            mAddLinkedComponent = new ToolStripMenuItem();
            mAddLinkedComponent.Text = "Add Linked Component";
            mAddLinkedComponent.Click += HandleAddLinkedComponentClick;

            mAddInstance = new ToolStripMenuItem();
            mAddInstance.Text = "Add Instance";
            mAddInstance.Click += AddInstanceClick;

            mSaveObject = new ToolStripMenuItem();
            mSaveObject.Text = "Force Save Object";
            mSaveObject.Click += ForceSaveObjectClick;

            mGoToDefinition = new ToolStripMenuItem();
            mGoToDefinition.Text = "Go to definition";
            mGoToDefinition.Click += OnGoToDefinitionClick;

            mAddFolder = new ToolStripMenuItem();
            mAddFolder.Text = "Add Folder";
            mAddFolder.Click += AddFolderClick;

            mDeleteObject = new ToolStripMenuItem();
            mDeleteObject.Text = "Delete";
            mDeleteObject.Click += HandleDeleteObjectClick;

            duplicateElement = new ToolStripMenuItem();
            duplicateElement.Text = "To Be Replaced...";
            duplicateElement.Click += HandleDuplicateElementClick;
        }



        void HandleDeleteObjectClick(object sender, EventArgs e)
        {
            DeleteLogic.Self.HandleDelete();
        }

        void HandleDuplicateElementClick(object sender, EventArgs e)
        {
            if(SelectedState.Self.SelectedScreen != null ||
                SelectedState.Self.SelectedComponent != null)
            {
                GumCommands.Self.Edit.DuplicateSelectedElement();
            }
        }

        void OnGoToDefinitionClick(object sender, EventArgs e)
        {
            if (SelectedState.Self.SelectedInstance != null)
            {
                ElementSave element = ObjectFinder.Self.GetElementSave(SelectedState.Self.SelectedInstance.BaseType);

                SelectedState.Self.SelectedElement = element;
            }
        }

        void ForceSaveObjectClick(object sender, EventArgs e)
        {
            ProjectManager.Self.SaveElement(SelectedState.Self.SelectedElement);
        }

        void AddFolderClick(object sender, EventArgs e)
        {
            TextInputWindow tiw = new TextInputWindow();
            tiw.Message = "Enter new folder name";

            DialogResult result = tiw.ShowDialog();

            if (result == DialogResult.OK)
            {
                string folderName = tiw.Result;

                string whyNotValid;

                if (!NameVerifier.Self.IsFolderNameValid(folderName, out whyNotValid))
                {
                    MessageBox.Show(whyNotValid);
                }
                else
                {
                    TreeNode parentTreeNode = SelectedNode;

                    string folder = parentTreeNode.GetFullFilePath() + folderName + "\\";

                    // If the path is relative
                    // that means that the root
                    // hasn't been set yet.
                    if (!FileManager.IsRelative(folder))
                    {
                        System.IO.Directory.CreateDirectory(folder);
                    }

                    GumCommands.Self.GuiCommands.RefreshElementTreeView();
                }
            }
        }


        void HandleViewInExplorer(object sender, EventArgs e)
        {
            TreeNode treeNode = SelectedState.Self.SelectedTreeNode;

            if (treeNode != null)
            {
                string fullFile;
                if (treeNode.Tag is ElementSave elementSave)
                {
                    fullFile = elementSave.GetFullPathXmlFile().FullPath;
                }
                else if(treeNode.Tag is BehaviorSave behaviorSave)
                {
                    fullFile = behaviorSave.GetFullPathXmlFile().FullPath;
                }
                else
                {
                    fullFile = treeNode.GetFullFilePath().FullPath;
                }
                fullFile = fullFile.Replace("/", "\\");

                if (fullFile.EndsWith("\\") || fullFile.EndsWith("/"))
                {
                    try
                    {
                        var doesExist = System.IO.File.Exists(fullFile);

                        if(!doesExist)
                        {
                            // file doesn't exist, but if it's a standard folder we can make one:
                            if(treeNode.IsTopComponentContainerTreeNode() ||
                                treeNode.IsTopScreenContainerTreeNode())
                            {
                                System.IO.Directory.CreateDirectory(fullFile);
                            }
                        }
                        Process.Start(fullFile);
                    }
                    catch(Exception exc)
                    {
                        MessageBox.Show("Could not open location:\n\n" + exc.ToString());
                    }
                }
                else
                {
                    Process.Start("explorer.exe", "/select," + fullFile);
                }
            }
        }

        void HandleDeleteFolder(object sender, EventArgs e)
        {
            TreeNode treeNode = SelectedState.Self.SelectedTreeNode;

            if (treeNode != null)
            {
                string fullFile = treeNode.GetFullFilePath().FullPath;

                // Initially we won't allow deleting of the entire
                // folder because the user may have to make decisions
                // about what to do with Screens or Components contained
                // in the folder.

                if (!Directory.Exists(fullFile))
                {
                    // It doesn't exist, so let's just refresh the UI for this and it will go away
                    GumCommands.Self.GuiCommands.RefreshElementTreeView();
                }
                else
                {
                    string[] files = Directory.GetFiles(fullFile);
                    string[] directories = Directory.GetDirectories(fullFile);

                    if (files != null && files.Length > 0)
                    {
                        MessageBox.Show("Cannot delete this folder, it currently contains " + files.Length + " files.");
                    }
                    else if (directories != null && directories.Length > 0)
                    {
                        MessageBox.Show("Cannot delete this folder, it currently contains " + directories.Length + " directories.");
                    }

                    else
                    {
                        DialogResult result = MessageBox.Show("Delete folder " + treeNode.Text + "?", "Delete", MessageBoxButtons.YesNo);

                        if (result == DialogResult.Yes)
                        {
                            try
                            {
                                Directory.Delete(fullFile);
                                GumCommands.Self.GuiCommands.RefreshElementTreeView();
                            }
                            catch
                            {
                                MessageBox.Show("Could not delete folder");
                            }
                        }
                    }
                }
            }
        }

        #endregion


        public void PopulateMenuStrip()
        {
            mMenuStrip.Items.Clear();

            if (SelectedNode != null)
            {
                #region InstanceSave 
                // InstanceSave selected
                if (SelectedState.Self.SelectedInstance != null)
                {
                    mMenuStrip.Items.Add(mGoToDefinition);

                    var container = SelectedState.Self.SelectedElement;
                    if(!string.IsNullOrEmpty(container.BaseType))
                    {
                        var containerBase = ObjectFinder.Self.GetElementSave(container.BaseType);

                        if(containerBase is ScreenSave || containerBase is ComponentSave)
                        {
                            mMenuStrip.Items.Add($"Add {SelectedState.Self.SelectedInstance.Name} to base {containerBase}", 
                                null, 
                                (not, used) => HandleMoveToBase(SelectedState.Self.SelectedInstances, SelectedState.Self.SelectedElement, containerBase));
                        }
                    }

                    AddPasteMenuItems();
                }

                #endregion

                #region Screen or Component
                // ScreenSave or ComponentSave
                else if (SelectedState.Self.SelectedScreen != null || SelectedState.Self.SelectedComponent != null)
                {
                    mMenuStrip.Items.Add("View in explorer", null, HandleViewInExplorer);

                    mMenuStrip.Items.Add("View References", null, HandleViewReferences);

                    mMenuStrip.Items.Add("-");

                    mAddInstance.Text = "Add object to " + SelectedState.Self.SelectedElement.Name;
                    mMenuStrip.Items.Add(mAddInstance);
                    mMenuStrip.Items.Add(mSaveObject);
                    if(SelectedState.Self.SelectedScreen != null)
                    {
                        duplicateElement.Text = $"Duplicate {SelectedState.Self.SelectedScreen.Name}";
                    }
                    else
                    {
                        duplicateElement.Text = $"Duplicate {SelectedState.Self.SelectedComponent.Name}";
                    }
                    mMenuStrip.Items.Add(duplicateElement);

                    AddPasteMenuItems();

                    mMenuStrip.Items.Add("-");


                    mDeleteObject.Text = "Delete " + SelectedState.Self.SelectedElement.ToString();
                    mMenuStrip.Items.Add(mDeleteObject);

                }
                #endregion

                #region Behavior

                else if (SelectedState.Self.SelectedBehavior != null)
                {
                    mMenuStrip.Items.Add("View in explorer", null, HandleViewInExplorer);
                    mMenuStrip.Items.Add("-");
                    mDeleteObject.Text = "Delete " + SelectedState.Self.SelectedBehavior.ToString();
                    mMenuStrip.Items.Add(mDeleteObject);
                }

                #endregion

                #region Standard Element

                else if (SelectedState.Self.SelectedStandardElement != null)
                {
                    mMenuStrip.Items.Add("View in explorer", null, HandleViewInExplorer);
                    
                    mMenuStrip.Items.Add("-");

                    mMenuStrip.Items.Add(mSaveObject);
                }

                #endregion

                #region Screens Folder (top or contained)

                else if (SelectedNode.IsTopScreenContainerTreeNode() || SelectedNode.IsScreensFolderTreeNode())
                {
                    mMenuStrip.Items.Add(mAddScreen);
                    mMenuStrip.Items.Add(mImportScreen);
                    mMenuStrip.Items.Add(mAddFolder);
                    mMenuStrip.Items.Add("View in explorer", null, HandleViewInExplorer);

                    if (SelectedNode.IsScreensFolderTreeNode())
                    {
                        mMenuStrip.Items.Add("Delete Folder", null, HandleDeleteFolder);
                        mMenuStrip.Items.Add("Rename Folder", null, HandleRenameFolder);
                    }
                }

                #endregion

                else if (SelectedNode.IsTopComponentContainerTreeNode() || SelectedNode.IsComponentsFolderTreeNode())
                {
                    mMenuStrip.Items.Add(mAddComponent);
                    mMenuStrip.Items.Add(mImportComponent);
                    mMenuStrip.Items.Add(mAddFolder);
                    mMenuStrip.Items.Add("View in explorer", null, HandleViewInExplorer);

                    if (SelectedNode.IsComponentsFolderTreeNode())
                    {
                        mMenuStrip.Items.Add("Delete Folder", null, HandleDeleteFolder);
                        mMenuStrip.Items.Add("Rename Folder", null, HandleRenameFolder);

                    }
                }
                else if(SelectedNode.IsTopBehaviorTreeNode())
                {
                    mMenuStrip.Items.Add("Add Behavior", null, HandleAddBehavior);
                }
            }
        }

        private void AddPasteMenuItems()
        {
            if (CopyPasteLogic.CopiedData.CopiedInstancesRecursive.Count > 0)
            {
                mMenuStrip.Items.Add("Paste", null, HandlePaste);
            }
            if (CopyPasteLogic.CopiedData.CopiedInstancesSelected.Count > 0)
            {
                string text;
                if (CopyPasteLogic.CopiedData.CopiedInstancesSelected.Count == 0)
                    text = "Paste Top Level Instance";
                else
                    text = "Paste Top Level Instances";

                mMenuStrip.Items.Add(text, null, HandlePasteTopLevel);
            }
        }

        private void HandleMoveToBase(IEnumerable<InstanceSave> instances, ElementSave derivedElement, ElementSave baseElement)
        {
            foreach(var instance in instances)
            {
                instance.DefinedByBase = true;
            }


            CopyPasteLogic.PasteInstanceSaves(
                instances.ToList(),
                derivedElement.DefaultState.Clone(),
                baseElement, 
                null);
        }

        private void HandlePaste(object sender, EventArgs e)
        {
            CopyPasteLogic.OnPaste(CopyType.InstanceOrElement, TopOrRecursive.Recursive);
        }

        private void HandlePasteTopLevel(object sender, EventArgs e)
        {
            CopyPasteLogic.OnPaste(CopyType.InstanceOrElement, TopOrRecursive.Top);
        }

        private void HandleViewReferences(object sender, EventArgs e)
        {
            GumCommands.Self.Edit.DisplayReferencesTo(SelectedState.Self.SelectedElement);
        }

        private void HandleRenameFolder(object sender, EventArgs e)
        {
            var tiw = new TextInputWindow();
            tiw.Message = "Enter new folder name";
            tiw.Result = SelectedNode.Text;
            var dialogResult = tiw.ShowDialog();

            if(dialogResult == DialogResult.OK && tiw.Result != SelectedNode.Text)
            {
                bool isValid = true;
                string whyNotValid;
                if(!NameVerifier.Self.IsFolderNameValid(tiw.Result, out whyNotValid))
                {
                    isValid = false;
                }


                // see if it already exists:
                string fullFilePath = FileManager.GetDirectory( SelectedNode.GetFullFilePath().FullPath) + tiw.Result + "\\";

                if(System.IO.Directory.Exists(fullFilePath))
                {
                    whyNotValid = $"Folder {tiw.Result} already exists.";
                    isValid = false;
                }

                if(!isValid)
                {
                    MessageBox.Show(whyNotValid);
                }
                else
                {
                    string rootForElement;
                    if(SelectedNode.IsScreensFolderTreeNode())
                    {
                        rootForElement = FileLocations.Self.ScreensFolder;
                    }
                    else if(SelectedNode.IsComponentsFolderTreeNode())
                    {
                        rootForElement = FileLocations.Self.ComponentsFolder;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }

                    var oldFullPath = SelectedNode.GetFullFilePath();

                    string oldPathRelativeToElementsRoot = FileManager.MakeRelative(SelectedNode.GetFullFilePath().FullPath, rootForElement, preserveCase:true);
                    SelectedNode.Text = tiw.Result;
                    string newPathRelativeToElementsRoot = FileManager.MakeRelative(SelectedNode.GetFullFilePath().FullPath, rootForElement, preserveCase:true);

                    if (SelectedNode.IsScreensFolderTreeNode())
                    {
                        foreach(var screen in ProjectState.Self.GumProjectSave.Screens)
                        {
                            if(screen.Name.StartsWith(oldPathRelativeToElementsRoot))
                            {
                                string oldVaue = screen.Name;
                                string newName = newPathRelativeToElementsRoot + screen.Name.Substring(oldPathRelativeToElementsRoot.Length);

                                screen.Name = newName;
                                RenameLogic.HandleRename(screen, (InstanceSave)null, oldVaue, NameChangeAction.Move, askAboutRename: false);
                            }
                        }
                    }
                    else if (SelectedNode.IsComponentsFolderTreeNode())
                    {
                        foreach (var component in ProjectState.Self.GumProjectSave.Components)
                        {
                            if(component.Name.ToLowerInvariant().StartsWith(oldPathRelativeToElementsRoot.ToLowerInvariant()))
                            {
                                string oldVaue = component.Name;
                                string newName = newPathRelativeToElementsRoot + component.Name.Substring(oldPathRelativeToElementsRoot.Length);
                                component.Name = newName;

                                RenameLogic.HandleRename(component, (InstanceSave)null, oldVaue, NameChangeAction.Move, askAboutRename:false);
                            }
                        }
                    }

                    bool isNowEmpty = Directory.GetFiles(oldFullPath.FullPath).Length == 0;
                    if(isNowEmpty)
                    {
                        Directory.Delete(oldFullPath.FullPath);
                        GumCommands.Self.GuiCommands.RefreshElementTreeView();

                    }


                }
            }
        }

        private void HandleAddBehavior(object sender, EventArgs e)
        {
            GumCommands.Self.Edit.AddBehavior();
        }

        public void AddScreenClick(object sender, EventArgs e)
        {
            if (ObjectFinder.Self.GumProjectSave == null || string.IsNullOrEmpty(ProjectManager.Self.GumProjectSave.FullFileName))
            {
                MessageBox.Show("You must first save a project before adding Screens");
            }
            else
            {
                TextInputWindow tiw = new TextInputWindow();
                tiw.Message = "Enter new Screen name:";

                if (tiw.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string name = tiw.Result;

                    string whyNotValid;

                    if (!NameVerifier.Self.IsScreenNameValid(name, null, out whyNotValid))
                    {
                        MessageBox.Show(whyNotValid);
                    }
                    else
                    {
                        TreeNode nodeToAddTo = ElementTreeViewManager.Self.SelectedNode;

                        while (nodeToAddTo != null && nodeToAddTo.Tag is ScreenSave && nodeToAddTo.Parent != null)
                        {
                            nodeToAddTo = nodeToAddTo.Parent;
                        }

                        if (nodeToAddTo == null || !nodeToAddTo.IsPartOfScreensFolderStructure())
                        {
                            nodeToAddTo = RootScreensTreeNode;
                        }

                        string path = nodeToAddTo.GetFullFilePath().FullPath;

                        string relativeToScreens = FileManager.MakeRelative(path,
                            FileLocations.Self.ScreensFolder);

                        ScreenSave screenSave = ProjectCommands.Self.AddScreen(relativeToScreens + name);


                        GumCommands.Self.GuiCommands.RefreshElementTreeView();

                        SelectedState.Self.SelectedScreen = screenSave;

                        GumCommands.Self.FileCommands.TryAutoSaveElement(screenSave);
                        GumCommands.Self.FileCommands.TryAutoSaveProject();
                    }
                }
            }
        }

        public void AddComponentClick(object sender, EventArgs e)
        {
            ProjectCommands.Self.AskToAddComponent();
        }

        public void ImportScreenClick(object sender, EventArgs e)
        {
            Plugins.ImportPlugin.Manager.ImportLogic.ShowImportScreenUi();
        }

        public void ImportComponentsClick(object sender, EventArgs e)
        {
            Plugins.ImportPlugin.Manager.ImportLogic.ShowImportComponentUi();
        }

        private void HandleAddLinkedComponentClick(object sender, EventArgs e)
        {
            ////////////////Early Out/////////////////////////
            if (ObjectFinder.Self.GumProjectSave == null || string.IsNullOrEmpty(ProjectManager.Self.GumProjectSave.FullFileName))
            {
                MessageBox.Show("You must first save the project before adding a new component");
                return;
            }
            //////////////End Early Out////////////////////////

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Gum Component (*.gucx)|*.gucx";

            /////////////////Another Early Out////////////////////////
            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            ///////////End Another Early Out//////////////////////////

            ComponentSave lastImportedComponent = null;

            for (int i = 0; i < openFileDialog.FileNames.Length; ++i)
            {
                FilePath componentFilePath = openFileDialog.FileNames[i];

                var gumProject = ObjectFinder.Self.GumProjectSave;
                var gumProjectDirectory = new FilePath(gumProject.FullFileName).GetDirectoryContainingThis();

                var relative = FileManager.MakeRelative(componentFilePath.FullPath, gumProjectDirectory.FullPath);

                var componentSave = FileManager.XmlDeserialize<ComponentSave>(componentFilePath.FullPath);

                var reference = new ElementReference();
                reference.Name = componentSave.Name;
                reference.Link = relative;
                reference.LinkType = LinkType.CopyLocally; // This is what FRB needs, so we'll make it the default. Eventually...we can change this? Make it optional?
                reference.ElementType = ElementType.Component;
                gumProject.ComponentReferences.Add(reference);
                gumProject.ComponentReferences.Sort();


                var components = ProjectManager.Self.GumProjectSave.Components;
                components.Add(componentSave);
                components.Sort((first, second) => first.Name.CompareTo(second.Name));

                componentSave.InitializeDefaultAndComponentVariables();

                lastImportedComponent = componentSave;
            }

            if (lastImportedComponent != null)
            {
                GumCommands.Self.GuiCommands.RefreshElementTreeView();
                SelectedState.Self.SelectedComponent = lastImportedComponent;
                GumCommands.Self.FileCommands.TryAutoSaveProject();
            }
        }


        public void AddInstanceClick(object sender, EventArgs e)
        {
            TextInputWindow tiw = new TextInputWindow();
            tiw.Message = "Enter new object name:";

            if (tiw.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string name = tiw.Result;
                string whyNotValid;

                if (!NameVerifier.Self.IsInstanceNameValid(name, null, SelectedState.Self.SelectedElement, out whyNotValid))
                {
                    MessageBox.Show(whyNotValid);
                }
                else
                {
                    AddInstance(name, StandardElementsManager.Self.DefaultType, SelectedState.Self.SelectedElement);
                }
            }
        }

        public InstanceSave AddInstance(string name, string type, ElementSave elementToAddTo, string parentName = null)
        {
            InstanceSave instanceSave = ElementCommands.Self.AddInstance(elementToAddTo, name);
            instanceSave.BaseType = type;

            TreeNode treeNodeForElement = GetTreeNodeFor(elementToAddTo);
            RefreshUi(treeNodeForElement);

            Wireframe.WireframeObjectManager.Self.RefreshAll(true);
            //SelectedState.Self.SelectedInstance = instanceSave;

            // Set the parent before adding the instance in case plugins want to reject the creation of the object...
            if(!string.IsNullOrEmpty(parentName))
            {
                elementToAddTo.DefaultState.SetValue($"{instanceSave.Name}.Parent", parentName, "string");
            }

            // We need to call InstanceAdd before we select the new object - the Undo manager expects it
            PluginManager.Self.InstanceAdd(elementToAddTo, instanceSave);

            // a plugin may have removed this instance. If so, we need to refresh the tree node again:
            if(elementToAddTo.Instances.Contains(instanceSave) == false)
            {
                // it was removed, so refresh...
                RefreshUi(treeNodeForElement);
                Wireframe.WireframeObjectManager.Self.RefreshAll(true);
            }
            else
            {
                Select(instanceSave, elementToAddTo);
            }

            if (ProjectManager.Self.GeneralSettingsFile.AutoSave)
            {
                ProjectManager.Self.SaveElement(elementToAddTo);
            }

            // August 2, 2022 - this is currently returned even if a plugin
            // removes the new instance. Should it be? Will it causes NullReferenceExceptions
            // on systems which always expect this to be non-null? Unsure....
            return instanceSave;
        }

    }
}
