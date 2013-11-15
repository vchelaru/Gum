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

namespace Gum.Wireframe
{
    public partial class EditingManager
    {
        #region Fields

        static EditingManager mSelf;

        ResizeSide mSideGrabbed = ResizeSide.None;
        bool mHasGrabbed = false;
        bool mHasChangedAnythingSinceLastPush = false;

        bool mHasMovedEnoughSincePush = false;
        float mXPush = 0;
        float mYPush = 0;

        float mAspectRatioOnGrab;
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
            RightClickInitialize(contextMenuStrip);

            GumEvents.Self.InstanceSelected += UpdateAspectRatioForGrabbedIpso;
        }

        private void UpdateAspectRatioForGrabbedIpso()
        {
            if (SelectedState.Self.SelectedInstance != null &&
                SelectedState.Self.SelectedIpso != null
                )
            {
                IPositionedSizedObject ipso = SelectedState.Self.SelectedIpso;

                float width = ipso.Width;
                float height = ipso.Height;

                if (height != 0)
                {
                    mAspectRatioOnGrab = width / height;
                }
            }
        }


        public void Activity()
        {
            if (SelectedState.Self.SelectedElement != null)
            {

                PushActivity();

                ClickActivity();

                CheckIfHasMovedEnough();

                HandlesActivity();

                BodyGrabbingActivity();

                
            }
        }

        private void CheckIfHasMovedEnough()
        {
            float pixelsToMoveBeforeApplying = 6;
            Cursor cursor = InputLibrary.Cursor.Self;

            if (cursor.PrimaryDown)
            {
                mHasMovedEnoughSincePush |=
                    Math.Abs(cursor.X - mXPush) > pixelsToMoveBeforeApplying ||
                    Math.Abs(cursor.Y - mYPush) > pixelsToMoveBeforeApplying;


            }
        }

