using Gum.Managers;
using Gum.Mvvm;
using System.Collections.Generic;

namespace Gum.Plugins.InternalPlugins.Hotkey.ViewModels
{
    /// <summary>
    /// The Hotkeys tab: one row per <see cref="IHotkeyManager"/> binding, rendered in the platform's
    /// modifier names. Keep the list in step with the manager's bindings.
    /// </summary>
    public class HotkeyViewModel : ViewModel
    {
        private readonly IHotkeyManager _hotkeyManager;
        private readonly IKeyCombinationFormatter _formatter;

        public List<HotkeyItemViewModel> Items { get; set; } = new List<HotkeyItemViewModel>();

        public HotkeyViewModel(IHotkeyManager hotkeyManager, IKeyCombinationFormatter formatter)
        {
            _hotkeyManager = hotkeyManager;
            _formatter = formatter;

            Add(_hotkeyManager.Delete, "Delete");
            Add(_hotkeyManager.Copy, "Copy");
            Add(_hotkeyManager.Paste, "Paste");
            Add(_hotkeyManager.Cut, "Cut");
            Add(_hotkeyManager.Duplicate, "Duplicate");
            Add(_hotkeyManager.Undo, "Undo");
            Add(_hotkeyManager.Redo, "Redo");
            Add(_hotkeyManager.RedoAlt, "Redo (Alternative)");
            Add(_hotkeyManager.ReorderUp, "Reorder Up");
            Add(_hotkeyManager.ReorderDown, "Reorder Down");
            Add(_hotkeyManager.GoToDefinition, "Go to Definition");
            Add(_hotkeyManager.Search, "Search");
            Add(_hotkeyManager.FocusVariableFilter, "Filter Variables");

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
            Add(_hotkeyManager.MultiSelect, "Multi-select (click)");
            Add(_hotkeyManager.ResizeFromCenter, "Resize from Center");

            Add(_hotkeyManager.MoveCameraUp, "Move Camera Up");
            Add(_hotkeyManager.MoveCameraDown, "Move Camera Down");
            Add(_hotkeyManager.MoveCameraLeft, "Move Camera Left");
            Add(_hotkeyManager.MoveCameraRight, "Move Camera Right");

            Add(_hotkeyManager.ZoomCameraIn, "Zoom In");
            Add(_hotkeyManager.ZoomCameraInAlternative, "Zoom In (Alternative)");
            Add(_hotkeyManager.ZoomCameraOut, "Zoom Out");
            Add(_hotkeyManager.ZoomCameraOutAlternative, "Zoom Out (Alternative)");

            Add(_hotkeyManager.Rename, "Rename");

            Add(_hotkeyManager.NavigateBack, "Navigate Back");
            Add(_hotkeyManager.NavigateForward, "Navigate Forward");

            Add(_hotkeyManager.ShowHotkeys, "Show Hotkeys");
        }

        private void Add(KeyCombination? keyCombination, string action)
        {
            if (keyCombination == null)
            {
                return;
            }
            Items.Add(new HotkeyItemViewModel
            {
                Display = action + ": " + _formatter.Format(keyCombination)
            });
        }
    }

    public class HotkeyItemViewModel
    {
        public string Display { get; set; }

        public override string ToString() => Display;
    }
}
