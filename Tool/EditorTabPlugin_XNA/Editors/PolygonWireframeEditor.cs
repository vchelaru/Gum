using Gum.DataTypes.Variables;
using Gum.Input;
using Gum.ToolStates;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ToolsUtilities;
using Vector2 = System.Numerics.Vector2;
using Color = System.Drawing.Color;
using Matrix = System.Numerics.Matrix4x4;
using Gum.Managers;
using ToolsUtilitiesStandard.Helpers;
using RenderingLibrary.Math;

namespace Gum.Wireframe.Editors
{
    public class PolygonWireframeEditor : WireframeEditor
    {
        #region Fields/Properties

        OriginDisplay originDisplay;

        const float RadiusAtNoZoom = 5;

        int? grabbedIndex = null;
        int? selectedIndex = null;

        Layer layer;

        List<GraphicalUiElement> selectedPolygons = new List<GraphicalUiElement>();
        LinePolygon SelectedLinePolygon => selectedPolygons.FirstOrDefault()?.RenderableComponent as LinePolygon;

        List<SolidRectangle> pointNodes = new List<SolidRectangle>();
        Sprite addPointSprite;
        static Texture2D addPointTexture;
        LineRectangle selectedPointLineRectangle;

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
                    var pointIndexOver = GetIndexOver(x, y);

                    if(pointIndexOver != null)
                    {
                        isOver = true;
                    }
                }

                if(!isOver)
                {
                    if (IsPointOverAddPointSprite(x, y))
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

        private bool IsPointOverAddPointSprite(float x, float y)
        {
            return x > addPointSprite.X && x < addPointSprite.X + addPointSprite.Width &&
                                    y > addPointSprite.Y && y < addPointSprite.Y + addPointSprite.Height;
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
            if(addPointTexture == null)
            {
                var gumExePath = System.IO.Path.GetDirectoryName(
                    System.Reflection.Assembly.GetEntryAssembly().Location).ToLower().Replace("/", "\\") + "\\";

                ContentLoader loader = new ContentLoader();
                var loaderManager = new LoaderManager();

                loaderManager.ContentLoader = loader;
                var fileName = gumExePath + "Content/AddPoint.png";
                //addPointTexture = loaderManager.LoadContent(fileName);

                using (var stream = FileManager.GetStreamForFile(fileName))
                {
                    addPointTexture = Texture2D.FromStream(SystemManagers.Default.Renderer.GraphicsDevice,
                        stream);

                    addPointTexture.Name = fileName;
                }

                //addPointTexture = LoaderManager.Self.LoadContent<Texture2D>(gumExePath + "Content/AddPoint.png");
            }
            this.layer = layer;

            addPointSprite = new Sprite(addPointTexture);
            addPointSprite.Name = "Add point sprite";
            SpriteManager.Self.Add(addPointSprite, layer);

            selectedPointLineRectangle = new LineRectangle();
            ShapeManager.Self.Add(selectedPointLineRectangle, layer);
            selectedPointLineRectangle.Color = Color.Magenta;
            selectedPointLineRectangle.IsDotted = false;
            selectedPointLineRectangle.LinePixelWidth = 3;

            originDisplay = new OriginDisplay(layer);
        }

        public override void UpdateToSelection(ICollection<GraphicalUiElement> selectedObjects)
        {
            selectedPolygons.Clear();

            selectedPolygons.AddRange(selectedObjects);

            UpdatePointNodes();
        }

        private void UpdatePointNodes()
        {
            var neededNumberOfPointCircles = 0;

            LinePolygon linePolygon = SelectedLinePolygon;

            if(linePolygon != null)
            {
                neededNumberOfPointCircles = linePolygon.PointCount;
            }

            // create needed circles
            while(pointNodes.Count < neededNumberOfPointCircles)
            {
                var rectangle = new SolidRectangle();
                rectangle.Width = NodeDisplayWidth;
                rectangle.Height = NodeDisplayWidth;
                ShapeManager.Self.Add(rectangle, layer);
                pointNodes.Add(rectangle);
            }

            // destroy excess circles
            while(pointNodes.Count > neededNumberOfPointCircles)
            {
                var node = pointNodes.Last();
                ShapeManager.Self.Remove(node);
                pointNodes.Remove(node);
            }

            if(linePolygon != null)
            {
                var nodeDimension = NodeDisplayWidth;
                // position circles
                for (int i = 0; i < linePolygon.PointCount; i++)
                {
                    var point = linePolygon.AbsolutePointAt(i);

                    pointNodes[i].X = point.X - nodeDimension/2;
                    pointNodes[i].Y = point.Y - nodeDimension/2;

                    pointNodes[i].Width = nodeDimension;
                    pointNodes[i].Height = nodeDimension;
                }
            }
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

                UpdatePointNodes();

                UpdateAddPointSprite();

                UpdateSelectedNodeLineRectangle();

                originDisplay.UpdateTo(selectedObjects.First());
            }
        }

