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

            Add(_hotkeyManager.Delete, "Delete", HotkeyGroups.Editing);
            Add(_hotkeyManager.DeleteAlt, "Delete (Alternative)", HotkeyGroups.Editing);
            Add(_hotkeyManager.Copy, "Copy", HotkeyGroups.Editing);
            Add(_hotkeyManager.Paste, "Paste", HotkeyGroups.Editing);
            Add(_hotkeyManager.Cut, "Cut", HotkeyGroups.Editing);
            Add(_hotkeyManager.Duplicate, "Duplicate", HotkeyGroups.Editing);
            Add(_hotkeyManager.Undo, "Undo", HotkeyGroups.Editing);
            Add(_hotkeyManager.Redo, "Redo", HotkeyGroups.Editing);
            Add(_hotkeyManager.RedoAlt, "Redo (Alternative)", HotkeyGroups.Editing);
            Add(_hotkeyManager.ReorderUp, "Reorder Up", HotkeyGroups.Navigation);
            Add(_hotkeyManager.ReorderDown, "Reorder Down", HotkeyGroups.Navigation);
            Add(_hotkeyManager.GoToDefinition, "Go to Definition", HotkeyGroups.Navigation);
            Add(_hotkeyManager.Search, "Search", HotkeyGroups.Navigation);
            Add(_hotkeyManager.FocusVariableFilter, "Filter Variables", HotkeyGroups.Navigation);

            Add(_hotkeyManager.NudgeUp, "Nudge Up", HotkeyGroups.Nudge);
            Add(_hotkeyManager.NudgeUp5, "Nudge Up 5", HotkeyGroups.Nudge);

            Add(_hotkeyManager.NudgeDown, "Nudge Down", HotkeyGroups.Nudge);
            Add(_hotkeyManager.NudgeDown5, "Nudge Down 5", HotkeyGroups.Nudge);

            Add(_hotkeyManager.NudgeLeft, "Nudge Left", HotkeyGroups.Nudge);
            Add(_hotkeyManager.NudgeLeft5, "Nudge Left 5", HotkeyGroups.Nudge);

            Add(_hotkeyManager.NudgeRight, "Nudge Right", HotkeyGroups.Nudge);
            Add(_hotkeyManager.NudgeRight5, "Nudge Right 5", HotkeyGroups.Nudge);

            Add(_hotkeyManager.LockMovementToAxis, "Lock movement to Axis", HotkeyGroups.CanvasKeys);
            Add(_hotkeyManager.MaintainResizeAspectRatio, "Maintain Aspect Ratio on Resize", HotkeyGroups.CanvasKeys);
            Add(_hotkeyManager.SnapRotationTo15Degrees, "Snap Rotation to 15 Degrees", HotkeyGroups.CanvasKeys);
            Add(_hotkeyManager.MultiSelect, "Multi-select (click)", HotkeyGroups.CanvasKeys);
            Add(_hotkeyManager.ResizeFromCenter, "Resize from Center", HotkeyGroups.CanvasKeys);

            Add(_hotkeyManager.MoveCameraUp, "Move Camera Up", HotkeyGroups.Camera);
            Add(_hotkeyManager.MoveCameraDown, "Move Camera Down", HotkeyGroups.Camera);
            Add(_hotkeyManager.MoveCameraLeft, "Move Camera Left", HotkeyGroups.Camera);
            Add(_hotkeyManager.MoveCameraRight, "Move Camera Right", HotkeyGroups.Camera);

            Add(_hotkeyManager.ZoomCameraIn, "Zoom In", HotkeyGroups.Camera);
            Add(_hotkeyManager.ZoomCameraInAlternative, "Zoom In (Alternative)", HotkeyGroups.Camera);
            Add(_hotkeyManager.ZoomCameraOut, "Zoom Out", HotkeyGroups.Camera);
            Add(_hotkeyManager.ZoomCameraOutAlternative, "Zoom Out (Alternative)", HotkeyGroups.Camera);

            Add(_hotkeyManager.Rename, "Rename", HotkeyGroups.Editing);

            Add(_hotkeyManager.NavigateBack, "Navigate Back", HotkeyGroups.Navigation);
            Add(_hotkeyManager.NavigateForward, "Navigate Forward", HotkeyGroups.Navigation);

            Add(_hotkeyManager.ShowHotkeys, "Show Hotkeys", HotkeyGroups.Navigation);
            Add(_hotkeyManager.FocusSelection, "Focus on Selection", HotkeyGroups.Camera);
        }

        private void Add(KeyCombination? keyCombination, string action, string group)
        {
            if (keyCombination == null)
            {
                return;
            }
            string shortcut = _formatter.Format(keyCombination);
            Items.Add(new HotkeyItemViewModel
            {
                Name = action,
                Shortcut = shortcut,
                Group = group,
                Display = action + ": " + shortcut
            });
        }
    }

    /// <summary>The areas the Keyboard shortcuts page groups its commands under, in display order.</summary>
    public static class HotkeyGroups
    {
        public const string Editing = "Editing";
        public const string Navigation = "Navigation";
        public const string CanvasKeys = "Canvas keys";
        public const string Nudge = "Nudge";
        public const string Camera = "Camera";

        /// <summary>Every group, in the order the page lists them.</summary>
        public static readonly string[] All = { Editing, Navigation, CanvasKeys, Nudge, Camera };
    }

    public class HotkeyItemViewModel
    {
        /// <summary>The command, such as "Duplicate".</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>The keys, in the platform's modifier names, such as "Ctrl+D".</summary>
        public string Shortcut { get; set; } = string.Empty;

        /// <summary>The area the command belongs to (see <see cref="HotkeyGroups"/>).</summary>
        public string Group { get; set; } = HotkeyGroups.Editing;

        public string Display { get; set; } = string.Empty;

        public override string ToString() => Display;
    }
}
