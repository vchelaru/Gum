using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using Gum.DataTypes.Variables;
using Gum.Wireframe.Editors.Visuals;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;
using RenderingLibrary.Math.Geometry;
using Matrix = System.Numerics.Matrix4x4;

namespace Gum.Wireframe.Editors.Handlers;

/// <summary>
/// Handles input for polygon point manipulation (selecting, dragging, adding, deleting points).
/// </summary>
public class PolygonPointInputHandler : InputHandlerBase
{
    private readonly PolygonPointNodesVisual _pointNodesVisual;
    private readonly AddPointSpriteVisual _addPointSpriteVisual;
    private readonly SelectedPointHighlightVisual _selectedPointHighlightVisual;

    private int? _grabbedIndex = null;
    private int? _selectedIndex = null;
    private GraphicalUiElement? _lastSelectedElement = null;

    public override int Priority => 95; // Higher than move, lower than resize

    /// <summary>
    /// Gets the currently selected point index and polygon.
    /// </summary>
    private LinePolygon? SelectedLinePolygon => Context.SelectedObjects
        .FirstOrDefault()?.RenderableComponent as LinePolygon;

    public PolygonPointInputHandler(
        EditorContext context,
        PolygonPointNodesVisual pointNodesVisual,
        AddPointSpriteVisual addPointSpriteVisual,
        SelectedPointHighlightVisual selectedPointHighlightVisual)
        : base(context)
    {
        _pointNodesVisual = pointNodesVisual;
        _addPointSpriteVisual = addPointSpriteVisual;
        _selectedPointHighlightVisual = selectedPointHighlightVisual;
    }

    public override bool HasCursorOver(float worldX, float worldY)
    {
        // Check if cursor is over an existing point node
        var pointIndexOver = _pointNodesVisual.GetIndexOver(worldX, worldY);
        if (pointIndexOver != null)
        {
            return true;
        }

        // Check if cursor is over the add point sprite
        if (_addPointSpriteVisual.IsPointOver(worldX, worldY))
        {
            return true;
        }

        return false;
    }

    public override Cursor? GetCursorToShow(float worldX, float worldY)
    {
        var pointOver = _pointNodesVisual.GetIndexOver(worldX, worldY);
        if (pointOver != null)
        {
            return System.Windows.Forms.Cursors.SizeAll;
        }

        if (_addPointSpriteVisual.IsPointOver(worldX, worldY))
        {
            return System.Windows.Forms.Cursors.Cross;
        }

        return null;
    }

    public override bool HandlePush(float worldX, float worldY)
    {
        Context.HasChangedAnythingSinceLastPush = false;

        var existingPointIndexOver = _pointNodesVisual.GetIndexOver(worldX, worldY);
        var isAddPointSpriteVisible = _addPointSpriteVisual.IsPointOver(worldX, worldY);

        if (existingPointIndexOver != null)
        {
            // Grabbing an existing point
            Context.GrabbedState.HandlePush();
            _grabbedIndex = existingPointIndexOver;
            _selectedIndex = _grabbedIndex;
            UpdateVisualState();
            IsActive = true;
            return true;
        }
        else if (isAddPointSpriteVisible)
        {
            // Adding a new point
            int newIndex = AddPointAt(worldX, worldY);
            _grabbedIndex = newIndex;
            _selectedIndex = newIndex;
            UpdateVisualState();
            IsActive = true;
            return true;
        }

        return false;
    }

    protected override void OnDrag()
    {
        if (_grabbedIndex == null) return;

        var cursor = InputLibrary.Cursor.Self;
        if (cursor.XChange == 0 && cursor.YChange == 0) return;

        MoveGrabbedPoint(cursor);
    }

    protected override void OnRelease()
    {
        _grabbedIndex = null;
        _addPointSpriteVisual.IsEnabled = true;

        if (Context.HasChangedAnythingSinceLastPush)
        {
            ApplyVertexValues();
            DoEndOfSettingValuesLogic();
        }

        UpdateVisualState();
    }

    public override void UpdateHover(float worldX, float worldY)
    {
        // Update which point is highlighted
        int? indexOver = _grabbedIndex ?? _pointNodesVisual.GetIndexOver(worldX, worldY);
        _pointNodesVisual.HighlightedIndex = indexOver;
        _pointNodesVisual.GrabbedIndex = _grabbedIndex;

        // Disable add point sprite while dragging
        _addPointSpriteVisual.IsEnabled = _grabbedIndex == null && !IsActive;

        // Hide add point sprite when over existing point
        if (indexOver != null && _addPointSpriteVisual.IsEnabled)
        {
            _addPointSpriteVisual.IsEnabled = false;
        }
    }

    public override void OnSelectionChanged()
    {
        _grabbedIndex = null;

        var currentSelection = Context.SelectedObjects.FirstOrDefault();

        // Only clear point selection if we switched to a different element
        if (currentSelection != _lastSelectedElement)
        {
            _selectedIndex = null;
        }

        _lastSelectedElement = currentSelection;
        UpdateVisualState();
    }

