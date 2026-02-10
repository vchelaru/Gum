using System.Collections;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using EditorTabPlugin_XNA.ExtensionMethods;
using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;

namespace Gum.Wireframe.Editors.Handlers;

/// <summary>
/// Handles moving (dragging) the selected object(s) by their body.
/// </summary>
public class MoveInputHandler : InputHandlerBase
{
    private bool _hasGrabbed = false;

    public override int Priority => 80; // Lower than resize/rotation

    public MoveInputHandler(EditorContext context) : base(context) { }

    public override bool HasCursorOver(float worldX, float worldY)
    {
        return Context.SelectionManager.IsOverBody;
    }

    public override Cursor? GetCursorToShow(float worldX, float worldY)
    {
        if (Context.SelectionManager.IsOverBody)
        {
            return System.Windows.Forms.Cursors.SizeAll;
        }
        return null;
    }

    protected override void OnPush(float worldX, float worldY)
    {
        _hasGrabbed = Context.SelectionManager.HasSelection;

        if (_hasGrabbed)
        {
            Context.UpdateAspectRatioForGrabbedIpso();
        }
    }

    protected override void OnDrag()
    {
        if (!_hasGrabbed || !Context.SelectionManager.IsOverBody) return;

        ApplyCursorMovement();
    }

    protected override void OnRelease()
    {
        if (Context.HasChangedAnythingSinceLastPush)
        {
            // Apply axis lock if held
            if (Context.HotkeyManager.LockMovementToAxis.IsPressedInControl())
            {
                ApplyAxisLockToSelectedState();
                Context.GuiCommands.RefreshVariables();
            }

            // Snap to unit values if enabled
            if (Context.RestrictToUnitValues)
            {
                SnapSelectedToUnitValues();
            }

            DoEndOfSettingValuesLogic();
        }

        _hasGrabbed = false;
    }

    private void ApplyCursorMovement()
    {
        var cursor = InputLibrary.Cursor.Self;

        float xToMoveBy = Context.IsXMovementEnabled
            ? cursor.XChange / Renderer.Self.Camera.Zoom
            : 0;
        float yToMoveBy = Context.IsYMovementEnabled
            ? cursor.YChange / Renderer.Self.Camera.Zoom
            : 0;

        var vector2 = new Vector2(xToMoveBy, yToMoveBy);
        var selectedObject = Context.WireframeObjectManager.GetSelectedRepresentation();

        if (selectedObject?.Parent != null)
        {
            var parentRotationDegrees = selectedObject.Parent.GetAbsoluteRotation();
            if (parentRotationDegrees != 0)
            {
                var parentRotation = MathHelper.ToRadians(parentRotationDegrees);
                MathFunctions.RotateVector(ref vector2, parentRotation);
                xToMoveBy = vector2.X;
                yToMoveBy = vector2.Y;
            }
        }

        Context.GrabbedState.AccumulatedXOffset += xToMoveBy;
        Context.GrabbedState.AccumulatedYOffset += yToMoveBy;

        var shouldSnapX = Context.SelectionManager.SelectedGues.Any(
            item => item.XUnits.GetIsPixelBased());
        var shouldSnapY = Context.SelectionManager.SelectedGues.Any(
            item => item.YUnits.GetIsPixelBased());

        var effectiveXToMoveBy = xToMoveBy;
        var effectiveYToMoveBy = yToMoveBy;

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

        var didMove = Context.ElementCommands.MoveSelectedObjectsBy(effectiveXToMoveBy, effectiveYToMoveBy);

        if (didMove)
        {
            ApplyAxisLockIfNeeded();
            MarkAsChanged();
        }
    }

    private void ApplyAxisLockIfNeeded()
    {
        bool isLockedToAxis = Context.HotkeyManager.LockMovementToAxis.IsPressedInControl();
        if (!isLockedToAxis) return;

        var selectedInstances = Context.SelectedState.SelectedInstances;

        if (selectedInstances.Count() == 0 &&
            (Context.SelectedState.SelectedComponent != null ||
             Context.SelectedState.SelectedStandardElement != null))
        {
            // Component/element selected
            var xOrY = Context.GrabbedState.AxisMovedFurthestAlong;
            var gue = Context.WireframeObjectManager.GetRepresentation(
                Context.SelectedState.SelectedElement);

            if (xOrY == XOrY.X)
            {
                gue.Y = Context.GrabbedState.ComponentPosition.Y;
            }
            else if (xOrY == XOrY.Y)
            {
                gue.X = Context.GrabbedState.ComponentPosition.X;
            }
        }
        else
        {
            // Instances selected
            foreach (InstanceSave instance in selectedInstances)
            {
                var xOrY = Context.GrabbedState.AxisMovedFurthestAlong;
                var gue = Context.WireframeObjectManager.GetRepresentation(instance);

                if (xOrY == XOrY.X)
                {
                    gue.Y = Context.GrabbedState.InstancePositions[instance].AbsoluteY;
                }
                else if (xOrY == XOrY.Y)
                {
                    gue.X = Context.GrabbedState.InstancePositions[instance].AbsoluteX;
                }
            }
        }
    }

