using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using System.Collections.Generic;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// Input-path additions to <see cref="StaticSnapshot"/> for the #6066 input spike. Kept in a
/// separate type so the original snapshot file is untouched. Everything here is restorable through a
/// public setter or a public mutable collection; state with no way back is listed in
/// <see cref="UncapturedInputState"/>.
/// </summary>
internal sealed class InputStaticSnapshot
{
    /// <summary>
    /// Statics that the Update, cursor, keyboard, focus and popup paths read or write that this
    /// snapshot (and <see cref="StaticSnapshot"/>) cannot capture or restore.
    /// </summary>
    public static readonly string[] UncapturedInputState =
    {
        "InteractiveGue.CurrentInputReceiver: one process-wide focus slot. Assignable, but assigning raises OnLoseFocus/OnGainFocus (LostFocus/GotFocus) on the controls, so it cannot be swapped silently. Left out of Restore unless asked for.",
        "InteractiveGue.nextPushActions / nextClickActions / holding lists (private): one queue for the process. ComboBox, TextBox and others enqueue 'close me on the next push' actions; the next service to Update with the cursor in the window runs them. Only ClearNextPushActions/ClearNextClickActions exist, which wipes both services' entries.",
        "InteractiveGue.CurrentGameTime (internal setter in GumCommon): written by every Update. Not settable from outside the assembly.",
        "ToolTipService hover/timer/open-tooltip fields (private statics): one hover state for the process, rewritten by every service's Update with that service's cursor. Only ResetForTesting exists.",
        "FormsUtilities.Gamepads (private setter): one shared GamePad[4]; every service's Update advances the same four GamePad objects, and a later service's InitializeDefaults replaced the earlier service's elements in place.",
        "FormsUtilities.innerList / innerRootList / popupModalRootPairs / _lastEventRoots and GueInteractiveExtensionMethods.internalList / previewList (private scratch statics): not state across frames, but LastEventRoots (internal) is what GetEventFailureReason diagnoses against, so it describes whichever service Updated last.",
        "FrameworkElement.MainCursor / MainKeyboard: public setters, but they are written as a side effect of FormsUtilities.SetCursor/SetKeyboard and also read directly by controls (TextBoxBase, ComboBox.TryHideFromPush). Restoring only FormsUtilities.Cursor/Keyboard without SetCursor/SetKeyboard leaves them stale.",
        "GumService.Cursor / Keyboard / PopupRoot / ModalRoot (instance properties): forward to the statics, so they answer for whichever service is current, not for the instance they are called on.",
        "IGumService.Default consumers: TextBoxBase and MenuItem reach DeferredQueue (and Clipboard) through the current default service, so work enqueued by a control in one service drains in the next Update of whichever service is current.",
    };

    public StaticSnapshot Base { get; private set; } = null!;

    private Gum.GumService _service = null!;

    // Public-setter statics the input path uses that StaticSnapshot does not carry.
    public InteractiveGue? LastVisualPushed { get; private set; }
    public System.Func<GraphicalUiElement, (InteractiveGue popup, InteractiveGue modal)>? ResolvePopupRoots { get; private set; }
    public List<(InteractiveGue popupRoot, InteractiveGue modalRoot)> AdditionalPopupRootPairs { get; private set; } = new();
    public List<KeyCombo> TabKeyCombos { get; private set; } = new();
    public List<KeyCombo> TabReverseKeyCombos { get; private set; } = new();
    public List<KeyCombo> ClickCombos { get; private set; } = new();
    public List<KeyCombo> UpKeyCombos { get; private set; } = new();
    public List<KeyCombo> DownKeyCombos { get; private set; } = new();
    public List<KeyCombo> LeftKeyCombos { get; private set; } = new();
    public List<KeyCombo> RightKeyCombos { get; private set; } = new();

    /// <summary>
    /// Focus slot saved when the service was last switched away from. Only used when
    /// Restore(restoreInputReceiver: true) is asked for, to show what a swap through the public setter does.
    /// </summary>
    public IInputReceiver? InputReceiver { get; private set; }

    /// <summary>
    /// Captures the statics as seen from <paramref name="service"/>: the ones that now live in its
    /// GumContext (Forms roots, cursor, keyboard, templates, defaults) are read inside its scope.
    /// </summary>
    public static InputStaticSnapshot Capture(Gum.GumService service)
    {
        service.Activate();
        InputStaticSnapshot snapshot = new InputStaticSnapshot
        {
            _service = service,
            Base = StaticSnapshot.Capture(service),
            LastVisualPushed = InteractiveGue.LastVisualPushed,
            ResolvePopupRoots = GraphicalUiElement.ResolvePopupRoots,
            AdditionalPopupRootPairs = new List<(InteractiveGue, InteractiveGue)>(FrameworkElement.AdditionalPopupRootPairs),
            TabKeyCombos = new List<KeyCombo>(FrameworkElement.TabKeyCombos),
            TabReverseKeyCombos = new List<KeyCombo>(FrameworkElement.TabReverseKeyCombos),
            ClickCombos = new List<KeyCombo>(FrameworkElement.ClickCombos),
            UpKeyCombos = new List<KeyCombo>(FrameworkElement.UpKeyCombos),
            DownKeyCombos = new List<KeyCombo>(FrameworkElement.DownKeyCombos),
            LeftKeyCombos = new List<KeyCombo>(FrameworkElement.LeftKeyCombos),
            RightKeyCombos = new List<KeyCombo>(FrameworkElement.RightKeyCombos),
            InputReceiver = InteractiveGue.CurrentInputReceiver,
        };
        return snapshot;
    }

    /// <summary>Saves the statics that change while the service runs, called when switching away from it.</summary>
    public void SaveDynamic()
    {
        LastVisualPushed = InteractiveGue.LastVisualPushed;
        InputReceiver = InteractiveGue.CurrentInputReceiver;
    }

    public void Restore(bool restoreInputReceiver = false)
    {
        _service.Activate();
        Base.Restore();

        InteractiveGue.LastVisualPushed = LastVisualPushed;
        GraphicalUiElement.ResolvePopupRoots = ResolvePopupRoots;
        FrameworkElement.AdditionalPopupRootPairs.Clear();
        FrameworkElement.AdditionalPopupRootPairs.AddRange(AdditionalPopupRootPairs);
        FrameworkElement.TabKeyCombos = new List<KeyCombo>(TabKeyCombos);
        FrameworkElement.TabReverseKeyCombos = new List<KeyCombo>(TabReverseKeyCombos);
        FrameworkElement.ClickCombos = new List<KeyCombo>(ClickCombos);
        FrameworkElement.UpKeyCombos = new List<KeyCombo>(UpKeyCombos);
        FrameworkElement.DownKeyCombos = new List<KeyCombo>(DownKeyCombos);
        FrameworkElement.LeftKeyCombos = new List<KeyCombo>(LeftKeyCombos);
        FrameworkElement.RightKeyCombos = new List<KeyCombo>(RightKeyCombos);

        if (restoreInputReceiver)
        {
            // Raises OnLoseFocus on the outgoing receiver and OnGainFocus on the incoming one.
            InteractiveGue.CurrentInputReceiver = InputReceiver;
        }
    }
}
