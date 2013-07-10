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

namespace Gum.Wireframe
{
    public partial class EditingManager
    {
        #region Fields

        static EditingManager mSelf;

        ResizeSide mSideGrabbed = ResizeSide.None;

        bool mHasChangedAnythingSinceLastPush = false;

        Text mDebugText;

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

        #endregion

        #region Methods

        public void Initialize(System.Windows.Forms.ContextMenuStrip contextMenuStrip)
        {

            mDebugText = new Text(null, "0, 0");
            TextManager.Self.Add(mDebugText);
            mDebugText.Y = -20;

            mDebugText.Visible = false;

            RightClickInitialize(contextMenuStrip);
        }


        public void Activity()
        {
            if (SelectedState.Self.SelectedElement != null)
            {
                PushActivity();

                ClickActivity();

                HandlesActivity();

                BodyGrabbingActivity();

                
            }
        }

        private void ClickActivity()
        {
            Cursor cursor = InputLibrary.Cursor.Self;

            if (cursor.PrimaryClick && mHasChangedAnythingSinceLastPush && ProjectManager.Self.GeneralSettingsFile.AutoSave)
            {
                ProjectManager.Self.SaveElement(SelectedState.Self.SelectedElement);
                mHasChangedAnythingSinceLastPush = false;
            }
        }

        private void PushActivity()
        {
            // The selected object is set in the SelectionManager

            Cursor cursor = InputLibrary.Cursor.Self;
            if (cursor.PrimaryPush)
            {
                mHasChangedAnythingSinceLastPush = false;
            }
        }

        private void BodyGrabbingActivity()
        {
            Cursor cursor = InputLibrary.Cursor.Self;
            if (SelectionManager.Self.IsOverBody && cursor.PrimaryDown)
            {
                float xToMoveBy = Cursor.Self.XChange / Renderer.Self.Camera.Zoom;
                float yToMoveBy = Cursor.Self.YChange / Renderer.Self.Camera.Zoom;

                MoveSelectedObjectsBy(xToMoveBy, yToMoveBy);
            }
        }

