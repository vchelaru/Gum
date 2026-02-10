using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System.Collections.Generic;
using Color = System.Drawing.Color;

namespace Gum.Wireframe.Editors;

/// <summary>
/// Provides shared context and dependencies for input handlers and visual components.
/// </summary>
public class EditorContext
{
    #region Dependencies (Injected)

    public ISelectedState SelectedState { get; }
    public SelectionManager SelectionManager { get; }
    public IElementCommands ElementCommands { get; }
    public IGuiCommands GuiCommands { get; }
    public IFileCommands FileCommands { get; }
    public ISetVariableLogic SetVariableLogic { get; }
    public IUndoManager UndoManager { get; }
    public IVariableInCategoryPropagationLogic VariablePropagationLogic { get; }
    public HotkeyManager HotkeyManager { get; }
    public IWireframeObjectManager WireframeObjectManager { get; }
    public Layer OverlayLayer { get; }

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
    public bool RestrictToUnitValues { get; set; }

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
        SelectionManager selectionManager,
        IElementCommands elementCommands,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        ISetVariableLogic setVariableLogic,
        IUndoManager undoManager,
        IVariableInCategoryPropagationLogic variablePropagationLogic,
        HotkeyManager hotkeyManager,
        IWireframeObjectManager wireframeObjectManager,
        Layer overlayLayer,
        GrabbedState grabbedState,
        Color lineColor,
        Color textColor)
    {
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
        GrabbedState = grabbedState;
        LineColor = lineColor;
        TextColor = textColor;
    }

    #region Helper Methods

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
            float width = ipso.GetAbsoluteWidth();
            float height = ipso.GetAbsoluteHeight();

            if (height != 0)
            {
                AspectRatioOnGrab = width / height;
            }
        }
    }

    #endregion
}
