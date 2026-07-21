using Gum.Commands;
using Gum.Controls;
using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Input;
using Gum.Logic;
using Gum.Plugins;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Themes;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Wireframe;
using System;
using System.Windows.Forms;
using Gum.SelectionHistory;
using Gum.Undo;
using Gum.Plugins.InternalPlugins.VariableGrid;

namespace Gum.Managers;

public class HotkeyManager : IHotkeyManager
{
    public KeyCombination Delete { get; private set; } = KeyCombination.Pressed(GumKey.Delete);
    public KeyCombination Copy { get; private set; } = KeyCombination.Ctrl(GumKey.C);
    public KeyCombination Paste { get; private set; } = KeyCombination.Ctrl(GumKey.V);
    public KeyCombination Cut { get; private set; } = KeyCombination.Ctrl(GumKey.X);
    public KeyCombination Undo { get; private set; } = KeyCombination.Ctrl(GumKey.Z);
    public KeyCombination Redo { get; private set; } = KeyCombination.Ctrl(GumKey.Y);

    public KeyCombination RedoAlt { get; private set; } = new KeyCombination()
    {
        IsCtrlDown = true,
        IsShiftDown = true,
        Key = GumKey.Z
    };

    public KeyCombination ReorderUp { get; private set; } = KeyCombination.Alt(GumKey.Up);
    public KeyCombination ReorderDown { get; private set; } = KeyCombination.Alt(GumKey.Down);
    public KeyCombination GoToDefinition { get; private set; } = KeyCombination.Pressed(GumKey.F12);
    public KeyCombination Search { get; private set; } = KeyCombination.Ctrl(GumKey.F);
    public KeyCombination NudgeUp { get; private set; } = KeyCombination.Pressed(GumKey.Up);
    public KeyCombination NudgeDown { get; private set; } = KeyCombination.Pressed(GumKey.Down);
    public KeyCombination NudgeRight { get; private set; } = KeyCombination.Pressed(GumKey.Right);
    public KeyCombination NudgeLeft { get; private set; } = KeyCombination.Pressed(GumKey.Left);
    public KeyCombination NudgeUp5 { get; private set; } = KeyCombination.Shift(GumKey.Up);
    public KeyCombination NudgeDown5 { get; private set; } = KeyCombination.Shift(GumKey.Down);
    public KeyCombination NudgeRight5 { get; private set; } = KeyCombination.Shift(GumKey.Right);
    public KeyCombination NudgeLeft5 { get; private set; } = KeyCombination.Shift(GumKey.Left);

    public KeyCombination LockMovementToAxis { get; private set; } = KeyCombination.Shift();
    public KeyCombination MaintainResizeAspectRatio { get; private set; } = KeyCombination.Shift();
    public KeyCombination SnapRotationTo15Degrees { get; private set; } = KeyCombination.Shift();
    public KeyCombination MultiSelect { get; private set; } = KeyCombination.Shift();
    public KeyCombination ResizeFromCenter { get; private set; } = KeyCombination.Alt();

    public KeyCombination MoveCameraLeft { get; private set; } = KeyCombination.Ctrl(GumKey.Left);
    public KeyCombination MoveCameraRight { get; private set; } = KeyCombination.Ctrl(GumKey.Right);
    public KeyCombination MoveCameraUp { get; private set; } = KeyCombination.Ctrl(GumKey.Up);
    public KeyCombination MoveCameraDown { get; private set; } = KeyCombination.Ctrl(GumKey.Down);

    public KeyCombination ZoomCameraIn { get; private set; } = KeyCombination.Ctrl(GumKey.Add);
    public KeyCombination ZoomCameraInAlternative { get; private set; } = KeyCombination.Ctrl(GumKey.Oemplus);

    public KeyCombination ZoomCameraOut { get; private set; } = KeyCombination.Ctrl(GumKey.Subtract);
    public KeyCombination ZoomCameraOutAlternative { get; private set; } = KeyCombination.Ctrl(GumKey.OemMinus);

