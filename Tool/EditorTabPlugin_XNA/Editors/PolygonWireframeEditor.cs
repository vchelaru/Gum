using Gum.Input;
using Gum.ToolStates;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Vector2 = System.Numerics.Vector2;
using Gum.Managers;
using Gum.Wireframe.Editors.Visuals;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.Wireframe.Editors.Handlers;

namespace Gum.Wireframe.Editors;

public class PolygonWireframeEditor : WireframeEditor
{
    #region Fields/Properties

    // Visual components
    private readonly PolygonPointNodesVisual _pointNodesVisual;
    private readonly AddPointSpriteVisual _addPointSpriteVisual;
    private readonly SelectedPointHighlightVisual _selectedPointHighlightVisual;
    private readonly OriginDisplayVisual _originDisplayVisual;
    
    // Input handler
    private readonly PolygonPointInputHandler _pointInputHandler;

    Layer layer;

    List<GraphicalUiElement> selectedPolygons = new List<GraphicalUiElement>();
    LinePolygon SelectedLinePolygon => selectedPolygons.FirstOrDefault()?.RenderableComponent as LinePolygon;

    public override bool HasCursorOverHandles
    {
        get
        {
            var cursor = InputLibrary.Cursor.Self;
            var x = cursor.GetWorldX();
            var y = cursor.GetWorldY();

            // Check if handler has cursor over (points, add point sprite)
            if (_pointInputHandler.HasCursorOver(x, y))
            {
                return true;
            }

            return false;
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
              selectedState,
              layer,
              System.Drawing.Color.White,
              System.Drawing.Color.White)
    {
        this.layer = layer;

        // Create visual components (using inherited _context from base class)
        _pointNodesVisual = new PolygonPointNodesVisual(_context, layer);
        _addPointSpriteVisual = new AddPointSpriteVisual(_context, layer);
        _selectedPointHighlightVisual = new SelectedPointHighlightVisual(_context, layer);
        _originDisplayVisual = new OriginDisplayVisual(_context);

        // Create input handler (uses the visual components)
        _pointInputHandler = new PolygonPointInputHandler(
            _context,
            _pointNodesVisual,
            _addPointSpriteVisual,
            _selectedPointHighlightVisual);
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

        // Notify handler of selection change
        _pointInputHandler.OnSelectionChanged();
    }

    #endregion

    #region Activity Functions

    public override void Activity(ICollection<GraphicalUiElement> selectedObjects, SystemManagers systemManagers)
    {
        if (selectedObjects.Count != 0)
        {
            var cursor = InputLibrary.Cursor.Self;
            var x = cursor.GetWorldX();
            var y = cursor.GetWorldY();

            // Handle input through the handler
            if (cursor.PrimaryPush)
            {
                // First let the point handler try to handle the push
                bool handledByPointHandler = _pointInputHandler.HandlePush(x, y);
                
                if (!handledByPointHandler)
                {
                    // If not handled by point handler, check for body grab
                    if (IsOverPolygonBody(x, y))
                    {
                        _context.HasChangedAnythingSinceLastPush = false;
                        _context.GrabbedState.HandlePush();
                        _moveInputHandler.HandlePush(x, y);
                    }
                }
            }

            // Handle drag
            if (cursor.PrimaryDown)
            {
                if (_pointInputHandler.IsActive)
                {
                    _pointInputHandler.HandleDrag();
                }
                else if (_context.GrabbedState.HasMovedEnough)
                {
                    // Body dragging - use base class MoveInputHandler
                    _moveInputHandler.HandleDrag();
                }
            }

            // Handle release
            if (cursor.PrimaryClick)
            {
                if (_pointInputHandler.IsActive)
                {
                    _pointInputHandler.HandleRelease();
                }
                else
                {
                    // Let MoveInputHandler handle the release (axis lock, snapping, etc.)
                    _moveInputHandler.HandleRelease();
                }
            }

            // Update hover state
            _pointInputHandler.UpdateHover(x, y);

            // Update all visual components
            _pointNodesVisual.Update();
            _addPointSpriteVisual.Update();
            _selectedPointHighlightVisual.Update();
            _originDisplayVisual.Update();
        }
    }

    private bool IsOverPolygonBody(float x, float y)
    {
        foreach (var gue in selectedPolygons)
        {
            var polygon = gue.RenderableComponent as LinePolygon;
            if (polygon.IsPointInside(x, y))
            {
                return true;
            }
        }
        return false;
    }

    #endregion

    public override bool TryHandleDelete()
    {
        return _pointInputHandler.TryHandleDelete();
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
        // First check if handler has a cursor to show
        var handlerCursor = _pointInputHandler.GetCursorToShow(worldXAt, worldYAt);
        if (handlerCursor != null)
        {
            return handlerCursor;
        }

        // Check if over polygon body
        if (IsOverPolygonBody(worldXAt, worldYAt))
        {
            return System.Windows.Forms.Cursors.SizeAll;
        }

        return defaultCursor;
    }

    #endregion
}