        private void UpdateSelectedNodeLineRectangle()
        {
            var selectedPolygon = SelectedLinePolygon;
            var hasSelection = selectedIndex != null && selectedIndex < selectedPolygon.PointCount;
            selectedPointLineRectangle.Visible = hasSelection;

            if(hasSelection)
            {
                var zoom = Renderer.Self.Camera.Zoom;
                selectedPointLineRectangle.Width = NodeDisplayWidth + 6/zoom;
                selectedPointLineRectangle.Height = NodeDisplayWidth + 6 / zoom;

                var selectedVertexPosition = selectedPolygon.AbsolutePointAt(selectedIndex.Value);

                selectedPointLineRectangle.X = selectedVertexPosition.X - selectedPointLineRectangle.Width / 2;
                selectedPointLineRectangle.Y = selectedVertexPosition.Y - selectedPointLineRectangle.Height / 2;
            }
        }

        private void UpdateAddPointSprite()
        {
            var canUpdatePoint = grabbedIndex == null && hasGrabbedBodyOrPoint == false;

            addPointSprite.Visible = false;

            const int maxPixelsForAddPoint = 15;

            if (canUpdatePoint)
            {

                var worldX = InputLibrary.Cursor.Self.GetWorldX();
                var worldY = InputLibrary.Cursor.Self.GetWorldY();

                var zoom = Renderer.Self.Camera.Zoom;

                this.addPointSprite.Width = this.addPointSprite.Height = 16 / zoom;

                var closestResult = GetClosestLineOver(worldX, worldY);

                addPointSprite.Visible = closestResult.MinDistance < (maxPixelsForAddPoint / zoom);

                if(addPointSprite.Visible)
                {
                    // give preverential treatment to existing points:
                    var existingPointIndexOver = GetIndexOver(worldX, worldY);
                    addPointSprite.Visible = existingPointIndexOver == null;
                }

                if (addPointSprite.Visible)
                {
                    var selectedPolygon = SelectedLinePolygon;
                    var addPointPosition = (selectedPolygon.AbsolutePointAt(closestResult.ClosestIndex) + selectedPolygon.AbsolutePointAt(closestResult.ClosestIndex+ 1)) / 2.0f;

                    addPointSprite.X = addPointPosition.X - addPointSprite.Width / 2.0f;
                    addPointSprite.Y = addPointPosition.Y - addPointSprite.Height / 2.0f;
                }

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

                var existingPointIndexOver = GetIndexOver(x, y);

                var isAddPointSpriteVisible = IsPointOverAddPointSprite(x, y);

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
                indexOver = GetIndexOver(cursor.GetWorldX(), cursor.GetWorldY());
            }

            for(int i = 0; i < pointNodes.Count; i++)
            {
                if(i == indexOver)
                {
                    pointNodes[i].Color = Color.Yellow;
                }
                else
                {
                    pointNodes[i].Color = Color.Gray;
                }

                if(indexOver == 0 && i == pointNodes.Count-1)
                {
                    pointNodes[i].Color = Color.Yellow;
                }
            }

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
            var roundMultiple = 1 / systemManagers.Renderer.Camera.Zoom;
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
            System.Diagnostics.Debug.WriteLine("Grabbed Index" + grabbedIndex);
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
            for (int i = 0; i < pointNodes.Count; i++)
            {
                ShapeManager.Self.Remove(pointNodes[i]);
            }

            SpriteManager.Self.Remove(addPointSprite);
            ShapeManager.Self.Remove(selectedPointLineRectangle);
            originDisplay.Destroy();
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
            var effectiveRadius = RadiusAtNoZoom / Renderer.Self.Camera.Zoom;
            // consider zoom:
            float currentZoomRadius = effectiveRadius * effectiveRadius;
            for(int i = 0; i < pointNodes.Count; i++)
            {
                var left = pointNodes[i].X;
                var top = pointNodes[i].Y;
                var right = left + effectiveRadius * 2;
                var bottom = top + effectiveRadius * 2;

                if(worldXAt > left && worldXAt < right &&
                    worldYAt > top && worldYAt < bottom)
                {
                    return i;
                }
            }

            return null;
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
