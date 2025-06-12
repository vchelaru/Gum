using System;
using System.Collections.Generic;
using System.Linq;
using CommonFormsAndControls;
using Gum.ToolCommands;
using Gum.DataTypes;
using Gum.ToolStates;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using ToolsUtilities;
using Gum.Logic;
using Gum.DataTypes.Behaviors;
using Gum.PropertyGridHelpers;
using Gum.Controls;
using GumCommon;

namespace Gum.Managers;

public partial class ElementTreeViewManager
{

    #region Fields



    ToolStripMenuItem mAddScreen;
    ToolStripMenuItem mImportScreen;

    ToolStripMenuItem mImportComponent;
    ToolStripMenuItem mAddLinkedComponent;

    ToolStripMenuItem mAddInstance;
    ToolStripMenuItem mAddParentInstance;
    ToolStripMenuItem mSaveObject;
    ToolStripMenuItem mGoToDefinition;
    ToolStripMenuItem mCreateComponent;
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

        mImportComponent = new ToolStripMenuItem();
        mImportComponent.Text = "Import Components";
        mImportComponent.Click += ImportComponentsClick;

        mAddLinkedComponent = new ToolStripMenuItem();
        mAddLinkedComponent.Text = "Add Linked Component";
        mAddLinkedComponent.Click += HandleAddLinkedComponentClick;

        mAddInstance = new ToolStripMenuItem();
        mAddInstance.Text = "Add Instance";
        mAddInstance.Click += AddInstanceClick;

        mAddParentInstance = new ToolStripMenuItem();
        mAddParentInstance.Text = "Add Parent Instance";
        mAddParentInstance.Click += AddParentInstanceClick;

        mSaveObject = new ToolStripMenuItem();
        mSaveObject.Text = "Force Save Object";
        mSaveObject.Click += ForceSaveObjectClick;

        mGoToDefinition = new ToolStripMenuItem();
        mGoToDefinition.Text = "Go to definition";
        mGoToDefinition.Click += OnGoToDefinitionClick;

