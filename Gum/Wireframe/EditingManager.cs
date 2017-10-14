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

namespace Gum.Wireframe
{
    public partial class EditingManager
    {
        #region Fields

        static EditingManager mSelf;

        GrabbedInitialState grabbedInitialState = new GrabbedInitialState();

        ResizeSide mSideGrabbed = ResizeSide.None;
        bool mHasGrabbed = false;
        bool mHasChangedAnythingSinceLastPush = false;

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

                HandlesActivity();

                BodyGrabbingActivity();
            }
        }
        
        
        bool GetIsShiftDown()
        {
            return InputLibrary.Keyboard.Self.KeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) ||
                    InputLibrary.Keyboard.Self.KeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift);
        }    

        bool GetIsAltDown()
        {
            return InputLibrary.Keyboard.Self.KeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt) ||
                    InputLibrary.Keyboard.Self.KeyDown(Microsoft.Xna.Framework.Input.Keys.RightAlt);
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

                bool isShiftDown = GetIsShiftDown();

                // If the user resized with a shift, then released, we don't want to apply this, because they are not doing axis constrained movement
                if(isShiftDown && mSideGrabbed == ResizeSide.None)
                {
                    var axis = grabbedInitialState.AxisMovedFurthestAlong;

                    bool isElementSelected = SelectedState.Self.SelectedInstances.Count() == 0 &&
                            (SelectedState.Self.SelectedComponent != null || SelectedState.Self.SelectedStandardElement != null);

                    if (axis == XOrY.X)
                    {
                        if (isElementSelected)
                        {
                            SelectedState.Self.SelectedStateSave.SetValue("Y", grabbedInitialState.ComponentPosition.Y, null, "float");
                        }
                        else
                        {
                            foreach(var instance in SelectedState.Self.SelectedInstances)
                            {
                                SelectedState.Self.SelectedStateSave.SetValue(instance.Name + ".Y", grabbedInitialState.InstancePositions[instance].Y, instance, "float");
                            }
                        }
                    }
                    else
                    {
                        if (isElementSelected)
                        {
                            SelectedState.Self.SelectedStateSave.SetValue("X", grabbedInitialState.ComponentPosition.Y, null, "float");
                        }
                        else
                        {
                            foreach (var instance in SelectedState.Self.SelectedInstances)
                            {
                                SelectedState.Self.SelectedStateSave.SetValue(instance.Name + ".X", grabbedInitialState.InstancePositions[instance].X, instance, "float");
                            }
                        }
                    }

                    GumCommands.Self.GuiCommands.RefreshPropertyGrid();
                }

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

        /// <summary>
        /// Returns the difference between the current X, Y, Width, and Height values and the nearest to-the-pixel value.
        /// </summary>
        /// <param name="gue">The GraphicalUiElement to use for current values.</param>
        /// <param name="differenceToUnitPositionX">The amount to add to the X value to snap it to-the-pixel.</param>
        /// <param name="differenceToUnitPositionY">The amount to add to the Y value to snap it to-the-pixel.</param>
        /// <param name="differenceToUnitWidth">The amount to add to the Width value to snap it to-the-pixel.</param>
        /// <param name="differenceToUnitHeight">The amount to add to the Height value to snap it to-the-pixel.</param>
        /// <remarks>
        /// The values returned here depend on the GraphicalUiElement's values for X,Y,Width, and Height. They also depend on the
        /// units for the corresponding values. 
        /// As an example, if the GraphicalUiElement is using an XUnits of PixelsFromLeft and has an X value of 4.9, then the 
        /// differenceToUnitPisitionX would be .1.
        /// </remarks>
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

                grabbedInitialState.HandlePush();

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
            if (SelectionManager.Self.IsOverBody && cursor.PrimaryDown && mHasGrabbed && 
                grabbedInitialState.HasMovedEnough)
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

                bool isShiftDown = InputLibrary.Keyboard.Self.KeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) ||
                    InputLibrary.Keyboard.Self.KeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift);

                Cursor cursor = InputLibrary.Cursor.Self;
                bool isGrabbedByMouseCursor = cursor.PrimaryDown && mSideGrabbed == ResizeSide.None;


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

                    if(isShiftDown && isGrabbedByMouseCursor)
                    {
                        var xOrY = grabbedInitialState.AxisMovedFurthestAlong;

                        if(xOrY == XOrY.X)
                        {
                            var gue = WireframeObjectManager.Self.GetRepresentation(SelectedState.Self.SelectedElement);

                            gue.Y = grabbedInitialState.ComponentPosition.Y;
                        }
                        else
                        {

                            var gue = WireframeObjectManager.Self.GetRepresentation(SelectedState.Self.SelectedElement);

                            gue.X = grabbedInitialState.ComponentPosition.X;
                        }
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

                            if (isShiftDown && isGrabbedByMouseCursor)
                            {
                                var xOrY = grabbedInitialState.AxisMovedFurthestAlong;

                                if (xOrY == XOrY.X)
                                {
                                    var gue = WireframeObjectManager.Self.GetRepresentation(instance);

                                    gue.Y = grabbedInitialState.InstancePositions[instance].Y;
                                }
                                else
                                {

                                    var gue = WireframeObjectManager.Self.GetRepresentation(instance);

                                    gue.X = grabbedInitialState.InstancePositions[instance].X;
                                }
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
            if (cursor.PrimaryDown && grabbedInitialState.HasMovedEnough)
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

            Vector2 reposition = new Vector2(cursorXChange * changeXMultiplier,cursorYChange * changeYMultiplier);
            // invert Y so up is positive
            reposition.Y *= -1;

            GraphicalUiElement representation = null;

            if(instanceSave != null)
            {
                representation = WireframeObjectManager.Self.GetRepresentation(instanceSave);
            }
            else
            {
                representation = WireframeObjectManager.Self.GetRepresentation(elementStack.Last().Element);
            }

            float rotation = MathHelper.ToRadians(representation?.GetAbsoluteRotation() ?? 0);

            MathFunctions.RotateVector(ref reposition, rotation);

            // flip Y back
            reposition.Y *= -1;

            if (reposition.X != 0)
            {
                hasChangeOccurred = true;
                if (instanceSave != null)
                {
                    ModifyVariable("X", reposition.X, instanceSave);
                }
                else
                {
                    ModifyVariable("X", reposition.X, elementStack.Last().Element);
                }
            }
            if (reposition.Y != 0)
            {
                hasChangeOccurred = true;
                if (instanceSave != null)
                {
                    ModifyVariable("Y", reposition.Y, instanceSave);
                }
                else
                {
                    ModifyVariable("Y", reposition.Y, elementStack.Last().Element);
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
                    IRenderableIpso ipso = WireframeObjectManager.Self.GetRepresentation(instanceSave, elementStack);

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

            if(GetIsAltDown())
            {
                if(widthMultiplier != 0)
                {
                    // user grabbed a corner that can change width, so adjust the x multiplier
                    changeXMultiplier = (changeXMultiplier - .5f) * 2;
                }

                if(heightMultiplier != 0)
                {
                    changeYMultiplier = (changeYMultiplier - .5f) * 2;
                }

                heightMultiplier *= 2;
                widthMultiplier *= 2;
            }
        }

        private static float GetRatioXOverInSelection(IPositionedSizedObject ipso, HorizontalAlignment horizontalAlignment)
        {
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
                float toReturn = 1 - ratioOver;


                return toReturn;
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
                float toReturn = 1 - ratioOver;



                return toReturn;
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

                var toReturn = 0 + ratioOver;

                return toReturn;
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

                var toReturn = 0 + ratioOver;

                return toReturn;
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

                modificationAmount = AdjustAmountAccordingToUnitType(baseVariableName, modificationAmount, unitsVariableAsObject);

                float newValue = currentValue + modificationAmount;
                SelectedState.Self.SelectedStateSave.SetValue(nameWithInstance, newValue, instanceSave, "float");

                var ipso = WireframeObjectManager.Self.GetRepresentation(instanceSave, null);

                ipso.SetProperty(baseVariableName, newValue);

                SetVariableLogic.Self.PropagateVariablesInCategory(nameWithInstance);


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

            SetVariableLogic.Self.PropagateVariablesInCategory(baseVariableName);

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
            else if (generalUnitType != GeneralUnitType.PixelsFromLarge && generalUnitType != GeneralUnitType.PixelsFromMiddle && generalUnitType != GeneralUnitType.PixelsFromSmall)
            {

                float parentWidth;
                float parentHeight;
                float fileWidth;
                float fileHeight;
                float outX;
                float outY;


                IRenderableIpso ipso = WireframeObjectManager.Self.GetSelectedRepresentation();
                ipso.GetFileWidthAndHeight(out fileWidth, out fileHeight);
                ipso.GetParentWidthAndHeight(
                    ProjectManager.Self.GumProjectSave.DefaultCanvasWidth, ProjectManager.Self.GumProjectSave.DefaultCanvasHeight,
                    out parentWidth, out parentHeight);

                var unitsVariable = UnitConverter.ConvertToGeneralUnit(unitsVariableAsObject);

                UnitConverter.Self.ConvertToUnitTypeCoordinates(xAmount, yAmount, unitsVariable, unitsVariable, ipso.Width, ipso.Height, parentWidth, parentHeight, fileWidth, fileHeight,
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
