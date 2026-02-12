using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using EditorTabPlugin_XNA.ExtensionMethods;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment;
using Gum.Input;
using Gum.Wireframe.Editors.Visuals;

namespace Gum.Wireframe.Editors.Handlers;

/// <summary>
/// Handles resizing objects via the 8 corner/edge resize handles.
/// </summary>
public class ResizeInputHandler : InputHandlerBase
{
    private ResizeSide _sideGrabbed = ResizeSide.None;
    private ResizeSide _sideOver = ResizeSide.None;

    private readonly ResizeHandlesVisual _resizeHandlesVisual;

    public override int Priority => 90; // Higher than move, lower than rotation

    /// <summary>
    /// The resize side currently under the cursor (if any).
    /// Used by visual components like dimension displays.
    /// </summary>
    public ResizeSide SideOver => _sideOver;

    public ResizeInputHandler(EditorContext context, ResizeHandlesVisual resizeHandlesVisual)
        : base(context)
    {
        _resizeHandlesVisual = resizeHandlesVisual;
    }

    public override bool HasCursorOver(float worldX, float worldY)
    {
        if (!_resizeHandlesVisual.Visible)
        {
            return false;
        }

        return _resizeHandlesVisual.Handles.GetSideOver(worldX, worldY) != ResizeSide.None;
    }

    public override Cursor? GetCursorToShow(float worldX, float worldY)
    {
        return _sideOver switch
        {
            ResizeSide.TopLeft or ResizeSide.BottomRight => System.Windows.Forms.Cursors.SizeNWSE,
            ResizeSide.TopRight or ResizeSide.BottomLeft => System.Windows.Forms.Cursors.SizeNESW,
            ResizeSide.Top or ResizeSide.Bottom => System.Windows.Forms.Cursors.SizeNS,
            ResizeSide.Left or ResizeSide.Right => System.Windows.Forms.Cursors.SizeWE,
            _ => null
        };
    }

    public override void UpdateHover(float worldX, float worldY)
    {
        if (!_resizeHandlesVisual.Visible)
        {
            _sideOver = ResizeSide.None;
            return;
        }

        var cursor = InputLibrary.Cursor.Self;

        // If dragging, don't change the side over
        if (cursor.PrimaryPush || (!cursor.PrimaryDown && !cursor.PrimaryClick))
        {
            _sideOver = _resizeHandlesVisual.Handles.GetSideOver(worldX, worldY);
        }
    }

    protected override void OnPush(float worldX, float worldY)
    {
        _sideGrabbed = _sideOver;
        Context.UpdateAspectRatioForGrabbedIpso();
    }

    protected override void OnDrag()
    {
        if (_sideGrabbed == ResizeSide.None) return;

        float cursorXChange = GetCursorXChange();
        float cursorYChange = GetCursorYChange();

        if (cursorXChange == 0 && cursorYChange == 0) return;

        Context.GrabbedState.AccumulatedXOffset += cursorXChange;
        Context.GrabbedState.AccumulatedYOffset += cursorYChange;

        var shouldSnapX = Context.SelectionManager.SelectedGues.Any(item => item.WidthUnits.GetIsPixelBased());
        var shouldSnapY = Context.SelectionManager.SelectedGues.Any(item => item.HeightUnits.GetIsPixelBased());

        var effectiveXToMoveBy = cursorXChange;
        var effectiveYToMoveBy = cursorYChange;

        if (shouldSnapX)
        {
            var accumulatedXAsInt = (int)Context.GrabbedState.AccumulatedXOffset;
            effectiveXToMoveBy = 0;
            if (accumulatedXAsInt != 0)
            {
                effectiveXToMoveBy = accumulatedXAsInt;
                Context.GrabbedState.AccumulatedXOffset -= accumulatedXAsInt;
            }
        }
        if (shouldSnapY)
        {
            var accumulatedYAsInt = (int)Context.GrabbedState.AccumulatedYOffset;
            effectiveYToMoveBy = 0;
            if (accumulatedYAsInt != 0)
            {
                effectiveYToMoveBy = accumulatedYAsInt;
                Context.GrabbedState.AccumulatedYOffset -= accumulatedYAsInt;
            }
        }

        bool hasChange = ApplySizeChange(effectiveXToMoveBy, effectiveYToMoveBy);

        if (hasChange)
        {
            Context.GuiCommands.RefreshVariables();
            MarkAsChanged();
        }
    }

