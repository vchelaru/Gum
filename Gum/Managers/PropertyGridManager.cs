using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Gum.ToolStates;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.PropertyGridHelpers;
using Gum.Wireframe;
using RenderingLibrary.Graphics.Fonts;
using CommonFormsAndControls;
using Gum.ToolCommands;
using RenderingLibrary;
using Gum.Converters;
using Gum.Plugins;
using Gum.RenderingLibrary;
using RenderingLibrary.Content;
using WpfDataUi.DataTypes;
using Gum.DataTypes.ComponentModel;

namespace Gum.Managers
{
    public partial class PropertyGridManager
    {
        #region Fields

        PropertyGrid mPropertyGrid;
        WpfDataUi.DataUiGrid mDataGrid;

        static PropertyGridManager mPropertyGridManager;

        ElementSaveDisplayer mPropertyGridDisplayer = new ElementSaveDisplayer();

        ToolStripMenuItem mExposeVariable;
        ToolStripMenuItem mResetToDefault;
        ToolStripMenuItem mUnExposeVariable;

        ElementSave mLastElement;
        InstanceSave mLastInstance;
        StateSave mLastState;


        #endregion

        #region Properties

        public static PropertyGridManager Self
        {
            get
            {
                if (mPropertyGridManager == null)
                {
                    mPropertyGridManager = new PropertyGridManager();
                }
                return mPropertyGridManager;
            }
        }


