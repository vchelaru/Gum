using Gum;

namespace GumPreview;

/// <summary>
/// Turns on the input a Forms control needs to be navigated without a mouse (issue #7260). A game
/// opts in with <c>GumService.Default.UseKeyboardDefaults()</c> and <c>UseGamepadDefaults()</c>;
/// the Preview does it for the user so tab order and gamepad navigation can be tested.
/// </summary>
public static class PreviewInputDefaults
{
    public static void Enable(GumService service)
    {
        service.UseKeyboardDefaults();
        service.UseGamepadDefaults();
    }
}