    protected override void OnRelease()
    {
        if (Context.HasChangedAnythingSinceLastPush)
        {
            DoEndOfSettingValuesLogic();
        }

        _sideGrabbed = ResizeSide.None;
    }

    public override void OnSelectionChanged()
    {
        // Visibility and updates are now handled by ResizeHandlesVisual.UpdateToSelection
    }

    private bool ApplySizeChange(float cursorXChange, float cursorYChange)
    {
        bool hasChangeOccurred = false;
        var elementStack = Context.SelectedState.GetTopLevelElementStack();

        if (Context.SelectionManager.HasSelection &&
            Context.SelectedState.SelectedInstances.Count() == 0)
        {
            // That means we have the entire component selected
            hasChangeOccurred |= ApplySizeChangeForInstance(
                cursorXChange, cursorYChange, instanceSave: null, elementStack: elementStack);
        }

        foreach (InstanceSave save in Context.SelectedState.SelectedInstances)
        {
            hasChangeOccurred |= ApplySizeChangeForInstance(
                cursorXChange, cursorYChange, instanceSave: save, elementStack: elementStack);
        }

        return hasChangeOccurred;
    }

    private bool ApplySizeChangeForInstance(
        float cursorXChange,
        float cursorYChange,
        InstanceSave? instanceSave,
        List<ElementWithState> elementStack)
    {
        CalculateMultipliers(instanceSave, elementStack,
            out float changeXMultiplier, out float changeYMultiplier,
            out float widthMultiplier, out float heightMultiplier);

        // Apply disabled axes
        var shouldDisableX = (Context.IsXMovementEnabled == false && changeXMultiplier != 0) ||
            (Context.IsWidthChangeEnabled == false && widthMultiplier != 0);
        var shouldDisableY = (Context.IsYMovementEnabled == false && changeYMultiplier != 0) ||
            (Context.IsHeightChangeEnabled == false && heightMultiplier != 0);

        if (shouldDisableX)
        {
            changeXMultiplier = 0;
            widthMultiplier = 0;
        }
        if (shouldDisableY)
        {
            changeYMultiplier = 0;
            heightMultiplier = 0;
        }

        AdjustCursorChangeValuesForAxisLockedDrag(
            ref cursorXChange, ref cursorYChange, instanceSave, elementStack);

        bool hasChange = false;

        // Apply position changes
        Vector2 reposition = new Vector2(
            cursorXChange * changeXMultiplier,
            cursorYChange * changeYMultiplier);
        // invert Y so up is positive
        reposition.Y *= -1;

        GraphicalUiElement? representation = null;

        if (instanceSave != null)
        {
            representation = Context.WireframeObjectManager.GetRepresentation(instanceSave);
        }
        else
        {
            representation = Context.WireframeObjectManager.GetRepresentation(elementStack.Last().Element);
        }

        float rotation = MathHelper.ToRadians(representation?.GetAbsoluteRotation() ?? 0);
        MathFunctions.RotateVector(ref reposition, rotation);
        // flip Y back
        reposition.Y *= -1;

        if (reposition.X != 0)
        {
            hasChange = true;
            if (instanceSave != null)
            {
                Context.ElementCommands.ModifyVariable("X", reposition.X, instanceSave);
            }
            else
            {
                Context.ElementCommands.ModifyVariable("X", reposition.X, elementStack.Last().Element);
            }
        }
        if (reposition.Y != 0)
        {
            hasChange = true;
            if (instanceSave != null)
            {
                Context.ElementCommands.ModifyVariable("Y", reposition.Y, instanceSave);
            }
            else
            {
                Context.ElementCommands.ModifyVariable("Y", reposition.Y, elementStack.Last().Element);
            }
        }

        // Apply size changes
        if (heightMultiplier != 0 && cursorYChange != 0)
        {
            hasChange = true;
            if (instanceSave != null)
            {
                Context.ElementCommands.ModifyVariable("Height", cursorYChange * heightMultiplier, instanceSave);
            }
            else
            {
                Context.ElementCommands.ModifyVariable("Height", cursorYChange * heightMultiplier, elementStack.Last().Element);
            }
        }
        if (widthMultiplier != 0 && cursorXChange != 0)
        {
            hasChange = true;
            if (instanceSave != null)
            {
                Context.ElementCommands.ModifyVariable("Width", cursorXChange * widthMultiplier, instanceSave);
            }
            else
            {
                Context.ElementCommands.ModifyVariable("Width", cursorXChange * widthMultiplier, elementStack.Last().Element);
            }
        }

        return hasChange;
    }