    public KeyCombination Rename { get; private set; } = KeyCombination.Pressed(GumKey.F2);

    public KeyCombination NavigateBack { get; private set; } = KeyCombination.Alt(GumKey.Left);
    public KeyCombination NavigateForward { get; private set; } = KeyCombination.Alt(GumKey.Right);

    // If adding any new keys here, modify HotkeyViewModel


    private readonly ICopyPasteLogic _copyPasteLogic;
    private readonly IGuiCommands _guiCommands;
    private readonly ISelectedState _selectedState;
    private readonly IElementCommands _elementCommands;
    private readonly IDialogService _dialogService;
    private readonly IFileCommands _fileCommands;
    private readonly ISetVariableLogic _setVariableLogic;
    private readonly IUiSettingsService _uiSettingsService;
    private readonly IUndoManager _undoManager;
    private readonly IEditCommands _editCommands;
    private readonly IReorderLogic _reorderLogic;
    private readonly IPluginManager _pluginManager;
    private readonly ISelectionHistory _selectionHistory;


    public HotkeyManager(IGuiCommands guiCommands,
        ISelectedState selectedState,
        IElementCommands elementCommands,
        IDialogService dialogService,
        IFileCommands fileCommands,
        ISetVariableLogic setVariableLogic,
        IUiSettingsService uiSettingsService,
        ICopyPasteLogic copyPasteLogic,
        IUndoManager undoManager,
        IEditCommands editCommands,
        IReorderLogic reorderLogic,
        IPluginManager pluginManager,
        ISelectionHistory selectionHistory)
    {
        _copyPasteLogic = copyPasteLogic;
        _guiCommands = guiCommands;
        _selectedState = selectedState;
        _elementCommands = elementCommands;
        _dialogService = dialogService;
        _fileCommands = fileCommands;
        _setVariableLogic = setVariableLogic;
        _uiSettingsService = uiSettingsService;
        _undoManager = undoManager;
        _editCommands = editCommands;
        _reorderLogic = reorderLogic;
        _pluginManager = pluginManager;
        _selectionHistory = selectionHistory;
    }

    public bool IsPressedInControl(KeyCombination combo)
    {
        // Reads the live WinForms modifier state (Control.ModifierKeys). This stays in the tool layer
        // (off the now-headless KeyCombination) and on the interface so it remains mockable in tests.
        if (combo.IsCtrlDown && (Control.ModifierKeys & Keys.Control) != Keys.Control) return false;
        if (combo.IsShiftDown && (Control.ModifierKeys & Keys.Shift) != Keys.Shift) return false;
        if (combo.IsAltDown && (Control.ModifierKeys & Keys.Alt) != Keys.Alt) return false;

        return combo.Key == null || (Control.ModifierKeys & Keys.KeyCode) == (Keys)(int)combo.Key.Value;
    }

    #region App Wide Keys


    public bool PreviewKeyDownAppWide(GumKeyEventArgs e, bool enableEntireAppZoom = true)
    {
        return PreviewKeyDownAppWideInternal(e, enableEntireAppZoom);
    }

    private bool PreviewKeyDownAppWideInternal(GumKeyEventArgs e, bool enableEntireAppZoom = true)
    {
        Action? match = true switch
        {
            _ when Search.IsPressed(e)  => _guiCommands.FocusSearch,
            _ when RedoAlt.IsPressed(e) || Redo.IsPressed(e) => _undoManager.PerformRedo,
            _ when Undo.IsPressed(e) => _undoManager.PerformUndo,
            _ when NavigateBack.IsPressed(e) => _selectionHistory.NavigateBack,
            _ when NavigateForward.IsPressed(e) => _selectionHistory.NavigateForward,
            _ when ZoomDirection() is { } dir && enableEntireAppZoom => () => _uiSettingsService.BaseFontSize += dir,
            _ => null
        };

        if (match is not null)
        {
            e.Handled = true;
            match.Invoke();
        }

        return match is not null;

        double? ZoomDirection() =>
            ZoomCameraIn.IsPressed(e) || ZoomCameraInAlternative.IsPressed(e) ? 1 :
            ZoomCameraOut.IsPressed(e) || ZoomCameraOutAlternative.IsPressed(e) ? -1 :
            null;
    }


