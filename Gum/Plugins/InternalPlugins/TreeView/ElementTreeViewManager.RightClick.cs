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
using Gum.Commands;
using Gum.Dialogs;
using Gum.Services;
using Gum.Services.Dialogs;

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
    ToolStripMenuItem mCreateComponentFromThis;
    ToolStripMenuItem mCreateComponentFromSelection;
    ToolStripMenuItem mExtractComponent;
    
    ToolStripMenuItem mDeleteObject;

    ToolStripMenuItem mAddFolder;

    ToolStripMenuItem duplicateElement;
    
    #endregion



    #region Initialize and event handlers

    private void InitializeMenuItems()
    {
        mAddScreen = new ToolStripMenuItem();
        mAddScreen.Text = "Add Screen";
        mAddScreen.Click += (_, _) => _dialogService.Show<AddScreenDialogViewModel>();

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
        mAddInstance.Click += (_, _) => _dialogService.Show<AddInstanceDialogViewModel>();

        mAddParentInstance = new ToolStripMenuItem();
        mAddParentInstance.Text = "Add Parent Instance";
        mAddParentInstance.Click +=
            (_, _) => _dialogService.Show<AddInstanceDialogViewModel>(x => x.ParentInstance = true);

        mSaveObject = new ToolStripMenuItem();
        mSaveObject.Text = "Force Save Object";
        mSaveObject.Click += ForceSaveObjectClick;

        mGoToDefinition = new ToolStripMenuItem();
        mGoToDefinition.Text = "Go to definition";
        mGoToDefinition.Click += OnGoToDefinitionClick;

        mCreateComponentFromThis = new ToolStripMenuItem();
        mCreateComponentFromThis.Text = "Create Component from this";
        mCreateComponentFromThis.Click += CreateComponentFromSelectedClick;
        
        mCreateComponentFromSelection = new ToolStripMenuItem();
        mCreateComponentFromSelection.Text = "Create Component from selection";
        mCreateComponentFromSelection.Click += CreateComponentFromSelectedClick;

        mExtractComponent = new ToolStripMenuItem();
        mExtractComponent.Text = "Extract Component";
        mExtractComponent.Click += ExtractComponentClick;

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
            _editCommands.DuplicateSelectedElement();
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
    
    void CreateComponentFromSelectedClick(object sender, EventArgs e)
    {
        if (_selectedState.SelectedScreen != null ||
            _selectedState.SelectedComponent != null)
        {
            _editCommands.ShowCreateComponentFromInstancesDialog();
        }
    }
    
    void ExtractComponentClick(object sender, EventArgs e)
    {
        if (_selectedState.SelectedScreen != null || _selectedState.SelectedComponent != null)
        {
            _editCommands.ExtractComponent();
        }
    }

    void ForceSaveObjectClick(object sender, EventArgs e)
    {
        _fileCommands.ForceSaveElement(_selectedState.SelectedElement);
    }

    void AddFolderClick(object sender, EventArgs e)
    {
        if (SelectedNode != null)
        {
            _dialogService.Show<AddFolderDialogViewModel>();
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
                    _fileCommands.GetFullPathXmlFile(behaviorSave).FullPath;
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
                _guiCommands.RefreshElementTreeView();
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
                            _guiCommands.RefreshElementTreeView();
                        }
                        catch(Exception exception)
                        {
                            _guiCommands.PrintOutput($"Exception attempting to delete folder:\n{exception}");
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

                    int instanceCount = _selectedState.SelectedInstances.Count();
                    if (instanceCount == 1)
                    {
                        mMenuStrip.Items.Add(mCreateComponentFromThis);
                        mMenuStrip.Items.Add(mExtractComponent);
                    }
                    else if (instanceCount > 1)
                    {
                        mMenuStrip.Items.Add(mCreateComponentFromSelection);
                    }
                }

                mMenuStrip.Items.Add("-");

                var deleteText = _selectedState.SelectedInstances.Count() > 1
                    ? $"Delete {_selectedState.SelectedInstances.Count()} instances"
                    : $"Delete {_selectedState.SelectedInstance.Name}";
                mMenuStrip.Items.Add(deleteText, null, (not, used) => _editCommands.DeleteSelection());



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
                

                mMenuStrip.Items.Add("Add Component", null, (_,_) => _dialogService.Show<AddComponentDialogViewModel>());
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
        _editCommands.DisplayReferencesTo(_selectedState.SelectedElement);
    }

    private void HandleRenameFolder(object sender, EventArgs e)
    {
        _dialogService.Show<RenameFolderDialogViewModel>(vm =>
        {
            vm.FolderNode = (SelectedNode as TreeNodeWrapper)?.Node;
        });
    }

    private void HandleAddBehavior(object sender, EventArgs e)
    {
        _editCommands.AddBehavior();
    }

    private void HandleImportBehavior(object sender, EventArgs args)
    {
        Plugins.ImportPlugin.Manager.ImportLogic.ShowImportBehaviorUi();
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
            _guiCommands.RefreshElementTreeView();
            _selectedState.SelectedComponent = lastImportedComponent;
            _fileCommands.TryAutoSaveProject();
        }
    }

}
