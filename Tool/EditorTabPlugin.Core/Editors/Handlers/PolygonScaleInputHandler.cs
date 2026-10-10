using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Gum.Converters;
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
    private bool _movedX;
    private bool _movedY;

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
        _movedX = false;
        _movedY = false;

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

        RemoveFloatNoiseFromPosition();

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
        if (change.X != 0)
        {
            ModifyPosition("X", change.X);
            _movedX = true;
        }
        if (change.Y != 0)
        {
            ModifyPosition("Y", change.Y);
            _movedY = true;
        }
    }

    private void ModifyPosition(string variableName, float amount)
    {
        InstanceSave? instance = Context.SelectedState.SelectedInstance;
        ElementSave? element = Context.SelectedState.SelectedElement;

        if (instance != null)
        {
            Context.ElementCommands.ModifyVariable(variableName, amount, instance);
        }
        else if (element != null)
        {
            Context.ElementCommands.ModifyVariable(variableName, amount, element);
        }
    }

    /// <summary>
    /// Every frame adds a float to X and Y, so after many frames they sit a hair off the value
    /// the drag was aiming for (47.999992 for 48). Only pixel units are rounded: other units
    /// convert the amount, so a correction in pixels would not land on a round value.
    /// </summary>
    private void RemoveFloatNoiseFromPosition()
    {
        GraphicalUiElement? selected = Context.SelectedObjects.FirstOrDefault();
        if (selected == null)
        {
            return;
        }

        if (_movedX && selected.XUnits.GetIsPixelBased())
        {
            RemoveFloatNoiseFrom("X");
        }
        if (_movedY && selected.YUnits.GetIsPixelBased())
        {
            RemoveFloatNoiseFrom("Y");
        }
    }

    private void RemoveFloatNoiseFrom(string variableName)
    {
        object? currentAsObject = Context.ElementCommands.GetCurrentValueForVariable(
            variableName, Context.SelectedState.SelectedInstance);

        if (currentAsObject is float current)
        {
            float cleaned = PolygonScaler.RemoveFloatNoise(current);
            if (cleaned != current)
            {
                ModifyPosition(variableName, cleaned - current);
            }
        }
    }
}
