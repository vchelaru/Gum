using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using MonoGameGum.Tests.Input;
using GumPreview;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Shouldly;
using Xunit;
using GamePad = Gum.Input.GamePad;
using GumKeys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.GumPreview;

public class PreviewInputDefaultsTests : MonoGameGum.Tests.Forms.BeginnerDriverTestBase
{
    private readonly GamePad[] _originalGamepads = FormsUtilities._gamepads;
    private readonly GamePad _pad = new();

    public override void Dispose()
    {
        FormsUtilities._gamepads = _originalGamepads;
        base.Dispose();
    }

    private Button[] AddButtons(int count)
    {
        StackPanel panel = new();
        panel.AddToRoot();
        Button[] buttons = new Button[count];
        for (int i = 0; i < count; i++)
        {
            buttons[i] = new Button { Name = $"B{i}" };
            panel.AddChild(buttons[i]);
        }
        Driver.Step();
        return buttons;
    }

    private void PadFrame(Buttons buttons = 0)
    {
        _pad.Activity(new GamePadState(Vector2.Zero, Vector2.Zero, 0, 0, buttons), 0);
        InteractiveGue.CurrentInputReceiver?.OnFocusUpdate();
    }

    private void PadTap(Buttons buttons)
    {
        PadFrame(buttons);
        PadFrame();
    }

    [Fact]
    public void Enable_TabMovesFocusToTheNextControl()
    {
        FormsUtilities._gamepads = new[] { _pad };
        PreviewInputDefaults.Enable(Gum.GumService.Default);
        Button[] buttons = AddButtons(3);
        buttons[0].IsFocused = true;

        Driver.PressKey(GumKeys.Tab);

        buttons[1].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void Enable_EnterClicksTheFocusedButton()
    {
        FormsUtilities._gamepads = new[] { _pad };
        PreviewInputDefaults.Enable(Gum.GumService.Default);
        Button[] buttons = AddButtons(2);
        int clicks = 0;
        buttons[0].Click += (_, _) => clicks++;
        buttons[0].IsFocused = true;

        Driver.PressKey(GumKeys.Enter);

        clicks.ShouldBe(1);
    }

    [Fact]
    public void Enable_GamepadDpadDownMovesFocus()
    {
        FormsUtilities._gamepads = new[] { _pad };
        PreviewInputDefaults.Enable(Gum.GumService.Default);
        Button[] buttons = AddButtons(3);
        buttons[0].IsFocused = true;

        PadTap(Buttons.DPadDown);

        buttons[1].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void Enable_GamepadAClicksTheFocusedButton()
    {
        FormsUtilities._gamepads = new[] { _pad };
        PreviewInputDefaults.Enable(Gum.GumService.Default);
        Button[] buttons = AddButtons(2);
        int clicks = 0;
        buttons[0].Click += (_, _) => clicks++;
        buttons[0].IsFocused = true;

        PadTap(Buttons.A);

        clicks.ShouldBe(1);
    }

    [Fact]
    public void Note_NamesTheCallsAGameUses()
    {
        PreviewInputDefaults.Note.ShouldContain("UseKeyboardDefaults");
        PreviewInputDefaults.Note.ShouldContain("UseGamepadDefaults");
    }
}
