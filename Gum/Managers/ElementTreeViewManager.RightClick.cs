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

namespace Gum.Managers
{
    public partial class ElementTreeViewManager
    {

        #region Fields

        ContextMenuStrip mMenuStrip;


        ToolStripMenuItem mAddScreen;
        ToolStripMenuItem mAddComponent;
        ToolStripMenuItem mAddInstance;
        ToolStripMenuItem mSaveObject;
        ToolStripMenuItem mGoToDefinition;
        ToolStripMenuItem mDeleteObject;


        ToolStripMenuItem mAddFolder;
        #endregion

        #region Initialize and event handlers

        private void InitializeTreeNodes()
        {
            mAddScreen = new ToolStripMenuItem();
            mAddScreen.Text = "Add Screen";
            mAddScreen.Click += new EventHandler(AddScreenClick);

            mAddComponent = new ToolStripMenuItem();
            mAddComponent.Text = "Add Component";
            mAddComponent.Click += new EventHandler(AddComponentClick);

            mAddInstance = new ToolStripMenuItem();
            mAddInstance.Text = "Add Instance";
            mAddInstance.Click += new EventHandler(AddInstanceClick);

            mSaveObject = new ToolStripMenuItem();
            mSaveObject.Text = "Force Save Object";
            mSaveObject.Click += new EventHandler(ForceSaveObjectClick);

            mGoToDefinition = new ToolStripMenuItem();
            mGoToDefinition.Text = "Go to definition";
            mGoToDefinition.Click += new EventHandler(OnGoToDefinitionClick);

            mAddFolder = new ToolStripMenuItem();
            mAddFolder.Text = "Add Folder";
            mAddFolder.Click += new EventHandler(AddFolderClick);

            mDeleteObject = new ToolStripMenuItem();
            mDeleteObject.Text = "Delete";
            mDeleteObject.Click += new EventHandler(HandleDeleteObjectClick);
        }

        void HandleDeleteObjectClick(object sender, EventArgs e)
        {
            object objectDeleted = null;
            DeleteOptionsWindow optionsWindow = null;

            if (SelectedState.Self.SelectedInstance != null)
            {

            }
            else if (SelectedState.Self.SelectedComponent != null)
            {

                DialogResult result = ShowDeleteDialog(SelectedState.Self.SelectedComponent, out optionsWindow);

                if (result == DialogResult.Yes || result == DialogResult.OK)
                {
                    objectDeleted = SelectedState.Self.SelectedComponent;
                    // We need to remove the reference
                    RemoveSelectedElement();

                }
            }
            else if (SelectedState.Self.SelectedScreen != null)
            {


                DialogResult result = ShowDeleteDialog(SelectedState.Self.SelectedScreen, out optionsWindow);

                if (result == DialogResult.Yes || result == DialogResult.OK)
                {
                    objectDeleted = SelectedState.Self.SelectedScreen;
                    // We need to remove the reference
                    RemoveSelectedElement();
                }

            }

            if (objectDeleted != null)
            {
                PluginManager.Self.DeleteConfirm(optionsWindow, objectDeleted);
            }
        }