    private void CalculateMultipliers(
        InstanceSave? instanceSave,
        List<ElementWithState> elementStack,
        out float changeXMultiplier,
        out float changeYMultiplier,
        out float widthMultiplier,
        out float heightMultiplier)
    {
        changeXMultiplier = 0;
        changeYMultiplier = 0;
        widthMultiplier = 0;
        heightMultiplier = 0;

        var ipso = instanceSave != null
            ? Context.WireframeObjectManager.GetRepresentation(instanceSave, elementStack)
            : Context.WireframeObjectManager.GetRepresentation(Context.SelectedState.SelectedElement);

        if (ipso == null) return;

        switch (_sideGrabbed)
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

        if (_resizeHandlesVisual.Handles.Width != 0)
        {
            widthMultiplier *= (((IPositionedSizedObject)ipso).Width / _resizeHandlesVisual.Handles.Width);
        }

        if (_resizeHandlesVisual.Handles.Height != 0)
        {
            heightMultiplier *= (((IPositionedSizedObject)ipso).Height / _resizeHandlesVisual.Handles.Height);
        }

        if (Context.HotkeyManager.ResizeFromCenter.IsPressedInControl())
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

    private float GetXMultiplierForLeft(InstanceSave? instanceSave, IPositionedSizedObject ipso)
    {
        object? xOriginAsObject = Context.ElementCommands.GetCurrentValueForVariable("XOrigin", instanceSave);
        if (xOriginAsObject != null)
        {
            HorizontalAlignment xOrigin = (HorizontalAlignment)xOriginAsObject;
            float ratioOver = GetRatioXOverInSelection(ipso, xOrigin);
            return 1 - ratioOver;
        }
        return 0;
    }

    private float GetYMultiplierForTop(InstanceSave? instanceSave, GraphicalUiElement gue)
    {
        object? yOriginAsObject = Context.ElementCommands.GetCurrentValueForVariable("YOrigin", instanceSave);
        if (yOriginAsObject != null)
        {
            VerticalAlignment yOrigin = (VerticalAlignment)yOriginAsObject;
            float ratioOver = GetRatioYDownInSelection(gue, yOrigin);
            return 1 - ratioOver;
        }
        return 0;
    }

    private float GetYMultiplierForBottom(InstanceSave? instanceSave, GraphicalUiElement ipso)
    {
        object? yOriginAsObject = Context.ElementCommands.GetCurrentValueForVariable("YOrigin", instanceSave);
        if (yOriginAsObject != null)
        {
            VerticalAlignment yOrigin = (VerticalAlignment)yOriginAsObject;
            float ratioOver = GetRatioYDownInSelection(ipso, yOrigin);
            return ratioOver;
        }
        return 0;
    }

    private float GetXMultiplierForRight(InstanceSave? instanceSave, IPositionedSizedObject ipso)
    {
        object? xOriginAsObject = Context.ElementCommands.GetCurrentValueForVariable("XOrigin", instanceSave);
        if (xOriginAsObject != null)
        {
            HorizontalAlignment xOrigin = (HorizontalAlignment)xOriginAsObject;
            float ratioOver = GetRatioXOverInSelection(ipso, xOrigin);
            return ratioOver;
        }
        return 0;
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
        else // Right
        {
            return 1;
        }
    }

    private static float GetRatioYDownInSelection(GraphicalUiElement gue, VerticalAlignment verticalAlignment)
    {
        if (verticalAlignment == VerticalAlignment.Top)
        {
            return 0;
        }
        else if (verticalAlignment == VerticalAlignment.TextBaseline)
        {
            if (gue.RenderableComponent is Text text && text.Height > 0)
            {
                return 1 - (text.DescenderHeight * text.FontScale / text.Height);
            }
            else
            {
                return 1;
            }
        }
        else if (verticalAlignment == VerticalAlignment.Center)
        {
            return .5f;
        }
        else // Bottom
        {
            return 1;
        }
    }

    private void AdjustCursorChangeValuesForAxisLockedDrag(
        ref float cursorXChange,
        ref float cursorYChange,
        InstanceSave? instanceSave,
        List<ElementWithState> elementStack)
    {
        var isAxisLocked = Context.HotkeyManager.LockMovementToAxis.IsPressedInControl();
        if (!isAxisLocked) return;

        bool supportsLockedAxis =
            _sideGrabbed == ResizeSide.TopLeft || _sideGrabbed == ResizeSide.TopRight ||
            _sideGrabbed == ResizeSide.BottomLeft || _sideGrabbed == ResizeSide.BottomRight;

        if (supportsLockedAxis && instanceSave != null)
        {
            IRenderableIpso? ipso = Context.WireframeObjectManager.GetRepresentation(instanceSave, elementStack);
            if (ipso == null) return;

            var cursor = InputLibrary.Cursor.Self;
            float cursorX = cursor.GetWorldX();
            float cursorY = cursor.GetWorldY();

            float top = ipso.GetAbsoluteTop();
            float bottom = ipso.GetAbsoluteBottom();
            float left = ipso.GetAbsoluteLeft();
            float right = ipso.GetAbsoluteRight();

            float absoluteXDifference = 1;
            float absoluteYDifference = 1;

            switch (_sideGrabbed)
            {
                case ResizeSide.BottomRight:
                    absoluteXDifference = Math.Abs(left - cursorX);
                    absoluteYDifference = Math.Abs(top - cursorY);
                    break;
                case ResizeSide.BottomLeft:
                    absoluteXDifference = Math.Abs(right - cursorX);
                    absoluteYDifference = Math.Abs(top - cursorY);
                    break;
                case ResizeSide.TopLeft:
                    absoluteXDifference = Math.Abs(right - cursorX);
                    absoluteYDifference = Math.Abs(bottom - cursorY);
                    break;
                case ResizeSide.TopRight:
                    absoluteXDifference = Math.Abs(left - cursorX);
                    absoluteYDifference = Math.Abs(bottom - cursorY);
                    break;
            }

            float aspectRatio = absoluteXDifference / absoluteYDifference;

            if (aspectRatio > Context.AspectRatioOnGrab)
            {
                float yToUse = 0;
                // We use the X, but adjust the Y
                switch (_sideGrabbed)
                {
                    case ResizeSide.BottomRight:
                        cursorXChange = cursorX - right;
                        yToUse = top + absoluteXDifference / Context.AspectRatioOnGrab;
                        cursorYChange = yToUse - bottom;
                        break;
                    case ResizeSide.BottomLeft:
                        cursorXChange = cursorX - left;
                        yToUse = top + absoluteXDifference / Context.AspectRatioOnGrab;
                        cursorYChange = yToUse - bottom;
                        break;
                    case ResizeSide.TopRight:
                        cursorXChange = cursorX - right;
                        yToUse = bottom - absoluteXDifference / Context.AspectRatioOnGrab;
                        cursorYChange = yToUse - top;
                        break;
                    case ResizeSide.TopLeft:
                        cursorXChange = cursorX - left;
                        yToUse = bottom - absoluteXDifference / Context.AspectRatioOnGrab;
                        cursorYChange = yToUse - top;
                        break;
                }
            }
            else
            {
                float xToUse;
                // We use the Y, but adjust the X
                switch (_sideGrabbed)
                {
                    case ResizeSide.BottomRight:
                        cursorYChange = cursorY - bottom;
                        xToUse = left + absoluteYDifference * Context.AspectRatioOnGrab;
                        cursorXChange = xToUse - right;
                        break;
                    case ResizeSide.BottomLeft:
                        cursorYChange = cursorY - bottom;
                        xToUse = right - absoluteYDifference * Context.AspectRatioOnGrab;
                        cursorXChange = xToUse - left;
                        break;
                    case ResizeSide.TopRight:
                        cursorYChange = cursorY - top;
                        xToUse = left + absoluteYDifference * Context.AspectRatioOnGrab;
                        cursorXChange = xToUse - right;
                        break;
                    case ResizeSide.TopLeft:
                        cursorYChange = cursorY - top;
                        xToUse = right - absoluteYDifference * Context.AspectRatioOnGrab;
                        cursorXChange = xToUse - left;
                        break;
                }
            }
        }
    }

    private void DoEndOfSettingValuesLogic()
    {
        var selectedElement = Context.SelectedState.SelectedElement;
        var stateSave = Context.SelectedState.SelectedStateSave;
        if (stateSave == null)
        {
            throw new InvalidOperationException("The SelectedStateSave is null, this should not happen");
        }

        Context.FileCommands.TryAutoSaveElement(selectedElement);

        using var undoLock = Context.UndoManager.RequestLock();

        Context.GuiCommands.RefreshVariableValues();

        var element = Context.SelectedState.SelectedElement;

        foreach (var possiblyChangedVariable in stateSave.Variables.ToList())
        {
            var oldValue = Context.GrabbedState.StateSave.GetValue(possiblyChangedVariable.Name);

            if (DoValuesDiffer(stateSave, possiblyChangedVariable.Name, oldValue))
            {
                var instance = element.GetInstance(possiblyChangedVariable.SourceObject);

                // should this be:
                Context.SetVariableLogic.PropertyValueChanged(possiblyChangedVariable.GetRootName(),
                   oldValue,
                   instance,
                   element.DefaultState,
                   refresh: true,
                   recordUndo: false,
                   trySave: false);
                // instead of this?
                //PluginManager.Self.VariableSet(element, instance, possiblyChangedVariable.GetRootName(), oldValue);
            }
        }

        foreach (var possiblyChangedVariableList in stateSave.VariableLists)
        {
            var oldValue = Context.GrabbedState.StateSave.GetVariableListSave(possiblyChangedVariableList.Name);

            if (DoValuesDiffer(stateSave, possiblyChangedVariableList.Name, oldValue))
            {
                var instance = element.GetInstance(possiblyChangedVariableList.SourceObject);
                PluginManager.Self.VariableSet(element, instance, possiblyChangedVariableList.GetRootName(), oldValue);
            }
        }

        Context.HasChangedAnythingSinceLastPush = false;
    }

    private bool DoValuesDiffer(StateSave newStateSave, string variableName, object oldValue)
    {
        var newValue = newStateSave.GetValue(variableName);
        if (newValue == null && oldValue != null)
        {
            return true;
        }
        if (newValue != null && oldValue == null)
        {
            return true;
        }
        if(oldValue == null && newValue == null)
        {
            return true;
        }
        // neither are null
        else
        {
            if (oldValue is float)
            {
                var oldFloat = (float)oldValue;
                var newFloat = (float)newValue;

                return oldFloat != newFloat;
            }
            else if (oldValue is string)
            {
                return (string)oldValue != (string)newValue;
            }
            else if (oldValue is bool)
            {
                return (bool)oldValue != (bool)newValue;
            }
            else if (oldValue is int)
            {
                return (int)oldValue != (int)newValue;
            }
            else if (oldValue is Vector2)
            {
                return (Vector2)oldValue != (Vector2)newValue;
            }
            else if (oldValue is IList oldList)
            {
                return AreListsSame(oldList, (IList)newValue);
            }
            else
            {
                return oldValue.Equals(newValue) == false;
            }
        }
    }

    private bool AreListsSame(IList oldList, IList newList)
    {
        if (oldList == null && newList == null)
        {
            return true;
        }
        if (oldList == null || newList == null)
        {
            return false;
        }

        for (int i = 0; i < oldList.Count; i++)
        {
            if (oldList[i].Equals(newList[i]) == false)
            {
                return false;
            }
        }
        return true;
    }
}
