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

namespace Gum.Commands
{
    public class GuiCommands
    {
        FlowLayoutPanel mFlowLayoutPanel;

        MainPanelControl mainPanelControl;

        internal void Initialize(MainWindow mainWindow, MainPanelControl mainPanelControl)
        {
            this.mainPanelControl = mainPanelControl;
            mFlowLayoutPanel = mainWindow.ToolbarPanel;
        }

        internal void RefreshStateTreeView()
        {
            StateTreeViewManager.Self.RefreshUI(SelectedState.Self.SelectedElement);
        }

        public void RefreshPropertyGrid(bool force = false)
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

        public PluginTab AddControl(System.Windows.FrameworkElement control, string tabTitle, TabLocation tabLocation = TabLocation.CenterBottom)
        {
            CheckForInitialization();
            return mainPanelControl.AddWpfControl(control, tabTitle, tabLocation);
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
        

        public void ShowMessage(string message)
        {
            MessageBox.Show(message);
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
                    GumCommands.Self.GuiCommands.RefreshPropertyGrid(force: true);

                }
            }
        }

        public void ShowEditVariableWindow(VariableSave variable)
        {

            var window = new AddVariableWindow();

            window.SelectedType = variable.Type;
            window.EnteredName = variable.Name;

            var result = window.ShowDialog();

            if (result == true)
            {
                var type = window.SelectedType;
                if (type == null)
                {
                    throw new InvalidOperationException("Type cannot be null");
                }
                var newName = window.EnteredName;

                string whyNotValid;
                bool isValid = NameVerifier.Self.IsVariableNameValid(
                    newName, out whyNotValid);

                if (!isValid)
                {
                    MessageBox.Show(whyNotValid);
                }
                else
                {
                    var behavior = SelectedState.Self.SelectedBehavior;


                    if (behavior != null)
                    {
                        var changedType = variable.Type != type;
                        if(changedType)
                        {
                            // todo - need to fix this by converting?
                            variable.Value = null;
                        }
                        variable.Name = newName;
                        variable.Type = type;

                        GumCommands.Self.FileCommands.TryAutoSaveBehavior(behavior);
                    }
                    else if (SelectedState.Self.SelectedElement != null)
                    {
                        var oldName = variable.Name;
                        var element = SelectedState.Self.SelectedElement;
                        if(ApplyEditVariableOnElement(element, oldName, newName, type))
                        {
                            GumCommands.Self.FileCommands.TryAutoSaveElement(element);
                        }

                        ApplyChangesToInstances(element, oldName, newName, type);

                        var derivedElements = ObjectFinder.Self.GetElementsInheritingFrom(element);
                        foreach(var derived in derivedElements)
                        {
                            if(ApplyEditVariableOnElement(derived, oldName, newName, type))
                            {
                                GumCommands.Self.FileCommands.TryAutoSaveElement(derived);
                            }

                            ApplyChangesToInstances(derived, oldName, newName, type);
                        }
                    }
                    GumCommands.Self.GuiCommands.RefreshPropertyGrid(force: true);
                }
            }
        }

        private void ApplyChangesToInstances(ElementSave element, string oldName, string newName, string type)
        {
            var references = ObjectFinder.Self.GetElementReferences(element)
                .Where(item => item.ReferenceType == ReferenceType.InstanceOfType)
                .ToArray();

            ////////////////////////// Early Out ///////////////////////////
            if (references.Length == 0) return;
            /////////////////////// End Early Out /////////////////////////

            HashSet<ElementSave> elementsToSave = new HashSet<ElementSave>();

            foreach(var reference in references)
            {
                var instance = reference.ReferencingObject as InstanceSave;

                var oldFullName = instance.Name + "." + oldName;
                var newFullName = instance.Name + "." + newName;

                if(ApplyEditVariableOnElement(reference.OwnerOfReferencingObject, oldFullName, newFullName, type ))
                {
                    elementsToSave.Add(reference.OwnerOfReferencingObject);
                }
            }

            foreach(var elementToSave in elementsToSave)
            {
                GumCommands.Self.FileCommands.TryAutoSaveElement(elementToSave);
            }
        }

        private bool ApplyEditVariableOnElement(ElementSave element, string oldName, string newName, string type)
        {
            var changed = false;
            var allStates = element.AllStates;

            foreach(var state in allStates)
            {
                foreach(var variable in state.Variables)
                {
                    if(variable.Name == oldName)
                    {
                        variable.Name = newName;
                        if(variable.Type != type)
                        {
                            variable.Type = type;
                            // todo - convert:
                            variable.Value = null;
                        }
                        changed = true;
                    }
                }
            }



            return changed;
        }

        public void DoOnUiThread(Action action)
        {
            mainPanelControl.Dispatcher.Invoke(action);
        }

    }
}
