using Gum.Commands;
using Gum.Input;
using Gum.Managers;
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
using System.Windows.Forms;
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
        IHotkeyManager hotkeyManager,
        SelectionManager selectionManager,
        ISelectedState selectedState,
        IElementCommands elementCommands,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        ISetVariableLogic setVariableLogic,
        IUndoManager undoManager,
        IVariableInCategoryPropagationLogic variableInCategoryPropagationLogic,
        IWireframeObjectManager wireframeObjectManager,
        IUiSettingsService uiSettingsService)
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

        // Register handlers and visuals with base class
        // Handlers will be checked in priority order (PolygonPoint=95, Move=80)
        _inputHandlers.Add(_pointInputHandler);
        _inputHandlers.Add(_moveInputHandler); // From base class

        _visuals.Add(_pointNodesVisual);
        _visuals.Add(_addPointSpriteVisual);
        _visuals.Add(_selectedPointHighlightVisual);
        _visuals.Add(_originDisplayVisual);
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
