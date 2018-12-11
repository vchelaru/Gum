using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Input;
using Gum.Managers;
using Gum.Plugins;
using Gum.PropertyGridHelpers;
using Gum.ToolStates;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;
using RenderingLibrary.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Wireframe
{
    public class StandardWireframeEditor : WireframeEditor
    {
        #region Fields/Properties

        ResizeSide SideGrabbed = ResizeSide.None;
        ResizeSide SideOver;

        ResizeHandles mResizeHandles;

        List<GraphicalUiElement> selectedObjects = 
            new List<GraphicalUiElement>();

        LineCircle rotationHandle;
        bool rotationHighlighted;
        bool rotationGrabbed;

        bool mHasGrabbed = false;

        public InputLibrary.Cursor Cursor
        {
            get
            {
                return InputLibrary.Cursor.Self;
            }
        }

        public override bool HasCursorOver
        {
            get
            {
                if(SideOver != ResizeSide.None)
                {
                    return true;
                }
                else if(rotationHighlighted)
                {
                    return true;
                }
                return false;
            }
        }

        #endregion

        public StandardWireframeEditor(Layer layer)
        {
            mResizeHandles = new ResizeHandles(layer);
            mResizeHandles.ShowOrigin = true;
            mResizeHandles.Visible = false;

            rotationHandle = new LineCircle();
            rotationHandle.Color = Color.Yellow;
            ShapeManager.Self.Add(rotationHandle, layer);
            rotationHandle.Visible = false;
        }

        public override void Destroy()
        {
            mResizeHandles.Destroy();

            ShapeManager.Self.Remove(rotationHandle);
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

        #region Activity

        public override void Activity(ICollection<GraphicalUiElement> selectedObjects)
        {
            if (selectedObjects.Count != 0)
            {
                RefreshSideOver();

                RefreshRotationGrabbed();

                PushActivity();

                ClickActivity();

                HandlesActivity();

                BodyGrabbingActivity();

                RotationHandleGrabbingActivity();

                bool shouldSkip = selectedObjects.Any(item => item.Tag is ScreenSave);

                if (!shouldSkip)
                {
                    mResizeHandles.SetValuesFrom(selectedObjects);

                    mResizeHandles.UpdateHandleRadius();

                    UpdateRotationHandlePosition();
                }
            }
        }

        private void RotationHandleGrabbingActivity()
        {
            if(rotationGrabbed)
            {
                var gue = selectedObjects.First();

                var originX = gue.AbsoluteX;
                var originY = gue.AbsoluteY;

                var cursorX = InputLibrary.Cursor.Self.GetWorldX();
                var cursorY = InputLibrary.Cursor.Self.GetWorldY();

                var angleInRadians = (float)System.Math.Atan2(cursorY - originY, cursorX - originX);

                var rotationValue =
                    -Microsoft.Xna.Framework.MathHelper.ToDegrees(angleInRadians);

                float parentRotation = 0;
                if(gue.Parent != null)
                {
                    parentRotation = gue.Parent.GetAbsoluteRotation();
                }

                gue.Rotation = rotationValue - parentRotation;

                string nameWithInstance = "Rotation";

                if(SelectedState.Self.SelectedInstance != null)
                {
                    nameWithInstance = SelectedState.Self.SelectedInstance.Name + 
                        "." + nameWithInstance;
                }

                SelectedState.Self.SelectedStateSave.SetValue(nameWithInstance, rotationValue - parentRotation, 
                    SelectedState.Self.SelectedInstance, "float");

                VariableInCategoryPropagationLogic.Self.PropagateVariablesInCategory(nameWithInstance);

                GumCommands.Self.GuiCommands.RefreshPropertyGridValues();

            }
        }

        private void RefreshRotationGrabbed()
        {
            var cursor = InputLibrary.Cursor.Self;
            var worldX = cursor.GetWorldX();
            var worldY = cursor.GetWorldY();

            rotationHighlighted = rotationHandle.HasCursorOver(worldX, worldY);
        }

        private void UpdateRotationHandlePosition()
        {


            GraphicalUiElement singleSelectedObject = null;
            if(selectedObjects.Count == 1)
            {
                singleSelectedObject = selectedObjects[0];
            }

            if(singleSelectedObject == null)
            {
                // hide the rotation handles
                rotationHandle.Visible = false;
            }
            else
            {
                rotationHandle.Visible = true;

                // right side
                const float minimumOffset = 24;


                float xOffset = 0;

                if(singleSelectedObject.XOrigin == HorizontalAlignment.Left)
                {
                    xOffset = singleSelectedObject.GetAbsoluteWidth() + minimumOffset;
                }
                else if (singleSelectedObject.XOrigin == HorizontalAlignment.Center)
                {
                    xOffset = singleSelectedObject.GetAbsoluteWidth()/2.0f + minimumOffset;

                }
                else if (singleSelectedObject.XOrigin == HorizontalAlignment.Right)
                {
                    xOffset = minimumOffset;
                }

                var offset = new Vector2(
                    xOffset,
                    0);

                MathFunctions.RotateVector(
                    ref offset, -MathHelper.ToRadians(singleSelectedObject.GetAbsoluteRotation()));

                rotationHandle.X = singleSelectedObject.AbsoluteX + offset.X;

                // consider the Y
                rotationHandle.Y = singleSelectedObject.AbsoluteY + offset.Y;

                rotationHandle.Radius = 8 / Renderer.Self.Camera.Zoom;
            }
        }

        private void ClickActivity()
        {
            var cursor = InputLibrary.Cursor.Self;

            if (cursor.PrimaryDown == false)
            {
                if(rotationGrabbed)
                {
                    DoEndOfSettingValuesLogic();
                }
                mHasGrabbed = false;
                rotationGrabbed = false;
            }

            if (cursor.PrimaryClick && mHasChangedAnythingSinceLastPush)
            {
                bool isShiftDown = GetIsShiftDown();

                // If the user resized with a shift, then released, we don't want to apply this, because they are not doing axis constrained movement
                if (isShiftDown && SideGrabbed == ResizeSide.None)
                {
                    ApplyShiftAxisLock();
                }

                // let's snap everything
                if (RestrictToUnitValues)
                {
                    SnapSelectedToUnitValues();
                }
            }

            if (cursor.PrimaryClick && mHasChangedAnythingSinceLastPush)
            {
                DoEndOfSettingValuesLogic();
            }

        }

        private void ApplyShiftAxisLock()
        {
            var axis = grabbedInitialState.AxisMovedFurthestAlong;

            bool isElementSelected = SelectedState.Self.SelectedInstances.Count() == 0 &&
                     // check specifically for components or standard elements, since Screens can't be moved
                     (SelectedState.Self.SelectedComponent != null || SelectedState.Self.SelectedStandardElement != null);


            if (axis == XOrY.X)
            {
                // If the X axis is the furthest-moved, set the Y values back to what they were.
                if (isElementSelected)
                {
                    SelectedState.Self.SelectedStateSave.SetValue("Y", grabbedInitialState.ComponentPosition.Y, "float");
                }
                else
                {
                    foreach (var instance in SelectedState.Self.SelectedInstances)
                    {
                        SelectedState.Self.SelectedStateSave.SetValue(instance.Name + ".Y", grabbedInitialState.InstancePositions[instance].Y, "float");
                    }
                }
            }
            else
            {
                // If the Y axis is the furthest-moved, set the X values back to what they were.
                if (isElementSelected)
                {
                    SelectedState.Self.SelectedStateSave.SetValue("X", grabbedInitialState.ComponentPosition.X, "float");
                }
                else
                {
                    foreach (var instance in SelectedState.Self.SelectedInstances)
                    {
                        SelectedState.Self.SelectedStateSave.SetValue(instance.Name + ".X", grabbedInitialState.InstancePositions[instance].X, "float");
                    }
                }
            }

            GumCommands.Self.GuiCommands.RefreshPropertyGrid();
        }

        private void HandlesActivity()
        {
            var cursor = InputLibrary.Cursor.Self;

            if (cursor.PrimaryPush)
            {
                SideGrabbed = SideOver;
            }
            if (cursor.PrimaryDown && grabbedInitialState.HasMovedEnough)
            {
                SideGrabbingActivity();
            }
            if (cursor.PrimaryClick)
            {
                SideGrabbed = ResizeSide.None;
            }
        }

        private void SideGrabbingActivity()
        {
            float cursorXChange = InputLibrary.Cursor.Self.XChange / Renderer.Self.Camera.Zoom;
            float cursorYChange = InputLibrary.Cursor.Self.YChange / Renderer.Self.Camera.Zoom;

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
                hasChangeOccurred |= SideGrabbingActivityForInstanceSave(cursorXChange, cursorYChange, instanceSave: null, elementStack: elementStack);
            }

            foreach (InstanceSave save in SelectedState.Self.SelectedInstances)
            {
                hasChangeOccurred |= SideGrabbingActivityForInstanceSave(cursorXChange, cursorYChange, instanceSave: save, elementStack: elementStack);
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

        private void PushActivity()
        {
            // The selected object is set in the SelectionManager

            var cursor = InputLibrary.Cursor.Self;
            if (cursor.PrimaryPush)
            {
                rotationGrabbed = rotationHighlighted;

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
            var cursor = InputLibrary.Cursor.Self;
            if (SelectionManager.Self.IsOverBody && cursor.PrimaryDown && mHasGrabbed &&
                grabbedInitialState.HasMovedEnough)
            {
                ApplyCursorMovement(cursor);
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

            Vector2 reposition = new Vector2(cursorXChange * changeXMultiplier, cursorYChange * changeYMultiplier);
            // invert Y so up is positive
            reposition.Y *= -1;

            GraphicalUiElement representation = null;

            if (instanceSave != null)
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
                    EditingManager.Self.ModifyVariable("X", reposition.X, instanceSave);
                }
                else
                {
                    EditingManager.Self.ModifyVariable("X", reposition.X, elementStack.Last().Element);
                }
            }
            if (reposition.Y != 0)
            {
                hasChangeOccurred = true;
                if (instanceSave != null)
                {
                    EditingManager.Self.ModifyVariable("Y", reposition.Y, instanceSave);
                }
                else
                {
                    EditingManager.Self.ModifyVariable("Y", reposition.Y, elementStack.Last().Element);
                }
            }



            if (heightMultiplier != 0 && cursorYChange != 0)
            {
                hasChangeOccurred = true;
                if (instanceSave != null)
                {
                    EditingManager.Self.ModifyVariable("Height", cursorYChange * heightMultiplier, instanceSave);
                }
                else
                {
                    EditingManager.Self.ModifyVariable("Height", cursorYChange * heightMultiplier, elementStack.Last().Element);
                }
            }
            if (widthMultiplier != 0 && cursorXChange != 0)
            {
                hasChangeOccurred = true;
                if (instanceSave != null)
                {
                    EditingManager.Self.ModifyVariable("Width", cursorXChange * widthMultiplier, instanceSave);
                }
                else
                {
                    EditingManager.Self.ModifyVariable("Width", cursorXChange * widthMultiplier, elementStack.Last().Element);
                }
            }
            return hasChangeOccurred;
        }

        #endregion

        #region Update To

        public override void UpdateToSelection(ICollection<GraphicalUiElement> selectedObjects)
        {
            this.selectedObjects.Clear();
            this.selectedObjects.AddRange(selectedObjects);

            if(selectedObjects.Count == 0 || selectedObjects.Any(item => item.Tag is ScreenSave))
            {
                mResizeHandles.Visible = false;
            }
            else
            {
                mResizeHandles.Visible = true;
                mResizeHandles.SetValuesFrom(selectedObjects);
            }
        }

        #endregion

        private void SnapSelectedToUnitValues()
        {
            bool wasAnythingModified = false;

            if (SelectedState.Self.SelectedInstances.Count() == 0 &&
                (SelectedState.Self.SelectedComponent != null || SelectedState.Self.SelectedStandardElement != null))
            {
                GraphicalUiElement gue = SelectionManager.Self.SelectedGue;


                float differenceToUnitX;
                float differenceToUnitY;
                float differenceToUnitWidth;
                float differenceToUnitHeight;
                GetDifferenceToUnit(gue, out differenceToUnitX, out differenceToUnitY, out differenceToUnitWidth, out differenceToUnitHeight);

                if (differenceToUnitX != 0)
                {
                    gue.X = EditingManager.Self.ModifyVariable("X", differenceToUnitX, SelectedState.Self.SelectedElement);
                    wasAnythingModified = true;
                }
                if (differenceToUnitY != 0)
                {
                    gue.Y = EditingManager.Self.ModifyVariable("Y", differenceToUnitY, SelectedState.Self.SelectedElement);
                    wasAnythingModified = true;
                }
                if (differenceToUnitWidth != 0)
                {
                    gue.Width = EditingManager.Self.ModifyVariable("Width", differenceToUnitWidth, SelectedState.Self.SelectedElement);
                    wasAnythingModified = true;
                }
                if (differenceToUnitHeight != 0)
                {
                    gue.Height = EditingManager.Self.ModifyVariable("Height", differenceToUnitHeight, SelectedState.Self.SelectedElement);
                    wasAnythingModified = true;
                }
            }
            else if (SelectedState.Self.SelectedInstances.Count() != 0)
            {
                foreach (var gue in SelectionManager.Self.SelectedGues)
                {
                    var instanceSave = gue.Tag as InstanceSave;

                    if (instanceSave != null && !EditingManager.Self.ShouldSkipDraggingMovementOn(instanceSave))
                    {
                        float differenceToUnitX;
                        float differenceToUnitY;

                        float differenceToUnitWidth;
                        float differenceToUnitHeight;

                        GetDifferenceToUnit(gue, out differenceToUnitX, out differenceToUnitY, out differenceToUnitWidth, out differenceToUnitHeight);

                        if (differenceToUnitX != 0)
                        {
                            gue.X = EditingManager.Self.ModifyVariable("X", differenceToUnitX, instanceSave);
                            wasAnythingModified = true;
                        }
                        if (differenceToUnitY != 0)
                        {
                            gue.Y = EditingManager.Self.ModifyVariable("Y", differenceToUnitY, instanceSave);
                            wasAnythingModified = true;
                        }
                        if (differenceToUnitWidth != 0)
                        {
                            gue.Width = EditingManager.Self.ModifyVariable("Width", differenceToUnitWidth, instanceSave);
                            wasAnythingModified = true;
                        }
                        if (differenceToUnitHeight != 0)
                        {
                            gue.Height = EditingManager.Self.ModifyVariable("Height", differenceToUnitHeight, instanceSave);
                            wasAnythingModified = true;
                        }
                    }

                }
            }

            if (wasAnythingModified)
            {
                GumCommands.Self.GuiCommands.RefreshPropertyGrid(true);
            }
        }

        public override System.Windows.Forms.Cursor GetWindowsCursorToShow(
            System.Windows.Forms.Cursor defaultCursor, float worldXAt, float worldYAt)
        {
            System.Windows.Forms.Cursor cursorToSet = defaultCursor;


            switch (SideOver)
            {
                case ResizeSide.TopLeft:
                case ResizeSide.BottomRight:
                    cursorToSet = System.Windows.Forms.Cursors.SizeNWSE;
                    break;
                case ResizeSide.TopRight:
                case ResizeSide.BottomLeft:
                    cursorToSet = System.Windows.Forms.Cursors.SizeNESW;
                    break;
                case ResizeSide.Top:
                case ResizeSide.Bottom:
                    cursorToSet = System.Windows.Forms.Cursors.SizeNS;
                    break;
                case ResizeSide.Left:
                case ResizeSide.Right:
                    cursorToSet = System.Windows.Forms.Cursors.SizeWE;
                    break;
                case ResizeSide.None:

                    break;
            }
            return cursorToSet;
        }

        private void RefreshSideOver()
        {
            var worldX = Cursor.GetWorldX();
            var worldY = Cursor.GetWorldY();

            if (mResizeHandles.Visible == false)
            {
                SideOver = ResizeSide.None;
            }
            else
            {
                // If the user is already dragging then there's
                // no need to re-check which side the user is over
                if (Cursor.PrimaryPush || (!Cursor.PrimaryDown && !Cursor.PrimaryClick))
                {
                    SideOver = mResizeHandles.GetSideOver(worldX, worldY);
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

            switch (this.SideGrabbed)
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

            if (mResizeHandles.Width != 0)
            {
                widthMultiplier *= (ipso.Width / mResizeHandles.Width);
            }

            if (mResizeHandles.Height != 0)
            {
                heightMultiplier *= (ipso.Height / mResizeHandles.Height);
            }

            if (GetIsAltDown())
            {
                if (widthMultiplier != 0)
                {
                    // user grabbed a corner that can change width, so adjust the x multiplier
                    changeXMultiplier = (changeXMultiplier - .5f) * 2;
                }

                if (heightMultiplier != 0)
                {
                    changeYMultiplier = (changeYMultiplier - .5f) * 2;
                }

                heightMultiplier *= 2;
                widthMultiplier *= 2;
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
            if (gue.YUnits.GetIsPixelBased())
            {
                float y = gue.Y;
                float desiredY = MathFunctions.RoundToInt(y);
                differenceToUnitPositionY = desiredY - y;
            }

            if (gue.WidthUnits.GetIsPixelBased())
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

        private float GetXMultiplierForLeft(InstanceSave instanceSave, IPositionedSizedObject ipso)
        {
            object xOriginAsObject = EditingManager.GetCurrentValueForVariable("X Origin", instanceSave);
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
            object yOriginAsObject = EditingManager.GetCurrentValueForVariable("Y Origin", instanceSave);
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
            object yOriginAsObject = EditingManager.GetCurrentValueForVariable("Y Origin", instanceSave);
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
            object xOriginAsObject = EditingManager.GetCurrentValueForVariable("X Origin", instanceSave);

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




        private void AdjustCursorChangeValuesForShiftDrag(ref float cursorXChange, ref float cursorYChange, InstanceSave instanceSave, List<ElementWithState> elementStack)
        {
            if (InputLibrary.Keyboard.Self.KeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) ||
                InputLibrary.Keyboard.Self.KeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift))
            {
                bool supportsShift =
                    SideGrabbed == ResizeSide.TopLeft || SideGrabbed == ResizeSide.TopRight ||
                    SideGrabbed == ResizeSide.BottomLeft || SideGrabbed == ResizeSide.BottomRight;

                if (supportsShift && instanceSave != null)
                {
                    IRenderableIpso ipso = WireframeObjectManager.Self.GetRepresentation(instanceSave, elementStack);

                    var cursor = InputLibrary.Cursor.Self;
                    float cursorX = cursor.GetWorldX();
                    float cursorY = cursor.GetWorldY();

                    float top = ipso.GetAbsoluteTop();
                    float bottom = ipso.GetAbsoluteBottom();
                    float left = ipso.GetAbsoluteLeft();
                    float right = ipso.GetAbsoluteRight();

                    float absoluteXDifference = 1;
                    float absoluteYDifference = 1;

                    switch (SideGrabbed)
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


                    if (aspectRatio > aspectRatioOnGrab)
                    {
                        float yToUse = 0;
                        // We use the X, but adjust the Y
                        switch (SideGrabbed)
                        {
                            case ResizeSide.BottomRight:
                                cursorXChange = cursorX - right;
                                yToUse = top + absoluteXDifference / aspectRatioOnGrab;
                                cursorYChange = yToUse - bottom;
                                break;
                            case ResizeSide.BottomLeft:
                                cursorXChange = cursorX - left;
                                yToUse = top + absoluteXDifference / aspectRatioOnGrab;
                                cursorYChange = yToUse - bottom;
                                break;
                            case ResizeSide.TopRight:
                                cursorXChange = cursorX - right;
                                yToUse = bottom - absoluteXDifference / aspectRatioOnGrab;
                                cursorYChange = yToUse - top;
                                break;
                            case ResizeSide.TopLeft:
                                cursorXChange = cursorX - left;
                                yToUse = bottom - absoluteXDifference / aspectRatioOnGrab;
                                cursorYChange = yToUse - top;
                                break;
                        }
                    }
                    else
                    {
                        float xToUse;
                        // We use the Y, but adjust the X
                        switch (SideGrabbed)
                        {
                            case ResizeSide.BottomRight:
                                cursorYChange = cursorY - bottom;
                                xToUse = left + absoluteYDifference * aspectRatioOnGrab;
                                cursorXChange = xToUse - right;
                                break;
                            case ResizeSide.BottomLeft:
                                cursorYChange = cursorY - bottom;
                                xToUse = right - absoluteYDifference * aspectRatioOnGrab;
                                cursorXChange = xToUse - left;
                                break;
                            case ResizeSide.TopRight:
                                cursorYChange = cursorY - top;
                                xToUse = left + absoluteYDifference * aspectRatioOnGrab;
                                cursorXChange = xToUse - right;
                                break;
                            case ResizeSide.TopLeft:
                                cursorYChange = cursorY - top;
                                xToUse = right - absoluteYDifference * aspectRatioOnGrab;
                                cursorXChange = xToUse - left;
                                break;
                        }
                    }


                }
            }
        }

    }
}