        mCreateComponent = new ToolStripMenuItem();
        mCreateComponent.Text = "Create Component";
        mCreateComponent.Click += CreateComponentClick;

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
        DeleteLogic.Self.HandleDeleteCommand();
    }

    void HandleDuplicateElementClick(object sender, EventArgs e)
    {
        if (_selectedState.SelectedScreen != null ||
            _selectedState.SelectedComponent != null)
        {
            GumCommands.Self.Edit.DuplicateSelectedElement();
        }
    }

    void OnGoToDefinitionClick(object sender, EventArgs e)
    {
        if (_selectedState.SelectedInstance != null)
        {
            ElementSave element = ObjectFinder.Self.GetElementSave(_selectedState.SelectedInstance.BaseType);

            _selectedState.SelectedElement = element;
        }
    }

    void CreateComponentClick(object sender, EventArgs e)
    {
        if (_selectedState.SelectedScreen != null ||
            _selectedState.SelectedComponent != null)
        {
            GumCommands.Self.Edit.ShowCreateComponentFromInstancesDialog();
        }
    }

    void ForceSaveObjectClick(object sender, EventArgs e)
    {
        GumCommands.Self.FileCommands.ForceSaveElement(_selectedState.SelectedElement);
    }

    void AddFolderClick(object sender, EventArgs e)
    {
        var node = (SelectedNode as TreeNodeWrapper)?.Node;
        if(node != null)
        {
            GumCommands.Self.GuiCommands.ShowAddFolderWindow(node);
        }
    }


    void HandleViewInExplorer(object sender, EventArgs e)
    {
        var treeNode = _selectedState.SelectedTreeNode;

        if (treeNode != null)
        {
            string fullFile;
            if (treeNode.Tag is ElementSave elementSave)
            {
                fullFile = elementSave.GetFullPathXmlFile().FullPath;
            }
            else if (treeNode.Tag is BehaviorSave behaviorSave)
            {
                fullFile =
                    GumCommands.Self.FileCommands.GetFullPathXmlFile(behaviorSave).FullPath;
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

                    if (!doesExist)
                    {
                        // file doesn't exist, but if it's a standard folder we can make one:
                        if (treeNode.IsTopComponentContainerTreeNode() ||
                            treeNode.IsTopScreenContainerTreeNode())
                        {
                            System.IO.Directory.CreateDirectory(fullFile);
                        }
                    }
                    Process.Start(fullFile);
                }
                catch (Exception exc)
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
        var treeNode = _selectedState.SelectedTreeNode;

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
                            FileManager.DeleteDirectory(fullFile);
                            GumCommands.Self.GuiCommands.RefreshElementTreeView();
                        }
                        catch(Exception exception)
                        {
                            GumCommands.Self.GuiCommands.PrintOutput($"Exception attempting to delete folder:\n{exception}");
                            MessageBox.Show("Could not delete folder\nSee the output tab for more info");
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
            if (_selectedState.SelectedInstance != null)
            {
                var containerElement = _selectedState.SelectedElement;
                mMenuStrip.Items.Add(mGoToDefinition);

                if (containerElement != null)
                {
                    mMenuStrip.Items.Add("-");

                    mMenuStrip.Items.Add(mCreateComponent);
                }

                mMenuStrip.Items.Add("-");

                var deleteText = _selectedState.SelectedInstances.Count() > 1
                    ? $"Delete {_selectedState.SelectedInstances.Count()} instances"
                    : $"Delete {_selectedState.SelectedInstance.Name}";
                mMenuStrip.Items.Add(deleteText, null, (not, used) => GumCommands.Self.Edit.DeleteSelection());



                if (containerElement != null)
                {
                    mMenuStrip.Items.Add("-");

                    mAddInstance.Text = $"Add child object to '{_selectedState.SelectedInstance.Name}'";
                    mMenuStrip.Items.Add(mAddInstance);
                    mAddParentInstance.Text = $"Add parent object to '{_selectedState.SelectedInstance.Name}'";
                    mMenuStrip.Items.Add(mAddParentInstance);

                    if (!string.IsNullOrEmpty(containerElement?.BaseType))
                    {
                        var containerBase = ObjectFinder.Self.GetElementSave(containerElement.BaseType);

                        if (containerBase is ScreenSave || containerBase is ComponentSave)
                        {
                            mMenuStrip.Items.Add($"Add {_selectedState.SelectedInstance.Name} to base {containerBase}",
                                null,
                                (not, used) => HandleMoveToBase(_selectedState.SelectedInstances, _selectedState.SelectedElement, containerBase));
                        }
                    }

                }
                AddPasteMenuItems();
            }

            #endregion

            #region Screen or Component
            // ScreenSave or ComponentSave
            else if (_selectedState.SelectedScreen != null || _selectedState.SelectedComponent != null)
            {
                mMenuStrip.Items.Add("View in explorer", null, HandleViewInExplorer);

                mMenuStrip.Items.Add("View References", null, HandleViewReferences);

                mMenuStrip.Items.Add("-");

                mAddInstance.Text = "Add object to " + _selectedState.SelectedElement.Name;
                mMenuStrip.Items.Add(mAddInstance);
                mMenuStrip.Items.Add(mSaveObject);
                if (_selectedState.SelectedScreen != null)
                {
                    duplicateElement.Text = $"Duplicate {_selectedState.SelectedScreen.Name}";
                }
                else
                {
                    duplicateElement.Text = $"Duplicate {_selectedState.SelectedComponent.Name}";
                }
                mMenuStrip.Items.Add(duplicateElement);

                AddPasteMenuItems();

                mMenuStrip.Items.Add("-");


                mDeleteObject.Text = "Delete " + _selectedState.SelectedElement.ToString();
                mMenuStrip.Items.Add(mDeleteObject);

            }
            #endregion

            #region Behavior

            else if (_selectedState.SelectedBehavior != null)
            {
                mMenuStrip.Items.Add("View in explorer", null, HandleViewInExplorer);
                mMenuStrip.Items.Add("-");
                mDeleteObject.Text = "Delete " + _selectedState.SelectedBehavior.ToString();
                mMenuStrip.Items.Add(mDeleteObject);
            }

            #endregion

            #region Standard Element

            else if (_selectedState.SelectedStandardElement != null)
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

            #region Component Folder (top or contained)

            else if (SelectedNode.IsTopComponentContainerTreeNode() || SelectedNode.IsComponentsFolderTreeNode())
            {
                mMenuStrip.Items.Add("Add Component", null, AddComponentClick);
                mMenuStrip.Items.Add(mImportComponent);
                mMenuStrip.Items.Add(mAddFolder);
                mMenuStrip.Items.Add("View in explorer", null, HandleViewInExplorer);

                if (SelectedNode.IsComponentsFolderTreeNode())
                {
                    mMenuStrip.Items.Add("Delete Folder", null, HandleDeleteFolder);
                    mMenuStrip.Items.Add("Rename Folder", null, HandleRenameFolder);

                }
            }

            #endregion

            #region Standard Elements Folder (top)

            else if (SelectedNode.IsTopStandardElementTreeNode())
            {
                mMenuStrip.Items.Add("View in explorer", null, HandleViewInExplorer);
            }

            #endregion

            #region Behavior Folder (top)

            else if (SelectedNode.IsTopBehaviorTreeNode())
            {
                mMenuStrip.Items.Add("Add Behavior", null, HandleAddBehavior);
                mMenuStrip.Items.Add("Import Behavior", null, HandleImportBehavior);
                mMenuStrip.Items.Add("View in explorer", null, HandleViewInExplorer);

            }

            #endregion
        }
    }

    private void AddPasteMenuItems()
    {
        if (_copyPasteLogic.CopiedData.CopiedInstancesRecursive.Count > 0)
        {
            mMenuStrip.Items.Add("Paste", null, HandlePaste);
        }
        if (_copyPasteLogic.CopiedData.CopiedInstancesSelected.Count > 0)
        {
            string text;
            if (_copyPasteLogic.CopiedData.CopiedInstancesSelected.Count == 0)
                text = "Paste Top Level Instance";
            else
                text = "Paste Top Level Instances";

            mMenuStrip.Items.Add(text, null, HandlePasteTopLevel);
        }
    }

    private void HandleMoveToBase(IEnumerable<InstanceSave> instances, ElementSave derivedElement, ElementSave baseElement)
    {
        foreach (var instance in instances)
        {
            instance.DefinedByBase = true;
        }


        _copyPasteLogic.PasteInstanceSaves(
            instances.ToList(),
            new List<DataTypes.Variables.StateSave> { derivedElement.DefaultState.Clone() },
            baseElement,
            null);
    }

    private void HandlePaste(object sender, EventArgs e)
    {
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement, TopOrRecursive.Recursive);
    }

    private void HandlePasteTopLevel(object sender, EventArgs e)
    {
        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement, TopOrRecursive.Top);
    }

    private void HandleViewReferences(object sender, EventArgs e)
    {
        GumCommands.Self.Edit.DisplayReferencesTo(_selectedState.SelectedElement);
    }

    private void HandleRenameFolder(object sender, EventArgs e)
    {
        var node = (SelectedNode as TreeNodeWrapper)?.Node;
        if (node != null)
        {
            GumCommands.Self.GuiCommands.ShowRenameFolderWindow(node);
        }
    }

    private void HandleAddBehavior(object sender, EventArgs e)
    {
        GumCommands.Self.Edit.AddBehavior();
    }

    private void HandleImportBehavior(object sender, EventArgs args)
    {
        Plugins.ImportPlugin.Manager.ImportLogic.ShowImportBehaviorUi();
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
            tiw.Title = "Add Screen";

            if (tiw.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string name = tiw.Result;

                string whyNotValid;

                if (!NameVerifier.Self.IsElementNameValid(name, null, null, out whyNotValid))
                {
                    MessageBox.Show(whyNotValid);
                }
                else
                {
                    var nodeToAddTo = (ElementTreeViewManager.Self.SelectedNode as TreeNodeWrapper)?.Node;

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

                    ScreenSave screenSave = GumCommands.Self.ProjectCommands.AddScreen(relativeToScreens + name);


                    GumCommands.Self.GuiCommands.RefreshElementTreeView();

                    _selectedState.SelectedScreen = screenSave;

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
            StandardElementsManagerGumTool.Self.FixCustomTypeConverters(componentSave);

            lastImportedComponent = componentSave;
        }

        if (lastImportedComponent != null)
        {
            GumCommands.Self.GuiCommands.RefreshElementTreeView();
            _selectedState.SelectedComponent = lastImportedComponent;
            GumCommands.Self.FileCommands.TryAutoSaveProject();
        }
    }

    private bool ShowNewObjectDialog(out string name)
    {
        var tiw = new TextInputWindow();
        tiw.Message = "Enter new object name:";
        tiw.Title = "New object";

        var result = tiw.ShowDialog() == System.Windows.Forms.DialogResult.OK;
        name = result ? tiw.Result : null;

        if (result)
        {
            string whyNotValid;

            if (!NameVerifier.Self.IsInstanceNameValid(name, null, _selectedState.SelectedElement, out whyNotValid))
            {
                MessageBox.Show(whyNotValid);

                return false;
            }
        }

        return result;
    }


    public void AddInstanceClick(object sender, EventArgs e)
    {
        if (!ShowNewObjectDialog(out var name)) return;

        var focusedInstance = _selectedState.SelectedInstance;
        var newInstance = GumCommands.Self.ProjectCommands.ElementCommands.AddInstance(_selectedState.SelectedElement, name, StandardElementsManager.Self.DefaultType);

        if (focusedInstance != null)
        {
            SetInstanceParent(_selectedState.SelectedElement, newInstance, focusedInstance);
        }
    }

    public void AddParentInstanceClick(object sender, EventArgs e)
    {
        if (!ShowNewObjectDialog(out var name)) return;

        var focusedInstance = _selectedState.SelectedInstance;
        var newInstance = GumCommands.Self.ProjectCommands.ElementCommands.AddInstance(_selectedState.SelectedElement, name, StandardElementsManager.Self.DefaultType);

        System.Diagnostics.Debug.Assert(focusedInstance != null);

        SetInstanceParentWrapper(_selectedState.SelectedElement, newInstance, focusedInstance);
    }

    private static void SetInstanceParentWrapper(ElementSave targetElement, InstanceSave newInstance, InstanceSave existingInstance)
    {
        // Vic October 13, 2023
        // Currently new parents can
        // only be created as Containers,
        // so they won't have Default Child 
        // Containers. In the future we will
        // probably add the ability to select
        // the type of parent to add, and when
        // that happens we'll want to add assignment
        // of the parent's default child container.

        // From DragDropManager:
        // "Since the Parent property can only be set in the default state, we will
        // set the Parent variable on that instead of the _selectedState.SelectedStateSave"
        var stateToAssignOn = targetElement.DefaultState;

        var variableName = newInstance.Name + ".Parent";
        var existingInstanceVar = existingInstance.Name + ".Parent";
        var oldValue = stateToAssignOn.GetValue(variableName) as string;        // This will always be empty anyway...
        var oldParentValue = stateToAssignOn.GetValue(existingInstanceVar) as string;

        stateToAssignOn.SetValue(variableName, oldParentValue, "string");
        stateToAssignOn.SetValue(existingInstanceVar, newInstance.Name, "string");

        SetVariableLogic.Self.PropertyValueChanged("Parent", oldValue, newInstance, targetElement.DefaultState);
        SetVariableLogic.Self.PropertyValueChanged("Parent", oldParentValue, existingInstance, targetElement.DefaultState);
    }

    private static void SetInstanceParent(ElementSave targetElement, InstanceSave child, InstanceSave parent)
    {
        // From DragDropManager:
        // "Since the Parent property can only be set in the default state, we will
        // set the Parent variable on that instead of the _selectedState.SelectedStateSave"
        var stateToAssignOn = targetElement.DefaultState;
        var variableName = child.Name + ".Parent";
        var oldValue = stateToAssignOn.GetValue(variableName) as string;        // This will always be empty anyway...

        string newParent = parent.Name;
        var suffix = ObjectFinder.Self.GetDefaultChildName(parent);
        if (!string.IsNullOrEmpty(suffix))
        {
            newParent = parent.Name + "." + suffix;
        }

        stateToAssignOn.SetValue(variableName, newParent, "string");
        SetVariableLogic.Self.PropertyValueChanged("Parent", oldValue, child, targetElement.DefaultState);
    }
}
