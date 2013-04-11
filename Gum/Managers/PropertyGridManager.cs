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

namespace Gum.Managers
{
    public partial class PropertyGridManager
    {
        #region Fields

        PropertyGrid mPropertyGrid;

        static PropertyGridManager mPropertyGridManager;

        ElementSaveDisplayer mPropertyGridDisplayer = new ElementSaveDisplayer();

        ToolStripMenuItem mExposeVariable;
        ToolStripMenuItem mResetToDefault;
        ToolStripMenuItem mUnExposeVariable;

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
                return this.mPropertyGrid.SelectedGridItem.Label;
            }
        }

        #endregion


        public void Initialize(PropertyGrid propertyGrid)
        {
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
            }
            else
            {
                mPropertyGrid.Visible = true;

                mPropertyGrid.SelectedObject = mPropertyGridDisplayer;
                mPropertyGrid.Refresh();
            }
        }

        internal void PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {

            string changedMember = e.ChangedItem.PropertyDescriptor.Name;
            object oldValue = e.OldValue;
            object selectedObject = SelectedState.Self.SelectedStateSave;

            
            // We used to suppress
            // saving - not sure why.
            //bool saveProject = true;

            if (selectedObject is StateSave)
            {
                ElementSave parentElement = ((StateSave)selectedObject).ParentContainer;
                InstanceSave instance = SelectedState.Self.SelectedInstance;


                // Why do we do this before reacting to names?  I think we want to do it after
                //ElementTreeViewManager.Self.RefreshUI();

                ReactIfChangedMemberIsName(parentElement, instance, changedMember, oldValue);

                ReactIfChangedMemberIsBaseType(parentElement, changedMember, oldValue);

                ReactIfChangedMemberIsFont(parentElement, changedMember, oldValue);

                ReactIfChangedMemberIsUnitType(parentElement, changedMember, oldValue);

                ReactIfChangedMemberIsTexture(parentElement, changedMember, oldValue);

                PluginManager.Self.VariableSet(parentElement, changedMember, oldValue);



                // This used to be above the React methods but
                // we probably want to referesh the UI after everything
                // else has changed, don't we?
                ElementTreeViewManager.Self.RefreshUI();
            }


            // Save the change
            if (SelectedState.Self.SelectedElement != null)
            {
                ProjectManager.Self.SaveElement(SelectedState.Self.SelectedElement);
            }
                
            ProjectManager.Self.SaveProject();


            // Inefficient but let's do this for now - we can make it more efficient later
            WireframeObjectManager.Self.RefreshAll(true);
            SelectionManager.Self.Refresh();
        }

        private void ReactIfChangedMemberIsUnitType(ElementSave parentElement, string changedMember, object oldValue)
        {
            StateSave stateSave = SelectedState.Self.SelectedStateSave;

            IPositionedSizedObject currentIpso =
                WireframeObjectManager.Self.GetSelectedRepresentation();

            float parentWidth = ObjectFinder.Self.GumProjectSave.DefaultCanvasWidth;
            float parentHeight = ObjectFinder.Self.GumProjectSave.DefaultCanvasHeight;

            if (currentIpso != null && currentIpso.Parent != null)
            {
                parentWidth = currentIpso.Parent.Width;
                parentHeight = currentIpso.Parent.Height;
            }

            float outX = 0;
            float outY = 0;
            float valueToSet = 0;
            string variableToSet = null;

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
                    xOrY = XOrY.X;

                }
                else if (changedMember == "Height Units")
                {
                    variableToSet = "Height";
                    xOrY = XOrY.Y;
                }



                float valueOnObject = (float)stateSave.GetValueRecursive(GetQualifiedName(variableToSet));

                if (xOrY == XOrY.X)
                {
                    UnitConverter.Self.ConvertToPixelCoordinates(
                        valueOnObject, 0, oldValue, null, parentWidth, parentHeight, out outX, out outY);

                    UnitConverter.Self.ConvertToUnitTypeCoordinates(
                        outX, outY, unitType, null, parentWidth, parentHeight, out valueToSet, out outY);
                }
                else
                {
                    UnitConverter.Self.ConvertToPixelCoordinates(
                        0, valueOnObject, null, oldValue, parentWidth, parentHeight, out outX, out outY);

                    UnitConverter.Self.ConvertToUnitTypeCoordinates(
                        outX, outY, null, unitType, parentWidth, parentHeight, out outX, out valueToSet);
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
            bool shouldReset = false;

            if (SelectedState.Self.SelectedInstance != null)
            {
                variableName = SelectedState.Self.SelectedInstance.Name + "." + variableName;

                shouldReset = true;
            }


            if (shouldReset)
            {
                StateSave state = SelectedState.Self.SelectedStateSave;
                bool wasChangeMade = false;
                VariableSave variable = state.GetVariableSave(variableName);
                if (variable != null)
                {
                    state.Variables.Remove(variable);

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
                    ProjectManager.Self.SaveElement(SelectedState.Self.SelectedElement);
                }
            }
        }
    }
}
