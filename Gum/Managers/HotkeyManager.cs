using Gum.DataTypes;
using Gum.Logic;
using Gum.Plugins;
using Gum.ToolStates;
using Gum.Wireframe;
using System.Windows.Forms;

namespace Gum.Managers
{
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

        public bool IsPressed(InputLibrary.Keyboard keyboard)
        {
            if (IsShiftDown &&
                !keyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) &&
                !keyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift))
            {
                return false;
            }

            if (IsCtrlDown &&
                !keyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) &&
                !keyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.RightControl))
            {
                return false;
            }

            if (IsAltDown &&
                !keyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt) &&
                !keyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.RightAlt))
            {
                return false;
            }

            return Key == null ||
                // Most keys are the same in XNA - is this enough?
                keyboard.KeyDown((Microsoft.Xna.Framework.Input.Keys)Key);

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



        // If adding any new keys here, modify HotkeyViewModel

        #region Element Tree View

        public void HandleKeyDownElementTreeView(KeyEventArgs e)
        {
            HandleCopyCutPaste(e);
            HandleDelete(e);
            HandleReorder(e);
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
            {
                ElementTreeViewManager.Self.OnSelect(ElementTreeViewManager.Self.SelectedNode);
            }
            TryHandleCtrlF(e);
            HandleGoToDefinition(e);

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
                if (SelectedState.Self.SelectedInstance != null)
                {
                    elementToGoTo = ObjectFinder.Self.GetElementSave(SelectedState.Self.SelectedInstance.BaseType);
                }
                else if (!string.IsNullOrWhiteSpace(SelectedState.Self.SelectedElement?.BaseType))
                {
                    elementToGoTo = ObjectFinder.Self.GetElementSave(SelectedState.Self.SelectedElement.BaseType);
                }

                if (elementToGoTo != null)
                {
                    SelectedState.Self.SelectedElement = elementToGoTo;
                }
            }
        }

        void HandleCopyCutPaste(KeyEventArgs e)
        {
            if(Copy.IsPressed(e))
            {
                CopyPasteLogic.OnCopy(CopyType.InstanceOrElement);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            if(Paste.IsPressed(e))
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                CopyPasteLogic.OnPaste(CopyType.InstanceOrElement);
            }
            if(Cut.IsPressed(e))
            {
                CopyPasteLogic.OnCut(CopyType.InstanceOrElement);
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
                var instance = SelectedState.Self.SelectedInstance;

                var element = SelectedState.Self.SelectedElement;

                float oldX = 0;
                float oldY = 0;
                if(instance != null)
                {
                    oldX = (float)instance.GetValueFromThisOrBase(element, "X");
                    oldY = (float)instance.GetValueFromThisOrBase(element, "Y");
                }

                EditingManager.Self.MoveSelectedObjectsBy(nudgeX, nudgeY);
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
                CopyPasteLogic.OnCopy(CopyType.State);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (Paste.IsPressed(e))
            {
                CopyPasteLogic.OnPaste(CopyType.State);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        #endregion
    }
}
