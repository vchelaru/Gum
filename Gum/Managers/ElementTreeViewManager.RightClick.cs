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
            mImportComponent.Text = "Import Component";
            mImportComponent.Click += ImportComponentClick;

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
        }



        void HandleDeleteObjectClick(object sender, EventArgs e)
        {
            EditingManager.Self.HandleDelete();
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

                if (fullFile.EndsWith("\\") || fullFile.EndsWith("/"))
                {
                    try
                    {
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
                else if (SelectedState.Self.SelectedScreen != null ||
                    SelectedState.Self.SelectedComponent != null)
                {
                    mMenuStrip.Items.Add("View in explorer", null, HandleViewInExplorer);


                    mAddInstance.Text = "Add object to " + SelectedState.Self.SelectedElement.Name;
                    mMenuStrip.Items.Add(mAddInstance);
                    mMenuStrip.Items.Add(mSaveObject);

                    mDeleteObject.Text = "Delete " + SelectedState.Self.SelectedElement.ToString();
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
                    }

                }
                else if (SelectedNode.IsTopComponentContainerTreeNode() || SelectedNode.IsComponentsFolderTreeNode())
                {
                    mMenuStrip.Items.Add(mAddComponent);
                    mMenuStrip.Items.Add(mImportComponent);
                    mMenuStrip.Items.Add(mAddFolder);
                    mMenuStrip.Items.Add("View in explorer", null, HandleViewInExplorer);

                    if (SelectedNode.IsScreensFolderTreeNode())
                    {
                        mMenuStrip.Items.Add("Delete Folder", null, HandleDeleteFolder);
                    }
                }
            }

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

                        if (nodeToAddTo == null)
                        {
                            nodeToAddTo = RootScreensTreeNode;
                        }

                        if (nodeToAddTo.IsPartOfScreensFolderStructure() == false)
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
            if (ObjectFinder.Self.GumProjectSave == null || string.IsNullOrEmpty(ProjectManager.Self.GumProjectSave.FullFileName))
            {
                MessageBox.Show("You must first save the project before adding a new component");
            }
            else
            {
                TextInputWindow tiw = new TextInputWindow();
                tiw.Message = "Enter new Component name:";

                if (tiw.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string name = tiw.Result;

                    string whyNotValid;

                    if (!NameVerifier.Self.IsComponentNameValid(name, null, out whyNotValid))
                    {
                        MessageBox.Show(whyNotValid);
                    }
                    else
                    {
                        TreeNode nodeToAddTo = ElementTreeViewManager.Self.SelectedNode;

                        while (nodeToAddTo != null && nodeToAddTo.Tag is ComponentSave && nodeToAddTo.Parent != null)
                        {
                            nodeToAddTo = nodeToAddTo.Parent;
                        }

                        if (nodeToAddTo == null)
                        {
                            nodeToAddTo = RootComponentsTreeNode;
                        }

                        if (nodeToAddTo.IsPartOfComponentsFolderStructure() == false)
                        {
                            nodeToAddTo = RootComponentsTreeNode;
                        }

                        string path = nodeToAddTo.GetFullFilePath();

                        string relativeToComponents = FileManager.MakeRelative(path,
                            FileLocations.Self.ComponentsFolder);



                        ComponentSave componentSave = ProjectCommands.Self.AddComponent(relativeToComponents + name);


                        GumCommands.Self.GuiCommands.RefreshElementTreeView();

                        SelectedState.Self.SelectedComponent = componentSave;

                        GumCommands.Self.FileCommands.TryAutoSaveProject();
                        GumCommands.Self.FileCommands.TryAutoSaveElement(componentSave);
                    }
                }
            }
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

        public void ImportComponentClick(object sender, EventArgs e)
        {
            bool succeeded = true;
            if (ObjectFinder.Self.GumProjectSave == null || string.IsNullOrEmpty(ProjectManager.Self.GumProjectSave.FullFileName))
            {
                MessageBox.Show("You must first save the project before adding a new component");
                succeeded = false;
            }
            else
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Gum Component (*.gucx)|*.gucx";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string fileName = openFileDialog.FileName;

                    string desiredDirectory =
                        FileManager.GetDirectory(
                        ProjectManager.Self.GumProjectSave.FullFileName) + "Components/";

                    if (FileManager.IsRelativeTo(fileName, desiredDirectory) == false)
                    {
                        MessageBox.Show("The file must be in the Gum project's Components folder.  " +
                            "This file will be copied, and the copy will be referenced.");

                        try
                        {
                            string destination = desiredDirectory + FileManager.RemovePath(fileName);
                            System.IO.File.Copy(fileName,
                                destination);

                            fileName = destination;
                        }
                        catch(Exception ex)
                        {
                            MessageBox.Show("Error copying the file: " + ex.ToString());
                            succeeded = false;
                        }
                    }

                    if (succeeded)
                    {
                        string strippedName = FileManager.RemovePath(FileManager.RemoveExtension(fileName));

                        ProjectManager.Self.GumProjectSave.ComponentReferences.Add(
                            new ElementReference { Name = strippedName, ElementType = ElementType.Component });

                        ProjectManager.Self.GumProjectSave.ComponentReferences.Sort(
                            (first, second) => first.Name.CompareTo(second.Name));

                        var componentSave = FileManager.XmlDeserialize<ComponentSave>(fileName);

                        ProjectManager.Self.GumProjectSave.Components.Add(componentSave);

                        ProjectManager.Self.GumProjectSave.Components.Sort(
                            (first, second) => first.Name.CompareTo(second.Name));

                        componentSave.InitializeDefaultAndComponentVariables();

                        GumCommands.Self.GuiCommands.RefreshElementTreeView();

                        SelectedState.Self.SelectedComponent = componentSave;

                        GumCommands.Self.FileCommands.TryAutoSaveProject();
                        GumCommands.Self.FileCommands.TryAutoSaveElement(componentSave);
                    }

                }
                else
                {
                    succeeded = false;
                }
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
            RefreshUI(treeNodeForElement);

            Wireframe.WireframeObjectManager.Self.RefreshAll(true);
            //SelectedState.Self.SelectedInstance = instanceSave;

            // We need to call InstanceAdd before we select the new object - the Undo manager expects it
            PluginManager.Self.InstanceAdd(elementToAddTo, instanceSave);

            Select(instanceSave, elementToAddTo);

            if (ProjectManager.Self.GeneralSettingsFile.AutoSave)
            {
                ProjectManager.Self.SaveElement(elementToAddTo);
                // I don't think we need to save the entire project
                // if we add a new instance, only the element.  I'm going
                // to take this out:
                //ProjectManager.Self.SaveProject();
            }

            return instanceSave;
        }

    }
}
