using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Input;
using Gum.Managers;
using Gum.PropertyGridHelpers;
using Gum.ToolStates;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;
using RenderingLibrary.Math.Geometry;
using System.Collections.Generic;
using System.Linq;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Vector2 = System.Numerics.Vector2;
using Color = System.Drawing.Color;
using Matrix = System.Numerics.Matrix4x4;
using System.Windows.Input;
using System;
using Gum.ToolCommands;
using EditorTabPlugin_XNA.ExtensionMethods;
using Gum.Services;
using Gum.Commands;
using Gum.Undo;
using Gum.Wireframe.Editors.Handlers;
using Gum.Plugins.InternalPlugins.VariableGrid;

namespace Gum.Wireframe.Editors;

/// <summary>
/// Editor which includes ability to move, resize, and rotate an object.
/// </summary>
public class StandardWireframeEditor : WireframeEditor
{
    #region Fields/Properties

    ResizeHandles mResizeHandles;
    ResizeInputHandler _resizeInputHandler;

    List<GraphicalUiElement> selectedObjects =
        new List<GraphicalUiElement>();

    LineCircle rotationHandle;
    bool isRotationHighlighted;
    bool isRotationGrabbed;

    DimensionDisplay widthDimensionDisplay;
    DimensionDisplay heightDimensionDisplay;

    bool mHasGrabbed = false;
    private readonly IElementCommands _elementCommands;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly SelectionManager _selectionManager;
    private readonly IVariableInCategoryPropagationLogic _variableInCategoryPropagationLogic;

    public InputLibrary.Cursor Cursor
    {
        get
        {
            return InputLibrary.Cursor.Self;
        }
    }

    public override bool HasCursorOverHandles
    {
        get
        {
            var cursor = InputLibrary.Cursor.Self;
            float worldX = cursor.GetWorldX();
            float worldY = cursor.GetWorldY();

            if (_resizeInputHandler.HasCursorOver(worldX, worldY))
            {
                return true;
            }
            else if(isRotationHighlighted)
            {
                return true;
            }
            return false;
        }
    }

    #endregion

    public StandardWireframeEditor(Layer layer, 
        Color lineColor, 
        Color textColor, 
        global::Gum.Managers.HotkeyManager hotkeyManager,
        SelectionManager selectionManager,
        ISelectedState selectedState,
        IVariableInCategoryPropagationLogic variableInCategoryPropagationLogic,
        IWireframeObjectManager wireframeObjectManager)
        : base(
              hotkeyManager, 
              selectionManager,
              selectedState,
              layer,
              lineColor,
              textColor)
    {
        _elementCommands = Locator.GetRequiredService<IElementCommands>();
        _wireframeObjectManager = wireframeObjectManager;
        _selectionManager = selectionManager;
        _variableInCategoryPropagationLogic = variableInCategoryPropagationLogic;

        mResizeHandles = new ResizeHandles(layer, lineColor);
        mResizeHandles.ShowOrigin = true;
        mResizeHandles.Visible = false;

        _resizeInputHandler = new ResizeInputHandler(_context, mResizeHandles);

        rotationHandle = new LineCircle();
        rotationHandle.Color = Color.Yellow;
        ShapeManager.Self.Add(rotationHandle, layer);
        rotationHandle.Visible = false;

        widthDimensionDisplay = new DimensionDisplay();
        widthDimensionDisplay.AddToManagers(SystemManagers.Default, layer);
        widthDimensionDisplay.SetColor(lineColor, textColor);

        heightDimensionDisplay = new DimensionDisplay();
        heightDimensionDisplay.AddToManagers(SystemManagers.Default, layer);
        heightDimensionDisplay.SetColor(lineColor, textColor);
    }

    public override void Destroy()
    {
        mResizeHandles.Destroy();

        ShapeManager.Self.Remove(rotationHandle);

        widthDimensionDisplay.Destroy();
        heightDimensionDisplay.Destroy();
    }

    #region Activity

