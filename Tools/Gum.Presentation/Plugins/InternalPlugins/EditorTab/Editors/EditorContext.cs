using System.Collections.Generic;
using System.Linq;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Input;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Color = System.Drawing.Color;

namespace Gum.Wireframe.Editors;

/// <summary>
/// Provides shared context and dependencies for input handlers and visual components.
/// </summary>
public class EditorContext
{
    #region Dependencies (Injected)

    public ISelectedState SelectedState { get; }
    public ISelectionManager SelectionManager { get; }
    public IElementCommands ElementCommands { get; }
    public IGuiCommands GuiCommands { get; }
    public IFileCommands FileCommands { get; }
    public ISetVariableLogic SetVariableLogic { get; }
    public IUndoManager UndoManager { get; }
    public IVariableInCategoryPropagationLogic VariablePropagationLogic { get; }
    public IHotkeyManager HotkeyManager { get; }
    public IWireframeObjectManager WireframeObjectManager { get; }
    public Layer OverlayLayer { get; }
    public IUiSettingsService UiSettingsService { get; }
    public IPluginManager PluginManager { get; }

    /// <summary>
    /// The live editor camera. Injected directly (rather than read through the XNALIKE-only
    /// <c>Renderer.Self</c>/<c>SystemManagers.Default</c> singletons, which aren't reachable from
    /// this headless assembly) so zoom/world-coordinate math here works the same as before the
    /// move — same object at runtime, just obtained explicitly.
    /// </summary>
    public Camera Camera { get; }

    /// <summary>
    /// Live cursor state, injected the same way as <see cref="Camera"/> — the concrete
    /// <c>InputLibrary.Cursor</c> singleton can't be referenced from this headless assembly (its
    /// project targets <c>net8.0-windows7.0</c>, which fails a <c>Gum.Presentation</c> project
    /// reference at restore). Same object at runtime, just obtained explicitly.
    /// </summary>
    public IGumCursorState Cursor { get; }

    /// <summary>
    /// The OS display scale, which overlay visuals multiply their sizes by. See
    /// <see cref="ToWorldOverlaySize"/>.
    /// </summary>
    public ICanvasDisplayScale DisplayScale { get; }

    #endregion

    #region State

    /// <summary>
    /// Tracks the state when an object was grabbed (mouse down).
    /// </summary>
    public GrabbedState GrabbedState { get; }

    /// <summary>
    /// The currently selected GraphicalUiElements.
    /// </summary>
    public List<GraphicalUiElement> SelectedObjects { get; } = new();

    #endregion

    #region Settings

    public Color LineColor { get; }
    public Color TextColor { get; }

    public bool IsXMovementEnabled { get; set; } = true;
    public bool IsYMovementEnabled { get; set; } = true;
    public bool IsWidthChangeEnabled { get; set; } = true;
    public bool IsHeightChangeEnabled { get; set; } = true;
    public bool IsRotationEnabled { get; set; } = true;
    public bool RestrictToUnitValues { get; set; }

    /// <summary>
    /// Whether move/resize dragging snaps pixel-unit objects to the grid.
    /// </summary>
    public bool SnapToGrid { get; set; }

    /// <summary>
    /// The size, in pixels, of each grid cell used by <see cref="SnapToGrid"/>.
    /// </summary>
    public int GridSize { get; set; } = 16;

    #endregion

    #region Editing State

    /// <summary>
    /// Whether any changes have been made since the last push (mouse down).
    /// Used to determine if we need to record undo and notify plugins.
    /// </summary>
    public bool HasChangedAnythingSinceLastPush { get; set; }

    /// <summary>
    /// The aspect ratio of the selected object when grabbed.
    /// Used for aspect-ratio-locked resizing.
    /// </summary>
    public float AspectRatioOnGrab { get; set; }

    #endregion

