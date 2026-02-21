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
using System.Linq;
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
    public IHotkeyManager HotkeyManager { get; }
    public IWireframeObjectManager WireframeObjectManager { get; }
    public Layer OverlayLayer { get; }
    public IUiSettingsService UiSettingsService { get; }

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
        IHotkeyManager hotkeyManager,
        IWireframeObjectManager wireframeObjectManager,
        IUiSettingsService uiSettingsService,
        Layer overlayLayer,
        Color lineColor,
        Color textColor)
    {
        UiSettingsService = uiSettingsService;
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
        GrabbedState = new GrabbedState(selectedState, wireframeObjectManager);
        LineColor = lineColor;
        TextColor = textColor;
    }

    #region Helper Methods

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
            float width = ipso.GetAbsoluteWidth();
            float height = ipso.GetAbsoluteHeight();

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

        FileCommands.TryAutoSaveElement(selectedElement);

        using var undoLock = UndoManager.RequestLock();

        GuiCommands.RefreshVariableValues();

        var element = SelectedState.SelectedElement;

        foreach (var possiblyChangedVariable in stateSave.Variables.ToList())
        {
            var oldValue = GrabbedState.StateSave.GetValue(possiblyChangedVariable.Name);

            if (DoValuesDiffer(stateSave, possiblyChangedVariable.Name, oldValue))
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
            var oldValue = GrabbedState.StateSave.GetVariableListSave(possiblyChangedVariableList.Name);

            if (DoValuesDiffer(stateSave, possiblyChangedVariableList.Name, oldValue))
            {
                var instance = element.GetInstance(possiblyChangedVariableList.SourceObject);
                Gum.Plugins.PluginManager.Self.VariableSet(element, instance, possiblyChangedVariableList.GetRootName(), oldValue);
            }
        }

        HasChangedAnythingSinceLastPush = false;
    }

    private bool DoValuesDiffer(DataTypes.Variables.StateSave newStateSave, string variableName, object oldValue)
    {
        var newValue = newStateSave.GetValue(variableName);
        if (newValue == null && oldValue != null)
        {
            return true;
        }
        if (newValue != null && oldValue == null)
        {
            return true;
        }
        if (newValue == null && oldValue == null)
        {
            return false;
        }
        // neither are null
        else
        {
            if (oldValue is float oldFloat)
            {
                var newFloat = (float)newValue;
                return oldFloat != newFloat;
            }
            else if (oldValue is string)
            {
                return (string)oldValue != (string)newValue;
            }
            else if (oldValue is bool)
            {
                return (bool)oldValue != (bool)newValue;
            }
            else if (oldValue is int)
            {
                return (int)oldValue != (int)newValue;
            }
            else if (oldValue is System.Numerics.Vector2)
            {
                return (System.Numerics.Vector2)oldValue != (System.Numerics.Vector2)newValue;
            }
            else if (oldValue is System.Collections.IList oldList)
            {
                return AreListsSame(oldList, (System.Collections.IList)newValue);
            }
            else
            {
                return oldValue.Equals(newValue) == false;
            }
        }
    }

    private bool AreListsSame(System.Collections.IList oldList, System.Collections.IList newList)
    {
        if (oldList == null && newList == null)
        {
            return true;
        }
        if (oldList == null || newList == null)
        {
            return false;
        }

        for (int i = 0; i < oldList.Count; i++)
        {
            if (oldList[i].Equals(newList[i]) == false)
            {
                return false;
            }
        }
        return true;
    }

    #endregion
}
