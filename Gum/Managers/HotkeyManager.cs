﻿using Gum.Commands;
using Gum.Controls;
using Gum.DataTypes;
using Gum.Logic;
using Gum.Plugins;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Wireframe;
using System;
using System.Windows.Forms;
using GumCommon;

namespace Gum.Managers;

#region KeyCombination Class
public class KeyCombination
{
    public Keys? Key { get; set; }
    public bool IsCtrlDown { get; set; }
    public bool IsShiftDown { get; set; }
    public bool IsAltDown { get; set; }

    public static KeyCombination Pressed(Keys key) => new KeyCombination { Key = key };
    public static KeyCombination Ctrl(Keys key) => new KeyCombination { Key = key, IsCtrlDown = true };
    public static KeyCombination Alt(Keys? key = null) => new KeyCombination { Key = key, IsAltDown = true };
    public static KeyCombination Shift(Keys? key = null) => new KeyCombination { Key = key, IsShiftDown = true };


    public bool IsPressed(KeyEventArgs args)
    {
        if (IsCtrlDown && (args.Modifiers & Keys.Control) != Keys.Control) return false;
        if (IsShiftDown && (args.Modifiers & Keys.Shift) != Keys.Shift) return false;
        if (IsAltDown && (args.Modifiers & Keys.Alt) != Keys.Alt) return false;

        return Key == null || args.KeyCode == Key;
    }

    public bool IsPressed(System.Windows.Input.KeyEventArgs args)
    {
        if (IsCtrlDown && (args.KeyboardDevice.Modifiers & System.Windows.Input.ModifierKeys.Control) != System.Windows.Input.ModifierKeys.Control) return false;
        if (IsShiftDown && (args.KeyboardDevice.Modifiers & System.Windows.Input.ModifierKeys.Shift) != System.Windows.Input.ModifierKeys.Shift) return false;
        if (IsAltDown && (args.KeyboardDevice.Modifiers & System.Windows.Input.ModifierKeys.Alt) != System.Windows.Input.ModifierKeys.Alt) return false;

        if(Key == null)
        {
            return true;
        }
        else
        {
            var expectedWpfKey = System.Windows.Input.KeyInterop.KeyFromVirtualKey((int)Key);
            return args.Key == expectedWpfKey || args.SystemKey == expectedWpfKey;

        }
    }

    public bool IsPressed(Keys keyData)
    {
        Keys extracted = keyData;

        if (IsAltDown)
        {
            if (Key == null)
            {
                return keyData == Keys.Alt;
            }
            else
            {
                return keyData == (Keys.Alt | Key);
            }
        }
        else
        {
            if (keyData >= Keys.KeyCode)
            {
                int value = (int)(keyData) + (int)Keys.Modifiers;

                extracted = (Keys)value;
            }

            bool shiftDown = (keyData & Keys.Shift) == Keys.Shift;
            bool ctrlDown = (keyData & Keys.Control) == Keys.Control;
            bool altDown = (keyData & Keys.Alt) == Keys.Alt;

            if (IsCtrlDown && !ctrlDown) return false;
            if (IsShiftDown && !shiftDown) return false;
            if (IsAltDown && !altDown) return false;

            return Key == null || extracted == Key;

        }

    }

    public bool IsPressedInControl()
    {
        if (IsCtrlDown && (Control.ModifierKeys & Keys.Control) != Keys.Control) return false;
        if (IsShiftDown && (Control.ModifierKeys & Keys.Shift) != Keys.Shift) return false;
        if (IsAltDown && (Control.ModifierKeys & Keys.Alt) != Keys.Alt) return false;

        return Key == null || (Control.ModifierKeys & Keys.KeyCode) == Key;
    }

    public override string ToString()
    {
        string toReturn = "";

        if (IsCtrlDown)
        {
            toReturn += "Ctrl";
        }
        if (IsShiftDown)
        {
            if(toReturn.Length != 0)
            {
                toReturn += "+";
            }
            toReturn += "Shift";
        }
        if (IsAltDown)
        {
            if (toReturn.Length != 0)
            {
                toReturn += "+";
            }
            toReturn += "Alt";
        }

        if (Key != null)
        {
            if (toReturn.Length != 0)
            {
                toReturn += "+";
            }
            toReturn += Key.ToString();
        }

        return toReturn;
    }
}
#endregion

