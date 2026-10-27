using Gum.Input;
using Gum.Unity;
using Shouldly;
using Keys = Gum.Forms.Input.Keys;

namespace UnityGum.Tests;

/// <summary>
/// The legacy Input Manager backend: UnityEngine.KeyCode values (held as integers) mapped onto Gum's
/// keyboard, which a project on "Input Manager (Old)" gets instead of the Input System path.
/// </summary>
public class LegacyKeyMapTests
{
    // Unity's KeyCode values, which are public and serialized, so they do not change.
    private const int KeyCodeA = 97;
    private const int KeyCodeAlpha0 = 48;
    private const int KeyCodeReturn = 13;
    private const int KeyCodeKeypadEnter = 271;
    private const int KeyCodeLeftArrow = 276;
    private const int KeyCodeLeftShift = 304;
    private const int KeyCodeF12 = 293;
    private const int KeyCodeKeypad9 = 265;

    private static Keyboard PushHeld(params int[] held)
    {
        Keyboard keyboard = new Keyboard();
        LegacyKeyMap.Push(keyboard, code => Array.IndexOf(held, code) >= 0);
        keyboard.Activity(0);
        return keyboard;
    }

    [Theory]
    [InlineData(KeyCodeA, Keys.A)]
    [InlineData(KeyCodeAlpha0, Keys.D0)]
    [InlineData(KeyCodeReturn, Keys.Enter)]
    [InlineData(KeyCodeKeypadEnter, Keys.Enter)]
    [InlineData(KeyCodeLeftArrow, Keys.Left)]
    [InlineData(KeyCodeLeftShift, Keys.LeftShift)]
    [InlineData(KeyCodeF12, Keys.F12)]
    [InlineData(KeyCodeKeypad9, Keys.NumPad9)]
    public void Push_HeldKeyCode_ReportsTheGumKeyPushed(int keyCode, Keys expected)
    {
        Keyboard keyboard = PushHeld(keyCode);

        keyboard.KeyPushed(expected).ShouldBeTrue();
        keyboard.KeyDown(expected).ShouldBeTrue();
    }

    [Fact]
    public void Push_NothingHeld_ReportsNoKeys()
    {
        Keyboard keyboard = PushHeld();

        keyboard.KeyPushed(Keys.A).ShouldBeFalse();
        keyboard.KeyDown(Keys.Enter).ShouldBeFalse();
    }

    [Fact]
    public void Push_OneOfTwoEnters_KeepsEnterDownWhenTheOtherIsUp()
    {
        Keyboard keyboard = new Keyboard();
        LegacyKeyMap.Push(keyboard, code => code == KeyCodeKeypadEnter);
        keyboard.Activity(0);

        keyboard.KeyDown(Keys.Enter).ShouldBeTrue();
    }

    [Fact]
    public void Push_KeyReleased_ReportsTheRelease()
    {
        Keyboard keyboard = new Keyboard();
        LegacyKeyMap.Push(keyboard, code => code == KeyCodeA);
        keyboard.Activity(0);

        LegacyKeyMap.Push(keyboard, code => false);
        keyboard.Activity(0.016);

        keyboard.KeyReleased(Keys.A).ShouldBeTrue();
        keyboard.KeyDown(Keys.A).ShouldBeFalse();
    }

    [Fact]
    public void Entries_HaveNoDuplicateKeyCodes()
    {
        LegacyKeyMap.Entries.Select(e => e.KeyCode).Distinct().Count().ShouldBe(LegacyKeyMap.Entries.Length);
    }

    [Fact]
    public void Entries_CoverEveryLetterDigitAndFunctionKey()
    {
        var gumKeys = LegacyKeyMap.Entries.Select(e => e.Gum).ToHashSet();

        for (Keys key = Keys.A; key <= Keys.Z; key++) gumKeys.ShouldContain(key);
        for (Keys key = Keys.D0; key <= Keys.D9; key++) gumKeys.ShouldContain(key);
        for (Keys key = Keys.F1; key <= Keys.F12; key++) gumKeys.ShouldContain(key);
    }
}
