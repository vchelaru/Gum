using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins;
using Gum.ToolStates;
using Gum.Undo;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System.Collections.Generic;
using System.Linq;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Vector2 = System.Numerics.Vector2;
using Matrix = System.Numerics.Matrix4x4;
using Gum.Managers;
using System.Collections;
using System;
using Gum.PropertyGridHelpers;
using EditorTabPlugin_XNA.ExtensionMethods;
using Gum.Commands;
using Gum.Services;
using Gum.ToolCommands;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Wireframe.Editors;
using Gum.Wireframe.Editors.Handlers;
using Gum.Wireframe.Editors.Visuals;
using Gum.Input;
using Color = System.Drawing.Color;

namespace Gum.Wireframe;

public abstract class WireframeEditor
{
    #region Fields/Properties

    // Shared context and move handler for all wireframe editors
    protected readonly EditorContext _context;
    protected readonly MoveInputHandler _moveInputHandler;

    // Collections for handlers and visuals to enable unified Activity loop
    protected readonly List<IInputHandler> _inputHandlers = new List<IInputHandler>();
    protected readonly List<IEditorVisual> _visuals = new List<IEditorVisual>();

    public bool RestrictToUnitValues
    {
        get => _context.RestrictToUnitValues;
        set => _context.RestrictToUnitValues = value;
    }

    #endregion

    public WireframeEditor(
        global::Gum.Managers.IHotkeyManager hotkeyManager,
        SelectionManager selectionManager,
        ISelectedState selectedState,
        IElementCommands elementCommands,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        ISetVariableLogic setVariableLogic,
        IUndoManager undoManager,
        IVariableInCategoryPropagationLogic variableInCategoryPropagationLogic,
        IWireframeObjectManager wireframeObjectManager,
        IUiSettingsService uiSettingsService,
        Layer layer,
        Color lineColor,
        Color textColor)
    {
        // Create shared EditorContext and MoveInputHandler
        _context = new EditorContext(
            selectedState,
            selectionManager,
            elementCommands,
            guiCommands,
            fileCommands,
            setVariableLogic,
            undoManager,
            variableInCategoryPropagationLogic,
            hotkeyManager,
            wireframeObjectManager,
            uiSettingsService,
            layer,
            lineColor,
            textColor);

        _moveInputHandler = new MoveInputHandler(_context);
    }

    /// <summary>
    /// Updates all visuals and handlers to reflect the current selection.
    /// Override to add custom selection handling, but call base.UpdateToSelection() to ensure
    /// visuals and handlers are updated.
    /// </summary>
    public virtual void UpdateToSelection(ICollection<GraphicalUiElement> selectedObjects)
    {
        // Update context's selected objects
        _context.SelectedObjects.Clear();
        _context.SelectedObjects.AddRange(selectedObjects);

        // Update all visuals
        foreach (var visual in _visuals)
        {
            visual.UpdateToSelection(selectedObjects);
        }

        // Notify all handlers of selection change
        foreach (var handler in _inputHandlers)
        {
            handler.OnSelectionChanged();
        }
    }

    public abstract bool HasCursorOverHandles { get; }

    /// <summary>
    /// Returns true if any input handler (resize, rotate, polygon points, etc.) is currently active/dragging.
    /// </summary>
    public bool IsAnyHandlerActive => _inputHandlers.Any(h => h.IsActive);

    public void UpdateAspectRatioForGrabbedIpso()
    {
        _context.UpdateAspectRatioForGrabbedIpso();
    }

    /// <summary>
    /// Updates hover state for all handlers.
    /// This should be called EVERY frame to show hover highlights correctly.
    /// </summary>
    public void UpdateHover(float worldX, float worldY)
    {
        foreach (var handler in _inputHandlers)
        {
            handler.UpdateHover(worldX, worldY);
        }
    }

    /// <summary>
    /// Processes input for handles (resize, rotate, polygon points).
    /// This is called DIRECTLY from SelectionManager, not in a separate Activity method.
    /// This ensures input decisions are made in the same frame.
    /// NOTE: Hover updates are called separately in UpdateHover() every frame.
    /// </summary>
    public void ProcessHandleInput(InputLibrary.Cursor cursor, float worldX, float worldY)
    {
        // NOTE: Hover state is updated separately in UpdateHover() which is called every frame
        // We don't update hover here to avoid duplicate updates

        // Process input based on cursor state
        if (cursor.PrimaryPush)
        {
            _context.HasChangedAnythingSinceLastPush = false;
            _context.GrabbedState.HandlePush();

            // Try handlers in priority order
            foreach (var handler in _inputHandlers.OrderByDescending(h => h.Priority))
            {
                if (handler.HandlePush(worldX, worldY))
                {
                    break; // Handler claimed the input
                }
            }
        }
        else if (cursor.PrimaryDown && _context.GrabbedState.HasMovedEnough)
        {
            var activeHandler = _inputHandlers.FirstOrDefault(h => h.IsActive);
            activeHandler?.HandleDrag();
        }
        else if (cursor.PrimaryClick)
        {
            var activeHandler = _inputHandlers.FirstOrDefault(h => h.IsActive);
            activeHandler?.HandleRelease();
        }
    }

