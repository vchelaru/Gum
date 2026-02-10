using Gum.DataTypes.Variables;
using Gum.Input;
using Gum.ToolStates;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Vector2 = System.Numerics.Vector2;
using Matrix = System.Numerics.Matrix4x4;
using Gum.Managers;
using ToolsUtilitiesStandard.Helpers;
using RenderingLibrary.Math;
using Gum.Wireframe.Editors.Visuals;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;

namespace Gum.Wireframe.Editors
{
    public class PolygonWireframeEditor : WireframeEditor
    {
        #region Fields/Properties

        // Visual components
        private readonly PolygonPointNodesVisual _pointNodesVisual;
        private readonly AddPointSpriteVisual _addPointSpriteVisual;
        private readonly SelectedPointHighlightVisual _selectedPointHighlightVisual;
        private readonly OriginDisplayVisual _originDisplayVisual;
        private readonly EditorContext _context;

        const float RadiusAtNoZoom = 5;

        int? grabbedIndex = null;
        int? selectedIndex = null;

        Layer layer;

        List<GraphicalUiElement> selectedPolygons = new List<GraphicalUiElement>();
        LinePolygon SelectedLinePolygon => selectedPolygons.FirstOrDefault()?.RenderableComponent as LinePolygon;

        bool hasGrabbedBodyOrPoint = false;

        public override bool HasCursorOver
        {
            get
            {
                var isOver = false;

                var cursor = InputLibrary.Cursor.Self;

                var x = cursor.GetWorldX();
                var y = cursor.GetWorldY();

                foreach(var gue in selectedPolygons)
                {
                    var polygon = gue.RenderableComponent as LinePolygon;

                    if(polygon.IsPointInside(x, y))
                    {
                        isOver = true;
                        break;
                    }
                }

                if(!isOver)
                {
                    var pointIndexOver = _pointNodesVisual.GetIndexOver(x, y);

                    if(pointIndexOver != null)
                    {
                        isOver = true;
                    }
                }

                if(!isOver)
                {
                    if (_addPointSpriteVisual.IsPointOver(x, y))
                    {
                        isOver = true;
                    }
                }

                return isOver;
            }
        }

        float NodeDisplayWidth
        {
            get
            {
                return RadiusAtNoZoom * 2/ Renderer.Self.Camera.Zoom;
            }
        }

        #endregion

        #region Constructor/Update To

        public PolygonWireframeEditor(
            Layer layer, 
            HotkeyManager hotkeyManager, 
            SelectionManager selectionManager,
            ISelectedState selectedState) 
            : base(
                  hotkeyManager, 
                  selectionManager,
                  selectedState)
        {
            this.layer = layer;

            // Create EditorContext for visual components
            _context = new EditorContext(
                selectedState,
                selectionManager,
                Gum.Services.Locator.GetRequiredService<Gum.ToolCommands.IElementCommands>(),
                Gum.Services.Locator.GetRequiredService<Gum.Commands.IGuiCommands>(),
                Gum.Services.Locator.GetRequiredService<Gum.Commands.IFileCommands>(),
                Gum.Services.Locator.GetRequiredService<ISetVariableLogic>(),
                Gum.Services.Locator.GetRequiredService<Gum.Undo.IUndoManager>(),
                Gum.Services.Locator.GetRequiredService<IVariableInCategoryPropagationLogic>(),
                hotkeyManager,
                Gum.Services.Locator.GetRequiredService<IWireframeObjectManager>(),
                layer,
                grabbedState,
                System.Drawing.Color.White,
                System.Drawing.Color.White);

            // Create visual components
            _pointNodesVisual = new PolygonPointNodesVisual(_context, layer);
            _addPointSpriteVisual = new AddPointSpriteVisual(_context, layer);
            _selectedPointHighlightVisual = new SelectedPointHighlightVisual(_context, layer);
            _originDisplayVisual = new OriginDisplayVisual(_context);
        }

