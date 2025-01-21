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

namespace Gum.Commands
{
    public class GuiCommands
    {
        #region FieldsProperties

        public MainWindow MainWindow { get; private set; }

        MainPanelControl mainPanelControl;

        #endregion

        internal void Initialize(MainWindow mainWindow, MainPanelControl mainPanelControl)
        {
            this.MainWindow = mainWindow;
            this.mainPanelControl = mainPanelControl;
        }

        internal void BroadcastRefreshBehaviorView()
        {
            PluginManager.Self.RefreshBehaviorView(
                SelectedState.Self.SelectedElement);
        }

        internal void BroadcastBehaviorReferencesChanged()
        {
            PluginManager.Self.BehaviorReferencesChanged(
                SelectedState.Self.SelectedElement);
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
            ElementTreeViewManager.Self.RefreshUi();
        }

        public void RefreshElementTreeView(IInstanceContainer instanceContainer)
        {
            ElementTreeViewManager.Self.RefreshUi(instanceContainer);
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

        #endregion

        #region Move to Cursor

        public void PositionWindowByCursor(System.Windows.Window window)
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
                height = 0;
            }

            var mousePosition = GumCommands.Self.GuiCommands.GetMousePosition();
            window.Left = mousePosition.X - width / 2;
            window.Top = mousePosition.Y - height / 2;
        }

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
            OutputManager.Self.AddOutput(output);
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


        internal void ToggleToolVisibility()
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
            ElementTreeViewManager.Self.FocusSearch();
        }

        #region Show General Messages

        public void ShowMessage(string message)
        {
            MessageBox.Show(message);
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

        public void ShowAddVariableWindow()
        {
            var canShow = SelectedState.Self.SelectedBehavior != null || SelectedState.Self.SelectedElement != null;

            /////////////// Early Out///////////////
            if (!canShow)
            {
                return;
            }
            //////////////End Early Out/////////////
            var host = Builder.App;
            var services = host.Services;

            var vm = services.GetRequiredService<AddVariableViewModel>();
            vm.RenameType = RenameType.NormalName;
            vm.Element = SelectedState.Self.SelectedElement;
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

            var target = SelectedState.Self.SelectedStateContainer as IStateCategoryListContainer;
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
                    using (UndoManager.Self.RequestLock())
                    {
                        StateSaveCategory category = ElementCommands.Self.AddCategory(
                            target, name);

                        SelectedState.Self.SelectedStateCategorySave = category;
                    }
                }
            }

        }

        public void ShowAddStateWindow()
        {
            if (SelectedState.Self.SelectedStateCategorySave == null && SelectedState.Self.SelectedElement == null)
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

                    if (!NameVerifier.Self.IsStateNameValid(name, SelectedState.Self.SelectedStateCategorySave, null, out string whyNotValid))
                    {
                        GumCommands.Self.GuiCommands.ShowMessage(whyNotValid);
                    }
                    else
                    {
                        using (UndoManager.Self.RequestLock())
                        {
                            StateSave stateSave = ElementCommands.Self.AddState(
                                SelectedState.Self.SelectedStateContainer, SelectedState.Self.SelectedStateCategorySave, name);


                            SelectedState.Self.SelectedStateSave = stateSave;

                        }
                    }
                }
            }
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
            window.Result = oldName;
            window.Message = "Enter new name:";
            window.HighlightText();
            var dialogResult = window.ShowDialog();

            if(dialogResult == true)
            {
                element.Name = window.Result;
                SetVariableLogic.Self.PropertyValueChanged("Name", oldName, null, refresh: true,
                    recordUndo: true,
                    trySave: true);
            }
        }

        public void DoOnUiThread(Action action)
        {
            mainPanelControl.Dispatcher.Invoke(action);
        }
    }
}
