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
using Color = System.Drawing.Color;

namespace Gum.Wireframe;

public abstract class WireframeEditor
{
    #region Fields/Properties

    protected HotkeyManager _hotkeyManager { get; private set; }

    private readonly SelectionManager _selectionManager;
    private readonly ISetVariableLogic _setVariableLogic;
    protected readonly ISelectedState _selectedState;
    private readonly IElementCommands _elementCommands;
    private readonly IUndoManager _undoManager;
    protected readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly WireframeObjectManager _wireframeObjectManager;
    protected GrabbedState grabbedState = new GrabbedState();

    // Shared context and move handler for all wireframe editors
    protected readonly EditorContext _context;
    protected readonly MoveInputHandler _moveInputHandler;

    protected bool mHasChangedAnythingSinceLastPush = false;

    protected float aspectRatioOnGrab;

    public bool IsXMovementEnabled { get; set; } = true;
    public bool IsYMovementEnabled { get; set; } = true;
    public bool IsWidthChangeEnabled { get; set; } = true;
    public bool IsHeightChangeEnabled { get; set; } = true;


    public bool RestrictToUnitValues { get; set; }

    #endregion

    public WireframeEditor(
        global::Gum.Managers.HotkeyManager hotkeyManager,
        SelectionManager selectionManager,
        ISelectedState selectedState,
        Layer layer,
        Color lineColor,
        Color textColor)
    {
        _hotkeyManager = hotkeyManager;
        _selectionManager = selectionManager;
        _setVariableLogic = Locator.GetRequiredService<ISetVariableLogic>();
        _selectedState = selectedState;
        _elementCommands = Locator.GetRequiredService<IElementCommands>();
        _undoManager = Locator.GetRequiredService<IUndoManager>();
        _guiCommands = Locator.GetRequiredService<IGuiCommands>();
        _fileCommands = Locator.GetRequiredService<IFileCommands>();
        _wireframeObjectManager = Locator.GetRequiredService<WireframeObjectManager>();

        // Create shared EditorContext and MoveInputHandler
        _context = new EditorContext(
            selectedState,
            selectionManager,
            _elementCommands,
            _guiCommands,
            _fileCommands,
            _setVariableLogic,
            _undoManager,
            Locator.GetRequiredService<IVariableInCategoryPropagationLogic>(),
            hotkeyManager,
            Locator.GetRequiredService<IWireframeObjectManager>(),
            layer,
            grabbedState,
            lineColor,
            textColor);

        _moveInputHandler = new MoveInputHandler(_context);
    }

    public abstract void UpdateToSelection(ICollection<GraphicalUiElement> selectedObjects);

    public abstract bool HasCursorOverHandles { get; }

    public void UpdateAspectRatioForGrabbedIpso()
    {
        if (_selectedState.SelectedInstance != null &&
            _selectedState.SelectedIpso != null
            )
        {
            var ipso = (GraphicalUiElement)_selectedState.SelectedIpso;

            float width = ipso.GetAbsoluteWidth();
            float height = ipso.GetAbsoluteHeight();

            if (height != 0)
            {
                aspectRatioOnGrab = width / height;
            }
        }
    }

    public abstract void Activity(ICollection<GraphicalUiElement> selectedObjects, SystemManagers systemManagers);

    public abstract System.Windows.Forms.Cursor GetWindowsCursorToShow(
        System.Windows.Forms.Cursor defaultCursor, float worldXAt, float worldYAt);

    public abstract void Destroy();

    public virtual bool TryHandleDelete()
    {
        return false;
    }


    protected void DoEndOfSettingValuesLogic()
    {
        var selectedElement = _selectedState.SelectedElement;
        var stateSave = _selectedState.SelectedStateSave;
        if (stateSave == null)
        {
            throw new System.InvalidOperationException("The SelectedStateSave is null, this should not happen");
        }

        _fileCommands.TryAutoSaveElement(selectedElement);

        using var undoLock = _undoManager.RequestLock();

        _guiCommands.RefreshVariableValues();

        var element = _selectedState.SelectedElement;

        foreach (var possiblyChangedVariable in stateSave.Variables.ToList())
        {
            var oldValue = grabbedState.StateSave.GetValue(possiblyChangedVariable.Name);

            if (DoValuesDiffer(stateSave, possiblyChangedVariable.Name, oldValue))
            {
                var instance = element.GetInstance(possiblyChangedVariable.SourceObject);

                // should this be:
                _setVariableLogic.PropertyValueChanged(possiblyChangedVariable.GetRootName(),
                   oldValue,
                   instance,
                   element.DefaultState,
                   refresh: true,
                   recordUndo: false,
                   trySave: false);
                // instead of this?
                //PluginManager.Self.VariableSet(element, instance, possiblyChangedVariable.GetRootName(), oldValue);
            }
        }

        foreach (var possiblyChangedVariableList in stateSave.VariableLists)
        {
            var oldValue = grabbedState.StateSave.GetVariableListSave(possiblyChangedVariableList.Name);

            if (DoValuesDiffer(stateSave, possiblyChangedVariableList.Name, oldValue))
            {
                var instance = element.GetInstance(possiblyChangedVariableList.SourceObject);
                PluginManager.Self.VariableSet(element, instance, possiblyChangedVariableList.GetRootName(), oldValue);
            }
        }

        mHasChangedAnythingSinceLastPush = false;
    }

    protected bool DoValuesDiffer(StateSave newStateSave, string variableName, object oldValue)
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
            if (oldValue is float)
            {
                var oldFloat = (float)oldValue;
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
            else if (oldValue is Vector2)
            {
                return (Vector2)oldValue != (Vector2)newValue;
            }
            else if (oldValue is IList oldList)
            {
                return AreListsSame(oldList, (IList)newValue);
            }
            else
            {
                return oldValue.Equals(newValue) == false;
            }
        }
    }

    private bool AreListsSame(IList oldList, IList newList)
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
}
