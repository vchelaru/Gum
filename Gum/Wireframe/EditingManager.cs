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

        public bool RestrictToUnitValues { get; set; }

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

            if (cursor.PrimaryClick && mHasChangedAnythingSinceLastPush)
            {
                // let's snap everything
                if (RestrictToUnitValues)
                {
                    SnapSelectedToUnitValues();
                }
            }

            if (cursor.PrimaryClick && mHasChangedAnythingSinceLastPush && ProjectManager.Self.GeneralSettingsFile.AutoSave)
            {
                ProjectManager.Self.SaveElement(SelectedState.Self.SelectedElement);
                mHasChangedAnythingSinceLastPush = false;
            }
        }

        private void SnapSelectedToUnitValues()
        {
            bool wasAnythingModified = false;

            if(SelectedState.Self.SelectedInstances.Count() == 0 && 
                (SelectedState.Self.SelectedComponent != null || SelectedState.Self.SelectedStandardElement != null))
            {
                GraphicalUiElement gue = SelectionManager.Self.SelectedIpso;


                float differenceToUnitX;
                float differenceToUnitY;
                float differenceToUnitWidth;
                float differenceToUnitHeight;
                GetDifferenceToUnit(gue, out differenceToUnitX, out differenceToUnitY, out differenceToUnitWidth, out differenceToUnitHeight);
                
                if (differenceToUnitX != 0)
                {
                    gue.X = ModifyVariable("X", differenceToUnitX, SelectedState.Self.SelectedElement);
                    wasAnythingModified = true;
                }
                if (differenceToUnitY != 0)
                {
                    gue.Y = ModifyVariable("Y", differenceToUnitY, SelectedState.Self.SelectedElement);
                    wasAnythingModified = true;
                }
                if(differenceToUnitWidth != 0)
                {
                    gue.Width = ModifyVariable("Width", differenceToUnitWidth, SelectedState.Self.SelectedElement);
                    wasAnythingModified = true;
                }
                if (differenceToUnitHeight != 0)
                {
                    gue.Height = ModifyVariable("Height", differenceToUnitHeight, SelectedState.Self.SelectedElement);
                    wasAnythingModified = true;
                }
            }
            else if(SelectedState.Self.SelectedInstances.Count() != 0)
            {
                foreach(var gue in SelectionManager.Self.SelectedIpsos)
                {
                    var instanceSave = gue.Tag as InstanceSave;

                    if(instanceSave != null && !ShouldSkipDraggingMovementOn(instanceSave))
                    {
                        float differenceToUnitX;
                        float differenceToUnitY;

                        float differenceToUnitWidth;
                        float differenceToUnitHeight;

                        GetDifferenceToUnit(gue, out differenceToUnitX, out differenceToUnitY, out differenceToUnitWidth, out differenceToUnitHeight);

                        if (differenceToUnitX != 0)
                        {
                            gue.X = ModifyVariable("X", differenceToUnitX, instanceSave);
                            wasAnythingModified = true;
                        }
                        if (differenceToUnitY != 0)
                        {
                            gue.Y = ModifyVariable("Y", differenceToUnitY, instanceSave);
                            wasAnythingModified = true;
                        }
                        if (differenceToUnitWidth != 0)
                        {
                            gue.Width = ModifyVariable("Width", differenceToUnitWidth, instanceSave);
                            wasAnythingModified = true;
                        }
                        if (differenceToUnitHeight != 0)
                        {
                            gue.Height = ModifyVariable("Height", differenceToUnitHeight, instanceSave);
                            wasAnythingModified = true;
                        }
                    }

                }
            }

            if(wasAnythingModified)
            {
                GumCommands.Self.GuiCommands.RefreshPropertyGrid(true);
            }
        }

        private static void GetDifferenceToUnit(GraphicalUiElement gue, 
            out float differenceToUnitPositionX, out float differenceToUnitPositionY,
            out float differenceToUnitWidth, out float differenceToUnitHeight
            
            )
        {
            differenceToUnitPositionX = 0;
            differenceToUnitPositionY = 0;
            differenceToUnitWidth = 0;
            differenceToUnitHeight = 0;


            if (gue.XUnits.GetIsPixelBased())
            {
                float x = gue.X;
                float desiredX = MathFunctions.RoundToInt(x);
                differenceToUnitPositionX = desiredX - x;
            }
            if(gue.YUnits.GetIsPixelBased())
            {
                float y = gue.Y;
                float desiredY = MathFunctions.RoundToInt(y);
                differenceToUnitPositionY = desiredY - y;
            }

            if(gue.WidthUnits.GetIsPixelBased())
            {
                float width = gue.Width;
                float desiredWidth = MathFunctions.RoundToInt(width);
                differenceToUnitWidth = desiredWidth - width;
            }

            if (gue.HeightUnits.GetIsPixelBased())
            {
                float height = gue.Height;
                float desiredHeight = MathFunctions.RoundToInt(height);
                differenceToUnitHeight = desiredHeight - height;
            }

        }

        private bool ShouldSkipDraggingMovementOn(InstanceSave instanceSave)
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

                    mHasChangedAnythingSinceLastPush = true;
                }
            }
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
                hasChangeOccurred |= SideGrabbingActivityForInstanceSave(cursorXChange, cursorYChange, instanceSave:null, elementStack:elementStack);
            }

            foreach (InstanceSave save in SelectedState.Self.SelectedInstances)
            {
                hasChangeOccurred |= SideGrabbingActivityForInstanceSave(cursorXChange, cursorYChange, instanceSave:save, elementStack:elementStack);
            }

            if (hasChangeOccurred)
            {
                //UpdateSelectedObjectsPositionAndDimensions();
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
                if (instanceSave != null)
                {
                    ModifyVariable("X", cursorXChange * changeXMultiplier, instanceSave);
                }
                else
                {
                    ModifyVariable("X", cursorXChange * changeXMultiplier, elementStack.Last().Element);
                }
            }
            if (changeYMultiplier != 0 && cursorYChange != 0)
            {
                hasChangeOccurred = true;
                if (instanceSave != null)
                {
                    ModifyVariable("Y", cursorYChange * changeYMultiplier, instanceSave);
                }
                else
                {
                    ModifyVariable("Y", cursorYChange * changeYMultiplier, elementStack.Last().Element);
                }
            }
            if (heightMultiplier != 0 && cursorYChange != 0)
            {
                hasChangeOccurred = true;
                if (instanceSave != null)
                {
                    ModifyVariable("Height", cursorYChange * heightMultiplier, instanceSave);
                }
                else
                {
                    ModifyVariable("Height", cursorYChange * heightMultiplier, elementStack.Last().Element);
                }
            }
            if (widthMultiplier != 0 && cursorXChange != 0)
            {
                hasChangeOccurred = true;
                if (instanceSave != null)
                {
                    ModifyVariable("Width", cursorXChange * widthMultiplier, instanceSave);
                }
                else
                {
                    ModifyVariable("Width", cursorXChange * widthMultiplier, elementStack.Last().Element);
                }
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

            //float handleLeft = SelectionManager.Self.ResizeHandles.X;
            //float handleWidth = SelectionManager.Self.ResizeHandles.Width;

            //float ipsoXToUse = ipso.GetAbsoluteX();

            //if (horizontalAlignment == HorizontalAlignment.Center)
            //{
            //    ipsoXToUse += ipso.Width / 2.0f;
            //}
            //else if (horizontalAlignment == HorizontalAlignment.Right)
            //{
            //    ipsoXToUse += ipso.Width;
            //}

            //return (ipsoXToUse - handleLeft) / handleWidth;
            // Isn't this easier?
            if (horizontalAlignment == HorizontalAlignment.Left)
            {
                return 0;
            }
            else if (horizontalAlignment == HorizontalAlignment.Center)
            {
                return .5f;
            }
            else// if (horizontalAlignment == HorizontalAlignment.Right)
            {
                return 1;
            }
        }

        private static float GetRatioYDownInSelection(IPositionedSizedObject ipso, VerticalAlignment verticalAlignment)
        {
            if (verticalAlignment == VerticalAlignment.Top)
            {
                return 0;
            }
            else if (verticalAlignment == VerticalAlignment.Center)
            {
                return .5f;
            }
            else //if (verticalAlignment == VerticalAlignment.Bottom)
            {
                return 1;
            }
        }

        private float GetXMultiplierForLeft(InstanceSave instanceSave, IPositionedSizedObject ipso)
        {
            object xOriginAsObject = GetCurrentValueForVariable("X Origin", instanceSave);
            bool shouldContiue = xOriginAsObject != null;
            if (shouldContiue)
            {
                HorizontalAlignment xOrigin = (HorizontalAlignment)xOriginAsObject;

                float ratioOver = GetRatioXOverInSelection(ipso, xOrigin);
                return 1 - ratioOver;
            }
            else
            {
                return 0;
            }
        }



        private float GetYMultiplierForTop(InstanceSave instanceSave, IPositionedSizedObject ipso)
        {
            object yOriginAsObject = GetCurrentValueForVariable("Y Origin", instanceSave);
            bool shouldContiue = yOriginAsObject != null;
            if (shouldContiue)
            {
                VerticalAlignment yOrigin = (VerticalAlignment)yOriginAsObject;

                float ratioOver = GetRatioYDownInSelection(ipso, yOrigin);
                return 1 - ratioOver;
            }
            else
            {
                return 0;
            }
        }

        private float GetYMultiplierForBottom(InstanceSave instanceSave, IPositionedSizedObject ipso)
        {
            object yOriginAsObject = GetCurrentValueForVariable("Y Origin", instanceSave);
            bool shouldContiue = yOriginAsObject != null;
            if (shouldContiue)
            {
                VerticalAlignment yOrigin = (VerticalAlignment)yOriginAsObject;

                float ratioOver = GetRatioYDownInSelection(ipso, yOrigin);

                return 0 + ratioOver;
            }
            else
            {
                return 0;
            }
        }

        private float GetXMultiplierForRight(InstanceSave instanceSave, IPositionedSizedObject ipso)
        {
            object xOriginAsObject = GetCurrentValueForVariable("X Origin", instanceSave);

            bool shouldContiue = xOriginAsObject != null;

            if (shouldContiue)
            {
                HorizontalAlignment xOrigin = (HorizontalAlignment)xOriginAsObject;
                float ratioOver = GetRatioXOverInSelection(ipso, xOrigin);

                return 0 + ratioOver;
            }
            else
            {
                return 0;
            }
        }

        private float ModifyVariable(string baseVariableName, float modificationAmount, InstanceSave instanceSave)
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

                float currentValue = (float)currentValueAsObject;

                string unitsVariableName = baseVariableName + " Units";
                string unitsNameWithInstance;
                object unitsVariableAsObject;
                GetCurrentValueForVariable(unitsVariableName, instanceSave, out unitsNameWithInstance, out unitsVariableAsObject);

                currentValue = AdjustCurrentValueIfScale(currentValue, baseVariableName, unitsVariableAsObject);

                modificationAmount = AdjustAmountAccordingToUnitType(baseVariableName, modificationAmount, unitsVariableAsObject);

                float newValue = currentValue + modificationAmount;
                SelectedState.Self.SelectedStateSave.SetValue(nameWithInstance, newValue, instanceSave, "float");

                var ipso = WireframeObjectManager.Self.GetRepresentation(instanceSave, null);

                ipso.SetProperty(baseVariableName, newValue);

                return newValue;
            }
            else
            {
                return 0;
            }
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
            SelectedState.Self.SelectedStateSave.SetValue(baseVariableName, newValue, null, "float");

            
            var ipso = WireframeObjectManager.Self.GetRepresentation(elementSave);
            ipso.SetProperty(baseVariableName, newValue);


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
                else if ((DimensionUnitType)unitsValue == DimensionUnitType.RelativeToContainer)
                {
                    // We don't do anything special with "0" when RelativeToContainer, so don't modify the currentValue
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
            else if (generalUnitType != GeneralUnitType.PixelsFromLarge && generalUnitType != GeneralUnitType.PixelsFromMiddle && generalUnitType != GeneralUnitType.PixelsFromSmall)
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

                var unitsVariable = UnitConverter.ConvertToGeneralUnit(unitsVariableAsObject);

                UnitConverter.Self.ConvertToUnitTypeCoordinates(xAmount, yAmount, unitsVariable, unitsVariable, parentWidth, parentHeight, fileWidth, fileHeight,
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
