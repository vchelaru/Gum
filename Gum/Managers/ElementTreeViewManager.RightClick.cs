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

namespace Gum.Managers
{
    public partial class ElementTreeViewManager
    {

        #region Fields

        ContextMenuStrip mMenuStrip;


        ToolStripMenuItem mAddScreen;
        ToolStripMenuItem mImportScreen;

        ToolStripMenuItem mAddComponent;
        ToolStripMenuItem mImportComponent;

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
                string fullFile = treeNode.GetFullFilePath();
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
                string fullFile = treeNode.GetFullFilePath();

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
                // InstanceSave selected
                if (SelectedState.Self.SelectedInstance != null)
                {
                    mMenuStrip.Items.Add(mGoToDefinition);
                }
                // ScreenSave or ComponentSave
                else if (SelectedState.Self.SelectedScreen != null || SelectedState.Self.SelectedComponent != null)
                {
                    mMenuStrip.Items.Add("View in explorer", null, HandleViewInExplorer);

                    mAddInstance.Text = "Add object to " + SelectedState.Self.SelectedElement.Name;
                    mMenuStrip.Items.Add(mAddInstance);
                    mMenuStrip.Items.Add(mSaveObject);

                    mDeleteObject.Text = "Delete " + SelectedState.Self.SelectedElement.ToString();
                    mMenuStrip.Items.Add(mDeleteObject);

                    if(SelectedState.Self.SelectedScreen != null)
                    {
                        duplicateElement.Text = $"Duplicate {SelectedState.Self.SelectedScreen.Name}";
                    }
                    else
                    {
                        duplicateElement.Text = $"Duplicate {SelectedState.Self.SelectedComponent.Name}";
                    }
                    mMenuStrip.Items.Add(duplicateElement);
                }
                else if(SelectedState.Self.SelectedBehavior != null)
                {
                    mDeleteObject.Text = "Delete " + SelectedState.Self.SelectedBehavior.ToString();
                    mMenuStrip.Items.Add(mDeleteObject);
                }
                else if (SelectedState.Self.SelectedStandardElement != null)
                {
                    mMenuStrip.Items.Add(mSaveObject);
                }
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
                string fullFilePath = FileManager.GetDirectory( SelectedNode.GetFullFilePath()) + tiw.Result + "\\";

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

                    string oldFullPath = SelectedNode.GetFullFilePath();

                    string oldPathRelativeToElementsRoot = FileManager.MakeRelative(SelectedNode.GetFullFilePath(), rootForElement, preserveCase:true);
                    SelectedNode.Text = tiw.Result;
                    string newPathRelativeToElementsRoot = FileManager.MakeRelative(SelectedNode.GetFullFilePath(), rootForElement, preserveCase:true);

                    if (SelectedNode.IsScreensFolderTreeNode())
                    {
                        foreach(var screen in ProjectState.Self.GumProjectSave.Screens)
                        {
                            if(screen.Name.StartsWith(oldPathRelativeToElementsRoot))
                            {
                                string oldVaue = screen.Name;
                                string newName = newPathRelativeToElementsRoot + screen.Name.Substring(oldPathRelativeToElementsRoot.Length);

                                screen.Name = newName;
                                RenameManager.Self.HandleRename(screen, (InstanceSave)null, oldVaue, NameChangeAction.Move, askAboutRename: false);
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

                                RenameManager.Self.HandleRename(component, (InstanceSave)null, oldVaue, NameChangeAction.Move, askAboutRename:false);
                            }
                        }
                    }

