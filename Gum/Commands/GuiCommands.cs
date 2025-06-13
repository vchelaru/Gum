using Gum.Managers;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Gum.DataTypes;
using Gum.Plugins;
using Gum.Controls;
using Gum.Extensions;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Plugins.VariableGrid;
using Gum.ToolCommands;
using CommonFormsAndControls;
using Gum.Undo;
using Gum.Logic;
using static System.ComponentModel.Design.ObjectSelectorEditor;
using System.IO;
using ToolsUtilities;
using WpfDataUi.DataTypes;
using Gum.PropertyGridHelpers;
using System.Xml.Linq;
using Gum.Plugins.InternalPlugins.VariableGrid.ViewModels;
using Microsoft.Extensions.Hosting;
using Gum.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Media;
using GumCommon;

namespace Gum.Commands;

public class GuiCommands
{
    #region Fields/Properties

    public MainWindow MainWindow { get; private set; }

    public System.Windows.Forms.Cursor AddCursor { get; set; }


    MainPanelControl mainPanelControl;

    private readonly ISelectedState _selectedState;

    #endregion

    public GuiCommands()
    {
        _selectedState = Locator.GetRequiredService<ISelectedState>();
    }

    internal void Initialize(MainWindow mainWindow, MainPanelControl mainPanelControl)
    {
        this.MainWindow = mainWindow;
        this.mainPanelControl = mainPanelControl;
    }

    internal void BroadcastRefreshBehaviorView()
    {
        PluginManager.Self.RefreshBehaviorView(
            _selectedState.SelectedElement);
    }

    internal void BroadcastBehaviorReferencesChanged()
    {
        PluginManager.Self.BehaviorReferencesChanged(
            _selectedState.SelectedElement);
    }

    #region Refresh Commands

    internal void RefreshStateTreeView()
    {
        PluginManager.Self.RefreshStateTreeView();
    }

    public void RefreshVariables(bool force = false)
    {
        PluginManager.Self.RefreshVariableView(force);
    }

    /// <summary>
    /// Refreshes the displayed values without clearing and recreating the grid
    /// </summary>
    public void RefreshVariableValues()
    {
        PropertyGridManager.Self.RefreshVariablesDataGridValues();
    }

    const int DefaultFontSize = 11;

    int _uiZoomValue = 100;
    const int MinUiZoomValue = 70;
    const int MaxUiZoomValue = 500;
    public int UiZoomValue
    {
        get => _uiZoomValue;
        set
        {
            if (value > MaxUiZoomValue)
            {
                _uiZoomValue = MaxUiZoomValue;
            }
            else if (value < MinUiZoomValue)
            {
                _uiZoomValue = MinUiZoomValue;
            }
            else
            {
                _uiZoomValue = value;
            }
            UpdateUiToZoomValue();
        }
    }

    private void UpdateUiToZoomValue()
    {
        var fontSize = DefaultFontSize * UiZoomValue / 100.0f;

        mainPanelControl.FontSize = fontSize;

        PluginManager.Self.HandleUiZoomValueChanged();
    }


    public void RefreshElementTreeView()
    {
        PluginManager.Self.RefreshElementTreeView();
    }

    public void RefreshElementTreeView(IInstanceContainer instanceContainer)
    {
        PluginManager.Self.RefreshElementTreeView(instanceContainer);
    }

    #endregion

    #region Tab Controls

    public PluginTab AddControl(System.Windows.FrameworkElement control, string tabTitle, TabLocation tabLocation = TabLocation.CenterBottom)
    {
        CheckForInitialization();
        return mainPanelControl.AddWpfControl(control, tabTitle, tabLocation);
    }

    public void ShowTab(PluginTab tab, bool focus = true) =>
        mainPanelControl.ShowTab(tab, focus);

    public void HideTab(PluginTab tab)
    {
        mainPanelControl.HideTab(tab);
    }