        public override void UpdateToSelection(ICollection<GraphicalUiElement> selectedObjects)
        {
            selectedPolygons.Clear();
            selectedPolygons.AddRange(selectedObjects);

            // Update context's selected objects for visuals
            _context.SelectedObjects.Clear();
            _context.SelectedObjects.AddRange(selectedObjects);

            // Notify visual components of selection change
            _pointNodesVisual.UpdateToSelection(selectedObjects);
            _addPointSpriteVisual.UpdateToSelection(selectedObjects);
            _selectedPointHighlightVisual.UpdateToSelection(selectedObjects);
            _originDisplayVisual.UpdateToSelection(selectedObjects);

            // Clear selected index when selection changes
            selectedIndex = null;
        }

        #endregion

        #region Activity Functions

        public override void Activity(ICollection<GraphicalUiElement> selectedObjects, SystemManagers systemManagers)
        {
            if (selectedObjects.Count != 0)
            {
                PushActivity();

                ClickActivity();

                HandlesActivity(systemManagers);

                BodyGrabbingActivity();

                // Update visual component state
                _addPointSpriteVisual.IsEnabled = grabbedIndex == null && hasGrabbedBodyOrPoint == false;
                _selectedPointHighlightVisual.SelectedIndex = selectedIndex;

                // Update all visual components
                _pointNodesVisual.Update();
                _addPointSpriteVisual.Update();
                _selectedPointHighlightVisual.Update();
                _originDisplayVisual.Update();
            }
        }

        private void PushActivity()
        {
            var cursor = InputLibrary.Cursor.Self;
            if (cursor.PrimaryPush)
            {
                var x = cursor.GetWorldX();
                var y = cursor.GetWorldY();

                mHasChangedAnythingSinceLastPush = false;

                var existingPointIndexOver = _pointNodesVisual.GetIndexOver(x, y);

                var isAddPointSpriteVisible = _addPointSpriteVisual.IsPointOver(x, y);

                if (existingPointIndexOver != null || isAddPointSpriteVisible == false)
                {
                    grabbedState.HandlePush();

                    grabbedIndex = GetIndexOver(x, y);
                }
                else if (isAddPointSpriteVisible)
                {
                    int newIndex = AddPointAt(x, y);
                    grabbedIndex = newIndex;
                }
                
                hasGrabbedBodyOrPoint = HasCursorOver;

                selectedIndex = grabbedIndex;
            }
        }

        private int AddPointAt(float x, float y)
        {
            var newIndex = GetClosestLineOver(x, y).ClosestIndex + 1;

            var selectedPoly = SelectedLinePolygon;

            var newPoint = (selectedPoly.PointAt(newIndex - 1) + selectedPoly.PointAt(newIndex)) / 2.0f;

            selectedPoly.InsertPointAt(newPoint, newIndex);

            // fall through to the next part, to grab the point automatically


            grabbedState.HandlePush();

            hasGrabbedBodyOrPoint = true;
            mHasChangedAnythingSinceLastPush = true;

            _guiCommands.RefreshVariableValues();

            return newIndex;
        }

        private void ClickActivity()
        {
            var cursor = InputLibrary.Cursor.Self;

            if (cursor.PrimaryClick)
            {
                hasGrabbedBodyOrPoint = false;
                grabbedIndex = null;

                // todo - shift and snap to pixels
            }

            if (cursor.PrimaryClick && mHasChangedAnythingSinceLastPush)
            {
                ApplyVertexValues();
                DoEndOfSettingValuesLogic();
            }
        }

        private void ApplyVertexValues()
        {
            var linePolygon = SelectedLinePolygon;
            if(linePolygon != null)
            {
                List<Vector2> vectors = new List<Vector2>(linePolygon.PointCount);

                for(int i = 0; i < linePolygon.PointCount; i++)
                {
                    vectors.Add(linePolygon.PointAt(i));
                }

                var variableName = "Points";
                if(_selectedState.SelectedInstance != null)
                {
                    variableName = _selectedState.SelectedInstance.Name + "." + variableName;
                }

                var pointsVariableList = 
                    _selectedState.SelectedStateSave.VariableLists.FirstOrDefault(item => item.Name == variableName);

                // This might be null if the points aren't set in this state, but are inherited from a base 
                // state or object
                if(pointsVariableList == null)
                {
                    pointsVariableList = new VariableListSave<Vector2>();
                    pointsVariableList.Name = variableName;
                    pointsVariableList.Type = "Vector2";
                    _selectedState.SelectedStateSave.VariableLists.Add(pointsVariableList);
                }
                pointsVariableList.ValueAsIList = vectors;
            }
        }

