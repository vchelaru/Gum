using Gum.Input;
using Gum.Managers;
using Gum.Services.Dialogs;
using StateAnimationPlugin.Managers;
using StateAnimationPlugin.ViewModels;

namespace StateAnimationPlugin;

/// <summary>
/// The Animations tab's list hotkeys, shared by both heads' views: reorder, delete, copy and paste in
/// the animation list, and delete, copy and paste in the keyframe list. Each view converts its key
/// event to <see cref="GumKeyEventArgs"/> and forwards it here.
/// </summary>
public class AnimationTabKeyHandler
{
    private readonly IHotkeyManager _hotkeyManager;
    private readonly IDialogService _dialogService;

    /// <summary>Creates the handler over the tool's hotkey bindings.</summary>
    public AnimationTabKeyHandler(IHotkeyManager hotkeyManager, IDialogService dialogService)
    {
        _hotkeyManager = hotkeyManager;
        _dialogService = dialogService;
    }

    /// <summary>
    /// Handles a key pressed in the animation list. Returns true when the key was consumed (the view
    /// marks the event handled); copy and paste act but let the key through, as they always have.
    /// </summary>
    public bool HandleAnimationListKey(GumKeyEventArgs e, ElementAnimationsViewModel viewModel)
    {
        if (viewModel.SelectedAnimation != null && _hotkeyManager.ReorderUp.IsPressed(e))
        {
            return viewModel.MoveSelectedAnimationUp();
        }
        if (viewModel.SelectedAnimation != null && _hotkeyManager.ReorderDown.IsPressed(e))
        {
            return viewModel.MoveSelectedAnimationDown();
        }
        if (viewModel.SelectedAnimation != null && _hotkeyManager.Delete.IsPressed(e))
        {
            viewModel.DeleteSelectedAnimation();
            return true;
        }
        if (_hotkeyManager.Copy.IsPressed(e))
        {
            if (viewModel.SelectedAnimation is { } animation)
            {
                AnimationCopyPasteManager.Copy(animation);
            }
        }
        else if (_hotkeyManager.Paste.IsPressed(e))
        {
            AnimationCopyPasteManager.Paste(viewModel, _dialogService);
        }
        return false;
    }

    /// <summary>
    /// Handles a key pressed in the keyframe list while a keyframe is selected. Returns the keyframe a
    /// paste added, which the view hands to the plugin so the edit is recorded; otherwise null.
    /// </summary>
    public AnimatedKeyframeViewModel? HandleKeyframeListKey(GumKeyEventArgs e, ElementAnimationsViewModel viewModel)
    {
        if (viewModel.SelectedAnimation?.SelectedKeyframe == null)
        {
            return null;
        }

        if (_hotkeyManager.Delete.IsPressed(e))
        {
            viewModel.DeleteSelectedKeyframe();
        }
        else if (_hotkeyManager.Copy.IsPressed(e))
        {
            viewModel.CopySelectedKeyframe();
        }
        else if (_hotkeyManager.Paste.IsPressed(e))
        {
            return viewModel.PasteKeyframe();
        }
        return null;
    }
}