    private void ApplyAxisLockToSelectedState()
    {
        var axis = Context.GrabbedState.AxisMovedFurthestAlong;

        bool isElementSelected = Context.SelectedState.SelectedInstances.Count() == 0 &&
                 (Context.SelectedState.SelectedComponent != null || Context.SelectedState.SelectedStandardElement != null);

        if (axis == XOrY.X)
        {
            // If the X axis is the furthest-moved, set the Y values back to what they were.
            if (isElementSelected)
            {
                Context.SelectedState.SelectedStateSave.SetValue("Y", Context.GrabbedState.ComponentPosition.Y, "float");
            }
            else
            {
                foreach (var instance in Context.SelectedState.SelectedInstances)
                {
                    Context.SelectedState.SelectedStateSave.SetValue(instance.Name + ".Y", Context.GrabbedState.InstancePositions[instance].StateY, "float");
                }
            }
        }
        else if (axis == XOrY.Y)
        {
            // If the Y axis is the furthest-moved, set the X values back to what they were.
            if (isElementSelected)
            {
                Context.SelectedState.SelectedStateSave.SetValue("X", Context.GrabbedState.ComponentPosition.X, "float");
            }
            else
            {
                foreach (var instance in Context.SelectedState.SelectedInstances)
                {
                    Context.SelectedState.SelectedStateSave.SetValue(instance.Name + ".X", Context.GrabbedState.InstancePositions[instance].StateX, "float");
                }
            }
        }
    }

    private void SnapSelectedToUnitValues()
    {
        bool wasAnythingModified = false;

        if (Context.SelectedState.SelectedInstances.Count() == 0 &&
            (Context.SelectedState.SelectedComponent != null || Context.SelectedState.SelectedStandardElement != null))
        {
            GraphicalUiElement gue = Context.SelectionManager.SelectedGue;

            GetDifferenceToUnit(gue, out float differenceToUnitX, out float differenceToUnitY,
                out float differenceToUnitWidth, out float differenceToUnitHeight);

            if (differenceToUnitX != 0)
            {
                gue.X = Context.ElementCommands.ModifyVariable("X", differenceToUnitX, Context.SelectedState.SelectedElement);
                wasAnythingModified = true;
            }
            if (differenceToUnitY != 0)
            {
                gue.Y = Context.ElementCommands.ModifyVariable("Y", differenceToUnitY, Context.SelectedState.SelectedElement);
                wasAnythingModified = true;
            }
            if (differenceToUnitWidth != 0)
            {
                gue.Width = Context.ElementCommands.ModifyVariable("Width", differenceToUnitWidth, Context.SelectedState.SelectedElement);
                wasAnythingModified = true;
            }
            if (differenceToUnitHeight != 0)
            {
                gue.Height = Context.ElementCommands.ModifyVariable("Height", differenceToUnitHeight, Context.SelectedState.SelectedElement);
                wasAnythingModified = true;
            }
        }
        else if (Context.SelectedState.SelectedInstances.Count() != 0)
        {
            var gues = Context.SelectionManager.SelectedGues.ToArray();
            foreach (var gue in gues)
            {
                var instanceSave = gue.Tag as InstanceSave;

                if (instanceSave != null && !Context.ElementCommands.ShouldSkipDraggingMovementOn(instanceSave))
                {
                    GetDifferenceToUnit(gue, out float differenceToUnitX, out float differenceToUnitY,
                        out float differenceToUnitWidth, out float differenceToUnitHeight);

                    if (differenceToUnitX != 0)
                    {
                        gue.X = Context.ElementCommands.ModifyVariable("X", differenceToUnitX, instanceSave);
                        wasAnythingModified = true;
                    }
                    if (differenceToUnitY != 0)
                    {
                        gue.Y = Context.ElementCommands.ModifyVariable("Y", differenceToUnitY, instanceSave);
                        wasAnythingModified = true;
                    }
                    if (differenceToUnitWidth != 0)
                    {
                        gue.Width = Context.ElementCommands.ModifyVariable("Width", differenceToUnitWidth, instanceSave);
                        wasAnythingModified = true;
                    }
                    if (differenceToUnitHeight != 0)
                    {
                        gue.Height = Context.ElementCommands.ModifyVariable("Height", differenceToUnitHeight, instanceSave);
                        wasAnythingModified = true;
                    }
                }
            }
        }

        if (wasAnythingModified)
        {
            Context.GuiCommands.RefreshVariables(true);
        }
    }

    private static void GetDifferenceToUnit(GraphicalUiElement gue,
        out float differenceToUnitPositionX, out float differenceToUnitPositionY,
        out float differenceToUnitWidth, out float differenceToUnitHeight)
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

    private void DoEndOfSettingValuesLogic()
    {
        var selectedElement = Context.SelectedState.SelectedElement;
        var stateSave = Context.SelectedState.SelectedStateSave;
        if (stateSave == null)
        {
            throw new System.InvalidOperationException("The SelectedStateSave is null, this should not happen");
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

                Context.SetVariableLogic.PropertyValueChanged(possiblyChangedVariable.GetRootName(),
                   oldValue,
                   instance,
                   element.DefaultState,
                   refresh: true,
                   recordUndo: false,
                   trySave: false);
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
        if (newValue == null && oldValue == null)
        {
            return false;
        }
        // neither are null
        else
        {
            if (oldValue is float oldFloat)
            {
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
