using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ToolStates;
using InputLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using Vector2 = System.Numerics.Vector2;
using Matrix = System.Numerics.Matrix4x4;
using Gum.Services;

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

    public StateSave StateSave { get; private set; }

    public XOrY? AxisMovedFurthestAlong
    {
        get
        {
            Cursor cursor = InputLibrary.Cursor.Self;

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
    /// Returns whether the cursor has moved enough from the initial grab point to start applying movement/sizing.
    /// This is initially false to prevent accidental movement when clicking on an object.
    /// </summary>
    public bool HasMovedEnough
    {
        get
        {
            float pixelsToMoveBeforeApplying = 6;
            Cursor cursor = InputLibrary.Cursor.Self;

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
        IWireframeObjectManager wireframeObjectManager)
    {
        _selectedState = selectedState;
        _wireframeObjectManager = wireframeObjectManager;
    }

    public void HandlePush()
    {
        Cursor cursor = InputLibrary.Cursor.Self;

        CursorPushScreenX = cursor.X;
        CursorPushScreenY = cursor.Y;

        AccumulatedXOffset = 0;
        AccumulatedYOffset = 0;

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
