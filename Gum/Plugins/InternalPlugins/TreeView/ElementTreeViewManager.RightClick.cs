using System;
using System.Collections.Generic;
using System.Linq;
using CommonFormsAndControls;
using Gum.ToolCommands;
using Gum.DataTypes;
using Gum.ToolStates;
using System.Windows.Controls;
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
using Gum.Plugins.ImportPlugin.ViewModel;
using Gum.Plugins.InternalPlugins.VariableGrid;

namespace Gum.Managers;

public partial class ElementTreeViewManager
{

    #region Menu helpers

    private void AddMenuItem(string text, Action clickAction)
    {
        var menuItem = new MenuItem { Header = text };
        menuItem.Click += (_, _) => clickAction();
        _contextMenu.Items.Add(menuItem);
    }

    private void AddSeparator() => _contextMenu.Items.Add(new Separator());

    #endregion

    #region Event handlers

    void HandleDeleteObject()
    {
        using var undoLock = _undoManager.RequestLock();
        _deleteLogic.HandleDeleteCommand();
    }

    void HandleDuplicateElement()
    {
        if (_selectedState.SelectedScreen != null ||
            _selectedState.SelectedComponent != null)
        {
            _editCommands.DuplicateSelectedElement();
        }
    }

    void HandleGoToDefinition()
    {
        if (_selectedState.SelectedInstance != null)
        {
            ElementSave element = ObjectFinder.Self.GetElementSave(_selectedState.SelectedInstance.BaseType);

            _selectedState.SelectedElement = element;
        }
    }

    void HandleCreateComponent()
    {
        if (_selectedState.SelectedScreen != null ||
            _selectedState.SelectedComponent != null)
        {
            _editCommands.ShowCreateComponentFromInstancesDialog();
        }
    }

    void HandleForceSaveObject()
    {
        _fileCommands.ForceSaveElement(_selectedState.SelectedElement);
    }

    void HandleAddFolder()
    {
        if (SelectedNode != null)
        {
            _dialogService.Show<AddFolderDialogViewModel>();
        }
    }