        public void MoveSelectedObjectsBy(float xToMoveBy, float yToMoveBy)
        {
            // This can get called either by
            // click+drag or by nudge (and who
            // knows, maybe other parts of the code
            // in the future), so we should make sure
            // that something is really selected.
            if (SelectionManager.Self.HasSelection)
            {
                bool hasChangeOccurred = false;

                if (SelectedState.Self.SelectedInstances.Count() == 0 && SelectedState.Self.SelectedComponent != null)
                {
                    if (xToMoveBy != 0)
                    {
                        hasChangeOccurred = true;
                        float value = ModifyVariable("X", xToMoveBy, SelectedState.Self.SelectedComponent);
                    }
                    if (yToMoveBy != 0)
                    {
                        hasChangeOccurred = true;
                        float value = ModifyVariable("Y", yToMoveBy, SelectedState.Self.SelectedComponent);
                    }
                }
                else
                {
                    foreach (InstanceSave instance in SelectedState.Self.SelectedInstances)
                    {
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

                if (hasChangeOccurred)
                {
                    UpdateSelectedObjectsPositionAndDimensions();

                    PropertyGridManager.Self.RefreshUI();
                    // I don't think we need to do this anymore, the SelectionManager handles its own updates automatically
                    //SelectionManager.Self.ShowSizeHandlesFor(WireframeObjectManager.Self.GetSelectedRepresentation());
                    mHasChangedAnythingSinceLastPush = true;
                }
            }
        }

        public void UpdateSelectedObjectsPositionAndDimensions()
        {

            if (SelectedState.Self.SelectedInstances.GetCount() != 0)
            {
                foreach (var instance in SelectedState.Self.SelectedInstances)
                {
                    RefreshPositionsAndScalesForInstance(instance);

                    //WireframeObjectManager.Self.SetInstanceIpsoDimensionsAndPositions(ipso,
                    //    SelectedState.Self.SelectedInstance,
                    //    SelectedState.Self.SelectedElement,
                    //    WireframeObjectManager.Self.GetRepresentation(SelectedState.Self.SelectedElement));
                }
            }
            else
            {
                IPositionedSizedObject ipso = WireframeObjectManager.Self.GetSelectedRepresentation();

                if (ipso != null)
                {
                    ElementSave elementSave = SelectedState.Self.SelectedElement;

                    StateSave stateSave = new StateSave();
                    RecursiveVariableFinder rvf = new RecursiveVariableFinder(elementSave.DefaultState);
                    WireframeObjectManager.Self.FillStateWithVariables(rvf, stateSave, WireframeObjectManager.Self.PositionAndSizeVariables);

                    WireframeObjectManager.Self.SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(ipso, elementSave, stateSave);
                }
                else if(SelectedState.Self.SelectedElement != null)
                {
                    foreach (var instance in SelectedState.Self.SelectedElement.Instances)
                    {
                        RefreshPositionsAndScalesForInstance(instance);
                    }
                }
                //WireframeObjectManager.Self.SetElementIpsoDimensionsAndPositions(
                //    ipso,
                //    SelectedState.Self.SelectedElement);
            }


            WireframeObjectManager.Self.UpdateScalesAndPositionsForSelectedChildren();
        }

        public void RefreshPositionsAndScalesForInstance(InstanceSave instance)
        {
            IPositionedSizedObject ipso = WireframeObjectManager.Self.GetRepresentation(instance);
            StateSave stateSave = new StateSave();

            RecursiveVariableFinder rvf = new RecursiveVariableFinder(instance, SelectedState.Self.SelectedElement);
            WireframeObjectManager.Self.FillStateWithVariables(rvf, stateSave, WireframeObjectManager.Self.PositionAndSizeVariables);
            WireframeObjectManager.Self.SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(ipso, SelectedState.Self.SelectedElement, stateSave);
        }

        private void HandlesActivity()
        {
            Cursor cursor = InputLibrary.Cursor.Self;

            if (cursor.PrimaryPush)
            {
                mSideGrabbed = SelectionManager.Self.SideOver;
            }
            if (cursor.PrimaryDown)
            {
                SideGrabbingActivity();
            }
            if (cursor.PrimaryClick)
            {
                mSideGrabbed = ResizeSide.None;
            }
        }

        private void SideGrabbingActivity()
        {
            float cursorXChange = Cursor.Self.XChange / Renderer.Self.Camera.Zoom;
            float cursorYChange = Cursor.Self.YChange / Renderer.Self.Camera.Zoom;

            ////////////////////////////////EARLY OUT//////////////////////////////////////
            if (cursorXChange == 0 && cursorYChange == 0)
            {
                return;
            }
            //////////////////////////////END EARLY OUT////////////////////////////////////

            bool hasChangeOccurred = false;

            if (SelectionManager.Self.HasSelection && SelectedState.Self.SelectedInstances.Count() == 0)
            {
                // That means we have the entire component selected
                hasChangeOccurred |= SideGrabbingActivityForInstanceSave(cursorXChange, cursorYChange, null);
            }

            foreach (InstanceSave save in SelectedState.Self.SelectedInstances)
            {
                hasChangeOccurred |= SideGrabbingActivityForInstanceSave(cursorXChange, cursorYChange, save);
            }

            if (hasChangeOccurred)
            {

                UpdateSelectedObjectsPositionAndDimensions();

                PropertyGridManager.Self.RefreshUI();

                // I don't think we need this anymore because they're updated automatically in SelectionManager
                //SelectionManager.Self.ShowSizeHandlesFor(WireframeObjectManager.Self.GetSelectedRepresentation());
                mHasChangedAnythingSinceLastPush = true;
            }
        }

        private bool SideGrabbingActivityForInstanceSave(float cursorXChange, float cursorYChange, InstanceSave instanceSave)
        {
            bool hasChangeOccurred = false;

            float changeXMultiplier = 0;
            float changeYMultiplier = 0;
            float widthMultiplier = 0;
            float heightMultiplier = 0;

            IPositionedSizedObject ipso = WireframeObjectManager.Self.GetRepresentation(instanceSave);
            if (ipso == null)
            {
                ipso = WireframeObjectManager.Self.GetRepresentation(SelectedState.Self.SelectedElement);
            }

            switch (this.mSideGrabbed)
            {
                case ResizeSide.TopLeft:
                    changeXMultiplier = GetXMultiplierForLeft(instanceSave, ipso);
                    widthMultiplier = -1;
                    changeYMultiplier = GetYMultiplierForTop(instanceSave, ipso);
                    heightMultiplier = -1;
                    break;
                case ResizeSide.Top:
                    changeYMultiplier = GetYMultiplierForTop(instanceSave, ipso);
                    heightMultiplier = -1;
                    break;
                case ResizeSide.TopRight:
                    changeXMultiplier = GetXMultiplierForRight(instanceSave, ipso);
                    widthMultiplier = 1;
                    changeYMultiplier = GetYMultiplierForTop(instanceSave, ipso);
                    heightMultiplier = -1;
                    break;
                case ResizeSide.Right:
                    changeXMultiplier = GetXMultiplierForRight(instanceSave, ipso);
                    widthMultiplier = 1;
                    break;
                case ResizeSide.BottomRight:
                    changeXMultiplier = GetXMultiplierForRight(instanceSave, ipso);

                    changeYMultiplier = GetYMultiplierForBottom(instanceSave, ipso);
                    widthMultiplier = 1;
                    heightMultiplier = 1;
                    break;
                case ResizeSide.Bottom:
                    heightMultiplier = 1;
                    changeYMultiplier = GetYMultiplierForBottom(instanceSave, ipso);

                    break;
                case ResizeSide.BottomLeft:
                    changeYMultiplier = GetYMultiplierForBottom(instanceSave, ipso);
                    changeXMultiplier = GetXMultiplierForLeft(instanceSave, ipso);
                    widthMultiplier = -1;
                    heightMultiplier = 1;
                    break;
                case ResizeSide.Left:
                    changeXMultiplier = GetXMultiplierForLeft(instanceSave, ipso);
                    widthMultiplier = -1;
                    break;
            }

            widthMultiplier *= (ipso.Width / SelectionManager.Self.ResizeHandles.Width);
            heightMultiplier *= (ipso.Height / SelectionManager.Self.ResizeHandles.Height);



            if (changeXMultiplier != 0 && cursorXChange != 0)
            {
                hasChangeOccurred = true;
                float value = ModifyVariable("X", cursorXChange * changeXMultiplier, instanceSave);
            }
            if (changeYMultiplier != 0 && cursorYChange != 0)
            {
                hasChangeOccurred = true;
                float value = ModifyVariable("Y", cursorYChange * changeYMultiplier, instanceSave);
            }
            if (heightMultiplier != 0 && cursorYChange != 0)
            {
                hasChangeOccurred = true;
                float value = ModifyVariable("Height", cursorYChange * heightMultiplier, instanceSave);
            }
            if (widthMultiplier != 0 && cursorXChange != 0)
            {
                hasChangeOccurred = true;
                float value = ModifyVariable("Width", cursorXChange * widthMultiplier, instanceSave);
            }
            return hasChangeOccurred;
        }

        private static float GetRatioXOverInSelection(IPositionedSizedObject ipso, HorizontalAlignment horizontalAlignment)
        {
            float handleLeft = SelectionManager.Self.ResizeHandles.X;
            float handleWidth = SelectionManager.Self.ResizeHandles.Width;

            float ipsoXToUse = ipso.GetAbsoluteX();

            if (horizontalAlignment == HorizontalAlignment.Center)
            {
                ipsoXToUse += ipso.Width / 2.0f;
            }
            else if (horizontalAlignment == HorizontalAlignment.Right)
            {
                ipsoXToUse += ipso.Width;
            }

            return (ipsoXToUse - handleLeft) / handleWidth;
        }

        private static float GetRatioYDownInSelection(IPositionedSizedObject ipso, VerticalAlignment verticalAlignment)
        {
            float handleTop = SelectionManager.Self.ResizeHandles.Y;
            float handleHeight = SelectionManager.Self.ResizeHandles.Height;
            float ipsoYToUse = ipso.GetAbsoluteY();
            if (verticalAlignment == VerticalAlignment.Center)
            {
                ipsoYToUse += ipso.Height / 2.0f;
            }
            if (verticalAlignment == VerticalAlignment.Bottom)
            {
                ipsoYToUse += ipso.Height;
            }

            return (ipsoYToUse - handleTop) / handleHeight;
        }

        private float GetXMultiplierForLeft(InstanceSave instanceSave, IPositionedSizedObject ipso)
        {
            object xOriginAsObject = GetCurrentValueForVariable("X Origin", instanceSave);

            HorizontalAlignment xOrigin = (HorizontalAlignment)xOriginAsObject;

            float ratioOver = GetRatioXOverInSelection(ipso, xOrigin);
            return 1 - ratioOver;
        }



        private float GetYMultiplierForTop(InstanceSave instanceSave, IPositionedSizedObject ipso)
        {
            object yOriginAsObject = GetCurrentValueForVariable("Y Origin", instanceSave);

            VerticalAlignment yOrigin = (VerticalAlignment)yOriginAsObject;

            float ratioOver = GetRatioYDownInSelection(ipso, yOrigin);
            return 1 - ratioOver;
        }

        private float GetYMultiplierForBottom(InstanceSave instanceSave, IPositionedSizedObject ipso)
        {
            object yOriginAsObject = GetCurrentValueForVariable("Y Origin", instanceSave);

            VerticalAlignment yOrigin = (VerticalAlignment)yOriginAsObject;

            float ratioOver = GetRatioYDownInSelection(ipso, yOrigin);

            return 0 + ratioOver;
        }

        private float GetXMultiplierForRight(InstanceSave instanceSave, IPositionedSizedObject ipso)
        {
            object xOriginAsObject = GetCurrentValueForVariable("X Origin", instanceSave);
            if (!(xOriginAsObject is HorizontalAlignment))
            {
                int m = 3;
            }
            HorizontalAlignment xOrigin = (HorizontalAlignment)xOriginAsObject;
            float ratioOver = GetRatioXOverInSelection(ipso, xOrigin);

            return 0 + ratioOver;
        }

        private float ModifyVariable(string baseVariableName, float modificationAmount, InstanceSave instanceSave)
        {

            string nameWithInstance;
            object currentValueAsObject;
            GetCurrentValueForVariable(baseVariableName, instanceSave, out nameWithInstance, out currentValueAsObject);

            float currentValue = (float)currentValueAsObject;

            string unitsVariableName = baseVariableName + " Units";
            string unitsNameWithInstance;
            object unitsVariableAsObject;
            GetCurrentValueForVariable(unitsVariableName, instanceSave, out unitsNameWithInstance, out unitsVariableAsObject);

            currentValue = AdjustCurrentValueIfScale(currentValue, baseVariableName, unitsVariableAsObject);

            modificationAmount = AdjustAmountAccordingToUnitType(baseVariableName, modificationAmount, unitsVariableAsObject);

            float newValue = currentValue + modificationAmount;
            SelectedState.Self.SelectedStateSave.SetValue(nameWithInstance, newValue, instanceSave);
            return newValue;
        }

        private float ModifyVariable(string baseVariableName, float modificationAmount, ComponentSave componentSave)
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
            SelectedState.Self.SelectedStateSave.SetValue(baseVariableName, newValue, null);
            return newValue;
        }

        /// <summary>
        /// This method gets the actual width/height value of an object if the stored value is 0.  This is used for Sprites
        /// which have width/height of 0 if they let the Texture determine their size.
        /// </summary>
        /// <param name="currentValue">The value as stored in the StateSave - may be 0</param>
        /// <param name="baseVariableName">The base name - like Height</param>
        /// <param name="unitsValue">The units value - </param>
        /// <returns></returns>
        private float AdjustCurrentValueIfScale(float currentValue, string baseVariableName, object unitsValue)
        {
            if (currentValue == 0 && (baseVariableName == "Width" || baseVariableName == "Height"))
            {
                IPositionedSizedObject selectedIpso = SelectionManager.Self.SelectedIpso;

                if ((DimensionUnitType)unitsValue == DimensionUnitType.Absolute)
                {

                    if (baseVariableName == "Width")
                    {
                        currentValue = selectedIpso.Width;
                    }
                    else
                    {
                        currentValue = selectedIpso.Height;
                    }
                }
                else
                {
                    float parentValue;
                    
                    // need to support percentage based width
                    if (baseVariableName == "Width")
                    {
                        if (selectedIpso.Parent == null)
                        {
                            parentValue = ObjectFinder.Self.GumProjectSave.DefaultCanvasWidth;
                        }
                        else
                        {
                            parentValue = selectedIpso.Parent.Width;
                        }
                        currentValue = 100 * selectedIpso.Width/parentValue;
                    }
                    else
                    {
                        if (selectedIpso.Parent == null)
                        {
                            parentValue = ObjectFinder.Self.GumProjectSave.DefaultCanvasHeight;
                        }
                        else
                        {
                            parentValue = selectedIpso.Parent.Height;
                        }

                        currentValue = 100 * selectedIpso.Height / parentValue;
                    }

                }

            }
            return currentValue;
        }

        private static float AdjustAmountAccordingToUnitType(string baseVariableName, float amount, object unitsVariableAsObject)
        {
            GeneralUnitType generalUnitType = UnitConverter.Self.ConvertToGeneralUnit(unitsVariableAsObject);

            if (generalUnitType == GeneralUnitType.Percentage)
            {
                IPositionedSizedObject ipso = WireframeObjectManager.Self.GetSelectedRepresentation();
                float parentDimension = 0;

                if (ipso.Parent != null)
                {
                    if (baseVariableName == "X" || baseVariableName == "Width")
                    {
                        parentDimension = ipso.Parent.Width;
                    }
                    else
                    {
                        parentDimension = ipso.Parent.Height;
                    }
                }
                else
                {
                    if (baseVariableName == "X" || baseVariableName == "Width")
                    {
                        parentDimension = ProjectManager.Self.GumProjectSave.DefaultCanvasWidth;
                    }
                    else
                    {
                        parentDimension = ProjectManager.Self.GumProjectSave.DefaultCanvasHeight;
                    }
                }

                amount = 100 * amount / parentDimension;

            }
            return amount;
        }

        public static object GetCurrentValueForVariable(string baseVariableName, InstanceSave instanceSave)
        {
            string throwaway;
            object currentValueAsObject;
            GetCurrentValueForVariable(baseVariableName, instanceSave, out throwaway, out currentValueAsObject);
            return currentValueAsObject;
        }

        private static object GetCurrentValueForVariable(string baseVariableName, InstanceSave instanceSave, out string nameWithInstance, out object currentValue)
        {
            nameWithInstance = baseVariableName;


            if (instanceSave != null)
            {
                nameWithInstance = instanceSave.Name + "." + baseVariableName;
                currentValue = SelectedState.Self.SelectedStateSave.GetValueRecursive(nameWithInstance);
            }
            else
            {
                currentValue = SelectedState.Self.SelectedStateSave.GetValueRecursive(nameWithInstance);
            }

            return currentValue;
        }

        #endregion
    }
}
