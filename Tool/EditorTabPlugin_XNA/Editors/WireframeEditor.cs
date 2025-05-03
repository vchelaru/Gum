﻿using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins;
using Gum.ToolStates;
using Gum.Undo;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System.Collections.Generic;
using System.Linq;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Vector2 = System.Numerics.Vector2;
using Matrix = System.Numerics.Matrix4x4;
using Gum.Managers;
using System.Collections;
using System;
using Gum.PropertyGridHelpers;
using System.Security.Policy;
using EditorTabPlugin_XNA.ExtensionMethods;

namespace Gum.Wireframe;

public abstract class WireframeEditor
{
    protected HotkeyManager _hotkeyManager { get; private set; }

    private readonly SelectionManager _selectionManager;
    protected GrabbedState grabbedState = new GrabbedState();

    protected bool mHasChangedAnythingSinceLastPush = false;

    protected float aspectRatioOnGrab;

    public bool IsXMovementEnabled { get; set; } = true;
    public bool IsYMovementEnabled { get; set; } = true;
    public bool IsWidthChangeEnabled { get; set; } = true;
    public bool IsHeightChangeEnabled { get; set; } = true;


    public bool RestrictToUnitValues { get; set; }

    public WireframeEditor(global::Gum.Managers.HotkeyManager hotkeyManager,
        SelectionManager selectionManager)
    {
        _hotkeyManager = hotkeyManager;
        _selectionManager = selectionManager;
    }

    public abstract void UpdateToSelection(ICollection<GraphicalUiElement> selectedObjects);

    public abstract bool HasCursorOver { get; }

