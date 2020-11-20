using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InputLibrary;
using Gum.Managers;
using Gum.ToolStates;
using Gum.DataTypes;
using Gum.ToolCommands;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Gum.DataTypes.Variables;
using Gum.Converters;
using Gum.Events;
using Gum.Input;
using Gum.RenderingLibrary;
using RenderingLibrary.Math;
using Microsoft.Xna.Framework;
using Gum.PropertyGridHelpers;
using Gum.Plugins;

namespace Gum.Wireframe
{
    public partial class EditingManager
    {
        #region Fields

        static EditingManager mSelf;



        #endregion

        #region Properties

        public static EditingManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new EditingManager();
                }
                return mSelf;
            }
        }

        public bool RestrictToUnitValues
        {
            set
            {
                SelectionManager.Self.RestrictToUnitValues = value;
            }
        }

        #endregion

        #region Methods

        public void Initialize(System.Windows.Forms.ContextMenuStrip contextMenuStrip)
        {
            RightClickInitialize(contextMenuStrip);

            GumEvents.Self.InstanceSelected +=
                () =>
                {
                    SelectionManager.Self.WireframeEditor?.UpdateAspectRatioForGrabbedIpso();
                };
        }



        public void Activity()
        {

        }

        bool GetIsShiftDown()
        {
            return InputLibrary.Keyboard.Self.KeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) ||
                    InputLibrary.Keyboard.Self.KeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift);
        }



        public bool ShouldSkipDraggingMovementOn(InstanceSave instanceSave)
        {
            ElementWithState element = new ElementWithState(SelectedState.Self.SelectedElement);

            List<ElementWithState> stack = new List<ElementWithState>() { element };

            var selectedInstances = SelectedState.Self.SelectedInstances;

            bool shouldSkip = false;
            // Make sure this isn't attached to another instance
            var representation = WireframeObjectManager.Self.GetRepresentation(instanceSave, stack);

            if (representation != null && representation.Parent != null)
            {
                var parentRepresentation = representation.Parent;

                if (parentRepresentation != null)
                {
                    var parentInstance = WireframeObjectManager.Self.GetInstance(parentRepresentation, InstanceFetchType.InstanceInCurrentElement, stack);

                    if (selectedInstances.Contains(parentInstance))
                    {
                        shouldSkip = true;
                    }
                }
            }

            return shouldSkip;
        }





        public bool MoveSelectedObjectsBy(float xToMoveBy, float yToMoveBy)
        {
            bool hasChangeOccurred = false;
            // This can get called either by
            // click+drag or by nudge (and who
            // knows, maybe other parts of the code
            // in the future), so we should make sure
            // that something is really selected.
            if (SelectionManager.Self.HasSelection)
            {


                Cursor cursor = InputLibrary.Cursor.Self;


                if (SelectedState.Self.SelectedInstances.Count() == 0 && 
                    (SelectedState.Self.SelectedComponent != null || SelectedState.Self.SelectedStandardElement != null))
                {
                    if (xToMoveBy != 0)
                    {
                        hasChangeOccurred = true;
                        ModifyVariable("X", xToMoveBy, SelectedState.Self.SelectedElement);
                    }
                    if (yToMoveBy != 0)
                    {
                        hasChangeOccurred = true;
                        ModifyVariable("Y", yToMoveBy, SelectedState.Self.SelectedElement);
                    }

                }
                else
                {
                    var selectedInstances = SelectedState.Self.SelectedInstances;

                    foreach (InstanceSave instance in selectedInstances)
                    {
                        bool shouldSkip = ShouldSkipDraggingMovementOn(instance);

                        if (!shouldSkip)
                        {
                            // This could prevent a double-layout by locking layout until all values have been set
                            if (xToMoveBy != 0)
                            {
                                hasChangeOccurred = true;
                                float value = ModifyVariable("X", xToMoveBy, instance);
                            }
                            if (yToMoveBy != 0)
                            {
                                hasChangeOccurred = true;
                                float value = ModifyVariable("Y", yToMoveBy, instance);
                            }
                        }
                    }
                }

                if (hasChangeOccurred)
                {
                    //UpdateSelectedObjectsPositionAndDimensions();

                    PropertyGridManager.Self.RefreshUI();
                }
            }

            return hasChangeOccurred;
        }



        //public void UpdateSelectedObjectsPositionAndDimensions()
        //{
        //    var elementStack = SelectedState.Self.GetTopLevelElementStack();
        //    if (SelectedState.Self.SelectedInstances.GetCount() != 0)
        //    {
        //        //// Can we just update layout it?
        //        //foreach (var instance in SelectedState.Self.SelectedInstances)
        //        //{
        //        //    RefreshPositionsAndScalesForInstance(instance, elementStack);
        //        //}

        //        foreach (var ipso in SelectedState.Self.SelectedIpsos)
        //        {
        //            GraphicalUiElement asGue = ipso as GraphicalUiElement;
        //            if (asGue != null)
        //            {
        //                asGue.UpdateLayout();
        //                //RecursiveVariableFinder rvf = new RecursiveVariableFinder(asGue.Tag as InstanceSave, SelectedState.Self.SelectedElement);
        //                //asGue.SetGueValues(rvf);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        GraphicalUiElement ipso = WireframeObjectManager.Self.GetSelectedRepresentation();

        //        if (ipso != null)
        //        {
        //            ElementSave elementSave = SelectedState.Self.SelectedElement;

        //            var state = elementSave.DefaultState;
        //            if(SelectedState.Self.SelectedStateSave != null)
        //            {
        //                state = SelectedState.Self.SelectedStateSave;
        //            }
        //            RecursiveVariableFinder rvf = new RecursiveVariableFinder(state);
        //            (ipso as GraphicalUiElement).SetGueValues(rvf);
        //        }
        //        else if(SelectedState.Self.SelectedElement != null)
        //        {
        //            foreach (var instance in SelectedState.Self.SelectedElement.Instances)
        //            {
        //                RefreshPositionsAndScalesForInstance(instance, elementStack);
        //            }
        //        }
        //    }

        //    GuiCommands.Self.RefreshWireframe();
        //}

        public void RefreshPositionsAndScalesForInstance(InstanceSave instance, List<ElementWithState> elementStack)
        {
            GraphicalUiElement ipso = WireframeObjectManager.Self.GetRepresentation(instance, elementStack);
            ipso.UpdateLayout();
        }

        public float ModifyVariable(string baseVariableName, float modificationAmount, InstanceSave instanceSave)
        {

            string nameWithInstance;
            object currentValueAsObject;
            GetCurrentValueForVariable(baseVariableName, instanceSave, out nameWithInstance, out currentValueAsObject);

            bool shouldContinue = true;

            if(SelectedState.Self.CustomCurrentStateSave != null && currentValueAsObject == null)
            {
                // This is okay, we will do nothing here:
                shouldContinue = false;
            }

            if (shouldContinue)
            {
                var graphicalUiElement = WireframeObjectManager.Self.GetRepresentation(instanceSave, null);

                float currentValue = (float)currentValueAsObject;

                string unitsVariableName = baseVariableName + " Units";
                string unitsNameWithInstance;
                object unitsVariableAsObject;
                GetCurrentValueForVariable(unitsVariableName, instanceSave, out unitsNameWithInstance, out unitsVariableAsObject);

                if (float.IsPositiveInfinity(modificationAmount))
                {
                    throw new InvalidOperationException("Cannot be infinite");
                }

                modificationAmount = AdjustAmountAccordingToUnitType(baseVariableName, modificationAmount, unitsVariableAsObject);

                if(float.IsPositiveInfinity(modificationAmount))
                {
                    throw new InvalidOperationException("Cannot be infinite");
                }

                if(graphicalUiElement?.GetAbsoluteFlipHorizontal() == true && baseVariableName == "X")
                {
                    modificationAmount *= -1;
                }

                float newValue = currentValue + modificationAmount;
                SelectedState.Self.SelectedStateSave.SetValue(nameWithInstance, newValue, instanceSave, "float");


                graphicalUiElement.SetProperty(baseVariableName, newValue);

                VariableInCategoryPropagationLogic.Self.PropagateVariablesInCategory(nameWithInstance);


                return newValue;
            }
            else
            {
                return 0;
            }
        }

        public float ModifyVariable(string baseVariableName, float modificationAmount, ElementSave elementSave)
        {
            object currentValueAsObject;
            currentValueAsObject = GetCurrentValueForVariable(baseVariableName, null);

            float currentValue = (float)currentValueAsObject;
            string unitsVariableName = baseVariableName + " Units";
            string unitsNameWithInstance;
            object unitsVariableAsObject;
            GetCurrentValueForVariable(unitsVariableName, null, out unitsNameWithInstance, out unitsVariableAsObject);

            modificationAmount = AdjustAmountAccordingToUnitType(baseVariableName, modificationAmount, unitsVariableAsObject);

            float newValue = currentValue + modificationAmount;
            SelectedState.Self.SelectedStateSave.SetValue(baseVariableName, newValue, null, "float");

            
            var ipso = WireframeObjectManager.Self.GetRepresentation(elementSave);
            ipso.SetProperty(baseVariableName, newValue);

            VariableInCategoryPropagationLogic.Self.PropagateVariablesInCategory(baseVariableName);

            return newValue;
        }

        private static float AdjustAmountAccordingToUnitType(string baseVariableName, float amount, object unitsVariableAsObject)
        {
            GeneralUnitType generalUnitType = UnitConverter.ConvertToGeneralUnit(unitsVariableAsObject);

            float xAmount;
            float yAmount;

            if(baseVariableName == "X" || baseVariableName == "Width")
            {
                xAmount = amount;
                yAmount = 0;
            }
            else
            {
                xAmount = 0;
                yAmount = amount;
            }

            if(generalUnitType == GeneralUnitType.PixelsFromMiddleInverted)
            {
                return amount * -1;
            }
            else if (generalUnitType != GeneralUnitType.PixelsFromLarge && 
                generalUnitType != GeneralUnitType.PixelsFromMiddle && 
                generalUnitType != GeneralUnitType.PixelsFromSmall && 
                generalUnitType != GeneralUnitType.PixelsFromBaseline)
            {

                float parentWidth;
                float parentHeight;
                float fileWidth;
                float fileHeight;
                float outX;
                float outY;


                var ipso = WireframeObjectManager.Self.GetSelectedRepresentation();
                ipso.GetFileWidthAndHeightOrDefault(out fileWidth, out fileHeight);
                ipso.GetParentWidthAndHeight(
                    ProjectManager.Self.GumProjectSave.DefaultCanvasWidth, ProjectManager.Self.GumProjectSave.DefaultCanvasHeight,
                    out parentWidth, out parentHeight);

                var unitsVariable = UnitConverter.ConvertToGeneralUnit(unitsVariableAsObject);

                UnitConverter.Self.ConvertToUnitTypeCoordinates(xAmount, yAmount, unitsVariable, unitsVariable, 
                    ipso.Width, ipso.Height, 
                    parentWidth, parentHeight, 
                    fileWidth, fileHeight,
                    out outX, out outY);

                if(generalUnitType == GeneralUnitType.PercentageOfFile)
                {
                    // need to amplify the value based on the ratio of what is displayed to the file size
                    if(baseVariableName == "Width")
                    {
                        var ratio = ipso.TextureWidth / fileWidth;

                        if(float.IsPositiveInfinity(ratio) == false && ratio != 0)
                        {
                            outX /= ratio;
                        }
                    }
                    if(baseVariableName == "Height")
                    {
                        var ratio = ipso.TextureHeight / fileHeight;
                        if (float.IsPositiveInfinity(ratio) == false && ratio != 0)
                        {
                            outY /= ratio;
                        }
                    }
                }

                if (baseVariableName == "X" || baseVariableName == "Width")
                {
                    return outX;
                }
                else
                {
                    return outY;
                }
            }
            else
            {
                return amount;
            }
        }

        public static object GetCurrentValueForVariable(string baseVariableName, InstanceSave instanceSave)
        {
            string throwaway;
            object currentValueAsObject;
            GetCurrentValueForVariable(baseVariableName, instanceSave, out throwaway, out currentValueAsObject);
            return currentValueAsObject;
        }

        /// <summary>
        /// Returns the current value for a variable, considering inheritance and states. It returns the "effective" value of the variable.
        /// This value is in the current object's units.
        /// </summary>
        /// <param name="baseVariableName"></param>
        /// <param name="instanceSave"></param>
        /// <param name="nameWithInstance"></param>
        /// <param name="currentValue"></param>
        /// <returns></returns>
        private static object GetCurrentValueForVariable(string baseVariableName, InstanceSave instanceSave, out string nameWithInstance, out object currentValue)
        {
            nameWithInstance = baseVariableName;

            currentValue = null;

            if (SelectedState.Self.SelectedStateSave != null)
            {
                if (instanceSave != null)
                {
                    nameWithInstance = instanceSave.Name + "." + baseVariableName;
                    currentValue = SelectedState.Self.SelectedStateSave.GetValueRecursive(nameWithInstance);
                }
                else
                {
                    currentValue = SelectedState.Self.SelectedStateSave.GetValueRecursive(nameWithInstance);
                }
            }

            return currentValue;
        }

        #endregion
    }
}
