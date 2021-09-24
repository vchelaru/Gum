using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins;
using Gum.ToolStates;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Wireframe
{
    public abstract class WireframeEditor
    {
        protected GrabbedInitialState grabbedInitialState = new GrabbedInitialState();

        protected bool mHasChangedAnythingSinceLastPush = false;

        protected float aspectRatioOnGrab;

        public bool RestrictToUnitValues { get; set; }

        public abstract void UpdateToSelection(ICollection<GraphicalUiElement> selectedObjects);

        public abstract bool HasCursorOver { get; }

        public void UpdateAspectRatioForGrabbedIpso()
        {
            if (SelectedState.Self.SelectedInstance != null &&
                SelectedState.Self.SelectedIpso != null
                )
            {
                IPositionedSizedObject ipso = SelectedState.Self.SelectedIpso;

                float width = ipso.Width;
                float height = ipso.Height;

                if (height != 0)
                {
                    aspectRatioOnGrab = width / height;
                }
            }
        }

        public abstract void Activity(ICollection<GraphicalUiElement> selectedObjects);

        public abstract System.Windows.Forms.Cursor GetWindowsCursorToShow(
            System.Windows.Forms.Cursor defaultCursor, float worldXAt, float worldYAt);

        public abstract void Destroy();

        public virtual bool TryHandleDelete()
        {
            return false;
        }

        protected void ApplyCursorMovement(InputLibrary.Cursor cursor)
        {
            float xToMoveBy = cursor.XChange / Renderer.Self.Camera.Zoom;
            float yToMoveBy = cursor.YChange / Renderer.Self.Camera.Zoom;

            var vector2 = new Vector2(xToMoveBy, yToMoveBy);
            var selectedObject = WireframeObjectManager.Self.GetSelectedRepresentation();
            if(selectedObject?.Parent != null)
            {
                var parentRotation = MathHelper.ToRadians( selectedObject.Parent.GetAbsoluteRotation() );

                global::RenderingLibrary.Math.MathFunctions.RotateVector(ref vector2, parentRotation);

                xToMoveBy = vector2.X;
                yToMoveBy = vector2.Y;
            }

            var didMove = EditingManager.Self.MoveSelectedObjectsBy(xToMoveBy, yToMoveBy);

            bool isShiftDown = InputLibrary.Keyboard.Self.KeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) ||
                InputLibrary.Keyboard.Self.KeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift);


            if (SelectedState.Self.SelectedInstances.Count() == 0 &&
                (SelectedState.Self.SelectedComponent != null || SelectedState.Self.SelectedStandardElement != null))
            {
                if (isShiftDown)
                {
                    var xOrY = grabbedInitialState.AxisMovedFurthestAlong;

                    if (xOrY == XOrY.X)
                    {
                        var gue = WireframeObjectManager.Self.GetRepresentation(SelectedState.Self.SelectedElement);

                        gue.Y = grabbedInitialState.ComponentPosition.Y;
                    }
                    else
                    {

                        var gue = WireframeObjectManager.Self.GetRepresentation(SelectedState.Self.SelectedElement);

                        gue.X = grabbedInitialState.ComponentPosition.X;
                    }
                }
            }
            else
            {
                if (isShiftDown)
                {
                    var selectedInstances = SelectedState.Self.SelectedInstances;

                    foreach (InstanceSave instance in selectedInstances)
                    {

                        var xOrY = grabbedInitialState.AxisMovedFurthestAlong;

                        if (xOrY == XOrY.X)
                        {
                            var gue = WireframeObjectManager.Self.GetRepresentation(instance);

                            gue.Y = grabbedInitialState.InstancePositions[instance].Y;
                        }
                        else
                        {

                            var gue = WireframeObjectManager.Self.GetRepresentation(instance);

                            gue.X = grabbedInitialState.InstancePositions[instance].X;
                        }

                    }
                }
            }

            if (didMove)
            {
                mHasChangedAnythingSinceLastPush = true;
            }
        }


        protected void DoEndOfSettingValuesLogic()
        {
            var selectedElement = SelectedState.Self.SelectedElement;

            if (ProjectManager.Self.GeneralSettingsFile.AutoSave)
            {
                ProjectManager.Self.SaveElement(selectedElement);
            }


            var stateSave = SelectedState.Self.SelectedStateSave;

            var element = SelectedState.Self.SelectedElement;

            foreach (var newVariable in stateSave.Variables.ToList())
            {
                var oldValue = grabbedInitialState.StateSave.GetValue(newVariable.Name);

                if (DoValuesDiffer(stateSave, newVariable.Name, oldValue))
                {
                    // report this:
                    if (!string.IsNullOrEmpty(newVariable.SourceObject))
                    {
                        var instance = element.GetInstance(newVariable.SourceObject);
                        PluginManager.Self.VariableSet(element, instance, newVariable.GetRootName(), oldValue);
                    }
                    else
                    {
                        PluginManager.Self.VariableSet(element, null, newVariable.GetRootName(), oldValue);
                    }
                }
            }

            mHasChangedAnythingSinceLastPush = false;
        }

        protected bool DoValuesDiffer(StateSave newStateSave, string variableName, object oldValue)
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
            if (newValue == null && oldValue == null)
            {
                return false;
            }
            // neither are null
            else
            {
                if (oldValue is float)
                {
                    var oldFloat = (float)oldValue;
                    var newFloat = (float)newValue;

                    return oldFloat != newFloat;
                }
                else if (oldValue is string)
                {
                    return (string)oldValue != (string)newValue;
                }
                else if (oldValue is bool)
                {
                    return (bool)oldValue != (bool)newValue;
                }
                else if (oldValue is int)
                {
                    return (int)oldValue != (int)newValue;
                }
                else if (oldValue is Vector2)
                {
                    return (Vector2)oldValue != (Vector2)newValue;
                }
                else
                {
                    return oldValue.Equals(newValue) == false;
                }
            }
        }

    }
}
