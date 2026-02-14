using Gum.Managers;
using Gum.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Plugins.InternalPlugins.Hotkey.ViewModels
{
    class HotkeyViewModel : ViewModel
    {
        private IHotkeyManager _hotkeyManager;

        public List<HotkeyItemViewModel> Items { get; set; } = new List<HotkeyItemViewModel>();

        public HotkeyViewModel(IHotkeyManager hotkeyManager)
        {
            _hotkeyManager = hotkeyManager;

            Add(_hotkeyManager.Delete, "Delete");
            Add(_hotkeyManager.Copy, "Copy");
            Add(_hotkeyManager.Paste, "Paste");
            Add(_hotkeyManager.Cut, "Cut");
            Add(_hotkeyManager.Undo, "Undo");
            Add(_hotkeyManager.Redo, "Redo");
            Add(_hotkeyManager.RedoAlt, "Redo (Alternative)");
            Add(_hotkeyManager.ReorderUp, "Reorder Up");
            Add(_hotkeyManager.ReorderDown, "Reorder Down");
            Add(_hotkeyManager.GoToDefinition, "Go to Definition");
            Add(_hotkeyManager.Search, "Search");
            
            Add(_hotkeyManager.NudgeUp, "Nudge Up");
            Add(_hotkeyManager.NudgeUp5, "Nudge Up 5");

            Add(_hotkeyManager.NudgeDown, "Nudge Down");
            Add(_hotkeyManager.NudgeDown5, "Nudge Down 5");

            Add(_hotkeyManager.NudgeLeft, "Nudge Left");
            Add(_hotkeyManager.NudgeLeft5, "Nudge Left 5");

            Add(_hotkeyManager.NudgeRight, "Nudge Right");
            Add(_hotkeyManager.NudgeRight5, "Nudge Right 5");

            Add(_hotkeyManager.LockMovementToAxis, "Lock movement to Axis");
            Add(_hotkeyManager.MaintainResizeAspectRatio, "Maintain Aspect Ratio on Resize");
            Add(_hotkeyManager.SnapRotationTo15Degrees, "Snap Rotation to 15 Degrees");
            Add(_hotkeyManager.ResizeFromCenter, "Resize from Center");

            Add(_hotkeyManager.MoveCameraUp, "Move Camera Up");
            Add(_hotkeyManager.MoveCameraDown, "Move Camera Down");
            Add(_hotkeyManager.MoveCameraLeft, "Move Camera Left");
            Add(_hotkeyManager.MoveCameraRight, "Move Camera Right");

            Add(_hotkeyManager.ZoomCameraIn, "Zoom In");
            Add(_hotkeyManager.ZoomCameraOut, "Zoom Out");

            Add(_hotkeyManager.Rename, "Rename State");

        }

        private void Add(KeyCombination keyCombination, string action)
        {
            Items.Add(new HotkeyItemViewModel
            {
                Display = action + ": " + keyCombination
            });
        }
    }

    class HotkeyItemViewModel
    {
        public string Display { get; set; }



        public override string ToString() => Display;
    }
}
