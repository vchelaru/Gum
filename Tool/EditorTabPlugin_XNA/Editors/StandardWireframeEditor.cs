using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Input;
using Gum.Managers;
using Gum.PropertyGridHelpers;
using Gum.ToolStates;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;
using RenderingLibrary.Math.Geometry;
using System.Collections.Generic;
using System.Linq;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Vector2 = System.Numerics.Vector2;
using Color = System.Drawing.Color;
using Matrix = System.Numerics.Matrix4x4;
using System.Windows.Input;
using System;
using Gum.ToolCommands;
using EditorTabPlugin_XNA.ExtensionMethods;
using Gum.Services;
using Gum.Commands;
using Gum.Undo;
using Gum.Wireframe.Editors.Handlers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Wireframe.Editors.Visuals;

namespace Gum.Wireframe.Editors;

/// <summary>
/// Editor which includes ability to move, resize, and rotate an object.
/// </summary>
public class StandardWireframeEditor : WireframeEditor
{
    #region Fields/Properties

    ResizeHandlesVisual _resizeHandlesVisual;
    ResizeInputHandler _resizeInputHandler;
    RotationInputHandler _rotationInputHandler;
    RotationHandleVisual _rotationHandleVisual;

    List<GraphicalUiElement> selectedObjects =
        new List<GraphicalUiElement>();

    DimensionDisplayVisual widthDimensionDisplay;
    DimensionDisplayVisual heightDimensionDisplay;

    private readonly IElementCommands _elementCommands;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly SelectionManager _selectionManager;
    private readonly IVariableInCategoryPropagationLogic _variableInCategoryPropagationLogic;

    public InputLibrary.Cursor Cursor
    {
        get
        {
            return InputLibrary.Cursor.Self;
        }
    }

    public override bool HasCursorOverHandles
    {
        get
        {
            var cursor = InputLibrary.Cursor.Self;
            float worldX = cursor.GetWorldX();
            float worldY = cursor.GetWorldY();

            if (_resizeInputHandler.HasCursorOver(worldX, worldY))
            {
                return true;
            }
            else if(_rotationInputHandler.HasCursorOver(worldX, worldY))
            {
                return true;
            }
            return false;
        }
    }

    #endregion

    public StandardWireframeEditor(Layer layer,
        Color lineColor,
        Color textColor,
        global::Gum.Managers.IHotkeyManager hotkeyManager,
        SelectionManager selectionManager,
        ISelectedState selectedState,
        IVariableInCategoryPropagationLogic variableInCategoryPropagationLogic,
        IWireframeObjectManager wireframeObjectManager)
        : base(
              hotkeyManager,
              selectionManager,
              selectedState,
              layer,
              lineColor,
              textColor)
    {
        _elementCommands = Locator.GetRequiredService<IElementCommands>();
        _wireframeObjectManager = wireframeObjectManager;
        _selectionManager = selectionManager;
        _variableInCategoryPropagationLogic = variableInCategoryPropagationLogic;

        _resizeHandlesVisual = new ResizeHandlesVisual(_context, lineColor);
        _resizeHandlesVisual.ShowOrigin = true;

        _resizeInputHandler = new ResizeInputHandler(_context, _resizeHandlesVisual);

        _rotationHandleVisual = new RotationHandleVisual(_context, Color.Yellow);
        _rotationInputHandler = new RotationInputHandler(_context, _rotationHandleVisual);

        widthDimensionDisplay = new DimensionDisplayVisual(_context, WidthOrHeight.Width, _resizeInputHandler);
        heightDimensionDisplay = new DimensionDisplayVisual(_context, WidthOrHeight.Height, _resizeInputHandler);

        // Register handlers and visuals with base class
        // Handlers will be checked in priority order (Rotation=100, Resize=90, Move=80)
        _inputHandlers.Add(_rotationInputHandler);
        _inputHandlers.Add(_resizeInputHandler);
        _inputHandlers.Add(_moveInputHandler); // From base class

        _visuals.Add(_resizeHandlesVisual);
        _visuals.Add(_rotationHandleVisual);
        _visuals.Add(widthDimensionDisplay);
        _visuals.Add(heightDimensionDisplay);
    }

    #region Activity

    protected override bool ShouldProcessActivity(ICollection<GraphicalUiElement> selectedObjects)
    {
        return selectedObjects.Count != 0
            && _context.SelectedState.SelectedStateSave != null
            && _context.SelectedState.CustomCurrentStateSave == null;
    }

    protected override void OnActivityComplete(ICollection<GraphicalUiElement> selectedObjects)
    {
        bool shouldSkip = selectedObjects.Any(item => item.Tag is ScreenSave);

        if (!shouldSkip)
        {
            UpdateLockedVariables(selectedObjects);
        }
    }

    private void UpdateLockedVariables(ICollection<GraphicalUiElement> selectedObjects)
    {
        var item = selectedObjects.FirstOrDefault();

        _context.IsXMovementEnabled = true;
        _context.IsYMovementEnabled = true;
        _context.IsWidthChangeEnabled = true;
        _context.IsHeightChangeEnabled = true;


        if (item == null) return;

        var tag = item.Tag;

        RecursiveVariableFinder? rfv = null;

        if (tag is InstanceSave instance)
        {
            rfv = new RecursiveVariableFinder(instance, instance.ParentContainer);
        }
        if(tag is ElementSave element)
        {
            rfv = new RecursiveVariableFinder(_context.SelectedState.SelectedStateSave);
        }

        var variableReferences = rfv?.GetVariableList("VariableReferences");

        if(variableReferences != null)
        {
            var list = variableReferences.ValueAsIList;

            foreach (string variableReference in list)
            {
                var split = variableReference.Split('=');

                if(split.Length == 2)
                {
                    var variable = split[0].Trim();

                    if(variable == "X")
                    {
                        _context.IsXMovementEnabled = false;
                    }
                    if(variable == "Y")
                    {
                        _context.IsYMovementEnabled = false;

                    }
                    if (variable == "Width")
                    {
                        _context.IsWidthChangeEnabled = false;
                    }
                    if (variable == "Height")
                    {
                        _context.IsHeightChangeEnabled = false;
                    }
                }
            }
        }
    }


    #endregion

    #region Update To

    public override void UpdateToSelection(ICollection<GraphicalUiElement> selectedObjects)
    {
        this.selectedObjects.Clear();
        this.selectedObjects.AddRange(selectedObjects);

        // Base class handles updating context, visuals, and handlers
        base.UpdateToSelection(selectedObjects);
    }

    #endregion



}