        private void ClickActivity()
        {
            Cursor cursor = InputLibrary.Cursor.Self;

            if (cursor.PrimaryClick)
            {
                mHasGrabbed = false;
            }

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
                mXPush = cursor.X;
                mYPush = cursor.Y;
                mHasMovedEnoughSincePush = false;
                mHasGrabbed = SelectionManager.Self.HasSelection;

                if (mHasGrabbed)
                {
                    UpdateAspectRatioForGrabbedIpso();
                }
            }
        }

        private void BodyGrabbingActivity()
        {
            Cursor cursor = InputLibrary.Cursor.Self;
            if (SelectionManager.Self.IsOverBody && cursor.PrimaryDown && mHasGrabbed && mHasMovedEnoughSincePush)
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
                else if (SelectedState.Self.SelectedInstances.Count() == 0 && SelectedState.Self.SelectedStandardElement != null)
                {
                    if (xToMoveBy != 0)
                    {
                        hasChangeOccurred = true;
                        float value = ModifyVariable("X", xToMoveBy, SelectedState.Self.SelectedStandardElement);
                    }
                    if (yToMoveBy != 0)
                    {
                        hasChangeOccurred = true;
                        float value = ModifyVariable("Y", yToMoveBy, SelectedState.Self.SelectedStandardElement);
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
            var elementStack = SelectedState.Self.GetTopLevelElementStack();
            if (SelectedState.Self.SelectedInstances.GetCount() != 0)
            {
                foreach (var instance in SelectedState.Self.SelectedInstances)
                {
                    RefreshPositionsAndScalesForInstance(instance, elementStack);


                    
                    //WireframeObjectManager.Self.SetInstanceIpsoDimensionsAndPositions(ipso,
                    //    SelectedState.Self.SelectedInstance,
                    //    SelectedState.Self.SelectedElement,
                    //    WireframeObjectManager.Self.GetRepresentation(SelectedState.Self.SelectedElement));
                }

                foreach (var ipso in SelectedState.Self.SelectedIpsos)
                {
                    GraphicalUiElement asGue = ipso as GraphicalUiElement;
                    if (asGue != null)
                    {
                        RecursiveVariableFinder rvf = new RecursiveVariableFinder(asGue.Tag as InstanceSave, SelectedState.Self.SelectedElement);

                        WireframeObjectManager.Self.SetGueWidthAndPositionValues(asGue, rvf);
                        //public void SetGueWidthAndPositionValues(GraphicalUiElement gue, RecursiveVariableFinder rvf)
                        //asGue.UpdateLayout();
                    }
                }
            }
            else
            {
                GraphicalUiElement ipso = WireframeObjectManager.Self.GetSelectedRepresentation();

                if (ipso != null)
                {
                    ElementSave elementSave = SelectedState.Self.SelectedElement;

                    RecursiveVariableFinder rvf = new RecursiveVariableFinder(elementSave.DefaultState);

                    WireframeObjectManager.Self.SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(ipso, elementSave, rvf);

                    ipso.UpdateLayout();
                }
                else if(SelectedState.Self.SelectedElement != null)
                {
                    foreach (var instance in SelectedState.Self.SelectedElement.Instances)
                    {
                        RefreshPositionsAndScalesForInstance(instance, elementStack);
                    }
                }
                //WireframeObjectManager.Self.SetElementIpsoDimensionsAndPositions(
                //    ipso,
                //    SelectedState.Self.SelectedElement);
            }
        }

        public void RefreshPositionsAndScalesForInstance(InstanceSave instance, List<ElementWithState> elementStack)
        {
            IPositionedSizedObject ipso = WireframeObjectManager.Self.GetRepresentation(instance, elementStack);

            RecursiveVariableFinder rvf = new RecursiveVariableFinder(instance, SelectedState.Self.SelectedElement);
            WireframeObjectManager.Self.SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(ipso, SelectedState.Self.SelectedElement, rvf);
        }

        private void HandlesActivity()
        {
            Cursor cursor = InputLibrary.Cursor.Self;

            if (cursor.PrimaryPush)
            {
                mSideGrabbed = SelectionManager.Self.SideOver;
            }
            if (cursor.PrimaryDown && mHasMovedEnoughSincePush)
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
            var elementStack = SelectedState.Self.GetTopLevelElementStack();
            if (SelectionManager.Self.HasSelection && SelectedState.Self.SelectedInstances.Count() == 0)
            {
                // That means we have the entire component selected
                hasChangeOccurred |= SideGrabbingActivityForInstanceSave(cursorXChange, cursorYChange, null, elementStack);
            }

            foreach (InstanceSave save in SelectedState.Self.SelectedInstances)
            {
                hasChangeOccurred |= SideGrabbingActivityForInstanceSave(cursorXChange, cursorYChange, save, elementStack);
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

        private bool SideGrabbingActivityForInstanceSave(float cursorXChange, float cursorYChange, InstanceSave instanceSave, List<ElementWithState> elementStack)
        {
            float changeXMultiplier;
            float changeYMultiplier;
            float widthMultiplier;
            float heightMultiplier;
            CalculateMultipliers(instanceSave, elementStack, out changeXMultiplier, out changeYMultiplier, out widthMultiplier, out heightMultiplier);

            AdjustCursorChangeValuesForShiftDrag(ref cursorXChange, ref cursorYChange, instanceSave, elementStack);

            bool hasChangeOccurred = false;

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

        private void AdjustCursorChangeValuesForShiftDrag(ref float cursorXChange, ref float cursorYChange, InstanceSave instanceSave, List<ElementWithState> elementStack)
        {
            
            if (InputLibrary.Keyboard.Self.KeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) ||
                InputLibrary.Keyboard.Self.KeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift))
            {
                bool supportsShift = 
                    mSideGrabbed == ResizeSide.TopLeft || mSideGrabbed == ResizeSide.TopRight ||
                    mSideGrabbed == ResizeSide.BottomLeft || mSideGrabbed == ResizeSide.BottomRight;

                if (supportsShift && instanceSave != null)
                {
                    IPositionedSizedObject ipso = WireframeObjectManager.Self.GetRepresentation(instanceSave, elementStack);

                    Cursor cursor = Cursor.Self;
                    float cursorX = cursor.GetWorldX();
                    float cursorY = cursor.GetWorldY();

                    float top = ipso.GetAbsoluteTop();
                    float bottom = ipso.GetAbsoluteBottom();
                    float left = ipso.GetAbsoluteLeft();
                    float right = ipso.GetAbsoluteRight();

                    float absoluteXDifference = 1;
                    float absoluteYDifference = 1;

                    switch (mSideGrabbed)
                    {
                        case ResizeSide.BottomRight:
                            absoluteXDifference = System.Math.Abs(left - cursorX);
                            absoluteYDifference = System.Math.Abs(top - cursorY);
                            break;
                        case ResizeSide.BottomLeft:
                            absoluteXDifference = System.Math.Abs(right - cursorX);
                            absoluteYDifference = System.Math.Abs(top - cursorY);
                            break;
                        case ResizeSide.TopLeft:
                            absoluteXDifference = System.Math.Abs(right - cursorX);
                            absoluteYDifference = System.Math.Abs(bottom - cursorY);
                            break;
                        case ResizeSide.TopRight:
                            absoluteXDifference = System.Math.Abs(left - cursorX);
                            absoluteYDifference = System.Math.Abs(bottom - cursorY);
                            break;

                    }

                    float aspectRatio = absoluteXDifference / absoluteYDifference;



                    if (aspectRatio > mAspectRatioOnGrab)
                    {
                        float yToUse = 0;
                        // We use the X, but adjust the Y
                        switch (mSideGrabbed)
                        {
                            case ResizeSide.BottomRight:
                                cursorXChange = cursorX - right;
                                yToUse = top + absoluteXDifference / mAspectRatioOnGrab;
                                cursorYChange = yToUse - bottom;
                                break;
                            case ResizeSide.BottomLeft:
                                cursorXChange = cursorX - left;
                                yToUse = top + absoluteXDifference / mAspectRatioOnGrab;
                                cursorYChange = yToUse - bottom;
                                break;
                            case ResizeSide.TopRight:
                                cursorXChange = cursorX - right;
                                yToUse = bottom - absoluteXDifference / mAspectRatioOnGrab;
                                cursorYChange = yToUse - top;
                                break;
                            case ResizeSide.TopLeft:
                                cursorXChange = cursorX - left;
                                yToUse = bottom - absoluteXDifference / mAspectRatioOnGrab;
                                cursorYChange = yToUse - top;
                                break;
                        }
                    }
                    else
                    {
                        float xToUse;
                        // We use the Y, but adjust the X
                        switch (mSideGrabbed)
                        {
                            case ResizeSide.BottomRight:
                                cursorYChange = cursorY - bottom;
                                xToUse = left + absoluteYDifference * mAspectRatioOnGrab;
                                cursorXChange = xToUse - right;
                                break;
                            case ResizeSide.BottomLeft:
                                cursorYChange = cursorY - bottom;
                                xToUse = right - absoluteYDifference * mAspectRatioOnGrab;
                                cursorXChange = xToUse - left;
                                break;
                            case ResizeSide.TopRight:
                                cursorYChange = cursorY - top;
                                xToUse = left + absoluteYDifference * mAspectRatioOnGrab;
                                cursorXChange = xToUse - right;
                                break;
                            case ResizeSide.TopLeft:
                                cursorYChange = cursorY - top;
                                xToUse = right - absoluteYDifference * mAspectRatioOnGrab;
                                cursorXChange = xToUse - left;
                                break;
                        }
                    }

                    
                }
            }
        }

        private void CalculateMultipliers(InstanceSave instanceSave, List<ElementWithState> elementStack, out float changeXMultiplier, out float changeYMultiplier, out float widthMultiplier, out float heightMultiplier)
        {
            changeXMultiplier = 0;
            changeYMultiplier = 0;
            widthMultiplier = 0;
            heightMultiplier = 0;

            IPositionedSizedObject ipso = WireframeObjectManager.Self.GetRepresentation(instanceSave, elementStack);
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

            if (SelectionManager.Self.ResizeHandles.Width != 0)
            {
                widthMultiplier *= (ipso.Width / SelectionManager.Self.ResizeHandles.Width);
            }

            if (SelectionManager.Self.ResizeHandles.Height != 0)
            {
                heightMultiplier *= (ipso.Height / SelectionManager.Self.ResizeHandles.Height);
            }
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

        private float ModifyVariable(string baseVariableName, float modificationAmount, ElementSave elementSave)
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
                else if ((DimensionUnitType)unitsValue == DimensionUnitType.PercentageOfSourceFile)
                {
                    if (selectedIpso is Sprite)
                    {
                        Microsoft.Xna.Framework.Graphics.Texture2D texture = (selectedIpso as Sprite).Texture;

                        if(texture != null)
                        {
                            if (baseVariableName == "Width")
                            {
                                currentValue = 100 * selectedIpso.Width / texture.Width;
                            }
                            else
                            {
                                currentValue = 100 * selectedIpso.Height / texture.Height;
                            }
                        }
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
                        currentValue = 100 * selectedIpso.Width / parentValue;
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


            if (generalUnitType != GeneralUnitType.PixelsFromLarge && generalUnitType != GeneralUnitType.PixelsFromMiddle && generalUnitType != GeneralUnitType.PixelsFromSmall)
            {

                float parentWidth;
                float parentHeight;
                float fileWidth;
                float fileHeight;
                float outX;
                float outY;


                IPositionedSizedObject ipso = WireframeObjectManager.Self.GetSelectedRepresentation();
                ipso.GetFileWidthAndHeight(out fileWidth, out fileHeight);
                ipso.GetParentWidthAndHeight(
                    ProjectManager.Self.GumProjectSave.DefaultCanvasWidth, ProjectManager.Self.GumProjectSave.DefaultCanvasHeight,
                    out parentWidth, out parentHeight);

                UnitConverter.Self.ConvertToUnitTypeCoordinates(xAmount, yAmount, unitsVariableAsObject, unitsVariableAsObject, parentWidth, parentHeight, fileWidth, fileHeight,
                    out outX, out outY);

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
