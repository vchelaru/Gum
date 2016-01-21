using Gum.Converters;
using Gum.DataTypes;
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

        public Vector2 ComponentPosition
        {
            get;
            private set;

        }
        public Dictionary<InstanceSave, Vector2> InstancePositions
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

            RecordInitialPositions();
        }

        private void RecordInitialPositions()
        {
            InstancePositions.Clear();
            if(SelectedState.Self.SelectedInstances.Count() == 0)
            {
                var ipso = WireframeObjectManager.Self.GetRepresentation(SelectedState.Self.SelectedElement);

                ComponentPosition = new Vector2(ipso.X, ipso.Y);
            }
            else if(SelectedState.Self.SelectedInstances.Count() != 0)
            {
                foreach(var instance in SelectedState.Self.SelectedInstances)
                {
                    var ipso = WireframeObjectManager.Self.GetRepresentation(instance);

                    if(ipso != null)
                    {
                        InstancePositions.Add(instance,
                            new Vector2(ipso.X, ipso.Y));
                    }

                }
            }
        }
    }
}