    public override bool TryHandleDelete()
    {
        if (_selectedIndex == null) return false;

        var selectedPolygon = SelectedLinePolygon;
        if (selectedPolygon == null) return false;

        // Can't delete if 4 points or less (3 visible + 1 dupe = 4)
        var canDelete = selectedPolygon.PointCount > 4;

        if (!canDelete)
        {
            Context.GuiCommands.PrintOutput("Cannot delete point, polygon requires at least 3 points");
            return true; // Handled, but didn't delete
        }

        var isDuplicatePoint = _selectedIndex == 0 ||
            _selectedIndex == selectedPolygon.PointCount - 1;

        if (!isDuplicatePoint)
        {
            selectedPolygon.RemovePointAtIndex(_selectedIndex.Value);
        }
        else
        {
            selectedPolygon.RemovePointAtIndex(0);
            var new0Position = selectedPolygon.PointAt(0);
            selectedPolygon.SetPointAt(new0Position, selectedPolygon.PointCount - 1);
        }

        // Adjust indices
        if (_selectedIndex >= selectedPolygon.PointCount)
        {
            _selectedIndex--;
        }
        if (_grabbedIndex >= selectedPolygon.PointCount)
        {
            _grabbedIndex--;
        }

        ApplyVertexValues();
        DoEndOfSettingValuesLogic();
        UpdateVisualState();

        return true;
    }

    private int AddPointAt(float x, float y)
    {
        var insertAfterIndex = _addPointSpriteVisual.InsertAfterIndex;
        var newIndex = insertAfterIndex + 1;

        var selectedPoly = SelectedLinePolygon!;
        var newPoint = (selectedPoly.PointAt(newIndex - 1) + selectedPoly.PointAt(newIndex)) / 2.0f;

        selectedPoly.InsertPointAt(newPoint, newIndex);

        Context.GrabbedState.HandlePush();
        _addPointSpriteVisual.IsEnabled = false;
        Context.HasChangedAnythingSinceLastPush = true;

        Context.GuiCommands.RefreshVariables();

        return newIndex;
    }

    private void MoveGrabbedPoint(InputLibrary.Cursor cursor)
    {
        var linePolygon = SelectedLinePolygon;
        if (linePolygon == null || _grabbedIndex == null) return;

        Context.HasChangedAnythingSinceLastPush = true;

        var pointAtIndex = linePolygon.PointAt(_grabbedIndex.Value);
        var zoom = Renderer.Self.Camera.Zoom;

        Matrix.Invert(linePolygon.GetAbsoluteRotationMatrix(), out Matrix rotationMatrix);

        var rightVector = new Vector3(rotationMatrix.M11, rotationMatrix.M12, rotationMatrix.M13);
        var upVector = new Vector3(rotationMatrix.M21, rotationMatrix.M22, rotationMatrix.M23);

        var change = new Vector2(
            cursor.XChange * rightVector.X + cursor.YChange * upVector.X,
            cursor.XChange * rightVector.Y + cursor.YChange * upVector.Y) / zoom;

        pointAtIndex.X += change.X;
        pointAtIndex.Y += change.Y;

        // Round to nearest pixel
        var roundMultiple = 1 / zoom;
        pointAtIndex.X = MathFunctions.RoundFloat(pointAtIndex.X, roundMultiple);
        pointAtIndex.Y = MathFunctions.RoundFloat(pointAtIndex.Y, roundMultiple);

        var shouldSetFirstAndLast = (_grabbedIndex == 0 || _grabbedIndex == linePolygon.PointCount - 1) &&
            linePolygon.PointAt(0) == linePolygon.PointAt(linePolygon.PointCount - 1);

        if (shouldSetFirstAndLast)
        {
            linePolygon.SetPointAt(pointAtIndex, 0);
            linePolygon.SetPointAt(pointAtIndex, linePolygon.PointCount - 1);
        }
        else
        {
            linePolygon.SetPointAt(pointAtIndex, _grabbedIndex.Value);
        }

        Context.GuiCommands.RefreshVariables();
    }

    private void ApplyVertexValues()
    {
        var linePolygon = SelectedLinePolygon;
        if (linePolygon == null) return;

        List<Vector2> vectors = new List<Vector2>(linePolygon.PointCount);

        for (int i = 0; i < linePolygon.PointCount; i++)
        {
            vectors.Add(linePolygon.PointAt(i));
        }

        var variableName = "Points";
        if (Context.SelectedState.SelectedInstance != null)
        {
            variableName = Context.SelectedState.SelectedInstance.Name + "." + variableName;
        }

        var stateSave = Context.SelectedState.SelectedStateSave;
        if (stateSave == null) return;

        var pointsVariableList =
            stateSave.VariableLists.FirstOrDefault(item => item.Name == variableName);

        if (pointsVariableList == null)
        {
            pointsVariableList = new VariableListSave<Vector2>();
            pointsVariableList.Name = variableName;
            pointsVariableList.Type = "Vector2";
            stateSave.VariableLists.Add(pointsVariableList);
        }
        pointsVariableList.ValueAsIList = vectors;
    }

    private void UpdateVisualState()
    {
        _pointNodesVisual.SelectedIndex = _selectedIndex;
        _pointNodesVisual.GrabbedIndex = _grabbedIndex;
        _selectedPointHighlightVisual.SelectedIndex = _selectedIndex;
    }

    private void DoEndOfSettingValuesLogic()
    {
        var selectedElement = Context.SelectedState.SelectedElement;
        var stateSave = Context.SelectedState.SelectedStateSave;
        if (stateSave == null) return;

        Context.FileCommands.TryAutoSaveElement(selectedElement);

        using var undoLock = Context.UndoManager.RequestLock();

        Context.GuiCommands.RefreshVariables();

        // Notify plugins of variable changes
        foreach (var possiblyChangedVariableList in stateSave.VariableLists)
        {
            var oldValue = Context.GrabbedState.StateSave?.GetVariableListSave(possiblyChangedVariableList.Name);
            if (oldValue != possiblyChangedVariableList)
            {
                var instance = selectedElement?.GetInstance(possiblyChangedVariableList.SourceObject);
                Gum.Plugins.PluginManager.Self.VariableSet(selectedElement, instance, 
                    possiblyChangedVariableList.GetRootName(), oldValue);
            }
        }

        Context.HasChangedAnythingSinceLastPush = false;
    }
}
