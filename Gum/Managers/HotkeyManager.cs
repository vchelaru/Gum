using Gum.Logic;
using Gum.ToolStates;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Gum.Managers
{
    public class HotkeyManager : Singleton<HotkeyManager>
    {
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
        }

        private void HandleReorder(KeyEventArgs e)
        {
            var altDown = (e.Modifiers & Keys.Alt) == Keys.Alt;

            if (altDown)
            {
                if (e.KeyCode == Keys.Up)
                {
                    ReorderLogic.Self.MoveSelectedInstanceBackward();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Down)
                {
                    ReorderLogic.Self.MoveSelectedInstanceForward();
                    e.Handled = true;
                }
            }
        }

        void HandleDelete(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                DeleteLogic.Self.HandleDelete();

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        void HandleCopyCutPaste(KeyEventArgs e)
        {
            if ((e.Modifiers & Keys.Control) == Keys.Control)
            {
                // copy, ctrl c, ctrl + c
                if (e.KeyCode == Keys.C)
                {
                    EditingManager.Self.OnCopy(CopyType.InstanceOrElement);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                // paste, ctrl v, ctrl + v
                else if (e.KeyCode == Keys.V)
                {
                    EditingManager.Self.OnPaste(CopyType.InstanceOrElement);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                // cut, ctrl x, ctrl + x
                else if (e.KeyCode == Keys.X)
                {
                    EditingManager.Self.OnCut(CopyType.InstanceOrElement);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
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

            // Handled in ProcessCmdKeyWireframe
            //HandleNudge(e);
        }

        public bool ProcessCmdKeyWireframe(ref Message msg, Keys keyData)
        {
            bool handled = false;

            handled = HandleNudge(keyData);

            return handled;
        }

        private bool HandleNudge(Keys keyData)
        {
            bool handled = false;

            int nudgeX = 0;
            int nudgeY = 0;

            Keys extracted = keyData;
            if (keyData >= Keys.KeyCode)
            {
                int value = (int)(keyData) + (int)Keys.Modifiers;

                extracted = (Keys)value;
            }


            if (extracted == Keys.Up)
            {
                nudgeY = -1;
            }
            if (extracted == Keys.Down)
            {
                nudgeY = 1;
            }
            if (extracted == Keys.Right)
            {
                nudgeX = 1;
            }
            if (extracted == Keys.Left)
            {
                nudgeX = -1;
            }

            bool shiftDown = (keyData & Keys.Shift) == Keys.Shift;
            if (shiftDown)
            {
                nudgeX *= 5;
                nudgeY *= 5;
            }

            if (nudgeX != 0 || nudgeY != 0)
            {
                EditingManager.Self.MoveSelectedObjectsBy(nudgeX, nudgeY);
                handled = true;

                GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
            }
            return handled;
        }


        #endregion

        #region State Tree View

        internal bool TryHandleCmdKeyStateView(Keys keyData)
        {

            switch (keyData)
            {
                // CTRL+F, control f, search focus, ctrl f, ctrl + f
                case Keys.Alt | Keys.Up:

                    StateTreeViewManager.Self.MoveStateInDirection(-1);
                    return true;

                case Keys.Alt | Keys.Down:
                    var stateSave = ProjectState.Self.Selected.SelectedStateSave;
                    bool isDefault = stateSave != null &&
                        stateSave == ProjectState.Self.Selected.SelectedElement.DefaultState;

                    if (!isDefault)
                    {
                        StateTreeViewManager.Self.MoveStateInDirection(1);
                    }
                    return true;
                //case Keys.Alt | Keys.Shift | Keys.Down:
                //    return RightClickHelper.MoveToBottom();
                //case Keys.Alt | Keys.Shift | Keys.Up:
                //    return RightClickHelper.MoveToTop();
                default:
                    return false;
            }

        }

        internal void HandleKeyDownStateView(KeyEventArgs e)
        {
            HandleCopyCutPasteState(e);
        }

        private void HandleCopyCutPasteState(KeyEventArgs e)
        {
            if ((e.Modifiers & Keys.Control) == Keys.Control)
            {
                // copy, ctrl c, ctrl + c
                if (e.KeyCode == Keys.C)
                {
                    EditingManager.Self.OnCopy(CopyType.State);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                // paste, ctrl v, ctrl + v
                else if (e.KeyCode == Keys.V)
                {
                    EditingManager.Self.OnPaste(CopyType.State);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                //// cut, ctrl x, ctrl + x
                //else if (e.KeyCode == Keys.X)
                //{
                //    EditingManager.Self.OnCut(CopyType.Instance);
                //    e.Handled = true;
                //    e.SuppressKeyPress = true;
                //}
            }
        }

        #endregion
    }
}