    public void UpdateAspectRatioForGrabbedIpso()
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
                aspectRatioOnGrab = width / height;
            }
        }
    }

    public abstract void Activity(ICollection<GraphicalUiElement> selectedObjects);

    public abstract System.Windows.Forms.Cursor GetWindowsCursorToShow(
        System.Windows.Forms.Cursor defaultCursor, float worldXAt, float worldYAt);

    public abstract void Destroy();

    public virtual bool TryHandleDelete()
    {
        return false;
    }

    protected void ApplyCursorMovement(InputLibrary.Cursor cursor)
    {
        float xToMoveBy = IsXMovementEnabled 
            ? cursor.XChange / Renderer.Self.Camera.Zoom
            : 0;
        float yToMoveBy = IsYMovementEnabled
            ? cursor.YChange / Renderer.Self.Camera.Zoom
            : 0;

        var vector2 = new Vector2(xToMoveBy, yToMoveBy);
        var selectedObject = WireframeObjectManager.Self.GetSelectedRepresentation();
        if(selectedObject?.Parent != null)
        {
            var parentRotationDegrees = selectedObject.Parent.GetAbsoluteRotation();
            if(parentRotationDegrees != 0)
            {
                var parentRotation = MathHelper.ToRadians(parentRotationDegrees);

                global::RenderingLibrary.Math.MathFunctions.RotateVector(ref vector2, parentRotation);

                xToMoveBy = vector2.X;
                yToMoveBy = vector2.Y;
            }
        }

        grabbedState.AccumulatedXOffset += xToMoveBy;
        grabbedState.AccumulatedYOffset += yToMoveBy;

        var shouldSnapX = _selectionManager.SelectedGues.Any(item => item.XUnits.GetIsPixelBased());
        var shouldSnapY = _selectionManager.SelectedGues.Any(item => item.YUnits.GetIsPixelBased());

        var effectiveXToMoveBy = xToMoveBy;
        var effectiveYToMoveBy = yToMoveBy;

        if(shouldSnapX)
        {
            var accumulatedXAsInt = (int)grabbedState.AccumulatedXOffset;
            effectiveXToMoveBy = 0;
            if(accumulatedXAsInt != 0)
            {
                effectiveXToMoveBy = accumulatedXAsInt;
                grabbedState.AccumulatedXOffset -= accumulatedXAsInt;
            }
        }
        if(shouldSnapY)
        {
            var accumulatedYAsInt = (int)grabbedState.AccumulatedYOffset;
            effectiveYToMoveBy = 0;
            if(accumulatedYAsInt != 0)
            {
                effectiveYToMoveBy = accumulatedYAsInt;
                grabbedState.AccumulatedYOffset -= accumulatedYAsInt;
            }
        }

        var editingCommands = GumCommands.Self.ProjectCommands.ElementCommands;
        var didMove = editingCommands.MoveSelectedObjectsBy(effectiveXToMoveBy, effectiveYToMoveBy);

        bool isLockedToAxis = _hotkeyManager.LockMovementToAxis.IsPressed(InputLibrary.Keyboard.Self);


        if (SelectedState.Self.SelectedInstances.Count() == 0 &&
            (SelectedState.Self.SelectedComponent != null || SelectedState.Self.SelectedStandardElement != null))
        {
            if (isLockedToAxis)
            {
                var xOrY = grabbedState.AxisMovedFurthestAlong;

                if (xOrY == XOrY.X)
                {
                    var gue = WireframeObjectManager.Self.GetRepresentation(SelectedState.Self.SelectedElement);

                    gue.Y = grabbedState.ComponentPosition.Y;
                }
                else if(xOrY == XOrY.Y)
                {

                    var gue = WireframeObjectManager.Self.GetRepresentation(SelectedState.Self.SelectedElement);

                    gue.X = grabbedState.ComponentPosition.X;
                }
            }
        }
        else
        {
            if (isLockedToAxis)
            {
                var selectedInstances = SelectedState.Self.SelectedInstances;

                foreach (InstanceSave instance in selectedInstances)
                {

                    var xOrY = grabbedState.AxisMovedFurthestAlong;

                    if (xOrY == XOrY.X)
                    {
                        var gue = WireframeObjectManager.Self.GetRepresentation(instance);

                        gue.Y = grabbedState.InstancePositions[instance].AbsoluteY;
                    }
                    else if(xOrY == XOrY.Y)
                    {

                        var gue = WireframeObjectManager.Self.GetRepresentation(instance);

                        gue.X = grabbedState.InstancePositions[instance].AbsoluteX;
                    }

                }
            }
        }

        if (didMove)
        {
            if(isLockedToAxis)
            {
                // December 3, 2024 
                // Currently when the
                // user moves snapped to
                // an axis, the unsnapped
                // value is displayed in the 
                // UI. By calling ApplyAxisLockToSelectedState,
                // the values do get snapped to the UI, but this
                // causes the non-moved axis to snap back to the default
                // value, so switching axes (if the cursor has moved nearly
                // perfect diagonally), causes the object ot reset back to the
                // origin. The value does get snapped back when the cursor is let
                // go so we'll just deal with that problem for now.
                //ApplyAxisLockToSelectedState();

                //GumCommands.Self.GuiCommands.RefreshPropertyGrid();
            }
            mHasChangedAnythingSinceLastPush = true;
        }
    }

    protected void ApplyAxisLockToSelectedState()
    {
        var axis = grabbedState.AxisMovedFurthestAlong;

        bool isElementSelected = SelectedState.Self.SelectedInstances.Count() == 0 &&
                 // check specifically for components or standard elements, since Screens can't be moved
                 (SelectedState.Self.SelectedComponent != null || SelectedState.Self.SelectedStandardElement != null);


        if (axis == XOrY.X)
        {
            // If the X axis is the furthest-moved, set the Y values back to what they were.
            if (isElementSelected)
            {
                SelectedState.Self.SelectedStateSave.SetValue("Y", grabbedState.ComponentPosition.Y, "float");
            }
            else
            {
                foreach (var instance in SelectedState.Self.SelectedInstances)
                {
                    SelectedState.Self.SelectedStateSave.SetValue(instance.Name + ".Y", grabbedState.InstancePositions[instance].StateY, "float");
                }
            }
        }
        else if(axis == XOrY.Y)
        {
            // If the Y axis is the furthest-moved, set the X values back to what they were.
            if (isElementSelected)
            {
                SelectedState.Self.SelectedStateSave.SetValue("X", grabbedState.ComponentPosition.X, "float");
            }
            else
            {
                foreach (var instance in SelectedState.Self.SelectedInstances)
                {
                    SelectedState.Self.SelectedStateSave.SetValue(instance.Name + ".X", grabbedState.InstancePositions[instance].StateX, "float");
                }
            }
        }

    }


    protected void DoEndOfSettingValuesLogic()
    {
        var selectedElement = SelectedState.Self.SelectedElement;
        var stateSave = SelectedState.Self.SelectedStateSave;
        if(stateSave == null)
        {
            throw new System.InvalidOperationException("The SelectedStateSave is null, this should not happen");
        }

        GumCommands.Self.FileCommands.TryAutoSaveElement(selectedElement);

        using var undoLock = UndoManager.Self.RequestLock();

        GumCommands.Self.GuiCommands.RefreshVariableValues();

        var element = SelectedState.Self.SelectedElement;

        foreach (var possiblyChangedVariable in stateSave.Variables.ToList())
        {
            var oldValue = grabbedState.StateSave.GetValue(possiblyChangedVariable.Name);

            if (DoValuesDiffer(stateSave, possiblyChangedVariable.Name, oldValue))
            {
                var instance = element.GetInstance(possiblyChangedVariable.SourceObject);

                // should this be:
                SetVariableLogic.Self.PropertyValueChanged(possiblyChangedVariable.GetRootName(), oldValue, 
                   instance, 
                   refresh: true,
                   recordUndo: false,
                   trySave: false);
                // instead of this?
                //PluginManager.Self.VariableSet(element, instance, possiblyChangedVariable.GetRootName(), oldValue);
            }
        }

        foreach (var possiblyChangedVariableList in stateSave.VariableLists)
        {
            var oldValue = grabbedState.StateSave.GetVariableListSave(possiblyChangedVariableList.Name);

            if (DoValuesDiffer(stateSave, possiblyChangedVariableList.Name, oldValue))
            {
                var instance = element.GetInstance(possiblyChangedVariableList.SourceObject);
                PluginManager.Self.VariableSet(element, instance, possiblyChangedVariableList.GetRootName(), oldValue);
            }
        }

        mHasChangedAnythingSinceLastPush = false;
    }

    protected bool DoValuesDiffer(StateSave newStateSave, string variableName, object oldValue)
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
        if (newValue == null && oldValue == null)
        {
            return false;
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
            else if(oldValue is IList oldList)
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
        if(oldList == null && newList == null)
        {
            return true;
        }
        if (oldList == null || newList == null)
        {
            return false;
        }

        for(int i = 0; i < oldList.Count; i++)
        {
            if (oldList[i].Equals(newList[i]) == false)
            {
                return false;
            }
        }
        return true;
    }
}
