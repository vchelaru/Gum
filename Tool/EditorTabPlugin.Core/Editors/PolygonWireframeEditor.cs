using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Commands;
using Gum.Input;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe.Editors.Handlers;
using Gum.Wireframe.Editors.Visuals;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System.Collections.Generic;
using System.Linq;
using Vector2 = System.Numerics.Vector2;

namespace Gum.Wireframe.Editors;

public class PolygonWireframeEditor : WireframeEditor
{
    #region Fields/Properties

    // Visual components
    private readonly PolygonPointNodesVisual _pointNodesVisual;
    private readonly AddPointSpriteVisual _addPointSpriteVisual;
    private readonly SelectedPointHighlightVisual _selectedPointHighlightVisual;
    private readonly OriginDisplayVisual _originDisplayVisual;
    
    private readonly PolygonScaleHandlesVisual _scaleHandlesVisual;

    // Input handlers
    private readonly PolygonPointInputHandler _pointInputHandler;
    private readonly PolygonScaleInputHandler _scaleInputHandler;

    Layer layer;

    List<GraphicalUiElement> selectedPolygons = new List<GraphicalUiElement>();
    LinePolygon? SelectedLinePolygon => selectedPolygons.FirstOrDefault()?.RenderableComponent as LinePolygon;

    public override bool HasCursorOverHandles
    {
        get
        {
            var cursor = InputLibrary.Cursor.Self;
            var x = cursor.GetWorldX();
            var y = cursor.GetWorldY();

            // Check if handler has cursor over (points, add point sprite)
            if (_pointInputHandler.HasCursorOver(x, y) || _scaleInputHandler.HasCursorOver(x, y))
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
        IHotkeyManager hotkeyManager,
        ISelectionManager selectionManager,
        ISelectedState selectedState,
        IElementCommands elementCommands,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        ISetVariableLogic setVariableLogic,
        IUndoManager undoManager,
        IVariableInCategoryPropagationLogic variableInCategoryPropagationLogic,
        IWireframeObjectManager wireframeObjectManager,
        IUiSettingsService uiSettingsService,
        Camera camera,
        IGumCursorState cursor,
        IPluginManager pluginManager,
        ICanvasDisplayScale displayScale)
        : base(
              hotkeyManager,
              selectionManager,
              selectedState,
              elementCommands,
              guiCommands,
              fileCommands,
              setVariableLogic,
              undoManager,
              variableInCategoryPropagationLogic,
              wireframeObjectManager,
              uiSettingsService,
              layer,
              System.Drawing.Color.White,
              System.Drawing.Color.White,
              camera,
              cursor,
              pluginManager,
              displayScale)
    {
        this.layer = layer;

        // Create visual components (using inherited _context from base class)
        _pointNodesVisual = new PolygonPointNodesVisual(_context, layer);
        _addPointSpriteVisual = new AddPointSpriteVisual(_context, layer);
        _selectedPointHighlightVisual = new SelectedPointHighlightVisual(_context, layer);
        _originDisplayVisual = new OriginDisplayVisual(_context);
        _scaleHandlesVisual = new PolygonScaleHandlesVisual(_context, System.Drawing.Color.White);

        // Create input handlers (use the visual components)
        _pointInputHandler = new PolygonPointInputHandler(
            _context,
            _pointNodesVisual,
            _addPointSpriteVisual,
            _selectedPointHighlightVisual);
        _scaleInputHandler = new PolygonScaleInputHandler(_context, _scaleHandlesVisual,
            (worldX, worldY) => _pointNodesVisual.GetIndexOver(worldX, worldY) != null);

        // Register handlers and visuals with base class
        // Handlers will be checked in priority order (PolygonScale=96, PolygonPoint=95, Move=80)
        _inputHandlers.Add(_pointInputHandler);
        _inputHandlers.Add(_scaleInputHandler);
        _inputHandlers.Add(_moveInputHandler); // From base class

        _visuals.Add(_scaleHandlesVisual);
        _visuals.Add(_pointNodesVisual);
        _visuals.Add(_addPointSpriteVisual);
        _visuals.Add(_selectedPointHighlightVisual);
        _visuals.Add(_originDisplayVisual);
    }

    public override void UpdateHover(float worldX, float worldY)
    {
        base.UpdateHover(worldX, worldY);

        // The add-point marker sits on edge midpoints, where the scale handles also reach.
        if (_scaleInputHandler.HasCursorOver(worldX, worldY))
        {
            _addPointSpriteVisual.IsEnabled = false;
        }
    }

    public override void UpdateToSelection(ICollection<GraphicalUiElement> selectedObjects)
    {
        selectedPolygons.Clear();
        selectedPolygons.AddRange(selectedObjects);

        // Base class handles updating context, visuals, and handlers
        base.UpdateToSelection(selectedObjects);
    }

    #endregion

    #region Activity Functions

    // Note: Activity is now handled by base class which iterates through registered handlers and visuals

    #endregion
}