        private void HandlesActivity(SystemManagers systemManagers)
        {
            var linePolygon = SelectedLinePolygon;

            var cursor = InputLibrary.Cursor.Self;

            int? indexOver = grabbedIndex;

            if (grabbedIndex == null && linePolygon != null)
            {
                indexOver = _pointNodesVisual.GetIndexOver(cursor.GetWorldX(), cursor.GetWorldY());
            }

            // Update visual component state for highlighting
            _pointNodesVisual.HighlightedIndex = indexOver;
            _pointNodesVisual.GrabbedIndex = grabbedIndex;

            if(grabbedIndex != null && linePolygon != null && (cursor.XChange != 0 || cursor.YChange != 0))
            {
                MoveGrabbedPoint(cursor, systemManagers);
            }
        }

        private void MoveGrabbedPoint(InputLibrary.Cursor cursor, SystemManagers systemManagers)
        {
            var linePolygon = SelectedLinePolygon;
            mHasChangedAnythingSinceLastPush = true;

            var pointAtIndex = linePolygon.PointAt(grabbedIndex.Value);

            var zoom = Renderer.Self.Camera.Zoom;

            Matrix.Invert(linePolygon.GetAbsoluteRotationMatrix(), out Matrix rotationMatrix);

            var change = 
                (cursor.XChange * rotationMatrix.Right().ToVector2() +
                cursor.YChange * rotationMatrix.Up().ToVector2()) / zoom;

            pointAtIndex.X += change.X;
            pointAtIndex.Y += change.Y;

            // So we don't get wacky coordinates, let's round the value to the nearest unit 
            // finish here:
            var camera = systemManagers.Renderer.Camera;
            var roundMultiple =  1 / camera.Zoom;
            pointAtIndex.X = MathFunctions.RoundFloat(pointAtIndex.X, roundMultiple);
            pointAtIndex.Y = MathFunctions.RoundFloat(pointAtIndex.Y, roundMultiple);

            var shouldSetFirstAndLast = (grabbedIndex == 0 || grabbedIndex == linePolygon.PointCount - 1) &&
                linePolygon.PointAt(0) == linePolygon.PointAt(linePolygon.PointCount - 1);

            if (shouldSetFirstAndLast)
            {
                linePolygon.SetPointAt(pointAtIndex, 0);
                linePolygon.SetPointAt(pointAtIndex, linePolygon.PointCount - 1);
            }
            else
            {
                linePolygon.SetPointAt(pointAtIndex, grabbedIndex.Value);
            }

            // The values haven't yet been pushed up to the 
            // element/Instance, so this won't do anything yet.
            // Instead we rely on DoEndOfSettingValuesLogic 
            _guiCommands.RefreshVariableValues();
        }

        private void BodyGrabbingActivity()
        {
            var cursor = InputLibrary.Cursor.Self;
            if (cursor.PrimaryDown && hasGrabbedBodyOrPoint &&
                grabbedState.HasMovedEnough && grabbedIndex == null)
            {
                ApplyCursorMovement(cursor);
            }
        }

        #endregion