    public PluginTab AddControl(System.Windows.Forms.Control control, string tabTitle, TabLocation tabLocation)
    {
        CheckForInitialization();
        return mainPanelControl.AddWinformsControl(control, tabTitle, tabLocation);
    }

    private void CheckForInitialization()
    {
        if (mainPanelControl == null)
        {
            throw new InvalidOperationException("Need to call Initialize first");
        }
    }

    public PluginTab AddWinformsControl(Control control, string tabTitle, TabLocation tabLocation)
    {
        return mainPanelControl.AddWinformsControl(control, tabTitle, tabLocation);
    }

    public bool IsTabVisible(PluginTab pluginTab)
    {
        return mainPanelControl.IsTabVisible(pluginTab);
    }


    public void RemoveControl(System.Windows.Controls.UserControl control)
    {
        mainPanelControl.RemoveWpfControl(control);
    }

    /// <summary>
    /// Selects the tab which contains the argument control
    /// </summary>
    /// <param name="control">The control to show.</param>
    /// <returns>Whether the control was shown. If the control is not found, false is returned.</returns>
    public bool ShowTabForControl(System.Windows.Controls.UserControl control)
    {
        return mainPanelControl.ShowTabForControl(control);
    }


    internal bool IsTabFocused(PluginTab pluginTab) =>
                    mainPanelControl.IsTabFocused(pluginTab);

    #endregion

    #region Move to Cursor

    public void PositionWindowByCursor(System.Windows.Forms.Form window)
    {
        var mousePosition = GumCommands.Self.GuiCommands.GetMousePosition();

        window.Location = new System.Drawing.Point(mousePosition.X - window.Width / 2, mousePosition.Y - window.Height / 2);
    }

    public void MoveToCursor(System.Windows.Window window)
    {
        window.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;

        double width = window.Width;
        if (double.IsNaN(width))
        {
            width = 0;
        }
        double height = window.Height;
        if (double.IsNaN(height))
        {
            // Let's just assume some small height so it doesn't appear down below the cursor:
            //height = 0;
            height = 64;
        }

        var scaledX = MainWindow.LogicalToDeviceUnits(System.Windows.Forms.Control.MousePosition.X);

        var source = System.Windows.PresentationSource.FromVisual(mainPanelControl);


        double mousePositionX = Control.MousePosition.X;
        double mousePositionY = Control.MousePosition.Y;

        if (source != null)
        {
            mousePositionX /= source.CompositionTarget.TransformToDevice.M11;
            mousePositionY /= source.CompositionTarget.TransformToDevice.M22;
        }

        window.Left = mousePositionX - width / 2;
        window.Top = mousePositionY - height / 2;

        window.ShiftWindowOntoScreen();
    }
    #endregion

    public void PrintOutput(string output)
    {
        DoOnUiThread(() => OutputManager.Self.AddOutput(output));
    }

    #region Show/Hide Tools

    public System.Drawing.Point GetMousePosition()
    {
        return MainWindow.MousePosition;
    }

    public void HideTools()
    {
        mainPanelControl.HideTools();
    }

    public void ShowTools()
    {
        mainPanelControl.ShowTools();
    }


    public void ToggleToolVisibility()
    {
        //var areToolsVisible = mMainWindow.LeftAndEverythingContainer.Panel1Collapsed == false;

        //if(areToolsVisible)
        //{
        //    HideTools();
        //}
        //else
        //{
        //    ShowTools();
        //}
    }


    #endregion

    internal void FocusSearch()
    {
        PluginManager.Self.FocusSearch();
    }

    #region Show General Messages

    public void ShowMessage(string message, string caption = "")
    {
        MessageBox.Show(message, caption);
    }