    #endregion


    #region Element Tree View

    public void HandleKeyDownElementTreeView(GumKeyEventArgs e)
    {
        if (PreviewKeyDownAppWideInternal(e))
        {
            e.Handled = true;
            return;
        }

        HandleCopyCutPaste(e);
        HandleDelete(e);
        HandleReorder(e);
        TryHandleCtrlF(e);
        HandleGoToDefinition(e);
        HandleRename(e);
    }

    private void HandleRename(GumKeyEventArgs e)
    {
        if(Rename.IsPressed(e))
        {
            if(_selectedState.SelectedInstance is { } selectedInstance)
            {
                string oldName = selectedInstance.Name;
                GetUserStringOptions options = new()
                {
                    InitialValue = oldName,
                    PreSelect = true
                };
                
                if (_dialogService.GetUserString("Enter new name", "Rename Instance", options) is { } newName)
                {
                    selectedInstance.Name = newName;
                    _setVariableLogic.PropertyValueChanged("Name", oldName,
                        selectedInstance,
                        selectedInstance.ParentContainer?.DefaultState,
                        refresh: true,
                        recordUndo: true,
                        trySave: true);
                }
                e.Handled = true;
            }
            else if(_selectedState.SelectedElement is { } selectedElement and not StandardElementSave)
            {
                _dialogService.Show<RenameElementDialogViewModel>(vm =>
                {
                    vm.ElementSave = selectedElement;
                });
                e.Handled = true;
            }
            else if(_selectedState.SelectedBehavior is { } selectedBehavior)
            {
                _editCommands.AskToRenameBehavior(selectedBehavior);
                e.Handled = true;
            }
            else if(_selectedState.SelectedTreeNode is { } selectedTreeNode
                && (selectedTreeNode.IsScreensFolderTreeNode() || selectedTreeNode.IsComponentsFolderTreeNode()))
            {
                _dialogService.Show<RenameFolderDialogViewModel>(vm =>
                {
                    vm.FolderNode = selectedTreeNode;
                });
                e.Handled = true;
            }
        }
    }

    private void TryHandleCtrlF(GumKeyEventArgs e)
    {

        if(Search.IsPressed(e))
        {
            _guiCommands.FocusSearch();
            e.Handled = true;
        }
    }

    private void HandleReorder(GumKeyEventArgs e)
    {
        if(ReorderUp.IsPressed(e))
        {
            _reorderLogic.MoveSelectedInstanceBackward();
            e.Handled = true;
        }
        if(ReorderDown.IsPressed(e))
        {
            _reorderLogic.MoveSelectedInstanceForward();
            e.Handled = true;
        }
    }