public class HotkeyManager : Singleton<HotkeyManager>
{
    public KeyCombination Delete { get; private set; } = KeyCombination.Pressed(Keys.Delete);
    public KeyCombination Copy { get; private set; } = KeyCombination.Ctrl(Keys.C);
    public KeyCombination Paste { get; private set; } = KeyCombination.Ctrl(Keys.V);
    public KeyCombination Cut { get; private set; } = KeyCombination.Ctrl(Keys.X);
    public KeyCombination Undo { get; private set; } = KeyCombination.Ctrl(Keys.Z);
    public KeyCombination Redo { get; private set; } = KeyCombination.Ctrl(Keys.Y);
    public KeyCombination ReorderUp { get; private set; } = KeyCombination.Alt(Keys.Up);
    public KeyCombination ReorderDown { get; private set; } = KeyCombination.Alt(Keys.Down);
    public KeyCombination GoToDefinition { get; private set; } = KeyCombination.Pressed(Keys.F12);
    public KeyCombination Search { get; private set; } = KeyCombination.Ctrl(Keys.F);
    public KeyCombination NudgeUp { get; private set; } = KeyCombination.Pressed(Keys.Up);
    public KeyCombination NudgeDown { get; private set; } = KeyCombination.Pressed(Keys.Down);
    public KeyCombination NudgeRight { get; private set; } = KeyCombination.Pressed(Keys.Right);
    public KeyCombination NudgeLeft { get; private set; } = KeyCombination.Pressed(Keys.Left);
    public KeyCombination NudgeUp5 { get; private set; } = KeyCombination.Shift(Keys.Up);
    public KeyCombination NudgeDown5 { get; private set; } = KeyCombination.Shift(Keys.Down);
    public KeyCombination NudgeRight5 { get; private set; } = KeyCombination.Shift(Keys.Right);
    public KeyCombination NudgeLeft5 { get; private set; } = KeyCombination.Shift(Keys.Left);

    public KeyCombination LockMovementToAxis { get; private set; } = KeyCombination.Shift();
    public KeyCombination MaintainResizeAspectRatio { get; private set; } = KeyCombination.Shift();
    public KeyCombination SnapRotationTo15Degrees { get; private set; } = KeyCombination.Shift();
    public KeyCombination ResizeFromCenter { get; private set; } = KeyCombination.Alt();

    public KeyCombination MoveCameraLeft { get; private set; } = KeyCombination.Ctrl(Keys.Left);
    public KeyCombination MoveCameraRight { get; private set; } = KeyCombination.Ctrl(Keys.Right);
    public KeyCombination MoveCameraUp { get; private set; } = KeyCombination.Ctrl(Keys.Up);
    public KeyCombination MoveCameraDown { get; private set; } = KeyCombination.Ctrl(Keys.Down);

    public KeyCombination ZoomCameraIn { get; private set; } = KeyCombination.Ctrl(Keys.Add);
    public KeyCombination ZoomCameraInAlternative { get; private set; } = KeyCombination.Ctrl(Keys.Oemplus);

    public KeyCombination ZoomCameraOut { get; private set; } = KeyCombination.Ctrl(Keys.Subtract);
    public KeyCombination ZoomCameraOutAlternative { get; private set; } = KeyCombination.Ctrl(Keys.OemMinus);

    public KeyCombination Rename { get; private set; } = KeyCombination.Pressed(Keys.F2);

    private readonly CopyPasteLogic _copyPasteLogic;
    private readonly Commands.GuiCommands _guiCommands;
    private readonly ISelectedState _selectedState;
    private readonly ElementCommands _elementCommands;

    // If adding any new keys here, modify HotkeyViewModel
    
    public HotkeyManager()
    {
        _copyPasteLogic = CopyPasteLogic.Self;
        _guiCommands = GumCommands.Self.GuiCommands;
        _selectedState = Locator.GetRequiredService<ISelectedState>();
        _elementCommands = Locator.GetRequiredService<ElementCommands>();
    }

    #region App Wide Keys


    public void HandleKeyDownAppWide(System.Windows.Input.KeyEventArgs e)
    {
        HandleKeyDownAppWide(ConvertToFormsKeyEventArgs(e));
    }

    private System.Windows.Forms.KeyEventArgs ConvertToFormsKeyEventArgs(System.Windows.Input.KeyEventArgs e)
    {
        // Convert WPF Key to WinForms Keys
        var winFormsKey = (System.Windows.Forms.Keys)System.Windows.Input.KeyInterop.VirtualKeyFromKey(e.Key);

        // Convert WPF modifiers to WinForms modifiers
        var modifiers = ConvertModifiers(System.Windows.Input.Keyboard.Modifiers);

        // Combine the key and modifiers to form the KeyData
        var keyData = winFormsKey | modifiers;

        // Return the WinForms KeyEventArgs
        return new System.Windows.Forms.KeyEventArgs(keyData);
    }

