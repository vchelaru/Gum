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

namespace Gum.Commands
{
    public class GuiCommands
    {
        #region FieldsProperties
        FlowLayoutPanel mFlowLayoutPanel;

        MainPanelControl mainPanelControl;

        #endregion

        internal void Initialize(MainWindow mainWindow, MainPanelControl mainPanelControl)
        {
            this.mainPanelControl = mainPanelControl;
            mFlowLayoutPanel = mainWindow.ToolbarPanel;
        }

        internal void RefreshStateTreeView()
        {
            PluginManager.Self.RefreshStateTreeView();
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

        public void RefreshVariables(bool force = false)
        {
            PropertyGridManager.Self.RefreshUI(force:force);
        }

        /// <summary>
        /// Refreshes the displayed values without clearing and recreating the grid
        /// </summary>
        public void RefreshPropertyGridValues()
        {
            PropertyGridManager.Self.RefreshVariablesDataGridValues();
        }

        #region Tab Controls

        public PluginTab AddControl(System.Windows.FrameworkElement control, string tabTitle, TabLocation tabLocation = TabLocation.CenterBottom)
        {
            CheckForInitialization();
            return mainPanelControl.AddWpfControl(control, tabTitle, tabLocation);
        }

        public void ShowTab(PluginTab tab)
        {
            mainPanelControl.ShowTab(tab);
        }

        public void HideTab(PluginTab tab)
        {
            mainPanelControl.HideTab(tab);
        }

        public PluginTab AddControl(System.Windows.Forms.Control control, string tabTitle, TabLocation tabLocation )
        {
            CheckForInitialization();
            return mainPanelControl.AddWinformsControl(control, tabTitle, tabLocation);
        }

        private void CheckForInitialization()
        {
            if(mainPanelControl == null)
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

        #endregion
        
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

        public void PrintOutput(string output)
        {
            OutputManager.Self.AddOutput(output);
        }

        public void RefreshElementTreeView()
        {
            ElementTreeViewManager.Self.RefreshUi();
        }

        public void RefreshElementTreeView(ElementSave element) => ElementTreeViewManager.Self.RefreshUi(element);
        public void RefreshElementTreeView(BehaviorSave behavior) => ElementTreeViewManager.Self.RefreshUi(behavior);

        #region Show/Hide Methods

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

        internal void FocusSearch()
        {
            ElementTreeViewManager.Self.FocusSearch();
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

            var scaledX = mFlowLayoutPanel.LogicalToDeviceUnits(System.Windows.Forms.Control.MousePosition.X);

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

        public void ShowAddVariableWindow()
        {
            var canShow = SelectedState.Self.SelectedBehavior != null || SelectedState.Self.SelectedElement != null;

            /////////////// Early Out///////////////
            if (!canShow)
            {
                return;
            }
            //////////////End Early Out/////////////

            var window = new AddVariableWindow();

            var result = window.ShowDialog();

            if (result == true)
            {
                var type = window.SelectedType;
                if (type == null)
                {
                    throw new InvalidOperationException("Type cannot be null");
                }
                var name = window.EnteredName;

                string whyNotValid;
                bool isValid = NameVerifier.Self.IsVariableNameValid(
                    name, out whyNotValid);

                if (!isValid)
                {
                    MessageBox.Show(whyNotValid);
                }
                else
                {
                    var behavior = SelectedState.Self.SelectedBehavior;

                    var newVariable = new VariableSave();
                    
                    newVariable.Name = name;
                    newVariable.Type = type;
                    if (behavior != null)
                    {
                        behavior.RequiredVariables.Variables.Add(newVariable);
                        GumCommands.Self.FileCommands.TryAutoSaveBehavior(behavior);
                    }
                    else if (SelectedState.Self.SelectedElement != null)
                    {
                        var element = SelectedState.Self.SelectedElement;
                        newVariable.IsCustomVariable = true;
                        element.DefaultState.Variables.Add(newVariable);
                        GumCommands.Self.FileCommands.TryAutoSaveElement(element);
                    }
                    GumCommands.Self.GuiCommands.RefreshVariables(force: true);

                }
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

        public void DoOnUiThread(Action action)
        {
            mainPanelControl.Dispatcher.Invoke(action);
        }
    }
}