        public override bool TryHandleDelete()
        {
            // handle it but don't actually allow deleting if 4 points or less (3 visible + 1 dupe = 4)
            if(selectedIndex != null)
            {
                var selectedPolygon = SelectedLinePolygon;
                var canDelete = selectedPolygon.PointCount > 4;

                if(canDelete == false)
                {
                    _guiCommands.PrintOutput("Cannot delete point, polygon requires at least 3 points");
                }
                else
                {
                    var isDuplicatePoint = selectedIndex == 0 ||
                        selectedIndex == selectedPolygon.PointCount - 1;

                    if(!isDuplicatePoint)
                    {
                        selectedPolygon.RemovePointAtIndex(selectedIndex.Value);
                    }
                    else
                    {
                        selectedPolygon.RemovePointAtIndex(0);
                        var new0Position = selectedPolygon.PointAt(0);
                        selectedPolygon.SetPointAt(new0Position, selectedPolygon.PointCount - 1);
                    }

                    if(selectedIndex >= selectedPolygon.PointCount)
                    {
                        selectedIndex--;
                    }
                    if (grabbedIndex >= selectedPolygon.PointCount)
                    {
                        grabbedIndex--;
                    }

                    ApplyVertexValues();
                    DoEndOfSettingValuesLogic();
                }

                return true;
            }
            return false;
        }

        public override void Destroy()
        {
            _pointNodesVisual.Destroy();
            _addPointSpriteVisual.Destroy();
            _selectedPointHighlightVisual.Destroy();
            _originDisplayVisual.Destroy();
        }

        #region Get/Find methods

        public override Cursor GetWindowsCursorToShow(Cursor defaultCursor, float worldXAt, float worldYAt)
        {
            var pointOver = GetIndexOver(worldXAt, worldYAt);

            if(pointOver != null)
            {
                // Do we want different cursors here?
                return System.Windows.Forms.Cursors.SizeAll;
            }
            else if(HasCursorOver)
            {
                // for now just return the move cursor, eventually do the 
                return System.Windows.Forms.Cursors.SizeAll;
            }
            return defaultCursor;
        }

        private int? GetIndexOver(float worldXAt, float worldYAt)
        {
            return _pointNodesVisual.GetIndexOver(worldXAt, worldYAt);
        }

        private (int ClosestIndex, float MinDistance) GetClosestLineOver(float worldXAt, float worldYAt)
        {
            float minSoFar = float.MaxValue;

            int closestIndex = -1;
            Vector2 cursorPosition = new Vector2(worldXAt, worldYAt);

            var linePolygon = SelectedLinePolygon;
            var linePolygonPosition = new Vector2(linePolygon.GetAbsoluteLeft(), linePolygon.GetAbsoluteTop());

            for (int i = 0; i < linePolygon.PointCount - 1; i++)
            {
                var point1 = linePolygon.AbsolutePointAt(i);
                var point2 = linePolygon.AbsolutePointAt(i + 1);
                var average = (point1 + point2) / 2.0f;

                var distance = (cursorPosition - average).Length();

                if(distance < minSoFar)
                {
                    minSoFar = distance;
                    closestIndex = i;
                }
            }

            return (closestIndex, minSoFar);
        }

        private float DistanceTo(Vector2 point1, Vector2 point2, float x, float y)
        {
            float segmentLength = (point2 - point1).Length();

            Vector2 normalizedLine = new Vector2(
                (float)(point2.X - point1.X) / segmentLength,
                (float)(point2.Y - point1.Y) / segmentLength);

            Vector2 pointVector = new Vector2((float)(x - point1.X), (float)(y - point1.Y));

            float length = Vector2.Dot(pointVector, normalizedLine);

            if (length < 0)
            {
                return (new Vector2(x, y) - point1).Length();
            }
            else if (length > segmentLength)
            {
                return (new Vector2(x, y) - point2).Length();
            }
            else
            {
                normalizedLine.X *= length;
                normalizedLine.Y *= length;

                float xDistanceSquared = pointVector.X - normalizedLine.X;
                xDistanceSquared = xDistanceSquared * xDistanceSquared;

                float yDistanceSquared = pointVector.Y - normalizedLine.Y;
                yDistanceSquared = yDistanceSquared * yDistanceSquared;

                return (float)System.Math.Sqrt(xDistanceSquared + yDistanceSquared);
            }
        }

        #endregion
    }
}