    public System.Windows.MessageBoxResult ShowYesNoMessageBox(string message, string caption = "Confirm", Action yesAction = null, Action noAction = null)
    {
        caption ??= "Confirm";
        var result = System.Windows.MessageBoxResult.None;

        result = System.Windows.MessageBox.Show(message, caption, System.Windows.MessageBoxButton.YesNo);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            yesAction?.Invoke();
        }
        else if (result == System.Windows.MessageBoxResult.No)
        {
            noAction?.Invoke();
        }

        return result;
    }

    #endregion

    #region Show Add XXX Widows
    public void ShowAddVariableWindow()
    {
        var canShow = _selectedState.SelectedBehavior != null || _selectedState.SelectedElement != null;

        /////////////// Early Out///////////////
        if (!canShow)
        {
            return;
        }
        //////////////End Early Out/////////////
        var vm = Locator.GetRequiredService<AddVariableViewModel>();
        vm.RenameType = RenameType.NormalName;
        vm.Element = _selectedState.SelectedElement;
        vm.Variable = null;

        var window = new AddVariableWindow(vm);

        var result = window.ShowDialog();

        if (result == true)
        {
            vm.AddVariableToSelectedItem();
        }
    }

    public void ShowAddCategoryWindow()
    {

        var target = _selectedState.SelectedStateContainer;
        if (target == null)
        {
            MessageBox.Show("You must first select an element or behavior to add a state category");
        }
        else
        {
            var tiw = new CustomizableTextInputWindow();
            tiw.Message = "Enter new category name:";
            tiw.Title = "New category";

            var canAdd = true;

            var result = tiw.ShowDialog();

            if (result != true)
            {
                canAdd = false;
            }

            string name = null;

            if (canAdd)
            {
                name = tiw.Result;

                // see if any base elements have thsi category
                if (target is ElementSave element)
                {
                    var existingCategory = element.GetStateSaveCategoryRecursively(name, out ElementSave categoryContainer);

                    if (existingCategory != null)
                    {
                        MessageBox.Show($"Cannot add category - a category with the name {name} is already defined in {categoryContainer}");
                        canAdd = false;
                    }
                }
            }


            if (canAdd)
            {
                using var undoLock = UndoManager.Self.RequestLock();
                StateSaveCategory category = ElementCommands.Self.AddCategory(
                    target, name);

                _selectedState.SelectedStateCategorySave = category;
            }
        }

    }

    public void ShowAddStateWindow()
    {
        if (_selectedState.SelectedStateCategorySave == null && _selectedState.SelectedElement == null)
        {
            MessageBox.Show("You must first select an element or a behavior category to add a state");
        }
        else
        {
            var tiw = new CustomizableTextInputWindow();
            tiw.Message = "Enter new state name:";
            tiw.Title = "Add state";

            if (tiw.ShowDialog() == true)
            {
                string name = tiw.Result;

                if (!NameVerifier.Self.IsStateNameValid(name, _selectedState.SelectedStateCategorySave, null, out string whyNotValid))
                {
                    GumCommands.Self.GuiCommands.ShowMessage(whyNotValid);
                }
                else
                {
                    using (UndoManager.Self.RequestLock())
                    {
                        StateSave stateSave = ElementCommands.Self.AddState(
                            _selectedState.SelectedStateContainer, _selectedState.SelectedStateCategorySave, name);


                        _selectedState.SelectedStateSave = stateSave;

                    }
                }
            }
        }
    }

    public void ShowAddFolderWindow(TreeNode node)
    {
        var tiw = new CustomizableTextInputWindow();
        tiw.Message = "Enter new folder name:";
        tiw.Title = "Add folder";

        var errorLabel = new System.Windows.Controls.TextBlock();
        errorLabel.TextWrapping = System.Windows.TextWrapping.Wrap;
        tiw.AddControl(errorLabel);

        tiw.TextEntered += (_,_) =>
        {
            var text = tiw.Result;

            string whyNotValid;
            if(!NameVerifier.Self.IsFolderNameValid(text, out whyNotValid))
            {
                errorLabel.Foreground = System.Windows.Media.Brushes.Red;
                errorLabel.Text = whyNotValid;
            }
            else if(text?.Contains(' ') == true)
            {
                errorLabel.Foreground = System.Windows.Media.Brushes.Orange;
                errorLabel.Text = "Folders with spaces are not recommended since they can break variable references";
            }
            else
            {
                errorLabel.Text = "";
            }

        };

        var dialogResult = tiw.ShowDialog();

        if (dialogResult == true)
        {
            string folderName = tiw.Result;

            string whyNotValid;

            if (!NameVerifier.Self.IsFolderNameValid(folderName, out whyNotValid))
            {
                MessageBox.Show(whyNotValid);
            }
            else
            {
                TreeNode parentTreeNode = node;

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

    public void ShowAddScreenWindow()
    {
        if (ObjectFinder.Self.GumProjectSave == null || string.IsNullOrEmpty(ProjectManager.Self.GumProjectSave.FullFileName))
        {
            MessageBox.Show("You must first save a project before adding Screens");
        }
        else
        {
            TextInputWindow tiw = new();
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
                    var nodeToAddTo = _selectedState.SelectedTreeNode;

                    while (nodeToAddTo != null && nodeToAddTo.Tag is ScreenSave && nodeToAddTo.Parent != null)
                    {
                        nodeToAddTo = nodeToAddTo.Parent;
                    }

                    string? path = nodeToAddTo?.GetFullFilePath().FullPath;

                    if (nodeToAddTo == null || !nodeToAddTo.IsPartOfScreensFolderStructure())
                    {
                        path = GumState.Self.ProjectState.ScreenFilePath.FullPath;
                    }


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


    public void ShowAddInstanceWindow()
    {
        if (!ShowNewObjectDialog(out var name)) return;

        var focusedInstance = _selectedState.SelectedInstance;
        var newInstance = GumCommands.Self.ProjectCommands.ElementCommands.AddInstance(_selectedState.SelectedElement, name, StandardElementsManager.Self.DefaultType);

        if (focusedInstance != null)
        {
            SetInstanceParent(_selectedState.SelectedElement, newInstance, focusedInstance);
        }
    }

    public void ShowAddParentInstance()
    {
        if (!ShowNewObjectDialog(out var name)) return;

        var focusedInstance = _selectedState.SelectedInstance;
        var newInstance = GumCommands.Self.ProjectCommands.ElementCommands.AddInstance(
            _selectedState.SelectedElement, name, StandardElementsManager.Self.DefaultType);

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


    #endregion


    public Spinner ShowSpinner()
    {
        var spinner = new Gum.Controls.Spinner();
        spinner.Show();

        return spinner;
    }

    public void ShowRenameFolderWindow(TreeNode node)
    {
        var tiw = new TextInputWindow();
        tiw.Message = "Enter new folder name:";
        tiw.Title = "Rename folder";
        tiw.Result = node.Text;
        var dialogResult = tiw.ShowDialog();

        if (dialogResult != DialogResult.OK || tiw.Result == node.Text)
        {
            return;
        }


        bool isValid = true;
        string whyNotValid;
        if (!NameVerifier.Self.IsFolderNameValid(tiw.Result, out whyNotValid))
        {
            isValid = false;
        }


        // see if it already exists:
        FilePath newFullPath = FileManager.GetDirectory(node.GetFullFilePath().FullPath) + tiw.Result + "\\";

        if (System.IO.Directory.Exists(newFullPath.FullPath))
        {
            whyNotValid = $"Folder {tiw.Result} already exists.";
            isValid = false;
        }

        if (!isValid)
        {
            MessageBox.Show(whyNotValid);
        }
        else
        {
            string rootForElement;
            if (node.IsScreensFolderTreeNode())
            {
                rootForElement = FileLocations.Self.ScreensFolder;
            }
            else if (node.IsComponentsFolderTreeNode())
            {
                rootForElement = FileLocations.Self.ComponentsFolder;
            }
            else
            {
                throw new InvalidOperationException();
            }

            var oldFullPath = node.GetFullFilePath();

            string oldPathRelativeToElementsRoot = FileManager.MakeRelative(node.GetFullFilePath().FullPath, rootForElement, preserveCase: true);
            node.Text = tiw.Result;
            string newPathRelativeToElementsRoot = FileManager.MakeRelative(node.GetFullFilePath().FullPath, rootForElement, preserveCase: true);

            if (node.IsScreensFolderTreeNode())
            {
                foreach (var screen in ProjectState.Self.GumProjectSave.Screens)
                {
                    if (screen.Name.StartsWith(oldPathRelativeToElementsRoot))
                    {
                        string oldVaue = screen.Name;
                        string newName = newPathRelativeToElementsRoot + screen.Name.Substring(oldPathRelativeToElementsRoot.Length);

                        screen.Name = newName;
                        RenameLogic.HandleRename(screen, (InstanceSave)null, oldVaue, NameChangeAction.Move, askAboutRename: false);
                    }
                }
            }
            else if (node.IsComponentsFolderTreeNode())
            {
                foreach (var component in ProjectState.Self.GumProjectSave.Components)
                {
                    if (component.Name.ToLowerInvariant().StartsWith(oldPathRelativeToElementsRoot.ToLowerInvariant()))
                    {
                        string oldVaue = component.Name;
                        string newName = newPathRelativeToElementsRoot + component.Name.Substring(oldPathRelativeToElementsRoot.Length);
                        component.Name = newName;

                        RenameLogic.HandleRename(component, (InstanceSave)null, oldVaue, NameChangeAction.Move, askAboutRename: false);
                    }
                }
            }

            try
            {
                Directory.Move(oldFullPath.FullPath, newFullPath.FullPath);
                GumCommands.Self.GuiCommands.RefreshElementTreeView();
            }
            catch (Exception e)
            {
                var message = "Could not move the old folder." +
                    $" Additional information: \n{e}";
                MessageBox.Show(message);
            }
        }
    }

    public void ShowRenameElementWindow(ElementSave element)
    {
        var oldName = element.Name;

        var window = new CustomizableTextInputWindow();
        window.Title = "Enter new name:";
        window.Message = string.Empty;
        window.HighlightText();

        string folder = "";
        string rootElementName = element.Name;
        if(element.Name.Contains("/"))
        {
            folder = Path.GetDirectoryName(element.Name).Replace("\\", "/") + "/";
            rootElementName = Path.GetFileName(element.Name);
        }

        window.Result = rootElementName;
        if(!string.IsNullOrEmpty(folder))
        {
            var label = new System.Windows.Controls.Label();
            label.Content = folder;
            window.AddControl(label, ControlLocation.LeftOfTextBox);
        }


        var dialogResult = window.ShowDialog();

        if(dialogResult == true)
        {
            element.Name = folder + window.Result;
            SetVariableLogic.Self.PropertyValueChanged("Name", 
                oldName, 
                null,
                element.DefaultState,
                refresh: true,
                recordUndo: true,
                trySave: true);
        }
    }

    public void ShowRenameInstanceWidow(InstanceSave instance)
    {
        var oldName = instance.Name;

        var window = new CustomizableTextInputWindow();
        window.Title = "Enter new name:";
        window.Message = string.Empty;
        window.HighlightText();

        window.Result = oldName;

        var dialogResult = window.ShowDialog();

        if (dialogResult == true)
        {
            instance.Name = window.Result;
            SetVariableLogic.Self.PropertyValueChanged("Name", oldName, 
                instance,
                instance.ParentContainer?.DefaultState,
                refresh: true,
                recordUndo: true,
                trySave: true);
        }
    }

    public void DoOnUiThread(Action action)
    {
        mainPanelControl.Dispatcher.Invoke(action);
    }

}