        DialogResult ShowDeleteDialog(object objectToDelete, out DeleteOptionsWindow optionsWindow)
        {
            string titleText;
            if (objectToDelete is ComponentSave)
            {
                titleText = "Delete Component?";
            }
            else if (objectToDelete is ScreenSave)
            {
                titleText = "Delete Screen?";
            }
            else if (objectToDelete is InstanceSave)
            {
                titleText = "Delete Instance?";
            }
            else
            {
                titleText = "Delete?";
            }

            optionsWindow = new DeleteOptionsWindow();
            optionsWindow.Text = titleText;
            optionsWindow.Message = "Are you sure you want to delete:\n" + objectToDelete.ToString();
            optionsWindow.ObjectToDelete = objectToDelete;

            PluginManager.Self.ShowDeleteDialog(optionsWindow, objectToDelete);

            DialogResult result = optionsWindow.ShowDialog();



            return result;
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

        void AddInstanceClick(object sender, EventArgs e)
        {
            AddInstanceClick();
        }

        void AddComponentClick(object sender, EventArgs e)
        {
            AddComponentClick();
        }

        void AddScreenClick(object sender, EventArgs e)
        {
            AddScreenClick();
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

                    RefreshUI();
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
                    Process.Start(fullFile);
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
                    RefreshUI();
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
                        DialogResult result = MessageBox.Show("Delete folder " + treeNode.Text + "?", "Delete", MessageBoxButtons.YesNo  );

                        if (result == DialogResult.Yes)
                        {
                            try
                            {
                                Directory.Delete(fullFile);
                                RefreshUI();
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
                    mMenuStrip.Items.Add(mAddFolder);
                    mMenuStrip.Items.Add("View in explorer", null, HandleViewInExplorer);

                    if (SelectedNode.IsScreensFolderTreeNode())
                    {
                        mMenuStrip.Items.Add("Delete Folder", null, HandleDeleteFolder);
                    }
                }
            }

        }

        public void AddScreenClick()
        {
            TextInputWindow tiw = new TextInputWindow();
            tiw.Message = "Enter new Screen name:";

            if (tiw.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string name = tiw.Result;

                string whyNotValid;

                if (!NameVerifier.Self.IsScreenNameValid(name, out whyNotValid))
                {
                    MessageBox.Show(whyNotValid);
                }
                else
                {
                    string path = ElementTreeViewManager.Self.SelectedNode.GetFullFilePath();

                    string relativeToScreens = FileManager.MakeRelative(path,
                        FileLocations.Self.ScreensFolder);

                    ScreenSave screenSave = ProjectCommands.Self.AddScreen(relativeToScreens + name);


                    RefreshUI();

                    SelectedState.Self.SelectedScreen = screenSave;


                    ProjectManager.Self.SaveElement(screenSave);
                    ProjectManager.Self.SaveProject();
                }
            }

        }

        public void AddComponentClick()
        {
            TextInputWindow tiw = new TextInputWindow();
            tiw.Message = "Enter new Component name:";

            if (tiw.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string name = tiw.Result;

                string whyNotValid;

                if (!NameVerifier.Self.IsComponentNameValid(name, out whyNotValid))
                {
                    MessageBox.Show(whyNotValid);
                }
                else
                {


                    ComponentSave componentSave = ProjectCommands.Self.AddComponent(name);


                    RefreshUI();

                    SelectedState.Self.SelectedComponent = componentSave;

                    ProjectManager.Self.SaveElement(componentSave);
                    ProjectManager.Self.SaveProject();
                }
            }
        }

        public void AddInstanceClick()
        {
            TextInputWindow tiw = new TextInputWindow();
            tiw.Message = "Enter new object name:";

            if (tiw.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string name = tiw.Result;
                string whyNotValid;

                if (!NameVerifier.Self.IsInstanceNameValid(name, out whyNotValid))
                {
                    MessageBox.Show(whyNotValid);
                }
                else
                {
                    AddInstance(name, StandardElementsManager.Self.DefaultType, SelectedState.Self.SelectedElement);
                }
            }

        }

        public void AddInstance(string name, string type, ElementSave elementToAddTo)
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


            ProjectManager.Self.SaveElement(elementToAddTo);
            ProjectManager.Self.SaveProject();

        }



        public void RemoveSelectedElementOrInstance()
        {
            if (SelectedState.Self.SelectedInstance != null)
            {


            }
            else if (SelectedState.Self.SelectedElement != null)
            {
                RemoveSelectedElement();

            }


        }

        public void RemoveSelectedElement()
        {
            ElementSave elementToRemove = SelectedState.Self.SelectedElement;

            ScreenSave asScreenSave = elementToRemove as ScreenSave;
            ComponentSave asComponentSave = elementToRemove as ComponentSave;


            if (asScreenSave != null)
            {
                ProjectCommands.Self.RemoveScreen(asScreenSave);
            }
            else if (asComponentSave != null)
            {
                ProjectCommands.Self.RemoveComponent(asComponentSave);
            }

            ElementTreeViewManager.Self.RefreshUI();
            StateTreeViewManager.Self.RefreshUI(null);
            PropertyGridManager.Self.RefreshUI();
            Wireframe.WireframeObjectManager.Self.RefreshAll(true);
            ProjectManager.Self.SaveProject();
        }


    }
}