    void HandleViewInExplorer()
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
                    var startInfo = new ProcessStartInfo
                    {
                        UseShellExecute = true ,
                        FileName = fullFile
                    };
                    Process.Start(startInfo );
                }
                catch (Exception exc)
                {
                    _dialogService.ShowMessage("Could not open location:\n\n" + exc.ToString());
                }
            }
            else
            {
                Process.Start("explorer.exe", "/select," + fullFile);
            }
        }
    }

    void HandleDeleteFolder()
    {
        var treeNode = _selectedState.SelectedTreeNode;

        if (treeNode != null)
        {
            _deleteLogic.DeleteFolder(treeNode);
        }
    }

    #endregion


    public void PopulateContextMenu()
    {
        _contextMenu.Items.Clear();

        if (SelectedNode != null)
        {
            // Check for mixed-type selections by reading directly from tree view
            var allSelectedTags = _selectedState.SelectedTreeNodes
                .Select(n => n.Tag)
                .Where(t => t != null)
                .ToList();

            var elementsFromTree = allSelectedTags.OfType<ElementSave>()
                .Where(e => e is not StandardElementSave).ToList();
            var behaviorsFromTree = allSelectedTags.OfType<BehaviorSave>().ToList();
            var instancesFromTree = allSelectedTags.OfType<InstanceSave>().ToList();

            int typesPresent = (elementsFromTree.Count > 0 ? 1 : 0)
                + (behaviorsFromTree.Count > 0 ? 1 : 0)
                + (instancesFromTree.Count > 0 ? 1 : 0);

            if (typesPresent > 1)
            {
                int totalCount = elementsFromTree.Count + behaviorsFromTree.Count + instancesFromTree.Count;
                AddMenuItem($"Delete {totalCount} items", HandleDeleteObject);
            }

            #region InstanceSave
            // InstanceSave selected
            else if (_selectedState.SelectedInstance != null)
            {
                var containerElement = _selectedState.SelectedElement;
                AddMenuItem("Go to definition", HandleGoToDefinition);

                if (containerElement != null)
                {
                    AddSeparator();

                    AddMenuItem("Create Component", HandleCreateComponent);
                }

                AddSeparator();

                var instances = _selectedState.SelectedInstances.ToList();
                var allLocked = instances.All(i => i.Locked);
                var lockText = instances.Count > 1
                    ? (allLocked ? $"Unlock {instances.Count} instances" : $"Lock {instances.Count} instances")
                    : (allLocked ? $"Unlock {_selectedState.SelectedInstance.Name}" : $"Lock {_selectedState.SelectedInstance.Name}");
                AddMenuItem(lockText, HandleToggleLock);

                var deleteText = instances.Count > 1
                    ? $"Delete {instances.Count} instances"
                    : $"Delete {_selectedState.SelectedInstance.Name}";
                AddMenuItem(deleteText, () => _editCommands.DeleteSelection());

                if (containerElement != null)
                {
                    AddSeparator();

                    AddCreateInstanceMenuItems($"Add child object to '{_selectedState.SelectedInstance.Name}'");

                    AddMenuItem($"Add parent object to '{_selectedState.SelectedInstance.Name}'",
                        () => _dialogService.Show<AddInstanceDialogViewModel>(x => x.IsAddingAsParentToSelectedInstance = true));

                    if (!string.IsNullOrEmpty(containerElement?.BaseType))
                    {
                        var containerBase = ObjectFinder.Self.GetElementSave(containerElement.BaseType);

                        if (containerBase is ScreenSave || containerBase is ComponentSave)
                        {
                            AddMenuItem($"Add {_selectedState.SelectedInstance.Name} to base {containerBase}",
                                () => HandleMoveToBase(_selectedState.SelectedInstances, _selectedState.SelectedElement, containerBase));
                        }
                    }

                }
                AddCopyMenuItems();
                AddCutMenuItems();
                AddPasteMenuItems();
            }

            #endregion

            #region Screen or Component
            // ScreenSave or ComponentSave
            else if (_selectedState.SelectedScreen != null || _selectedState.SelectedComponent != null)
            {
                AddMenuItem("View in explorer", HandleViewInExplorer);

                AddMenuItem("View References", HandleViewReferences);

                AddSeparator();

                AddCreateInstanceMenuItems("Add object to " + _selectedState.SelectedElement!.Name);

                var duplicateText = _selectedState.SelectedScreen != null
                    ? $"Duplicate {_selectedState.SelectedScreen.Name}"
                    : $"Duplicate {_selectedState.SelectedComponent!.Name}";
                AddMenuItem(duplicateText, HandleDuplicateElement);

                AddSeparator();

                var selectedElements = _selectedState.SelectedElements.ToList();
                string elementDeleteText;
                if (selectedElements.Count > 1)
                {
                    bool allScreens = selectedElements.All(e => e is ScreenSave);
                    bool allComponents = selectedElements.All(e => e is ComponentSave);
                    if (allScreens)
                        elementDeleteText = $"Delete {selectedElements.Count} Screens";
                    else if (allComponents)
                        elementDeleteText = $"Delete {selectedElements.Count} Components";
                    else
                        elementDeleteText = $"Delete {selectedElements.Count} elements";
                }
                else
                {
                    elementDeleteText = "Delete " + _selectedState.SelectedElement.ToString();
                }
                AddMenuItem(elementDeleteText, HandleDeleteObject);

                AddSeparator();

                AddCopyMenuItems();
                AddCutMenuItems();
                AddPasteMenuItems();

                AddSeparator();

                AddMenuItem("Force Save Object", HandleForceSaveObject);

                // Add favorite toggle for components only
                if (_selectedState.SelectedComponent != null)
                {
                    var isFavorite = _favoriteComponentManager.IsFavorite(_selectedState.SelectedComponent);
                    var favoriteText = isFavorite ? "Remove from Favorites" : "Add to Favorites";
                    AddMenuItem(favoriteText, HandleToggleFavorite);
                }

            }
            #endregion

            #region Behavior

            else if (_selectedState.SelectedBehavior != null)
            {
                AddMenuItem("View in explorer", HandleViewInExplorer);
                AddSeparator();
                var selectedBehaviors = _selectedState.SelectedBehaviors.ToList();
                var behaviorDeleteText = selectedBehaviors.Count > 1
                    ? $"Delete {selectedBehaviors.Count} Behaviors"
                    : "Delete " + _selectedState.SelectedBehavior.ToString();
                AddMenuItem(behaviorDeleteText, HandleDeleteObject);
            }

            #endregion

            #region Standard Element

            else if (_selectedState.SelectedStandardElement != null)
            {
                AddMenuItem("View in explorer", HandleViewInExplorer);

                AddSeparator();

                AddMenuItem("Force Save Object", HandleForceSaveObject);
            }

            #endregion

            #region Screens Folder (top or contained)

            else if (SelectedNode.IsTopScreenContainerTreeNode() || SelectedNode.IsScreensFolderTreeNode())
            {
                AddMenuItem("Add Screen", () => _dialogService.Show<AddScreenDialogViewModel>());
                AddMenuItem("Import Screen", HandleImportScreen);
                AddMenuItem("Add Folder", HandleAddFolder);
                AddMenuItem("View in explorer", HandleViewInExplorer);

                if (SelectedNode.IsScreensFolderTreeNode())
                {
                    AddMenuItem("Delete Folder", HandleDeleteFolder);
                    AddMenuItem("Rename Folder", HandleRenameFolder);
                }
            }

            #endregion

            #region Component Folder (top or contained)

            else if (SelectedNode.IsTopComponentContainerTreeNode() || SelectedNode.IsComponentsFolderTreeNode())
            {
                AddMenuItem("Add Component", () => _dialogService.Show<AddComponentDialogViewModel>());
                AddMenuItem("Import Components", HandleImportComponents);
                AddMenuItem("Add Folder", HandleAddFolder);
                AddMenuItem("View in explorer", HandleViewInExplorer);

                if (SelectedNode.IsComponentsFolderTreeNode())
                {
                    AddMenuItem("Delete Folder", HandleDeleteFolder);
                    AddMenuItem("Rename Folder", HandleRenameFolder);

                }
            }

            #endregion

            #region Standard Elements Folder (top)

            else if (SelectedNode.IsTopStandardElementTreeNode())
            {
                AddMenuItem("View in explorer", HandleViewInExplorer);
            }

            #endregion

            #region Behavior Folder (top)

            else if (SelectedNode.IsTopBehaviorTreeNode())
            {
                AddMenuItem("Add Behavior", HandleAddBehavior);
                AddMenuItem("Import Behavior", HandleImportBehavior);
                AddMenuItem("View in explorer", HandleViewInExplorer);

            }

            #endregion
        }
    }

    private void AddCreateInstanceMenuItems(string itemText)
    {
        var parentMenuItem = new MenuItem { Header = itemText };
        _contextMenu.Items.Add(parentMenuItem);

        // Add favorited components first
        var favoritedComponents = _favoriteComponentManager.GetFilteredFavoritedComponentsFor(
            _selectedState.SelectedElement,
            _circularReferenceManager);
        if (favoritedComponents.Count > 0)
        {
            foreach (var component in favoritedComponents)
            {
                var menuItem = new MenuItem { Header = component.Name };
                parentMenuItem.Items.Add(menuItem);

                var componentName = component.Name;
                menuItem.Click += (_, _) =>
                {
                    var selectedElement = _selectedState.SelectedElement;
                    if (selectedElement != null)
                    {
                        var newInstanceElementType = ObjectFinder.Self.GetElementSave(componentName)!;
                        var name = _elementCommands.GetUniqueNameForNewInstance(newInstanceElementType, selectedElement);

                        var viewModel = new AddInstanceDialogViewModel(
                            _selectedState,
                            _nameVerifier,
                            _elementCommands,
                            _setVariableLogic);
                        viewModel.TypeToCreate = componentName;
                        viewModel.Value = name;
                        viewModel.OnAffirmative();
                    }
                };
            }

            // Add separator after favorited components
            parentMenuItem.Items.Add(new Separator());
        }

        // Add child menu items for each type
        var types = new[] {
            "Circle",
            "ColoredRectangle",
            "Container",
            "NineSlice",
            "Polygon",
            "Rectangle",
            "Sprite",
            "Text"
        };

        foreach (var type in types)
        {
            var menuItem = new MenuItem { Header = type };
            parentMenuItem.Items.Add(menuItem);

            menuItem.Click += (_, _) =>
            {
                var selectedElement = _selectedState.SelectedElement;
                if (selectedElement != null)
                {
                    var newInstanceElementType = ObjectFinder.Self.GetElementSave(type)!;
                    var name = _elementCommands.GetUniqueNameForNewInstance(newInstanceElementType, selectedElement);

                    var viewModel = new AddInstanceDialogViewModel(
                        _selectedState,
                        _nameVerifier,
                        _elementCommands,
                        _setVariableLogic);
                    viewModel.TypeToCreate = type;
                    viewModel.Value = name;
                    viewModel.OnAffirmative();
                }
            };
        }
    }

    private void AddCopyMenuItems()
    {
        AddMenuItem("Copy", () => _copyPasteLogic.OnCopy(CopyType.InstanceOrElement));
    }

    private void AddCutMenuItems()
    {
        AddMenuItem("Cut", () => _copyPasteLogic.OnCut(CopyType.InstanceOrElement));
    }

    private void AddPasteMenuItems()
    {
        if (_copyPasteLogic.CopiedData.CopiedInstancesRecursive.Count > 0)
        {
            AddMenuItem("Paste", () => _copyPasteLogic.OnPaste(CopyType.InstanceOrElement, TopOrRecursive.Recursive));
        }
        if (_copyPasteLogic.CopiedData.CopiedInstancesSelected.Count > 0)
        {
            string text;
            if (_copyPasteLogic.CopiedData.CopiedInstancesSelected.Count == 1)
                text = "Paste Top Level Instance";
            else
                text = "Paste Top Level Instances";

            AddMenuItem(text, () => _copyPasteLogic.OnPaste(CopyType.InstanceOrElement, TopOrRecursive.Top));
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

    private void HandleToggleLock()
    {
        var instances = _selectedState.SelectedInstances.ToList();
        var element = _selectedState.SelectedElement;
        if (element == null || instances.Count == 0) return;

        var shouldLock = instances.Any(i => !i.Locked);

        using var undoLock = _undoManager.RequestLock();

        for (int i = 0; i < instances.Count; i++)
        {
            var instance = instances[i];
            if (instance.Locked != shouldLock)
            {
                var oldValue = instance.Locked;
                instance.Locked = shouldLock;
                var isLast = i == instances.Count - 1;
                _setVariableLogic.PropertyValueChanged("Locked", oldValue, instance, element.DefaultState,
                    refresh: isLast, trySave: isLast);
            }
        }
    }

    private void HandleToggleFavorite()
    {
        var component = _selectedState.SelectedComponent;
        if (component == null) return;

        var isFavorite = _favoriteComponentManager.IsFavorite(component);
        if (isFavorite)
        {
            _favoriteComponentManager.RemoveFromFavorites(component);
        }
        else
        {
            _favoriteComponentManager.AddToFavorites(component);
        }
    }

    private void HandleViewReferences()
    {
        _dialogService.Show<DisplayReferencesDialog>(vm => vm.ElementSave = _selectedState.SelectedElement);
    }

    private void HandleRenameFolder()
    {
        _dialogService.Show<RenameFolderDialogViewModel>(vm =>
        {
            vm.FolderNode = SelectedNode;
        });
    }

    private void HandleAddBehavior()
    {
        _editCommands.AddBehavior();
    }

    private void HandleImportBehavior()
    {
        if (GuardProjectSaved("before importing behaviors"))
        {
            _dialogService.Show<ImportBehaviorDialog>();
        }
    }

    private void HandleImportScreen()
    {
        if (GuardProjectSaved("before importing screens"))
        {
            _dialogService.Show<ImportScreenDialog>();
        }
    }

    private void HandleImportComponents()
    {
        if (GuardProjectSaved("before importing components"))
        {
            _dialogService.Show<ImportComponentDialog>();
        }
    }

    private bool GuardProjectSaved(string? reason = null)
    {
        if (ObjectFinder.Self.GumProjectSave == null || string.IsNullOrEmpty(Locator.GetRequiredService<IProjectManager>().GumProjectSave.FullFileName))
        {
            _dialogService.ShowMessage("You must first save the project");
            return false;
        }

        return true;
    }

    private void HandleAddLinkedComponentClick()
    {
        ////////////////Early Out/////////////////////////
        if (string.IsNullOrEmpty(Locator.GetRequiredService<IProjectManager>().GumProjectSave?.FullFileName))
        {
            _dialogService.ShowMessage("You must first save the project before adding a new component");
            return;
        }
        //////////////End Early Out////////////////////////

        var openFileDialog = new System.Windows.Forms.OpenFileDialog();
        openFileDialog.Multiselect = true;
        openFileDialog.Filter = "Gum Component (*.gucx)|*.gucx";

        /////////////////Another Early Out////////////////////////
        if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }
        ///////////End Another Early Out//////////////////////////

        ComponentSave? lastImportedComponent = null;

        for (int i = 0; i < openFileDialog.FileNames.Length; ++i)
        {
            FilePath componentFilePath = openFileDialog.FileNames[i];

            var gumProject = ObjectFinder.Self.GumProjectSave!;
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


            var components = Locator.GetRequiredService<IProjectManager>().GumProjectSave.Components;
            components.Add(componentSave);
            components.Sort((first, second) => first.Name.CompareTo(second.Name));

            componentSave.InitializeDefaultAndComponentVariables();
            _standardElementsManagerGumTool.FixCustomTypeConverters(componentSave);

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