                    bool isNowEmpty = Directory.GetFiles(oldFullPath).Length == 0;
                    if(isNowEmpty)
                    {
                        Directory.Delete(oldFullPath);
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

                        string path = nodeToAddTo.GetFullFilePath();

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
            bool succeeded = true;
            if (ObjectFinder.Self.GumProjectSave == null || string.IsNullOrEmpty(ProjectManager.Self.GumProjectSave.FullFileName))
            {
                MessageBox.Show("You must first save the project before adding a new screen");
                succeeded = false;
            }
            else
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Gum Screen (*.gusx)|*.gusx";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string fileName = openFileDialog.FileName;

                    string desiredDirectory =
                        FileManager.GetDirectory(
                        ProjectManager.Self.GumProjectSave.FullFileName) + "Screens/";

                    if (FileManager.IsRelativeTo(fileName, desiredDirectory) == false)
                    {
                        MessageBox.Show("The file must be in the Gum project's Screens folder.  " +
                            "This file will be copied, and the copy will be referenced.");

                        try
                        {
                            string destination = desiredDirectory + FileManager.RemovePath(fileName);
                            System.IO.File.Copy(fileName,
                                destination);

                            fileName = destination;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error copying the file: " + ex.ToString());
                            succeeded = false;
                        }
                    }

                    if (succeeded)
                    {
                        string strippedName = FileManager.RemovePath(FileManager.RemoveExtension(fileName));

                        ProjectManager.Self.GumProjectSave.ScreenReferences.Add(
                            new ElementReference { Name = strippedName, ElementType = ElementType.Screen });

                        ProjectManager.Self.GumProjectSave.ScreenReferences.Sort(
                            (first, second) => first.Name.CompareTo(second.Name));

                        var screenSave = FileManager.XmlDeserialize<ScreenSave>(fileName);

                        ProjectManager.Self.GumProjectSave.Screens.Add(screenSave);

                        ProjectManager.Self.GumProjectSave.Screens.Sort(
                            (first, second) => first.Name.CompareTo(second.Name));

                        screenSave.Initialize(null);

                        GumCommands.Self.GuiCommands.RefreshElementTreeView();

                        SelectedState.Self.SelectedScreen = screenSave;

                        GumCommands.Self.FileCommands.TryAutoSaveProject();
                        GumCommands.Self.FileCommands.TryAutoSaveElement(screenSave);
                    }

                }
                else
                {
                    succeeded = false;
                }
            }
        }

        public void ImportComponentsClick(object sender, EventArgs e)
        {
            if (ObjectFinder.Self.GumProjectSave == null || string.IsNullOrEmpty(ProjectManager.Self.GumProjectSave.FullFileName))
            {
                MessageBox.Show("You must first save the project before adding a new component");
                return;
            }

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Gum Component (*.gucx)|*.gucx";

            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;

            ComponentSave lastImportedComponent = null;

            for (int i = 0; i < openFileDialog.FileNames.Length; ++i)
            {
                string fileName = openFileDialog.FileNames[i];
                string desiredDirectory = FileManager.GetDirectory(
                    ProjectManager.Self.GumProjectSave.FullFileName) + "Components/";

                if (!FileManager.IsRelativeTo(fileName, desiredDirectory))
                {
                    string fileNameWithoutPath = FileManager.RemovePath(fileName);

                    MessageBox.Show("The file " + fileNameWithoutPath + " must be in the Gum project's Components folder. " +
                        "This file will be copied, and the copy will be referenced.");

                    try
                    {
                        string destination = desiredDirectory + fileNameWithoutPath;
                        System.IO.File.Copy(fileName, destination);

                        fileName = destination;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error copying the file: " + ex.ToString());
                        break;
                    }
                }

                string strippedName = FileManager.RemovePath(FileManager.RemoveExtension(fileName));

                var componentSave = FileManager.XmlDeserialize<ComponentSave>(fileName);

                var componentReferences = ProjectManager.Self.GumProjectSave.ComponentReferences;
                componentReferences.Add(new ElementReference { Name = componentSave.Name, ElementType = ElementType.Component });
                componentReferences.Sort((first, second) => first.Name.CompareTo(second.Name));

                var components = ProjectManager.Self.GumProjectSave.Components;
                components.Add(componentSave);
                components.Sort((first, second) => first.Name.CompareTo(second.Name));

                componentSave.InitializeDefaultAndComponentVariables();

                GumCommands.Self.FileCommands.TryAutoSaveElement(componentSave);

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

        public InstanceSave AddInstance(string name, string type, ElementSave elementToAddTo)
        {
            InstanceSave instanceSave = ElementCommands.Self.AddInstance(elementToAddTo, name);
            instanceSave.BaseType = type;

            TreeNode treeNodeForElement = GetTreeNodeFor(elementToAddTo);
            RefreshUi(treeNodeForElement);

            Wireframe.WireframeObjectManager.Self.RefreshAll(true);
            //SelectedState.Self.SelectedInstance = instanceSave;

            // We need to call InstanceAdd before we select the new object - the Undo manager expects it
            PluginManager.Self.InstanceAdd(elementToAddTo, instanceSave);

            Select(instanceSave, elementToAddTo);

            if (ProjectManager.Self.GeneralSettingsFile.AutoSave)
            {
                ProjectManager.Self.SaveElement(elementToAddTo);
            }

            return instanceSave;
        }

    }
}
