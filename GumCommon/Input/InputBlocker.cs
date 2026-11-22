using System;
using Gum.Forms.Controls;
using Gum.Wireframe;
using GumKeys = Gum.Forms.Input.Keys;

namespace Gum.Input;

/// <summary>
/// Applies <see cref="InteractiveGue.IsInputBlocked"/> to the input Forms dispatches each frame for one
/// service: it ends a push on a blocked element and decides whether the focused control gets keyboard and
/// gamepad input. The cursor walk itself skips blocked subtrees.
/// </summary>
internal sealed class InputBlocker
{
    private static readonly GumKeys[] AllKeys = (GumKeys[])Enum.GetValues(typeof(GumKeys));
    private static readonly GamepadButton[] AllButtons = (GamepadButton[])Enum.GetValues(typeof(GamepadButton));

    // Set while the focused control is blocked, so a key or button held through the block cannot act by
    // repeating after it. Cleared on the first unblocked frame nothing is held.
    private bool _isWaitingForRelease;

    /// <summary>
    /// Whether the focused control may receive keyboard and gamepad input this frame: false while it or a
    /// parent blocks input, and after that until every key and gamepad button is released.
    /// </summary>
    public bool CanDispatchToFocusedReceiver(IInputReceiver? receiver, IInputReceiverKeyboard keyboard)
    {
        GraphicalUiElement? visual = (receiver as FrameworkElement)?.Visual ?? receiver as GraphicalUiElement;
        if (InteractiveGue.IsInputBlockedInParentChain(visual))
        {
            _isWaitingForRelease = true;
            return false;
        }
        if (_isWaitingForRelease && !IsAnyKeyOrButtonHeld(keyboard))
        {
            _isWaitingForRelease = false;
        }
        return !_isWaitingForRelease;
    }

    /// <summary>
    /// Ends a push on an element that blocks input (or is under one), raising RemovedAsPushed so it stops
    /// looking pushed. The release then has no push origin and cannot click. Never changes focus.
    /// </summary>
    public static void ReleaseBlockedPush(ICursor cursor)
    {
        if (cursor.WindowPushed is InteractiveGue pushed && InteractiveGue.IsInputBlockedInParentChain(pushed))
        {
            cursor.WindowPushed = null;
            InteractiveGue.LastVisualPushed = null;
            pushed.TryCallRemoveAsPushed();
        }
        if (cursor.VisualRightPushed is InteractiveGue rightPushed && InteractiveGue.IsInputBlockedInParentChain(rightPushed))
        {
            cursor.VisualRightPushed = null;
        }
    }

    private static bool IsAnyKeyOrButtonHeld(IInputReceiverKeyboard keyboard)
    {
        if (IsAnyKeyHeld(keyboard))
        {
            return true;
        }
        foreach (IInputReceiverKeyboard uiKeyboard in FrameworkElement.KeyboardsForUiControl)
        {
            if (IsAnyKeyHeld(uiKeyboard))
            {
                return true;
            }
        }
        foreach (IGamePad gamePad in FrameworkElement.GamePadsForUiControl)
        {
            foreach (GamepadButton button in AllButtons)
            {
                if (gamePad.ButtonDown(button))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static bool IsAnyKeyHeld(IInputReceiverKeyboard keyboard)
    {
        foreach (GumKeys key in AllKeys)
        {
            if (keyboard.KeyDown(key))
            {
                return true;
            }
        }
        return false;
    }
}
