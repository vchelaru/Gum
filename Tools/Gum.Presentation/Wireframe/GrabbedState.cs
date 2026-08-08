using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Input;
using Gum.ToolStates;
using Vector2 = System.Numerics.Vector2;

namespace Gum.Wireframe;

public struct StateAndAbsoluteVector2
{
    public float? StateX;
    public float AbsoluteX;
    public float? StateY;
    public float AbsoluteY;
}

public class GrabbedState
{

    private readonly ISelectedState _selectedState;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly IGumCursorState _cursor;

    public StateSave StateSave { get; private set; }

    public XOrY? AxisMovedFurthestAlong
    {
        get
        {
            var cursor = _cursor;

            if (cursor.X == CursorPushScreenX && cursor.Y == CursorPushScreenY)
            {
                return null;
            }
            else if (System.Math.Abs( cursor.X - CursorPushScreenX) >
                System.Math.Abs( cursor.Y - CursorPushScreenY))
            {
                return XOrY.X;
            }
            else
            {
                return XOrY.Y;
            }
        }
    }

    public float CursorPushScreenX
    {
        get;
        set;
    }

    public float CursorPushScreenY
    {
        get;
        set;
    }

    public float AccumulatedXOffset { get; set; }
    public float AccumulatedYOffset { get; set; }

    /// <summary>
    /// The X and Y of the selected component when grabbed. This is the effective position as opposed to the value stored in the selected state
    /// </summary>
    public Vector2 ComponentPosition
    {
        get;
        private set;
    }
    public Vector2 ComponentSize
    {
        get;
        private set;
    }

    public Dictionary<InstanceSave, StateAndAbsoluteVector2> InstancePositions
    {
        get;
        private set;
    } = new Dictionary<InstanceSave, StateAndAbsoluteVector2>();

    public Dictionary<InstanceSave, Vector2> InstanceSizes
    {
        get;
        private set;
    } = new Dictionary<InstanceSave, Vector2>();

    /// <summary>
    /// Grid-snap needs to know where an object would be with NO snapping applied, tracked
    /// continuously across the whole drag - reading the live (already-snapped) X/Y/Width/Height
    /// every frame causes each small move to be immediately reverted back to the same grid line
    /// it was already snapped to, so the object only visibly moves once a single frame's raw
    /// delta is big enough to land in a different grid cell. These accumulate the raw, unsnapped
    /// per-frame deltas since grab (added to <see cref="ComponentPosition"/>/<see cref="ComponentSize"/>
    /// or the per-instance grab position/size to get the "true" value) and are never touched by
    /// the snap correction itself.
    /// </summary>
    public Vector2 TrueComponentPositionOffset { get; set; }
    public Vector2 TrueComponentSizeOffset { get; set; }
    public Dictionary<InstanceSave, Vector2> TrueInstancePositionOffsets { get; } = new Dictionary<InstanceSave, Vector2>();
    public Dictionary<InstanceSave, Vector2> TrueInstanceSizeOffsets { get; } = new Dictionary<InstanceSave, Vector2>();

    public void AccumulateTruePositionOffset(InstanceSave? instance, float deltaX, float deltaY)
    {
        if (instance == null)
        {
            TrueComponentPositionOffset += new Vector2(deltaX, deltaY);
        }
        else
        {
            TrueInstancePositionOffsets.TryGetValue(instance, out Vector2 current);
            TrueInstancePositionOffsets[instance] = current + new Vector2(deltaX, deltaY);
        }
    }

    public void AccumulateTrueSizeOffset(InstanceSave? instance, float deltaWidth, float deltaHeight)
    {
        if (instance == null)
        {
            TrueComponentSizeOffset += new Vector2(deltaWidth, deltaHeight);
        }
        else
        {
            TrueInstanceSizeOffsets.TryGetValue(instance, out Vector2 current);
            TrueInstanceSizeOffsets[instance] = current + new Vector2(deltaWidth, deltaHeight);
        }
    }

    public Vector2 GetTruePositionOffset(InstanceSave? instance)
    {
        if (instance == null)
        {
            return TrueComponentPositionOffset;
        }
        TrueInstancePositionOffsets.TryGetValue(instance, out Vector2 current);
        return current;
    }

    public Vector2 GetTrueSizeOffset(InstanceSave? instance)
    {
        if (instance == null)
        {
            return TrueComponentSizeOffset;
        }
        TrueInstanceSizeOffsets.TryGetValue(instance, out Vector2 current);
        return current;
    }

    /// <summary>
    /// Returns whether the cursor has moved enough from the initial grab point to start applying movement/sizing.
    /// This is initially false to prevent accidental movement when clicking on an object.
    /// </summary>
    public bool HasMovedEnough
    {
        get
        {
            float pixelsToMoveBeforeApplying = 6;
            var cursor = _cursor;

            bool toReturn = false;

            if (cursor.PrimaryDown)
            {
                toReturn |=
                    Math.Abs(cursor.X - CursorPushScreenX) > pixelsToMoveBeforeApplying ||
                    Math.Abs(cursor.Y - CursorPushScreenY) > pixelsToMoveBeforeApplying;


            }

            return toReturn;
        }
    }

    public GrabbedState(ISelectedState selectedState,
        IWireframeObjectManager wireframeObjectManager,
        IGumCursorState cursor)
    {
        _selectedState = selectedState;
        _wireframeObjectManager = wireframeObjectManager;
        _cursor = cursor;
    }

    public void HandlePush()
    {
        var cursor = _cursor;

        CursorPushScreenX = cursor.X;
        CursorPushScreenY = cursor.Y;

        AccumulatedXOffset = 0;
        AccumulatedYOffset = 0;

        TrueComponentPositionOffset = Vector2.Zero;
        TrueComponentSizeOffset = Vector2.Zero;
        TrueInstancePositionOffsets.Clear();
        TrueInstanceSizeOffsets.Clear();

        if(_selectedState.SelectedStateSave != null)
        {
            RecordInitialPositions();
        }
    }

    private void RecordInitialPositions()
    {
        InstancePositions.Clear();
        InstanceSizes.Clear();

        StateSave = _selectedState.SelectedStateSave.Clone();

        if (_selectedState.SelectedInstances.Count() == 0 && _selectedState.SelectedElement != null)
        {
            var graphicalUiElement = _wireframeObjectManager.GetRepresentation(_selectedState.SelectedElement);

            ComponentPosition = new Vector2(graphicalUiElement.X, graphicalUiElement.Y);
            ComponentSize = new Vector2(graphicalUiElement.Width, graphicalUiElement.Height);
        }
        else if(_selectedState.SelectedInstances.Count() != 0)
        {
            var stateSave = _selectedState.SelectedStateSave;
            foreach(var instance in _selectedState.SelectedInstances)
            {
                var instanceGue = _wireframeObjectManager.GetRepresentation(instance);

                if(instanceGue != null)
                {
                    float? instanceStateX = stateSave.GetValue($"{instance.Name}.X") as float?;
                    float? instanceStateY = stateSave.GetValue($"{instance.Name}.Y") as float?;

                    var stateAndAbsolutePositions = new StateAndAbsoluteVector2
                    {
                        AbsoluteX = instanceGue.X,
                        AbsoluteY = instanceGue.Y,
                        StateX = instanceStateX,
                        StateY = instanceStateY
                    };
                    InstancePositions.Add(instance, stateAndAbsolutePositions);
                    InstanceSizes.Add(instance,
                        new Vector2(instanceGue.Width, instanceGue.Height));
                }

            }
        }
    }
}