    private System.Windows.Forms.Keys ConvertModifiers(System.Windows.Input.ModifierKeys wpfModifiers)
    {
        var winFormsModifiers = System.Windows.Forms.Keys.None;

        if (wpfModifiers.HasFlag(System.Windows.Input.ModifierKeys.Control))
            winFormsModifiers |= System.Windows.Forms.Keys.Control;
        if (wpfModifiers.HasFlag(System.Windows.Input.ModifierKeys.Alt))
            winFormsModifiers |= System.Windows.Forms.Keys.Alt;
        if (wpfModifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift))
            winFormsModifiers |= System.Windows.Forms.Keys.Shift;

        return winFormsModifiers;
    }


    public void HandleKeyDownAppWide(KeyEventArgs e)
    {
        // Don't try to process any keys if they've already been handled
        if (e.Handled)
            return;

        ScaleAppFont();
       
        void ScaleAppFont()
        {
            int? direction = ZoomCameraIn.IsPressed(e) || ZoomCameraInAlternative.IsPressed(e) ? 1 :
                ZoomCameraOut.IsPressed(e) || ZoomCameraOutAlternative.IsPressed(e) ? -1 : null;

            if (direction is {} dir)
            {
                var currentZoom = _guiCommands.UiZoomValue;
                if (dir > 0)
                {
                    if (currentZoom < 100)
                    {
                        _guiCommands.UiZoomValue += 10;
                    }
                    else
                    {
                        _guiCommands.UiZoomValue += 25;
                    }
                }
                else
                {
                    if (currentZoom <= 100)
                    {
                        _guiCommands.UiZoomValue -= 10;
                    }
                    else
                    {
                        _guiCommands.UiZoomValue -= 25;
                    }
                }
                e.Handled = true;
            }
        }
    }

    #endregion


    #region Element Tree View

    public void HandleKeyDownElementTreeView(KeyEventArgs e)
    {
        HandleCopyCutPaste(e);
        HandleDelete(e);
        HandleReorder(e);
        TryHandleCtrlF(e);
        HandleGoToDefinition(e);
        HandleKeyDownAppWide(e);
        HandleRename(e);
    }

    private void HandleRename(KeyEventArgs e)
    {
        if(Rename.IsPressed(e))
        {
            if(_selectedState.SelectedInstance != null)
            {
                _guiCommands.ShowRenameInstanceWidow(_selectedState.SelectedInstance);
                e.Handled = true;
            }
            else if(_selectedState.SelectedElement != null &&
                _selectedState.SelectedElement is not StandardElementSave)
            {
                _guiCommands.ShowRenameElementWindow(_selectedState.SelectedElement);
                e.Handled = true;
            }
        }
    }

    private void TryHandleCtrlF(KeyEventArgs e)
    {

        if(Search.IsPressed(e))
        {
            GumCommands.Self.GuiCommands.FocusSearch();
            e.Handled = true;
        }
    }

    private void HandleReorder(KeyEventArgs e)
    {
        if(ReorderUp.IsPressed(e))
        {
            ReorderLogic.Self.MoveSelectedInstanceBackward();
            e.Handled = true;
        }
        if(ReorderDown.IsPressed(e))
        {
            ReorderLogic.Self.MoveSelectedInstanceForward();
            e.Handled = true;
        }
    }

    void HandleDelete(KeyEventArgs e)
    {
        if (Delete.IsPressed(e))
        {
            DeleteLogic.Self.HandleDeleteCommand();

            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    void HandleGoToDefinition(KeyEventArgs e)
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

    void HandleCopyCutPaste(KeyEventArgs e)
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

    public void HandleKeyDownWireframe(KeyEventArgs e)
    {
        HandleCopyCutPaste(e);

        HandleDelete(e);
        // Up moves the control "up" in the tree view, but when you are in the wireframe
        // up should move it the opposite direction. We'll see how it goes...
        // Update - inverting is not a good idea because it will work differently when
        // dealing with stack layouts, and that's more complexity than I want to handle
        HandleReorder(e);

        HandleGoToDefinition(e);

    }

    public bool ProcessCmdKeyWireframe(ref Message msg, Keys keyData)
    {
        bool handled = false;

        handled = HandleNudge(keyData);

        return handled;
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
                PluginManager.Self.VariableSet(element, instance, "X", oldX);
            }
            if(nudgeY != 0)
            {
                PluginManager.Self.VariableSet(element, instance, "Y", oldY);
            }


            GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
        }
        return handled;
    }


    #endregion

    #region State Tree View

    internal void HandleKeyDownStateView(KeyEventArgs e)
    {
        HandleCopyCutPasteState(e);
    }

    private void HandleCopyCutPasteState(KeyEventArgs e)
    {
        if (Copy.IsPressed(e))
        {
            _copyPasteLogic.OnCopy(CopyType.State);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (Paste.IsPressed(e))
        {
            _copyPasteLogic.OnPaste(CopyType.State);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    #endregion
}