    /// <summary>
    /// Main activity loop that processes input through registered handlers and updates visuals.
    /// Derived classes can customize behavior by overriding ShouldProcessActivity and OnActivityComplete.
    /// DEPRECATED: Use ProcessHandleInput() for input processing and UpdateVisuals() for visual updates.
    /// </summary>
    public virtual void Activity(ICollection<GraphicalUiElement> selectedObjects, SystemManagers systemManagers)
    {
        if (!ShouldProcessActivity(selectedObjects))
        {
            return;
        }

        var cursor = InputLibrary.Cursor.Self;
        var worldX = cursor.GetWorldX();
        var worldY = cursor.GetWorldY();

        // Update hover state on all handlers
        foreach (var handler in _inputHandlers)
        {
            handler.UpdateHover(worldX, worldY);
        }

        // Handle push - try handlers in priority order until one claims the input
        if (cursor.PrimaryPush)
        {
            _context.HasChangedAnythingSinceLastPush = false;
            _context.GrabbedState.HandlePush();

            foreach (var handler in _inputHandlers.OrderByDescending(h => h.Priority))
            {
                if (handler.HandlePush(worldX, worldY))
                {
                    break; // Handler claimed the input
                }
            }
        }

        // Handle drag - only call on active handler
        if (cursor.PrimaryDown && _context.GrabbedState.HasMovedEnough)
        {
            var activeHandler = _inputHandlers.FirstOrDefault(h => h.IsActive);
            activeHandler?.HandleDrag();
        }

        // Handle release - call on active handler
        if (cursor.PrimaryClick)
        {
            var activeHandler = _inputHandlers.FirstOrDefault(h => h.IsActive);
            activeHandler?.HandleRelease();
        }

        // Update all visuals
        foreach (var visual in _visuals)
        {
            visual.Update();
        }

        // Allow derived classes to do custom post-processing
        OnActivityComplete(selectedObjects);
    }

    /// <summary>
    /// Updates visual state only. Called separately from input processing.
    /// This is the new preferred way to update visuals.
    /// </summary>
    public virtual void UpdateVisuals(ICollection<GraphicalUiElement> selectedObjects)
    {
        if (selectedObjects.Count == 0)
            return;

        // Update all visuals
        foreach (var visual in _visuals)
        {
            visual.Update();
        }

        // Allow derived classes to do custom post-processing
        OnActivityComplete(selectedObjects);
    }

    /// <summary>
    /// Determines whether activity processing should continue for the current frame.
    /// Override to add custom conditions (e.g., checking selection state).
    /// </summary>
    protected virtual bool ShouldProcessActivity(ICollection<GraphicalUiElement> selectedObjects)
    {
        return selectedObjects.Count != 0;
    }

    /// <summary>
    /// Called at the end of Activity after all handlers and visuals have been updated.
    /// Override to add custom post-processing logic.
    /// </summary>
    protected virtual void OnActivityComplete(ICollection<GraphicalUiElement> selectedObjects)
    {
    }

    /// <summary>
    /// Gets the Windows Forms cursor to display based on current handler states.
    /// Iterates through handlers by priority to find the first one that wants to change the cursor.
    /// </summary>
    public virtual System.Windows.Forms.Cursor GetWindowsCursorToShow(
        System.Windows.Forms.Cursor defaultCursor, float worldXAt, float worldYAt)
    {
        foreach (var handler in _inputHandlers.OrderByDescending(h => h.Priority))
        {
            var cursor = handler.GetCursorToShow(worldXAt, worldYAt);
            if (cursor != null) return cursor;
        }
        return defaultCursor;
    }

    /// <summary>
    /// Destroys all registered visuals and handlers.
    /// Override to add custom cleanup, but call base.Destroy() to ensure visuals are cleaned up.
    /// </summary>
    public virtual void Destroy()
    {
        foreach (var visual in _visuals)
        {
            visual.Destroy();
        }
    }

    public virtual bool TryHandleDelete()
    {
        foreach (var handler in _inputHandlers.OrderByDescending(h => h.Priority))
        {
            if (handler.TryHandleDelete())
            {
                return true;
            }
        }
        return false;
    }
}