    public EditorContext(
        ISelectedState selectedState,
        ISelectionManager selectionManager,
        IElementCommands elementCommands,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        ISetVariableLogic setVariableLogic,
        IUndoManager undoManager,
        IVariableInCategoryPropagationLogic variablePropagationLogic,
        IHotkeyManager hotkeyManager,
        IWireframeObjectManager wireframeObjectManager,
        IUiSettingsService uiSettingsService,
        Layer overlayLayer,
        Color lineColor,
        Color textColor,
        Camera camera,
        IGumCursorState cursor,
        IPluginManager pluginManager,
        ICanvasDisplayScale displayScale)
    {
        DisplayScale = displayScale;
        UiSettingsService = uiSettingsService;
        Camera = camera;
        Cursor = cursor;
        PluginManager = pluginManager;
        SelectedState = selectedState;
        SelectionManager = selectionManager;
        ElementCommands = elementCommands;
        GuiCommands = guiCommands;
        FileCommands = fileCommands;
        SetVariableLogic = setVariableLogic;
        UndoManager = undoManager;
        VariablePropagationLogic = variablePropagationLogic;
        HotkeyManager = hotkeyManager;
        WireframeObjectManager = wireframeObjectManager;
        OverlayLayer = overlayLayer;
        GrabbedState = new GrabbedState(selectedState, wireframeObjectManager, cursor);
        LineColor = lineColor;
        TextColor = textColor;
    }

    #region Helper Methods

    /// <summary>
    /// Converts an overlay size (a handle's width, a font scale, a padding) from device-independent
    /// pixels to world units: multiplied by the display scale, divided by the camera zoom. Use it
    /// for how the overlay looks, never for positions or measured values.
    /// </summary>
    public float ToWorldOverlaySize(float overlaySize) => DisplayScale.ToWorld(overlaySize, Camera.Zoom);

    /// <summary>
    /// Returns true if the currently selected instance is locked and should not be
    /// editable in the editor (no handles, no drag/resize/rotate/nudge).
    /// </summary>
    public bool IsSelectionLocked() => SelectedState.SelectedInstance?.Locked == true;

    /// <summary>
    /// Updates the aspect ratio based on the currently selected object.
    /// Call this on push when resizing might occur.
    /// </summary>
    public void UpdateAspectRatioForGrabbedIpso()
    {
        if (SelectedState.SelectedInstance != null &&
            SelectedState.SelectedIpso != null)
        {
            var ipso = (GraphicalUiElement)SelectedState.SelectedIpso;
            float width = ipso.AbsoluteWidth;
            float height = ipso.AbsoluteHeight;

            if (height != 0)
            {
                AspectRatioOnGrab = width / height;
            }
        }
    }

    /// <summary>
    /// Performs end-of-editing logic: saves the element, refreshes UI, and notifies plugins of changes.
    /// Call this after completing a drag/resize/rotate operation.
    /// </summary>
    public void DoEndOfSettingValuesLogic()
    {
        var selectedElement = SelectedState.SelectedElement;
        var stateSave = SelectedState.SelectedStateSave;
        if (stateSave == null)
        {
            throw new System.InvalidOperationException("The SelectedStateSave is null, this should not happen");
        }
        if (selectedElement == null)
        {
            throw new System.InvalidOperationException("The SelectedElement is null, this should not happen");
        }
        // The push that started this edit recorded the state, since edits only start with a state selected.
        var grabbedStateSave = GrabbedState.StateSave ??
            throw new System.InvalidOperationException("The GrabbedState has no StateSave, this should not happen");

        FileCommands.TryAutoSaveElement(selectedElement);

        using var undoLock = UndoManager.RequestLock();

        GuiCommands.RefreshVariableValues();

        var element = selectedElement;

        foreach (var possiblyChangedVariable in stateSave.Variables.ToList())
        {
            var oldValue = grabbedStateSave.GetValue(possiblyChangedVariable.Name);

            if (StateValueComparer.DoValuesDiffer(oldValue, stateSave.GetValue(possiblyChangedVariable.Name)))
            {
                var instance = element.GetInstance(possiblyChangedVariable.SourceObject);

                SetVariableLogic.PropertyValueChanged(possiblyChangedVariable.GetRootName(),
                   oldValue,
                   instance,
                   element.DefaultState,
                   refresh: true,
                   recordUndo: false,
                   trySave: false);
            }
        }

        foreach (var possiblyChangedVariableList in stateSave.VariableLists)
        {
            var oldValue = grabbedStateSave.GetVariableListSave(possiblyChangedVariableList.Name);

            if (StateValueComparer.DoValuesDiffer(oldValue?.ValueAsIList, possiblyChangedVariableList.ValueAsIList))
            {
                var instance = element.GetInstance(possiblyChangedVariableList.SourceObject);
                PluginManager.VariableSet(element, instance, possiblyChangedVariableList.GetRootName(), oldValue);
            }
        }

        HasChangedAnythingSinceLastPush = false;
    }

    #endregion
}
