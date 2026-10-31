using Gum;

namespace GumPreview;

/// <summary>
/// Turns on the input a Forms control needs to be navigated without a mouse (issue #7260). A game
/// opts in with <c>GumService.Default.UseKeyboardDefaults()</c> and <c>UseGamepadDefaults()</c>;
/// the Preview does it for the user so tab order and gamepad navigation can be tested.
/// </summary>
public static class PreviewInputDefaults
{
    /// <summary>The note shown to the user about what the Preview turned on and how a game does the same.</summary>
    public const string Note =
        "Keyboard (Tab) and gamepad navigation are on in the Preview. A game turns them on in code with " +
        "GumService.Default.UseKeyboardDefaults() and GumService.Default.UseGamepadDefaults().";

    public static void Enable(GumService service)
    {
        service.UseKeyboardDefaults();
        service.UseGamepadDefaults();
    }
}
