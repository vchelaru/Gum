using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Gum.DataTypes;
using Gum.Input;
using Gum.Wireframe.Editors.Visuals;
using RenderingLibrary.Math.Geometry;

namespace Gum.Wireframe.Editors.Handlers;

/// <summary>
/// Scales a polygon by dragging the handles around its bounding box (#5934). Rewrites the
/// polygon's Points and moves its X/Y so the opposite edge stays put; the whole drag is one edit.
/// Stays tool-side for the same reason as <see cref="PolygonPointInputHandler"/>: it reads and
/// writes <see cref="LinePolygon"/> geometry.
/// </summary>
public class PolygonScaleInputHandler : InputHandlerBase
{
    private readonly IResizeHandlesVisual _handlesVisual;
    private readonly Func<float, float, bool>? _isOverPointNode;

    private PolygonScaleDrag? _drag;
    private Vector2 _totalCursorChange;
    private Vector2 _appliedPositionShift;

    // Higher than PolygonPointInputHandler so an edge handle wins over the add-point marker,
    // which sits on the same edge midpoints. A point node under the cursor still wins.
    public override int Priority => 96;

    private LinePolygon? SelectedLinePolygon => Context.SelectedObjects
        .FirstOrDefault()?.RenderableComponent as LinePolygon;

    /// <param name="isOverPointNode">Whether a polygon point node is under the given world position.
    /// Such a node takes the gesture, so a point on the bounding box can still be grabbed.</param>
    public PolygonScaleInputHandler(EditorContext context, IResizeHandlesVisual handlesVisual,
        Func<float, float, bool>? isOverPointNode = null)
        : base(context)
    {
        _handlesVisual = handlesVisual;
        _isOverPointNode = isOverPointNode;
    }

    public override bool HasCursorOver(float worldX, float worldY)
    {
        return _handlesVisual.Visible &&
            _handlesVisual.GetSideOver(worldX, worldY) != ResizeSide.None &&
            _isOverPointNode?.Invoke(worldX, worldY) != true;
    }

    public override GumCursorKind? GetCursorToShow(float worldX, float worldY)
    {
        if (!HasCursorOver(worldX, worldY))
        {
            return null;
        }

        return _handlesVisual.GetSideOver(worldX, worldY) switch
        {
            ResizeSide.TopLeft or ResizeSide.BottomRight => GumCursorKind.SizeNWSE,
            ResizeSide.TopRight or ResizeSide.BottomLeft => GumCursorKind.SizeNESW,
            ResizeSide.Top or ResizeSide.Bottom => GumCursorKind.SizeNS,
            ResizeSide.Left or ResizeSide.Right => GumCursorKind.SizeWE,
            _ => null
        };
    }

    protected override void OnPush(float worldX, float worldY)
    {
        _totalCursorChange = Vector2.Zero;
        _appliedPositionShift = Vector2.Zero;

        LinePolygon? polygon = SelectedLinePolygon;
        if (polygon == null)
        {
            return;
        }

        _drag = new PolygonScaleDrag(polygon, _handlesVisual.GetSideOver(worldX, worldY),
            Context.SnapToGrid, Context.GridSize);
    }

    protected override void OnDrag()
    {
        if (_drag == null)
        {
            return;
        }

        float xChange = GetCursorXChange();
        float yChange = GetCursorYChange();
        if (xChange == 0 && yChange == 0)
        {
            return;
        }

        _totalCursorChange += new Vector2(xChange, yChange);

        bool lockAspectRatio = Context.HotkeyManager.IsPressedInControl(Context.HotkeyManager.LockMovementToAxis);
        bool fromCenter = Context.HotkeyManager.IsPressedInControl(Context.HotkeyManager.ResizeFromCenter);

        Vector2 positionShift = _drag.Apply(_totalCursorChange, lockAspectRatio, fromCenter);
        MovePositionBy(positionShift - _appliedPositionShift);
        _appliedPositionShift = positionShift;

        MarkAsChanged();
        Context.GuiCommands.RefreshVariables();
    }

    protected override void OnRelease()
    {
        _drag = null;

        if (!Context.HasChangedAnythingSinceLastPush)
        {
            return;
        }

        if (SelectedLinePolygon is { } polygon)
        {
            List<Vector2> points = new List<Vector2>(polygon.PointCount);
            for (int i = 0; i < polygon.PointCount; i++)
            {
                points.Add(polygon.PointAt(i));
            }
            PolygonPointsStateWriter.Write(Context, points);
        }

        Context.DoEndOfSettingValuesLogic();
        Context.GuiCommands.RefreshVariables();
    }

    private void MovePositionBy(Vector2 change)
    {
        InstanceSave? instance = Context.SelectedState.SelectedInstance;
        ElementSave? element = Context.SelectedState.SelectedElement;

        if (change.X != 0)
        {
            if (instance != null)
            {
                Context.ElementCommands.ModifyVariable("X", change.X, instance);
            }
            else if (element != null)
            {
                Context.ElementCommands.ModifyVariable("X", change.X, element);
            }
        }
        if (change.Y != 0)
        {
            if (instance != null)
            {
                Context.ElementCommands.ModifyVariable("Y", change.Y, instance);
            }
            else if (element != null)
            {
                Context.ElementCommands.ModifyVariable("Y", change.Y, element);
            }
        }
    }
}