    public override void Activity(ICollection<GraphicalUiElement> selectedObjects, SystemManagers systemManagers)
    {
        if (selectedObjects.Count != 0 && _selectedState.SelectedStateSave != null && _selectedState.CustomCurrentStateSave == null)
        {
            var cursor = InputLibrary.Cursor.Self;
            float worldX = cursor.GetWorldX();
            float worldY = cursor.GetWorldY();

            _resizeInputHandler.UpdateHover(worldX, worldY);

            RefreshRotationGrabbed();

            PushActivity();

            ClickActivity();

            // ResizeInputHandler handles resize dragging
            if (cursor.PrimaryDown && grabbedState.HasMovedEnough)
            {
                _resizeInputHandler.HandleDrag();
            }

            // MoveInputHandler handles body grabbing (push/drag/release via PushActivity/ClickActivity)
            MoveInputHandlerDragActivity();

            RotationHandleGrabbingActivity();

            UpdateDimensionDisplay();

            bool shouldSkip = selectedObjects.Any(item => item.Tag is ScreenSave);

            if (!shouldSkip)
            {
                UpdateLockedVariables(selectedObjects);

                mResizeHandles.SetValuesFrom(selectedObjects);

                mResizeHandles.UpdateHandleSizes();

                UpdateRotationHandlePosition();
            }
        }
    }

    private void UpdateLockedVariables(ICollection<GraphicalUiElement> selectedObjects)
    {
        var item = selectedObjects.FirstOrDefault();

        IsXMovementEnabled = true;
        IsYMovementEnabled = true;
        IsWidthChangeEnabled = true;
        IsHeightChangeEnabled = true;


        if (item == null) return;

        var tag = item.Tag;

        RecursiveVariableFinder? rfv = null;

        if (tag is InstanceSave instance)
        {
            rfv = new RecursiveVariableFinder(instance, instance.ParentContainer);
        }
        if(tag is ElementSave element)
        {
            rfv = new RecursiveVariableFinder(_selectedState.SelectedStateSave);
        }
            
        var variableReferences = rfv?.GetVariableList("VariableReferences");

        if(variableReferences != null)
        {
            var list = variableReferences.ValueAsIList;

            foreach (string variableReference in list)
            {
                var split = variableReference.Split('=');

                if(split.Length == 2)
                {
                    var variable = split[0].Trim();

                    if(variable == "X")
                    {
                        IsXMovementEnabled = false;
                    }
                    if(variable == "Y")
                    {
                        IsYMovementEnabled = false;

                    }
                    if (variable == "Width")
                    {

                    }
                    if (variable == "Height")
                    {

                    }
                }
            }
        }
    }

    private void UpdateDimensionDisplay()
    {
        var sideOver = _resizeInputHandler.SideOver;

        var shouldShowHeight =
            sideOver == ResizeSide.TopLeft ||
            sideOver == ResizeSide.Top ||
            sideOver == ResizeSide.TopRight ||
            sideOver == ResizeSide.BottomLeft ||
            sideOver == ResizeSide.Bottom ||
            sideOver == ResizeSide.BottomRight;

        var shouldShowWidth =
            sideOver == ResizeSide.TopLeft ||
            sideOver == ResizeSide.Left ||
            sideOver == ResizeSide.BottomLeft ||
            sideOver == ResizeSide.TopRight ||
            sideOver == ResizeSide.Right ||
            sideOver == ResizeSide.BottomRight;

        widthDimensionDisplay.SetVisible(shouldShowWidth);
        if(shouldShowWidth)
        {
            widthDimensionDisplay.Activity(selectedObjects[0], WidthOrHeight.Width);
        }

        heightDimensionDisplay.SetVisible(shouldShowHeight);
        if(shouldShowHeight)
        {
            heightDimensionDisplay.Activity(selectedObjects[0], WidthOrHeight.Height);
        }
    }

