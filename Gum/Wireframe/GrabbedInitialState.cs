using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ToolStates;
using InputLibrary;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Wireframe
{
    public class GrabbedInitialState
    {

        public StateSave StateSave { get; private set; }

        public XOrY AxisMovedFurthestAlong
        {
            get
            {
                Cursor cursor = InputLibrary.Cursor.Self;

                if (System.Math.Abs( cursor.X - CursorPushX) > 
                    System.Math.Abs( cursor.Y - CursorPushY))
                {
                    return XOrY.X;
                }
                else
                {
                    return XOrY.Y;
                }
            }
        }

        public float CursorPushX
        {
            get;
            set;
        }

        public float CursorPushY
        {
            get;
            set;
        }

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
                        Math.Abs(cursor.X - CursorPushX) > pixelsToMoveBeforeApplying ||
                        Math.Abs(cursor.Y - CursorPushY) > pixelsToMoveBeforeApplying;


                }

                return toReturn;
            }
        }

        public void HandlePush()
        {
            Cursor cursor = InputLibrary.Cursor.Self;

            CursorPushX = cursor.X;
            CursorPushY = cursor.Y;

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