        public string SelectedLabel
        {
            get
            {
                if (this.mPropertyGrid.SelectedGridItem != null)
                {
                    return this.mPropertyGrid.SelectedGridItem.Label;
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion


        public void Initialize(PropertyGrid propertyGrid, WpfDataUi.DataUiGrid wpfDataUiGrid)
        {
            mDataGrid = wpfDataUiGrid;

            mPropertyGrid = propertyGrid;
            mPropertyGrid.PropertySort = PropertySort.Categorized;

            InitializeRightClickMenu();
        }

        public void RefreshUI()
        {
            if (SelectedState.Self.SelectedInstances.GetCount() > 1)
            {
                // I don't know if we want to eventually show these
                // but for now we'll hide the PropertyGrid:
                mPropertyGrid.Visible = false;
                mDataGrid.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                mPropertyGrid.Visible = true;

                //mPropertyGrid.SelectedObject = mPropertyGridDisplayer;
                //mPropertyGrid.Refresh();

                var element = SelectedState.Self.SelectedElement;
                var state = SelectedState.Self.SelectedStateSave;
                var instance = SelectedState.Self.SelectedInstance;

                bool shouldMakeYellow = element != null && state != element.DefaultState;

                if (shouldMakeYellow)
                {
                    mPropertyGrid.LineColor = System.Drawing.Color.Orange;
                }
                else
                {
                    mPropertyGrid.LineColor = System.Drawing.Color.FromArgb(244, 247, 252);
                }

                mDataGrid.Visibility = System.Windows.Visibility.Visible;

                bool hasChanged = element != mLastElement || instance != mLastInstance || state != mLastState;
                if (hasChanged)
                {
                    mLastElement = element;
                    mLastState = state;
                    mLastInstance = instance;

                    var stateSave = SelectedState.Self.SelectedStateSave;
                    mDataGrid.Instance = stateSave;
                    if (stateSave != null)
                    {
                        mDataGrid.Categories.Clear();
                        var properties = mPropertyGridDisplayer.GetProperties(null);
                        foreach (InstanceSavePropertyDescriptor propertyDescriptor in properties)
                        {
                            StateReferencingInstanceMember srim;
                            if (instance != null)
                            {
                                srim =
                                    new StateReferencingInstanceMember(propertyDescriptor, stateSave, instance.Name + "." + propertyDescriptor.Name, instance, element);
                            }
                            else
                            {
                                srim =
                                    new StateReferencingInstanceMember(propertyDescriptor, stateSave, propertyDescriptor.Name, instance, element);
                            }

                            srim.SetToDefault += ResetVariableToDefault;

                            string category = propertyDescriptor.Category.Trim();

                            var categoryToAddTo = mDataGrid.Categories.FirstOrDefault(item => item.Name == category);

                            if (categoryToAddTo == null)
                            {
                                categoryToAddTo = new MemberCategory(category);
                                mDataGrid.Categories.Add(categoryToAddTo);
                            }

                            categoryToAddTo.Members.Add(srim);

                        }

                        MemberCategory categoryToMove = mDataGrid.Categories.FirstOrDefault(item => item.Name == "Position");
                        if (categoryToMove != null)
                        {
                            mDataGrid.Categories.Remove(categoryToMove);
                            mDataGrid.Categories.Insert(1, categoryToMove);
                        }

                        categoryToMove = mDataGrid.Categories.FirstOrDefault(item => item.Name == "Dimensions");
                        if (categoryToMove != null)
                        {
                            mDataGrid.Categories.Remove(categoryToMove);
                            mDataGrid.Categories.Insert(2, categoryToMove);
                        }
                    }

                    mDataGrid.Refresh();
                }
                else
                {
                    mDataGrid.Refresh();
                }
            }
        }

        internal void PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {

            string changedMember = e.ChangedItem.PropertyDescriptor.Name;
            object oldValue = e.OldValue;



            PropertyValueChanged(changedMember, oldValue);
        }

        public void PropertyValueChanged(string changedMember, object oldValue)
        {
            object selectedObject = SelectedState.Self.SelectedStateSave;

            // We used to suppress
            // saving - not sure why.
            //bool saveProject = true;

            if (selectedObject is StateSave)
            {
                ElementSave parentElement = ((StateSave)selectedObject).ParentContainer;
                InstanceSave instance = SelectedState.Self.SelectedInstance;

                if (instance != null)
                {
                    SelectedState.Self.SelectedVariableSave = SelectedState.Self.SelectedStateSave.GetVariableSave(instance.Name + "." + changedMember);
                }
                else
                {
                    SelectedState.Self.SelectedVariableSave = SelectedState.Self.SelectedStateSave.GetVariableSave(changedMember);
                }
                // Why do we do this before reacting to names?  I think we want to do it after
                //ElementTreeViewManager.Self.RefreshUI();

                ReactIfChangedMemberIsName(parentElement, instance, changedMember, oldValue);

                ReactIfChangedMemberIsBaseType(parentElement, changedMember, oldValue);

                ReactIfChangedMemberIsFont(parentElement, changedMember, oldValue);

                ReactIfChangedMemberIsUnitType(parentElement, changedMember, oldValue);

                ReactIfChangedMemberIsTexture(parentElement, changedMember, oldValue);

                ReactIfChangedMemberIsCustomTextureCoordinates(parentElement, changedMember, oldValue);

                ReactIfChangedMemberIsParent(parentElement, changedMember, oldValue);

                PluginManager.Self.VariableSet(parentElement, instance, changedMember, oldValue);



                // This used to be above the React methods but
                // we probably want to referesh the UI after everything
                // else has changed, don't we?
                // I think this code makes things REALLY slow - we only want to refresh one of the tree nodes:
                //ElementTreeViewManager.Self.RefreshUI();
                ElementTreeViewManager.Self.RefreshUI(SelectedState.Self.SelectedElement);
            }


            // Save the change
            if (SelectedState.Self.SelectedElement != null)
            {
                GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
            }


            // Inefficient but let's do this for now - we can make it more efficient later
            WireframeObjectManager.Self.RefreshAll(true);
            SelectionManager.Self.Refresh();
        }

        private void ReactIfChangedMemberIsParent(ElementSave parentElement, string changedMember, object oldValue)
        {
            VariableSave variable = SelectedState.Self.SelectedVariableSave;
            // Eventually need to handle tunneled variables
            if (variable != null && changedMember == "Parent")
            {
                if ((variable.Value as string) == "<NONE>")
                {
                    variable.Value = null;
                }

            }
        }

        private void ReactIfChangedMemberIsCustomTextureCoordinates(ElementSave parentElement, string changedMember, object oldValue)
        {
            if (changedMember == "Custom Texture Coordinates")
            {
                RecursiveVariableFinder rvf;

                var instance = SelectedState.Self.SelectedInstance;
                if (instance != null)
                {
                    rvf = new RecursiveVariableFinder(SelectedState.Self.SelectedInstance, parentElement);
                }
                else
                {
                    rvf = new RecursiveVariableFinder(parentElement.DefaultState);
                }

                if(rvf.GetValue<bool>("Custom Texture Coordinates"))
                {
                    string sourceFile = rvf.GetValue<string>("SourceFile");

                    if (!string.IsNullOrEmpty(sourceFile))
                    {
                        string absolute = ProjectManager.Self.MakeAbsoluteIfNecessary(sourceFile);

                        if (System.IO.File.Exists(absolute))
                        {
                            var texture = LoaderManager.Self.Load(absolute, null);

                            if (texture != null)
                            {
                                if (instance != null)
                                {
                                    parentElement.DefaultState.SetValue(instance.Name + ".Texture Top", 0);
                                    parentElement.DefaultState.SetValue(instance.Name + ".Texture Left", 0);
                                    parentElement.DefaultState.SetValue(instance.Name + ".Texture Width", texture.Width);
                                    parentElement.DefaultState.SetValue(instance.Name + ".Texture Height", texture.Height);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ReactIfChangedMemberIsUnitType(ElementSave parentElement, string changedMember, object oldValue)
        {
            StateSave stateSave = SelectedState.Self.SelectedStateSave;

            IPositionedSizedObject currentIpso =
                WireframeObjectManager.Self.GetSelectedRepresentation();

            float parentWidth = ObjectFinder.Self.GumProjectSave.DefaultCanvasWidth;
            float parentHeight = ObjectFinder.Self.GumProjectSave.DefaultCanvasHeight;

            float fileWidth = 0;
            float fileHeight = 0;

            if (currentIpso != null)
            {
                currentIpso.GetFileWidthAndHeight(out fileWidth, out fileHeight);
                if (currentIpso.Parent != null)
                {
                    parentWidth = currentIpso.Parent.Width;
                    parentHeight = currentIpso.Parent.Height;
                }
            }


            float outX = 0;
            float outY = 0;
            float valueToSet = 0;
            string variableToSet = null;

            bool isWidthOrHeight = false;

            bool wasAnythingSet = false;

            if (changedMember == "X Units" || changedMember == "Y Units" || changedMember == "Width Units" || changedMember == "Height Units")
            {
                object unitType = EditingManager.GetCurrentValueForVariable(changedMember, SelectedState.Self.SelectedInstance);
                XOrY xOrY = XOrY.X;
                if (changedMember == "X Units")
                {
                    variableToSet = "X";
                    xOrY = XOrY.X;
                }
                else if (changedMember == "Y Units")
                {
                    variableToSet = "Y";
                    xOrY = XOrY.Y;
                }
                else if (changedMember == "Width Units")
                {
                    variableToSet = "Width";
                    isWidthOrHeight = true;
                    xOrY = XOrY.X;

                }
                else if (changedMember == "Height Units")
                {
                    variableToSet = "Height";
                    isWidthOrHeight = true;
                    xOrY = XOrY.Y;
                }



                float valueOnObject = (float)stateSave.GetValueRecursive(GetQualifiedName(variableToSet));

                if (xOrY == XOrY.X)
                {
                    UnitConverter.Self.ConvertToPixelCoordinates(
                        valueOnObject, 0, oldValue, null, parentWidth, parentHeight, fileWidth, fileHeight, out outX, out outY);

                    if (isWidthOrHeight && outX == 0)
                    {
                        outX = fileWidth;
                    }

                    UnitConverter.Self.ConvertToUnitTypeCoordinates(
                        outX, outY, unitType, null, parentWidth, parentHeight, fileWidth, fileHeight, out valueToSet, out outY);
                }
                else
                {
                    UnitConverter.Self.ConvertToPixelCoordinates(
                        0, valueOnObject, null, oldValue, parentWidth, parentHeight, fileWidth, fileHeight, out outX, out outY);

                    if (isWidthOrHeight && outY == 0)
                    {
                        outY = fileHeight;
                    }

                    UnitConverter.Self.ConvertToUnitTypeCoordinates(
                        outX, outY, null, unitType, parentWidth, parentHeight, fileWidth, fileHeight, out outX, out valueToSet);
                }
                wasAnythingSet = true;

            }

            if (wasAnythingSet)
            {
                InstanceSave instanceSave = SelectedState.Self.SelectedInstance;
                if (SelectedState.Self.SelectedInstance != null)
                {
                    variableToSet = SelectedState.Self.SelectedInstance.Name + "." + variableToSet;
                }

                stateSave.SetValue(variableToSet, valueToSet, instanceSave);

            }


        }

        private void ReactIfChangedMemberIsTexture(ElementSave parentElement, string changedMember, object oldValue)
        {
            VariableSave variable = SelectedState.Self.SelectedVariableSave;
            // Eventually need to handle tunneled variables
            if (variable != null && variable.GetRootName() == "SourceFile")
            {
                StateSave stateSave = SelectedState.Self.SelectedStateSave;

                RecursiveVariableFinder rvf = new RecursiveVariableFinder(stateSave);

                stateSave.SetValue("AnimationFrames", new List<string>());

            }

        }

        string GetQualifiedName(string variableName)
        {
            if (SelectedState.Self.SelectedInstance != null)
            {
                return SelectedState.Self.SelectedInstance.Name + "." + variableName;
            }
            else
            {
                return variableName;
            }
        }

        private void ReactIfChangedMemberIsFont(ElementSave parentElement, string changedMember, object oldValue)
        {
            if (changedMember == "Font" || changedMember == "FontSize")
            {
                StateSave stateSave = SelectedState.Self.SelectedStateSave;


                string prefix = "";
                if (SelectedState.Self.SelectedInstance != null)
                {
                    prefix = SelectedState.Self.SelectedInstance.Name + ".";
                }

                object fontSizeAsObject = stateSave.GetValueRecursive(prefix + "FontSize");

                BmfcSave.CreateBitmapFontFilesIfNecessary(
                    (int)fontSizeAsObject,
                    (string)stateSave.GetValueRecursive(prefix + "Font"));
            }
        }

        //private void ReactIfChangedMemberIsAnimation(ElementSave parentElement, string changedMember, object oldValue, out bool saveProject)
        //{
        //    const string sourceFileString = "SourceFile";
        //    if (changedMember == sourceFileString)
        //    {
        //        StateSave stateSave = SelectedState.Self.SelectedStateSave;

        //        string value = (string)stateSave.GetValueRecursive(sourceFileString);

        //        if (!FileManager.IsRelative)
        //        {

        //        }

        //        saveProject = true;
        //    }

        //    saveProject = false;
        //}

        private static void ReactIfChangedMemberIsName(ElementSave container, InstanceSave instance, string changedMember, object oldValue)
        {
            if (changedMember == "Name")
            {
                RenameManager.Self.HandleRename(container, instance, (string)oldValue);
                
            }
        }

        private static void ReactIfChangedMemberIsBaseType(object s, string changedMember, object oldValue)
        {
            if (changedMember == "Base Type")
            {
                ElementSave asElementSave = s as ElementSave;

                asElementSave.ReactToChangedBaseType(SelectedState.Self.SelectedInstance, oldValue.ToString());
            }

        }



        public void ResetSelectedValueToDefault()
        {
            GridItem gridItem = mPropertyGrid.SelectedGridItem;
            string variableName = gridItem.Label;

            ResetVariableToDefault(variableName);
        }

        private void ResetVariableToDefault(string variableName)
        {
            bool shouldReset = false;

            if (SelectedState.Self.SelectedInstance != null)
            {
                variableName = SelectedState.Self.SelectedInstance.Name + "." + variableName;

                shouldReset = true;
            }
            else if (SelectedState.Self.SelectedElement != null &&
                // Don't let the user reset standard element variables, they have to have some actual value
                (SelectedState.Self.SelectedElement is StandardElementSave) == false)
            {
                variableName = variableName;
                shouldReset = true;
            }


            if (shouldReset)
            {
                StateSave state = SelectedState.Self.SelectedStateSave;
                bool wasChangeMade = false;
                VariableSave variable = state.GetVariableSave(variableName);
                if (variable != null)
                {
                    // Don't remove the variable if it's part of an element - we still want it there
                    // so it can be set, we just don't want it to set a value
                    // Update August 13, 2013
                    // Actually, we do want to remove it if it's part of an element but not the
                    // default state
                    if (SelectedState.Self.SelectedInstance != null ||
                        SelectedState.Self.SelectedStateSave != SelectedState.Self.SelectedElement.DefaultState)
                    {
                        state.Variables.Remove(variable);
                    }
                    else
                    {
                        variable.Value = null;
                    }
                    variable.SetsValue = false; // just to be safe

                    wasChangeMade = true;
                    // We need to refresh the property grid and the wireframe display

                }
                else
                {
                    // Maybe this is a variable list?
                    VariableListSave variableList = state.GetVariableListSave(variableName);
                    if (variableList != null)
                    {
                        state.VariableLists.Remove(variableList);

                        // We don't support this yet:
                        // variableList.SetsValue = false; // just to be safe
                        wasChangeMade = true;
                    }
                }

                if (wasChangeMade)
                {
                    RefreshUI();
                    WireframeObjectManager.Self.RefreshAll(true);
                    SelectionManager.Self.Refresh();
                    if (ProjectManager.Self.GeneralSettingsFile.AutoSave)
                    {
                        ProjectManager.Self.SaveElement(SelectedState.Self.SelectedElement);
                    }
                }
            }
        }
    }
}