    private void RotationHandleGrabbingActivity()
    {
        if(isRotationGrabbed)
        {
            var gue = selectedObjects.First();

            var originX = gue.AbsoluteX;
            var originY = gue.AbsoluteY;

            var cursorX = InputLibrary.Cursor.Self.GetWorldX();
            var cursorY = InputLibrary.Cursor.Self.GetWorldY();

            var angleInRadians = (float)System.Math.Atan2(cursorY - originY, cursorX - originX);

            var rotationValueDegrees =
                -MathHelper.ToDegrees(angleInRadians);

            if(_hotkeyManager.SnapRotationTo15Degrees.IsPressedInControl())
            {
                rotationValueDegrees = MathFunctions.RoundFloat(rotationValueDegrees, 15);
            }

            float parentRotation = 0;
            if(gue.Parent != null)
            {
                parentRotation = gue.Parent.GetAbsoluteRotation();
            }

            gue.Rotation = rotationValueDegrees - parentRotation;

            string nameWithInstance = "Rotation";

            if(_selectedState.SelectedInstance != null)
            {
                nameWithInstance = _selectedState.SelectedInstance.Name + 
                    "." + nameWithInstance;
            }

            _selectedState.SelectedStateSave.SetValue(nameWithInstance, rotationValueDegrees - parentRotation, 
                _selectedState.SelectedInstance, "float");

            _variableInCategoryPropagationLogic.PropagateVariablesInCategory(nameWithInstance,
                _selectedState.SelectedElement, _selectedState.SelectedStateCategorySave);

            _guiCommands.RefreshVariableValues();

        }
    }

    private void RefreshRotationGrabbed()
    {
        var cursor = InputLibrary.Cursor.Self;
        var worldX = cursor.GetWorldX();
        var worldY = cursor.GetWorldY();

        isRotationHighlighted = rotationHandle.HasCursorOver(worldX, worldY);
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
            float minimumOffset = 24 / Renderer.Self.Camera.Zoom;


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
            if(isRotationGrabbed)
            {
                DoEndOfSettingValuesLogic();
            }
            mHasGrabbed = false;
            isRotationGrabbed = false;
        }

        // Let handlers handle the release
        if (cursor.PrimaryClick)
        {
            _moveInputHandler.HandleRelease();
            _resizeInputHandler.HandleRelease();
        }
    }



    private void PushActivity()
    {
        // The selected object is set in the SelectionManager

        var cursor = InputLibrary.Cursor.Self;
        if (cursor.PrimaryPush)
        {
            // do this first to get the rotation handles to update to the right size/position to prevent accidental clicks
            UpdateRotationHandlePosition();

            RefreshRotationGrabbed();

            isRotationGrabbed = isRotationHighlighted;

            mHasChangedAnythingSinceLastPush = false;

            grabbedState.HandlePush();

            mHasGrabbed = _selectionManager.HasSelection;

            if (mHasGrabbed)
            {
                UpdateAspectRatioForGrabbedIpso();
            }

            // Let handlers know about the push
            var worldX = cursor.GetWorldX();
            var worldY = cursor.GetWorldY();
            _resizeInputHandler.HandlePush(worldX, worldY);
            _moveInputHandler.HandlePush(worldX, worldY);
        }
    }

    private void MoveInputHandlerDragActivity()
    {
        var cursor = InputLibrary.Cursor.Self;
        if (cursor.PrimaryDown && grabbedState.HasMovedEnough)
        {
            _moveInputHandler.HandleDrag();
        }
    }


    #endregion

    #region Update To

    public override void UpdateToSelection(ICollection<GraphicalUiElement> selectedObjects)
    {
        this.selectedObjects.Clear();
        this.selectedObjects.AddRange(selectedObjects);

        _resizeInputHandler.OnSelectionChanged();

        if (selectedObjects.Count == 0 || selectedObjects.Any(item => item.Tag is ScreenSave))
        {
            mResizeHandles.Visible = false;
            rotationHandle.Visible = false;
        }
        else
        {
            mResizeHandles.Visible = true;
            rotationHandle.Visible = true;
            mResizeHandles.SetValuesFrom(selectedObjects);
            mResizeHandles.UpdateHandleSizes();
        }
    }

    #endregion

    #region Changing the cursor (for resizing

    public override System.Windows.Forms.Cursor GetWindowsCursorToShow(
        System.Windows.Forms.Cursor defaultCursor, float worldXAt, float worldYAt)
    {
        var cursorFromHandler = _resizeInputHandler.GetCursorToShow(worldXAt, worldYAt);
        return cursorFromHandler ?? defaultCursor;
    }

    #endregion



}
