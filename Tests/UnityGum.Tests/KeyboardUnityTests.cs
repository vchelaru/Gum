using Gum.Input;
using Gum.Wireframe;
using Shouldly;
using Keys = Gum.Forms.Input.Keys;

namespace UnityGum.Tests;

/// <summary>
/// The pushed keyboard: the host reports held keys and typed characters, and <c>Activity</c> turns
/// them into this frame's push/release edges and text.
/// </summary>
public class KeyboardUnityTests
{
    [Fact]
    public void SetKeyDown_ReportsPushThenHoldThenRelease()
    {
        Keyboard keyboard = new Keyboard();

        keyboard.SetKeyDown(Keys.A, isDown: true);
        keyboard.Activity(0);
        keyboard.KeyPushed(Keys.A).ShouldBeTrue();
        keyboard.KeyDown(Keys.A).ShouldBeTrue();

        keyboard.Activity(0.016);
        keyboard.KeyPushed(Keys.A).ShouldBeFalse();
        keyboard.KeyDown(Keys.A).ShouldBeTrue();

        keyboard.SetKeyDown(Keys.A, isDown: false);
        keyboard.Activity(0.032);
        keyboard.KeyReleased(Keys.A).ShouldBeTrue();
        keyboard.KeyDown(Keys.A).ShouldBeFalse();
    }

    [Fact]
    public void KeysTyped_IncludesAPushedKey()
    {
        Keyboard keyboard = new Keyboard();
        IInputReceiverKeyboard receiver = keyboard;

        keyboard.SetKeyDown(Keys.Left, isDown: true);
        keyboard.Activity(0);

        receiver.KeysTyped.ShouldContain(Keys.Left);
    }

    [Fact]
    public void AddTypedText_KeepsPrintableCharactersAndDropsControlCharacters()
    {
        Keyboard keyboard = new Keyboard();

        keyboard.AddTypedText('h');
        keyboard.AddTypedText('\b');
        keyboard.AddTypedText('\r');
        keyboard.AddTypedText((char)1);
        keyboard.AddTypedText('i');
        keyboard.AddTypedText(' ');
        keyboard.Activity(0);

        keyboard.GetStringTyped().ShouldBe("hi ");

        keyboard.Activity(0.016);
        keyboard.GetStringTyped().ShouldBe("");
    }
}
