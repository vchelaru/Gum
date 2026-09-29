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

    // Movement since the push as if no axis were locked, and the part of it applied to the
    // selection since each axis was last held at its grab position.
    private Vector2 _unlockedMovement;
    private Vector2 _appliedMovement;

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
        // The multi-select key (Shift) is also the axis-lock key. On the body of an object that
        // is already selected it starts an axis-locked move; anywhere else the push is left to
        // the rectangle selector, which adds to the selection.
        if (Context.HotkeyManager.IsPressedInControl(Context.HotkeyManager.MultiSelect) &&
            !Context.SelectionManager.IsOverSelectedBody)
        {
            return false;
        }
        return base.HandlePush(worldX, worldY);
    }

    protected override void OnPush(float worldX, float worldY)
    {
        _hasGrabbed = Context.SelectionManager.HasSelection;
        _unlockedMovement = Vector2.Zero;
        _appliedMovement = Vector2.Zero;

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
            // Drag frames already hold the axis, but the dominant axis can change after the last
            // one, for example when the cursor leaves the body.
            if (GetAxisHeldAtGrab() is { } axisHeldAtGrab && ReturnAxisToGrabPosition(axisHeldAtGrab))
            {
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
        float xToMoveBy = Context.IsXMovementEnabled
            ? GetCursorXChange()
            : 0;
        float yToMoveBy = Context.IsYMovementEnabled
            ? GetCursorYChange()
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

        XOrY? axisHeldAtGrab = GetAxisHeldAtGrab();
        effectiveXToMoveBy = GetMovementAfterAxisLock(effectiveXToMoveBy, axisHeldAtGrab == XOrY.X,
            ref _unlockedMovement.X, ref _appliedMovement.X);
        effectiveYToMoveBy = GetMovementAfterAxisLock(effectiveYToMoveBy, axisHeldAtGrab == XOrY.Y,
            ref _unlockedMovement.Y, ref _appliedMovement.Y);

        var didMove = Context.ElementCommands.MoveSelectedObjectsBy(effectiveXToMoveBy, effectiveYToMoveBy);

        // Only changes anything after the dominant axis switches mid-drag.
        if (axisHeldAtGrab is { } heldAxis && ReturnAxisToGrabPosition(heldAxis))
        {
            didMove = true;
            Context.GuiCommands.RefreshVariableValues();
        }

        if (didMove)
        {
            // Snap to grid live, as the object is dragged - not deferred to release, so the user
            // sees exactly where it will land instead of having to guess and re-grab.
            if (Context.SnapToGrid)
            {
                SnapSelectedToGrid(axisHeldAtGrab);
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

    /// <summary>
    /// The axis an axis-locked drag holds at its grab position, which is the one the cursor has
    /// moved less along. Null when axis lock is not held or the cursor is back at the push point.
    /// </summary>
    private XOrY? GetAxisHeldAtGrab()
    {
        if (!Context.HotkeyManager.IsPressedInControl(Context.HotkeyManager.LockMovementToAxis))
        {
            return null;
        }
        return Context.GrabbedState.AxisMovedFurthestAlong switch
        {
            XOrY.X => XOrY.Y,
            XOrY.Y => XOrY.X,
            _ => null
        };
    }

    /// <summary>
    /// Returns the amount to move an axis by this frame. A held axis does not move; the movement
    /// it skipped is applied once it stops being held, so it catches up to the cursor.
    /// </summary>
    private static float GetMovementAfterAxisLock(float movement, bool isHeld,
        ref float unlockedMovement, ref float appliedMovement)
    {
        float heldBackMovement = unlockedMovement - appliedMovement;
        unlockedMovement += movement;

        if (isHeld)
        {
            // ReturnAxisToGrabPosition puts the selection back at its grab position on this axis.
            appliedMovement = 0;
            return 0;
        }

        movement += heldBackMovement;
        appliedMovement += movement;
        return movement;
    }

    /// <summary>
    /// Sets the selection's state value and visual on <paramref name="axis"/> back to where they
    /// were when grabbed. Returns whether anything changed.
    /// </summary>
    private bool ReturnAxisToGrabPosition(XOrY axis)
    {
        // HandlePush only starts a move while a state is selected.
        if (Context.SelectedState.SelectedStateSave is not { } stateSave)
        {
            return false;
        }

        bool didChange = false;

        if (Context.SelectedState.SelectedInstances.Count() == 0 &&
            (Context.SelectedState.SelectedComponent != null || Context.SelectedState.SelectedStandardElement != null))
        {
            if (Context.SelectedState.SelectedElement is { } selectedElement &&
                Context.WireframeObjectManager.GetRepresentation(selectedElement) is { } gue)
            {
                float grabbedValue = axis == XOrY.X
                    ? Context.GrabbedState.ComponentPosition.X
                    : Context.GrabbedState.ComponentPosition.Y;
                didChange = SetAxisValue(gue, axis, stateSave, axis == XOrY.X ? "X" : "Y", grabbedValue, grabbedValue);
            }
        }
        else
        {
            foreach (InstanceSave instance in Context.SelectedState.SelectedInstances)
            {
                if (instance.Locked ||
                    !Context.GrabbedState.InstancePositions.TryGetValue(instance, out StateAndAbsoluteVector2 grabbed) ||
                    Context.WireframeObjectManager.GetRepresentation(instance) is not { } gue)
                {
                    continue;
                }

                // Despite the field names, AbsoluteX/Y are the local X/Y at grab.
                didChange |= axis == XOrY.X
                    ? SetAxisValue(gue, axis, stateSave, instance.Name + ".X", grabbed.AbsoluteX, grabbed.StateX)
                    : SetAxisValue(gue, axis, stateSave, instance.Name + ".Y", grabbed.AbsoluteY, grabbed.StateY);
            }
        }

        return didChange;
    }

    private static bool SetAxisValue(GraphicalUiElement gue, XOrY axis, StateSave stateSave,
        string variableName, float visualValue, float? stateValue)
    {
        float currentVisualValue = axis == XOrY.X ? gue.X : gue.Y;
        if (currentVisualValue == visualValue && stateSave.GetValue(variableName) as float? == stateValue)
        {
            return false;
        }

        stateSave.SetValue(variableName, stateValue, "float");
        if (axis == XOrY.X)
        {
            gue.X = visualValue;
        }
        else
        {
            gue.Y = visualValue;
        }
        return true;
    }

    private void SnapSelectedToUnitValues()
    {
        bool wasAnythingModified = false;

        // A selected component or standard element is the selected element, shown as the selected elementGue.
        if (Context.SelectedState.SelectedInstances.Count() == 0 &&
            (Context.SelectedState.SelectedComponent != null || Context.SelectedState.SelectedStandardElement != null) &&
            Context.SelectedState.SelectedElement is { } selectedElement &&
            Context.SelectionManager.SelectedGue is { } elementGue)
        {

            GetDifferenceToUnit(elementGue, out float differenceToUnitX, out float differenceToUnitY,
                out float differenceToUnitWidth, out float differenceToUnitHeight);

            if (differenceToUnitX != 0)
            {
                elementGue.X = Context.ElementCommands.ModifyVariable("X", differenceToUnitX, selectedElement);
                wasAnythingModified = true;
            }
            if (differenceToUnitY != 0)
            {
                elementGue.Y = Context.ElementCommands.ModifyVariable("Y", differenceToUnitY, selectedElement);
                wasAnythingModified = true;
            }
            if (differenceToUnitWidth != 0)
            {
                elementGue.Width = Context.ElementCommands.ModifyVariable("Width", differenceToUnitWidth, selectedElement);
                wasAnythingModified = true;
            }
            if (differenceToUnitHeight != 0)
            {
                elementGue.Height = Context.ElementCommands.ModifyVariable("Height", differenceToUnitHeight, selectedElement);
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

    private void SnapSelectedToGrid(XOrY? axisHeldAtGrab)
    {
        bool wasAnythingModified = false;
        float gridSize = Context.GridSize;

        // A selected component or standard element is the selected element, shown as the selected elementGue.
        if (Context.SelectedState.SelectedInstances.Count() == 0 &&
            (Context.SelectedState.SelectedComponent != null || Context.SelectedState.SelectedStandardElement != null) &&
            Context.SelectedState.SelectedElement is { } selectedElement &&
            Context.SelectionManager.SelectedGue is { } elementGue)
        {

            GetDifferenceToGrid(elementGue, gridSize,
                Context.GrabbedState.ComponentPosition, Context.GrabbedState.TrueComponentPositionOffset,
                out float differenceToGridX, out float differenceToGridY);
            ClearHeldAxis(axisHeldAtGrab, ref differenceToGridX, ref differenceToGridY);

            if (differenceToGridX != 0)
            {
                elementGue.X = Context.ElementCommands.ModifyVariable("X", differenceToGridX, selectedElement);
                wasAnythingModified = true;
            }
            if (differenceToGridY != 0)
            {
                elementGue.Y = Context.ElementCommands.ModifyVariable("Y", differenceToGridY, selectedElement);
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
                    ClearHeldAxis(axisHeldAtGrab, ref differenceToGridX, ref differenceToGridY);

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

    // An axis-locked drag leaves the held axis where it was grabbed, even off the grid.
    private static void ClearHeldAxis(XOrY? axisHeldAtGrab, ref float differenceX, ref float differenceY)
    {
        if (axisHeldAtGrab == XOrY.X)
        {
            differenceX = 0;
        }
        else if (axisHeldAtGrab == XOrY.Y)
        {
            differenceY = 0;
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