    void HandleDelete(GumKeyEventArgs e)
    {
        if (Delete.IsPressed(e))
        {
            _editCommands.DeleteSelection();

            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    void HandleGoToDefinition(GumKeyEventArgs e)
    {
        if (GoToDefinition.IsPressed(e))
        {
            DataTypes.ElementSave elementToGoTo = null;
            if (_selectedState.SelectedInstance != null)
            {
                elementToGoTo = ObjectFinder.Self.GetElementSave(_selectedState.SelectedInstance.BaseType);
            }
            else if (!string.IsNullOrWhiteSpace(_selectedState.SelectedElement?.BaseType))
            {
                elementToGoTo = ObjectFinder.Self.GetElementSave(_selectedState.SelectedElement.BaseType);
            }

            if (elementToGoTo != null)
            {
                _selectedState.SelectedElement = elementToGoTo;
            }
        }
    }

    void HandleCopyCutPaste(GumKeyEventArgs e)
    {
        if(Copy.IsPressed(e))
        {
            _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        if(Paste.IsPressed(e))
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);
        }
        if(Cut.IsPressed(e))
        {
            _copyPasteLogic.OnCut(CopyType.InstanceOrElement);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    #endregion

    #region Wireframe Control

    public void HandleEditorKeyDown(GumKeyEventArgs e)
    {
        if (PreviewKeyDownAppWideInternal(e, enableEntireAppZoom: false))
        {
            e.Handled = true;
            return;
        }

        HandleCopyCutPaste(e);

        HandleDelete(e);
        // Up moves the control "up" in the tree view, but when you are in the wireframe
        // up should move it the opposite direction. We'll see how it goes...
        // Update - inverting is not a good idea because it will work differently when
        // dealing with stack layouts, and that's more complexity than I want to handle
        HandleReorder(e);

        HandleGoToDefinition(e);
    }

    public void HandleKeyUpWireframe()
    {
        if (_isNudging)
        {
            _isNudging = false;
            _undoManager.RecordUndo();
            _fileCommands.TryAutoSaveCurrentElement();
        }
    }

    bool _isNudging;

    public bool ProcessCmdKeyWireframe(GumKey? key, bool isShiftDown, bool isCtrlDown, bool isAltDown)
    {
        // The interface is framework-neutral (GumKey, not WinForms Keys). Rebuild the Keys value the
        // nudge logic matches against: GumKey values equal Win32 virtual-key codes (pinned by GumKeyTests),
        // so the key is a pure cast, and the modifier bits are re-applied from the booleans. Reconstructing
        // here keeps HandleNudge / KeyCombination.IsPressed unchanged and behavior identical — including the
        // existing suppression of nudging while Ctrl/Alt are held (Ctrl+arrow pans the camera instead).
        Keys keyData = key.HasValue ? (Keys)(int)key.Value : Keys.None;
        if (isShiftDown) { keyData |= Keys.Shift; }
        if (isCtrlDown) { keyData |= Keys.Control; }
        if (isAltDown) { keyData |= Keys.Alt; }

        return HandleNudge(keyData);
    }

    private bool HandleNudge(Keys keyData)
    {

        int nudgeX = 0;
        int nudgeY = 0;
        
        if (NudgeUp5.IsPressed(keyData)) nudgeY = -5;
        else if (NudgeDown5.IsPressed(keyData)) nudgeY = 5;
        else if (NudgeUp.IsPressed(keyData)) nudgeY = -1;
        else if (NudgeDown.IsPressed(keyData)) nudgeY = 1;

        if (NudgeRight5.IsPressed(keyData)) nudgeX = 5;
        else if (NudgeLeft5.IsPressed(keyData)) nudgeX = -5;
        else if (NudgeRight.IsPressed(keyData)) nudgeX = 1;
        else if (NudgeLeft.IsPressed(keyData)) nudgeX = -1;

        bool handled = false;

        if (nudgeX != 0 || nudgeY != 0)
        {
            var instance = _selectedState.SelectedInstance;

            if (instance?.Locked == true)
            {
                return false;
            }

            if(!_isNudging)
            {
                _isNudging = true;
                _undoManager.RecordState();
            }

            var element = _selectedState.SelectedElement;

            float oldX = 0;
            float oldY = 0;
            if(instance != null)
            {
                oldX = (float)instance.GetValueFromThisOrBase(element, "X");
                oldY = (float)instance.GetValueFromThisOrBase(element, "Y");
            }
            
            _elementCommands.MoveSelectedObjectsBy(nudgeX, nudgeY);
            handled = true;
            if(nudgeX != 0)
            {
                _pluginManager.VariableSet(element, instance, "X", oldX);
            }
            if(nudgeY != 0)
            {
                _pluginManager.VariableSet(element, instance, "Y", oldY);
            }
        }
        return handled;
    }


    #endregion
}