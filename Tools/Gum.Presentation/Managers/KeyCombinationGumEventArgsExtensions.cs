using Gum.Input;

namespace Gum.Managers;

/// <summary>
/// Matches a <see cref="KeyCombination"/> against Gum's framework-neutral <see cref="GumKeyEventArgs"/>.
/// Framework-neutral (ADR-0005), unlike the WinForms/WPF <c>IsPressed</c> overloads in
/// <c>Gum.csproj</c>'s <c>KeyCombinationExtensions</c> — those match against the raw framework key
/// event types and so must stay in the tool layer, while this one is safe to live in Gum.Presentation
/// alongside <see cref="KeyCombination"/> itself. Used by callers that have already translated the
/// framework key event at the editor-host boundary (e.g. <c>CameraController.HandleKeyPress</c>).
/// </summary>
public static class KeyCombinationGumEventArgsExtensions
{
    public static bool IsPressed(this KeyCombination kc, GumKeyEventArgs args)
    {
        if (kc.IsCtrlDown && !args.IsCtrlDown) return false;
        if (kc.IsShiftDown && !args.IsShiftDown) return false;
        if (kc.IsAltDown && !args.IsAltDown) return false;

        return kc.Key == null || args.Key == kc.Key;
    }
}
