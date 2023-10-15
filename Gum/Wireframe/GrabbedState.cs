using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ToolStates;
using InputLibrary;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Wireframe
{
    public class GrabbedState
    {

        public StateSave StateSave { get; private set; }

        public XOrY AxisMovedFurthestAlong
        {
            get
            {
                Cursor cursor = InputLibrary.Cursor.Self;

                if (System.Math.Abs( cursor.X - CursorPushScreenX) > 
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

        public Dictionary<InstanceSave, Vector2> InstancePositions
        {
            get;
            private set;
        } = new Dictionary<InstanceSave, Vector2>();

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

        public void HandlePush()
        {
            Cursor cursor = InputLibrary.Cursor.Self;

            CursorPushScreenX = cursor.X;
            CursorPushScreenY = cursor.Y;

            AccumulatedXOffset = 0;
            AccumulatedYOffset = 0;

            if(SelectedState.Self.SelectedStateSave != null)
            {
                RecordInitialPositions();
            }
        }

        private void RecordInitialPositions()
        {
            InstancePositions.Clear();
            InstanceSizes.Clear();

            StateSave = SelectedState.Self.SelectedStateSave.Clone();

            if (SelectedState.Self.SelectedInstances.Count() == 0 && SelectedState.Self.SelectedElement != null)
            {
                var graphicalUiElement = WireframeObjectManager.Self.GetRepresentation(SelectedState.Self.SelectedElement);

                ComponentPosition = new Vector2(graphicalUiElement.X, graphicalUiElement.Y);
                ComponentSize = new Vector2(graphicalUiElement.Width, graphicalUiElement.Height);
            }
            else if(SelectedState.Self.SelectedInstances.Count() != 0)
            {
                foreach(var instance in SelectedState.Self.SelectedInstances)
                {
                    var instanceGue = WireframeObjectManager.Self.GetRepresentation(instance);

                    if(instanceGue != null)
                    {
                        InstancePositions.Add(instance,
                            new Vector2(instanceGue.X, instanceGue.Y));
                        InstanceSizes.Add(instance,
                            new Vector2(instanceGue.Width, instanceGue.Height));
                    }

                }
            }
        }
    }
}
