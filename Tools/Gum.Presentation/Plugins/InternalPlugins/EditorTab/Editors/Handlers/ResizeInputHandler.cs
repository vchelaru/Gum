using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Input;
using Gum.Wireframe.Editors.Visuals;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment;

namespace Gum.Wireframe.Editors.Handlers;

/// <summary>
/// Handles resizing objects via the 8 corner/edge resize handles.
/// </summary>
public class ResizeInputHandler : InputHandlerBase
{
    private ResizeSide _sideGrabbed = ResizeSide.None;
    private ResizeSide _sideOver = ResizeSide.None;

    private readonly IResizeHandlesVisual _resizeHandlesVisual;

    public override int Priority => 90; // Higher than move, lower than rotation

    /// <summary>
    /// The resize side currently under the cursor (if any).
    /// Used by visual components like dimension displays.
    /// </summary>
    public ResizeSide SideOver => _sideOver;

    public ResizeInputHandler(EditorContext context, IResizeHandlesVisual resizeHandlesVisual)
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

        return _resizeHandlesVisual.GetSideOver(worldX, worldY) != ResizeSide.None;
    }

    public override GumCursorKind? GetCursorToShow(float worldX, float worldY)
    {
        if (IsSideEffectivelyDisabled(_sideOver)) return null;

        return _sideOver switch
        {
            ResizeSide.TopLeft or ResizeSide.BottomRight => GumCursorKind.SizeNWSE,
            ResizeSide.TopRight or ResizeSide.BottomLeft => GumCursorKind.SizeNESW,
            ResizeSide.Top or ResizeSide.Bottom => GumCursorKind.SizeNS,
            ResizeSide.Left or ResizeSide.Right => GumCursorKind.SizeWE,
            _ => null
        };
    }

    /// <summary>
    /// Returns true if dragging the given side would produce no change, using the exact
    /// same shouldDisableX/shouldDisableY logic as OnDrag. Accounts for object origin
    /// (e.g. a Right handle on a left-origin object has changeXMultiplier=0, so X locking
    /// does not disable it).
    /// </summary>
    private bool IsSideEffectivelyDisabled(ResizeSide side)
    {
        if (side == ResizeSide.None) return false;

        var elementStack = Context.SelectedState.GetTopLevelElementStack();
        var instanceSave = Context.SelectedState.SelectedInstance;

        CalculateMultipliers(side, instanceSave, elementStack,
            out float changeXMultiplier, out float changeYMultiplier,
            out float widthMultiplier, out float heightMultiplier);

        var shouldDisableX = (Context.IsXMovementEnabled == false && changeXMultiplier != 0) ||
            (Context.IsWidthChangeEnabled == false && widthMultiplier != 0);
        var shouldDisableY = (Context.IsYMovementEnabled == false && changeYMultiplier != 0) ||
            (Context.IsHeightChangeEnabled == false && heightMultiplier != 0);

        // A side is disabled if all of its affected axes would be zeroed out.
        // For sides that only affect one axis (Left/Right or Top/Bottom) the other
        // axis multipliers are 0, so we must treat "not affected" as "not a problem".
        bool xFullyDisabled = shouldDisableX || (changeXMultiplier == 0 && widthMultiplier == 0);
        bool yFullyDisabled = shouldDisableY || (changeYMultiplier == 0 && heightMultiplier == 0);

        return xFullyDisabled && yFullyDisabled;
    }

    public override void UpdateHover(float worldX, float worldY)
    {
        if (!_resizeHandlesVisual.Visible)
        {
            _sideOver = ResizeSide.None;
            return;
        }

        var cursor = Context.Cursor;

        // If dragging, don't change the side over
        if (cursor.PrimaryPush || (!cursor.PrimaryDown && !cursor.PrimaryClick))
        {
            _sideOver = _resizeHandlesVisual.GetSideOver(worldX, worldY);
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
            // Snap to grid live, as the object is dragged - not deferred to release, so the user
            // sees exactly where it will land instead of having to guess and re-grab.
            if (Context.SnapToGrid)
            {
                SnapSelectedToGrid();
            }

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

    /// <summary>
    /// Snaps the current selection's position/size to the grid. Called live from
    /// <see cref="OnDrag"/> on every tick that changes something - the caller refreshes the
    /// Variables grid afterward, so this doesn't refresh itself.
    /// </summary>
    private void SnapSelectedToGrid()
    {
        float gridSize = Context.GridSize;
        var elementStack = Context.SelectedState.GetTopLevelElementStack();

        if (Context.SelectionManager.HasSelection &&
            Context.SelectedState.SelectedInstances.Count() == 0)
        {
            var gue = Context.WireframeObjectManager.GetRepresentation(elementStack.Last().Element) as GraphicalUiElement;
            SnapInstanceToGrid(gue, instanceSave: null, gridSize,
                Context.GrabbedState.ComponentPosition, Context.GrabbedState.ComponentSize,
                Context.GrabbedState.TrueComponentPositionOffset, Context.GrabbedState.TrueComponentSizeOffset,
                elementStack);
        }

        foreach (InstanceSave save in Context.SelectedState.SelectedInstances)
        {
            if (save.Locked)
            {
                continue;
            }

            var gue = Context.WireframeObjectManager.GetRepresentation(save, elementStack) as GraphicalUiElement;
            if (gue == null)
            {
                continue;
            }

            Vector2 grabStartPosition = Context.GrabbedState.InstancePositions.TryGetValue(save, out var grabbedPosition)
                ? new Vector2(grabbedPosition.AbsoluteX, grabbedPosition.AbsoluteY) // despite the field name, these are local X/Y at grab
                : new Vector2(gue.X, gue.Y);
            Vector2 grabStartSize = Context.GrabbedState.InstanceSizes.TryGetValue(save, out var grabbedSize)
                ? grabbedSize
                : new Vector2(gue.Width, gue.Height);

            SnapInstanceToGrid(gue, save, gridSize,
                grabStartPosition, grabStartSize,
                Context.GrabbedState.GetTruePositionOffset(save), Context.GrabbedState.GetTrueSizeOffset(save),
                elementStack);
        }
    }

    private void SnapInstanceToGrid(GraphicalUiElement? gue, InstanceSave? instanceSave, float gridSize,
        Vector2 grabStartPosition, Vector2 grabStartSize, Vector2 truePositionOffset, Vector2 trueSizeOffset,
        List<ElementWithState> elementStack)
    {
        if (gue == null)
        {
            return;
        }

        CalculateMultipliers(_sideGrabbed, instanceSave, elementStack,
            out _, out _, out float widthMultiplier, out float heightMultiplier);

        float xOriginRatio = GetRatioXOverInSelection(gue, GetCurrentXOrigin(instanceSave));
        float yOriginRatio = GetRatioYDownInSelection(gue, GetCurrentYOrigin(instanceSave));

        GetDifferenceToGridForResizeAxis(gridSize,
            grabStartPosition.X, grabStartSize.X, trueSizeOffset.X, widthMultiplier, xOriginRatio,
            gue.XUnits.GetIsPixelBased(), gue.WidthUnits.GetIsPixelBased(),
            gue.AbsoluteLeft, gue.X, gue.Width,
            out float differenceToGridX, out float differenceToGridWidth);

        GetDifferenceToGridForResizeAxis(gridSize,
            grabStartPosition.Y, grabStartSize.Y, trueSizeOffset.Y, heightMultiplier, yOriginRatio,
            gue.YUnits.GetIsPixelBased(), gue.HeightUnits.GetIsPixelBased(),
            gue.AbsoluteTop, gue.Y, gue.Height,
            out float differenceToGridY, out float differenceToGridHeight);

        if (differenceToGridX != 0)
        {
            gue.X = instanceSave != null
                ? Context.ElementCommands.ModifyVariable("X", differenceToGridX, instanceSave)
                : Context.ElementCommands.ModifyVariable("X", differenceToGridX, Context.SelectedState.SelectedElement);
        }
        if (differenceToGridY != 0)
        {
            gue.Y = instanceSave != null
                ? Context.ElementCommands.ModifyVariable("Y", differenceToGridY, instanceSave)
                : Context.ElementCommands.ModifyVariable("Y", differenceToGridY, Context.SelectedState.SelectedElement);
        }
        if (differenceToGridWidth != 0)
        {
            gue.Width = instanceSave != null
                ? Context.ElementCommands.ModifyVariable("Width", differenceToGridWidth, instanceSave)
                : Context.ElementCommands.ModifyVariable("Width", differenceToGridWidth, Context.SelectedState.SelectedElement);
        }
        if (differenceToGridHeight != 0)
        {
            gue.Height = instanceSave != null
                ? Context.ElementCommands.ModifyVariable("Height", differenceToGridHeight, instanceSave)
                : Context.ElementCommands.ModifyVariable("Height", differenceToGridHeight, Context.SelectedState.SelectedElement);
        }
    }

    private HorizontalAlignment GetCurrentXOrigin(InstanceSave? instanceSave)
    {
        object? xOriginAsObject = Context.ElementCommands.GetCurrentValueForVariable("XOrigin", instanceSave);
        return xOriginAsObject != null ? (HorizontalAlignment)xOriginAsObject : HorizontalAlignment.Left;
    }

    private VerticalAlignment GetCurrentYOrigin(InstanceSave? instanceSave)
    {
        object? yOriginAsObject = Context.ElementCommands.GetCurrentValueForVariable("YOrigin", instanceSave);
        return yOriginAsObject != null ? (VerticalAlignment)yOriginAsObject : VerticalAlignment.Top;
    }

    /// <summary>
    /// Computes the delta to apply to one axis's position and size for resize grid-snap. Unlike
    /// rounding Width/Height in isolation (which only lands the dragged edge on-grid when the
    /// opposite/anchor edge already happens to be grid-aligned) or independently grid-snapping X/Y
    /// (which fights with the size snap for the shared edge and drifts frame to frame - reported as
    /// "grab the top handle... the bottom doesn't stay fixed, it snaps around"), this resolves both
    /// edges together from a single source of truth: the anchor edge's position AT GRAB TIME
    /// (<paramref name="grabStartPositionAxis"/>/<paramref name="grabStartSizeAxis"/> combined via
    /// <paramref name="originRatio"/>), which is frame-invariant by construction - re-deriving it
    /// from "live" state each tick is exactly what let it drift. Only the dragged edge (identified by
    /// <paramref name="sizeMultiplier"/>'s sign - negative means Left/Top, positive means Right/
    /// Bottom, zero means this axis isn't resized this gesture) is snapped, from its true (never-
    /// snapped) position - anchor +/- the true, unsnapped size (<paramref name="grabStartSizeAxis"/> +
    /// <paramref name="trueSizeOffsetSinceGrabAxis"/>; see the "movement fights you" explanation on
    /// <see cref="MoveInputHandler.GetDifferenceToGrid"/>). Position and size are then both derived
    /// from the same resolved pair of edges via <paramref name="originRatio"/> (0 = Left/Top origin,
    /// .5 = Center, 1 = Right/Bottom), so a Center-origin object's X/Y (which tracks neither edge
    /// alone) comes out correct too. Axes where either the position or size units aren't pixel-based
    /// are left untouched.
    /// </summary>
    internal static void GetDifferenceToGridForResizeAxis(float gridSize,
        float grabStartPositionAxis, float grabStartSizeAxis, float trueSizeOffsetSinceGrabAxis,
        float sizeMultiplier, float originRatio,
        bool positionIsPixelBased, bool sizeIsPixelBased,
        float liveAbsoluteMin, float livePositionAxis, float liveSizeAxis,
        out float differenceToGridPosition, out float differenceToGridSize)
    {
        differenceToGridPosition = 0;
        differenceToGridSize = 0;

        if (sizeMultiplier == 0 || !positionIsPixelBased || !sizeIsPixelBased)
        {
            return;
        }

        // Always exact: AbsoluteMin (AbsoluteLeft/Top) is, by definition, the parent offset plus
        // the local min edge (position minus the origin's share of the size) - solving for the
        // parent offset here means the drag's own past snap corrections (baked into the live
        // position/size) cancel out exactly, regardless of how they drifted.
        float parentOffset = liveAbsoluteMin - (livePositionAxis - originRatio * liveSizeAxis);

        GetAnchorAndDraggedLocal(grabStartPositionAxis, grabStartSizeAxis, trueSizeOffsetSinceGrabAxis,
            sizeMultiplier, originRatio, out float anchorLocal, out float trueDraggedLocal);

        float snappedDraggedWorld = GridSnapper.SnapRound(parentOffset + trueDraggedLocal, gridSize);
        float snappedDraggedLocal = snappedDraggedWorld - parentOffset;

        ResolveEdgesToPositionAndSize(anchorLocal, snappedDraggedLocal, originRatio,
            out float newPositionLocal, out float newSize);

        differenceToGridPosition = newPositionLocal - livePositionAxis;
        differenceToGridSize = newSize - liveSizeAxis;
    }

    /// <summary>
    /// Resolves the raw (non-grid-snapped) per-axis position/size delta for THIS tick of a resize
    /// drag, allowing the dragged edge to cross the anchor edge instead of clamping Width/Height at
    /// 0. See <see cref="GetAnchorAndDraggedLocal"/> and <see cref="ResolveEdgesToPositionAndSize"/> -
    /// this is the same anchor/dragged-edge resolution <see cref="GetDifferenceToGridForResizeAxis"/>
    /// uses, minus the grid-snap rounding step, so both paths flip identically (#4385).
    ///
    /// The delta is computed ENTIRELY from grab-time state (<paramref name="trueSizeOffsetBeforeAxis"/>
    /// and <paramref name="trueSizeOffsetAfterAxis"/> - the accumulated true size offset immediately
    /// before and after this tick) rather than by diffing against the live representation's current
    /// X/Y. The live X/Y can't be used as the "before" reference for a rotated object: on a rotated
    /// object the position delta this method returns gets rotated before being applied (see
    /// ApplySizeChangeForInstance), so the STORED X/Y no longer represents the same local,
    /// pre-rotation frame that grabStartPositionAxis (and this method's own math) is expressed in
    /// once even one prior tick has written a rotated delta into it. Resolving "before" and "after"
    /// from the same grab-time-relative accumulator and diffing those keeps every tick in the local
    /// frame, so per-tick deltas sum to the same result as one big tick regardless of rotation
    /// (confirmed by ResizeInputHandlerFlipRotationTests's multi-tick case, which drifted under the
    /// live-X/Y-diffing approach this replaced).
    /// </summary>
    internal static void ResolveResizeAxis(
        float grabStartPositionAxis, float grabStartSizeAxis,
        float trueSizeOffsetBeforeAxis, float trueSizeOffsetAfterAxis,
        float sizeMultiplier, float originRatio,
        out float positionDelta, out float sizeDelta)
    {
        positionDelta = 0;
        sizeDelta = 0;

        if (sizeMultiplier == 0)
        {
            return;
        }

        GetAnchorAndDraggedLocal(grabStartPositionAxis, grabStartSizeAxis, trueSizeOffsetBeforeAxis,
            sizeMultiplier, originRatio, out float anchorLocal, out float oldDraggedLocal);
        ResolveEdgesToPositionAndSize(anchorLocal, oldDraggedLocal, originRatio,
            out float oldPositionLocal, out float oldSizeLocal);

        GetAnchorAndDraggedLocal(grabStartPositionAxis, grabStartSizeAxis, trueSizeOffsetAfterAxis,
            sizeMultiplier, originRatio, out anchorLocal, out float newDraggedLocal);
        ResolveEdgesToPositionAndSize(anchorLocal, newDraggedLocal, originRatio,
            out float newPositionLocal, out float newSizeLocal);

        positionDelta = newPositionLocal - oldPositionLocal;
        sizeDelta = newSizeLocal - oldSizeLocal;
    }

    /// <summary>
    /// Computes the grab-time ANCHOR edge (the edge opposite the one being dragged - invariant for
    /// the whole gesture) and the DRAGGED edge's raw, never-clamped local position (anchor plus or
    /// minus the true, unclamped size accumulated since grab). The dragged edge is free to cross
    /// past the anchor; see <see cref="ResolveEdgesToPositionAndSize"/> for how that's turned into
    /// a flip instead of a negative size.
    /// </summary>
    private static void GetAnchorAndDraggedLocal(
        float grabStartPositionAxis, float grabStartSizeAxis, float trueSizeOffsetSinceGrabAxis,
        float sizeMultiplier, float originRatio,
        out float anchorLocal, out float draggedLocal)
    {
        float grabStartMinLocal = grabStartPositionAxis - originRatio * grabStartSizeAxis;
        float grabStartMaxLocal = grabStartMinLocal + grabStartSizeAxis;

        bool draggedIsMin = sizeMultiplier < 0;
        anchorLocal = draggedIsMin ? grabStartMaxLocal : grabStartMinLocal;

        float trueSize = grabStartSizeAxis + trueSizeOffsetSinceGrabAxis;
        draggedLocal = draggedIsMin ? anchorLocal - trueSize : anchorLocal + trueSize;
    }

    /// <summary>
    /// Resolves the final local position/size from an anchor edge and a dragged edge. Taking
    /// Math.Min/Max of the two (rather than assuming the dragged edge is always the min or always
    /// the max) is what makes crossing the anchor "just work" as a flip: once the dragged edge
    /// passes the anchor, it naturally becomes the new min (or max) and the anchor becomes the
    /// other - Width/Height (their difference) can never go negative.
    /// </summary>
    private static void ResolveEdgesToPositionAndSize(
        float anchorLocal, float draggedLocal, float originRatio,
        out float newPositionLocal, out float newSizeLocal)
    {
        float newMinLocal = Math.Min(anchorLocal, draggedLocal);
        float newMaxLocal = Math.Max(anchorLocal, draggedLocal);
        newSizeLocal = newMaxLocal - newMinLocal;
        newPositionLocal = newMinLocal + originRatio * newSizeLocal;
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
            if (save.Locked)
                continue;

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

        GraphicalUiElement? representation = null;

        if (instanceSave != null)
        {
            representation = Context.WireframeObjectManager.GetRepresentation(instanceSave);
        }
        else
        {
            representation = Context.WireframeObjectManager.GetRepresentation(elementStack.Last().Element);
        }

        if (representation == null)
        {
            return false;
        }

        // Track the raw, never-clamped size change since grab - unconditionally (not just for
        // Snap To Grid, and toggled unconditionally so no discontinuity appears if the user toggles
        // Snap To Grid mid-drag) - so a resize that overflows past zero can reflect the dragged edge
        // to the opposite side (see ResolveResizeAxis) instead of getting stuck at Width/Height == 0.
        // Captured before AND after this tick's accumulation - ResolveResizeAxis diffs those two
        // grab-time-relative snapshots rather than the live representation, since the live X/Y gets
        // overwritten with a ROTATED delta each tick (see ResolveResizeAxis's own doc comment).
        Vector2 trueSizeOffsetBefore = Context.GrabbedState.GetTrueSizeOffset(instanceSave);
        Context.GrabbedState.AccumulateTrueSizeOffset(instanceSave,
            cursorXChange * widthMultiplier, cursorYChange * heightMultiplier);
        Vector2 trueSizeOffsetAfter = Context.GrabbedState.GetTrueSizeOffset(instanceSave);

        bool isResizeFromCenter = Context.HotkeyManager.IsPressedInControl(Context.HotkeyManager.ResizeFromCenter);

        float xOriginRatio = GetRatioXOverInSelection(representation, GetCurrentXOrigin(instanceSave));
        float yOriginRatio = GetRatioYDownInSelection(representation, GetCurrentYOrigin(instanceSave));

        Vector2 grabStartPosition = instanceSave == null
            ? Context.GrabbedState.ComponentPosition
            : Context.GrabbedState.InstancePositions.TryGetValue(instanceSave, out var grabbedPosition)
                ? new Vector2(grabbedPosition.AbsoluteX, grabbedPosition.AbsoluteY)
                : new Vector2(representation.X, representation.Y);
        Vector2 grabStartSize = instanceSave == null
            ? Context.GrabbedState.ComponentSize
            : Context.GrabbedState.InstanceSizes.TryGetValue(instanceSave, out var grabbedSize)
                ? grabbedSize
                : new Vector2(representation.Width, representation.Height);

        float positionDeltaX, sizeDeltaX;
        if (isResizeFromCenter && widthMultiplier != 0)
        {
            // Resize-from-center grows/shrinks symmetrically around a fixed CENTER point rather
            // than a fixed edge, so the anchor-edge/dragged-edge flip above doesn't apply here.
            // This combination doesn't yet support flip-at-zero (#4390 follow-up) - fall back to a
            // simple floor clamp so Width can never go negative.
            positionDeltaX = cursorXChange * changeXMultiplier;
            sizeDeltaX = cursorXChange * widthMultiplier;
            if (representation.Width + sizeDeltaX < 0)
            {
                sizeDeltaX = -representation.Width;
            }
        }
        else
        {
            ResolveResizeAxis(
                grabStartPosition.X, grabStartSize.X,
                trueSizeOffsetBefore.X, trueSizeOffsetAfter.X,
                widthMultiplier, xOriginRatio,
                out positionDeltaX, out sizeDeltaX);
        }

        float positionDeltaY, sizeDeltaY;
        if (isResizeFromCenter && heightMultiplier != 0)
        {
            positionDeltaY = cursorYChange * changeYMultiplier;
            sizeDeltaY = cursorYChange * heightMultiplier;
            if (representation.Height + sizeDeltaY < 0)
            {
                sizeDeltaY = -representation.Height;
            }
        }
        else
        {
            ResolveResizeAxis(
                grabStartPosition.Y, grabStartSize.Y,
                trueSizeOffsetBefore.Y, trueSizeOffsetAfter.Y,
                heightMultiplier, yOriginRatio,
                out positionDeltaY, out sizeDeltaY);
        }

        // Apply position changes
        Vector2 reposition = new Vector2(positionDeltaX, positionDeltaY);
        // invert Y so up is positive
        reposition.Y *= -1;

        float rotation = MathHelper.ToRadians(representation.GetAbsoluteRotation());
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
        if (heightMultiplier != 0 && sizeDeltaY != 0)
        {
            hasChange = true;
            if (instanceSave != null)
            {
                Context.ElementCommands.ModifyVariable("Height", sizeDeltaY, instanceSave);
            }
            else
            {
                Context.ElementCommands.ModifyVariable("Height", sizeDeltaY, elementStack.Last().Element);
            }
        }
        if (widthMultiplier != 0 && sizeDeltaX != 0)
        {
            hasChange = true;
            if (instanceSave != null)
            {
                Context.ElementCommands.ModifyVariable("Width", sizeDeltaX, instanceSave);
            }
            else
            {
                Context.ElementCommands.ModifyVariable("Width", sizeDeltaX, elementStack.Last().Element);
            }
        }

        if (Context.SnapToGrid)
        {
            Context.GrabbedState.AccumulateTruePositionOffset(instanceSave, reposition.X, reposition.Y);
        }

        return hasChange;
    }

    private void CalculateMultipliers(
        InstanceSave? instanceSave,
        List<ElementWithState> elementStack,
        out float changeXMultiplier,
        out float changeYMultiplier,
        out float widthMultiplier,
        out float heightMultiplier) =>
        CalculateMultipliers(_sideGrabbed, instanceSave, elementStack,
            out changeXMultiplier, out changeYMultiplier, out widthMultiplier, out heightMultiplier);

    private void CalculateMultipliers(
        ResizeSide side,
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

        switch (side)
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

        if (_resizeHandlesVisual.HandlesWidth != 0)
        {
            widthMultiplier *= (((IPositionedSizedObject)ipso).Width / _resizeHandlesVisual.HandlesWidth);
        }

        if (_resizeHandlesVisual.HandlesHeight != 0)
        {
            heightMultiplier *= (((IPositionedSizedObject)ipso).Height / _resizeHandlesVisual.HandlesHeight);
        }

        if (Context.HotkeyManager.IsPressedInControl(Context.HotkeyManager.ResizeFromCenter))
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
            // IWrappedText, not the concrete Text (XNALIKE-only, unreachable from this headless
            // assembly): every Text implements IWrappedText, so the runtime match is identical.
            if (gue.RenderableComponent is IWrappedText text && text.Height > 0)
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
        var isAxisLocked = Context.HotkeyManager.IsPressedInControl(Context.HotkeyManager.LockMovementToAxis);
        if (!isAxisLocked) return;

        bool supportsLockedAxis =
            _sideGrabbed == ResizeSide.TopLeft || _sideGrabbed == ResizeSide.TopRight ||
            _sideGrabbed == ResizeSide.BottomLeft || _sideGrabbed == ResizeSide.BottomRight;

        if (supportsLockedAxis && instanceSave != null)
        {
            IRenderableIpso? ipso = Context.WireframeObjectManager.GetRepresentation(instanceSave, elementStack);
            if (ipso == null) return;

            var cursor = Context.Cursor;
            Context.Camera.ScreenToWorld(cursor.X, cursor.Y, out float cursorX, out float cursorY);

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
                Context.PluginManager.VariableSet(element, instance, possiblyChangedVariableList.GetRootName(), oldValue);
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
            if (oldValue is float oldFloat)
            {
                var newFloat = (float)newValue!;

                return oldFloat != newFloat;
            }
            else if (oldValue is string oldString)
            {
                return oldString != (string)newValue!;
            }
            else if (oldValue is bool oldBool)
            {
                return oldBool != (bool)newValue!;
            }
            else if (oldValue is int oldInt)
            {
                return oldInt != (int)newValue!;
            }
            else if (oldValue is Vector2 oldVector2)
            {
                return oldVector2 != (Vector2)newValue!;
            }
            else if (oldValue is IList oldList)
            {
                return AreListsSame(oldList, (IList)newValue!);
            }
            else
            {
                return oldValue!.Equals(newValue) == false;
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
