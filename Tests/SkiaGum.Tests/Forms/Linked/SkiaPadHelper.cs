using Gum.Forms;
using Gum.Input;

namespace SkiaGum.Tests.Forms.Linked;

/// <summary>
/// Stand-in for the Microsoft.Xna <c>Buttons</c> flags the linked gamepad tests use (<c>XnaButtons</c>).
/// </summary>
[Flags]
public enum PadButtons
{
    DPadUp = 1,
    DPadDown = 2,
    DPadLeft = 4,
    DPadRight = 8,
    A = 0x1000,
    B = 0x2000,
}

/// <summary>
/// Drives the platform-neutral <see cref="GamePad"/> and one Forms frame for linked tests whose
/// MonoGame originals build an XNA <c>GamePadState</c> and call <c>GumService.Update(GameTime)</c>.
/// </summary>
public static class SkiaPadHelper
{
    public static void Apply(GamePad gamepad, PadButtons buttons, float leftStickX = 0, float leftStickY = 0)
    {
        foreach (PadButtons flag in Enum.GetValues<PadButtons>())
        {
            if (buttons.HasFlag(flag) && flag != 0)
            {
                gamepad.SetButtonState(Enum.Parse<GamepadButton>(flag.ToString()), true);
            }
        }
        gamepad.SetLeftStickPosition(leftStickX, leftStickY);
        gamepad.Activity(0);
    }

    /// <summary>
    /// One frame of the real Forms input dispatch, as <c>GumService.Update</c> does on MonoGame.
    /// </summary>
    public static void RunFrame()
    {
        Gum.Forms.Controls.FrameworkElement.MainKeyboard ??= new Moq.Mock<Gum.Wireframe.IInputReceiverKeyboard>().Object;
        FormsUtilities.Update(0.0, Gum.GumService.Default.Root);
    }
}
