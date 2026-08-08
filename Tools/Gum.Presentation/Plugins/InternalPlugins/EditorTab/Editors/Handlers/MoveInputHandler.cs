using System.Linq;
using System.Numerics;
using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Input;
using RenderingLibrary;
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

    public override GumCursorKind? GetCursorToShow(float worldX, float worldY)
    {
        if (Context.SelectionManager.IsOverBody)
        {
            bool canX = Context.IsXMovementEnabled;
            bool canY = Context.IsYMovementEnabled;

            if (canX && canY) return GumCursorKind.SizeAll;
            if (canX) return GumCursorKind.SizeWE;
            if (canY) return GumCursorKind.SizeNS;
            return null;
        }
        return null;
    }

    public override bool HandlePush(float worldX, float worldY)
    {
        // When multi-select key is held, don't claim the push.
        // Shift+click on body should add to selection via the rectangle-selector fallback,
        // not start a move operation.
        if (Context.HotkeyManager.IsPressedInControl(Context.HotkeyManager.MultiSelect))
        {
            return false;
        }
        return base.HandlePush(worldX, worldY);
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
            if (Context.HotkeyManager.IsPressedInControl(Context.HotkeyManager.LockMovementToAxis))
            {
                ApplyAxisLockToSelectedState();
                Context.GuiCommands.RefreshVariables();
            }

            // Snap to unit values if enabled
            if (Context.RestrictToUnitValues)
            {
                SnapSelectedToUnitValues();
            }

            Context.DoEndOfSettingValuesLogic();
        }

        _hasGrabbed = false;
    }

    private void ApplyCursorMovement()
    {
        var cursor = Context.Cursor;

        float xToMoveBy = Context.IsXMovementEnabled
            ? cursor.XChange / Context.Camera.Zoom
            : 0;
        float yToMoveBy = Context.IsYMovementEnabled
            ? cursor.YChange / Context.Camera.Zoom
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

        if (Context.SnapToGrid)
        {
            AccumulateTrueOffsetForGridSnap(xToMoveBy, yToMoveBy);
        }

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

            // Snap to grid live, as the object is dragged - not deferred to release, so the user
            // sees exactly where it will land instead of having to guess and re-grab.
            if (Context.SnapToGrid)
            {
                SnapSelectedToGrid();
            }

            MarkAsChanged();
        }
    }

    private void AccumulateTrueOffsetForGridSnap(float deltaX, float deltaY)
    {
        if (Context.SelectedState.SelectedInstances.Count() == 0 &&
            (Context.SelectedState.SelectedComponent != null || Context.SelectedState.SelectedStandardElement != null))
        {
            Context.GrabbedState.AccumulateTruePositionOffset(instance: null, deltaX, deltaY);
        }
        else
        {
            foreach (var instance in Context.SelectedState.SelectedInstances)
            {
                Context.GrabbedState.AccumulateTruePositionOffset(instance, deltaX, deltaY);
            }
        }
    }

    private void ApplyAxisLockIfNeeded()
    {
        bool isLockedToAxis = Context.HotkeyManager.IsPressedInControl(Context.HotkeyManager.LockMovementToAxis);
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
                if (instance.Locked)
                {
                    continue;
                }

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
                    if (instance.Locked)
                    {
                        continue;
                    }

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
                    if (instance.Locked)
                    {
                        continue;
                    }

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

                if (instanceSave != null && !instanceSave.Locked && !Context.ElementCommands.ShouldSkipDraggingMovementOn(instanceSave))
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

    private void SnapSelectedToGrid()
    {
        bool wasAnythingModified = false;
        float gridSize = Context.GridSize;

        if (Context.SelectedState.SelectedInstances.Count() == 0 &&
            (Context.SelectedState.SelectedComponent != null || Context.SelectedState.SelectedStandardElement != null))
        {
            GraphicalUiElement gue = Context.SelectionManager.SelectedGue;

            GetDifferenceToGrid(gue, gridSize,
                Context.GrabbedState.ComponentPosition, Context.GrabbedState.TrueComponentPositionOffset,
                out float differenceToGridX, out float differenceToGridY);

            if (differenceToGridX != 0)
            {
                gue.X = Context.ElementCommands.ModifyVariable("X", differenceToGridX, Context.SelectedState.SelectedElement);
                wasAnythingModified = true;
            }
            if (differenceToGridY != 0)
            {
                gue.Y = Context.ElementCommands.ModifyVariable("Y", differenceToGridY, Context.SelectedState.SelectedElement);
                wasAnythingModified = true;
            }
        }
        else if (Context.SelectedState.SelectedInstances.Count() != 0)
        {
            var gues = Context.SelectionManager.SelectedGues.ToArray();
            foreach (var gue in gues)
            {
                var instanceSave = gue.Tag as InstanceSave;

                if (instanceSave != null && !instanceSave.Locked && !Context.ElementCommands.ShouldSkipDraggingMovementOn(instanceSave))
                {
                    Vector2 grabStartLocal = Context.GrabbedState.InstancePositions.TryGetValue(instanceSave, out var grabbedPosition)
                        ? new Vector2(grabbedPosition.AbsoluteX, grabbedPosition.AbsoluteY) // despite the field name, these are local X/Y at grab
                        : new Vector2(gue.X, gue.Y);
                    Vector2 trueOffset = Context.GrabbedState.GetTruePositionOffset(instanceSave);

                    GetDifferenceToGrid(gue, gridSize, grabStartLocal, trueOffset,
                        out float differenceToGridX, out float differenceToGridY);

                    if (differenceToGridX != 0)
                    {
                        gue.X = Context.ElementCommands.ModifyVariable("X", differenceToGridX, instanceSave);
                        wasAnythingModified = true;
                    }
                    if (differenceToGridY != 0)
                    {
                        gue.Y = Context.ElementCommands.ModifyVariable("Y", differenceToGridY, instanceSave);
                        wasAnythingModified = true;
                    }
                }
            }
        }

        if (wasAnythingModified)
        {
            // Not forced (true) - this runs on every drag tick, not just once at release, so a
            // full grid rebuild here would be needlessly expensive.
            Context.GuiCommands.RefreshVariables();
        }
    }

    /// <summary>
    /// Computes the delta to apply to <paramref name="gue"/>'s local X/Y so its world-space anchor
    /// lands on the nearest grid line at or below where it would be with NO snapping ever applied
    /// this drag (<paramref name="grabStartLocal"/> + <paramref name="trueOffsetSinceGrab"/>).
    /// Snapping from the live <c>gue.X</c>/<c>gue.Y</c> instead would read back whatever the
    /// previous frame's snap already wrote, silently reverting every small drag back to the same
    /// grid line instead of letting the true position accumulate - the object would only move once
    /// a single frame's raw delta was big enough to cross into the next grid cell (issue #4137
    /// review: "movement fights you"). The live value is still what the returned delta is relative
    /// to, since that's what the caller applies via <c>ModifyVariable</c>. Axes using non-pixel
    /// units are left untouched (0 difference) - grid snap only applies to pixel-based positioning.
    /// </summary>
    internal static void GetDifferenceToGrid(GraphicalUiElement gue, float gridSize,
        Vector2 grabStartLocal, Vector2 trueOffsetSinceGrab,
        out float differenceToGridX, out float differenceToGridY)
    {
        differenceToGridX = 0;
        differenceToGridY = 0;

        // The parent's own absolute offset doesn't change during this drag (only this object
        // moves), so live AbsoluteX/Y minus live X/Y always yields the current parent offset -
        // valid at any point in the drag, not just at grab time.
        float parentOffsetX = gue.AbsoluteX - gue.X;
        float parentOffsetY = gue.AbsoluteY - gue.Y;

        if (gue.XUnits.GetIsPixelBased())
        {
            float trueWorldX = grabStartLocal.X + trueOffsetSinceGrab.X + parentOffsetX;
            float snappedWorldX = GridSnapper.Snap(trueWorldX, gridSize);
            differenceToGridX = snappedWorldX - gue.AbsoluteX;
        }

        if (gue.YUnits.GetIsPixelBased())
        {
            float trueWorldY = grabStartLocal.Y + trueOffsetSinceGrab.Y + parentOffsetY;
            float snappedWorldY = GridSnapper.Snap(trueWorldY, gridSize);
            differenceToGridY = snappedWorldY - gue.AbsoluteY;
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
}
