using Gum.Input;
using Gum.Wireframe;
using Moq;
using Moq.Protected;
using Raylib_cs;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;
using GumKeys = Gum.Forms.Input.Keys;

namespace RaylibGum.Tests.Inputs;

/// <summary>
/// Raylib reports key presses but its repeat comes from the OS. Gum times held-key repeat itself from
/// <see cref="IInputReceiverKeyboard.RepeatDelay"/> and <see cref="IInputReceiverKeyboard.RepeatRate"/>.
/// </summary>
public class KeyboardRepeatTests : BaseTestClass
{
    private bool _leftDown;
    private bool _leftPressed;
    private bool _leftOsRepeat;

    private Mock<Keyboard> CreateKeyboard()
    {
        Mock<Keyboard> sut = new Mock<Keyboard>();
        sut.Protected()
            .Setup<bool>("IsKeyDown", ItExpr.IsAny<KeyboardKey>())
            .Returns((KeyboardKey k) => k == KeyboardKey.Left && _leftDown);
        sut.Protected()
            .Setup<bool>("IsKeyPressed", ItExpr.IsAny<KeyboardKey>())
            .Returns((KeyboardKey k) => k == KeyboardKey.Left && _leftPressed);
        sut.Protected()
            .Setup<bool>("IsKeyPressedRepeat", ItExpr.IsAny<KeyboardKey>())
            .Returns((KeyboardKey k) => k == KeyboardKey.Left && _leftOsRepeat);
        sut.Protected()
            .Setup<int>("GetCharPressed")
            .Returns(0);
        return sut;
    }

    private void PressLeft(Keyboard keyboard, double time)
    {
        _leftDown = true;
        _leftPressed = true;
        _leftOsRepeat = false;
        keyboard.Activity(time);
    }

    private void HoldLeft(Keyboard keyboard, double time)
    {
        _leftDown = true;
        _leftPressed = false;
        _leftOsRepeat = false;
        keyboard.Activity(time);
    }

    [Fact]
    public void RepeatDelayAndRate_ByDefault_ShouldBe500And70Milliseconds()
    {
        Keyboard keyboard = CreateKeyboard().Object;

        keyboard.RepeatDelay.ShouldBe(TimeSpan.FromMilliseconds(500));
        keyboard.RepeatRate.ShouldBe(TimeSpan.FromMilliseconds(70));
        ((IInputReceiverKeyboard)keyboard).RepeatDelay.ShouldBe(TimeSpan.FromMilliseconds(500));
        ((IInputReceiverKeyboard)keyboard).RepeatRate.ShouldBe(TimeSpan.FromMilliseconds(70));
    }

    [Fact]
    public void KeyTyped_HeldBeforeRepeatDelay_ShouldBeFalse()
    {
        Keyboard keyboard = CreateKeyboard().Object;
        PressLeft(keyboard, 0);

        HoldLeft(keyboard, 0.4);

        keyboard.KeyTyped(GumKeys.Left).ShouldBeFalse();
    }

    [Fact]
    public void KeyTyped_HeldPastRepeatDelay_ShouldBeTrueThenRepeatAtRepeatRate()
    {
        Keyboard keyboard = CreateKeyboard().Object;
        PressLeft(keyboard, 0);
        keyboard.KeyTyped(GumKeys.Left).ShouldBeTrue();

        HoldLeft(keyboard, 0.51);
        keyboard.KeyTyped(GumKeys.Left).ShouldBeTrue();

        HoldLeft(keyboard, 0.55);
        keyboard.KeyTyped(GumKeys.Left).ShouldBeFalse();

        HoldLeft(keyboard, 0.59);
        keyboard.KeyTyped(GumKeys.Left).ShouldBeTrue();
    }

    [Fact]
    public void KeyTyped_AskedTwiceInTheSameFrame_ShouldAnswerTheSame()
    {
        Keyboard keyboard = CreateKeyboard().Object;
        PressLeft(keyboard, 0);
        HoldLeft(keyboard, 0.51);

        keyboard.KeyTyped(GumKeys.Left).ShouldBeTrue();
        keyboard.KeyTyped(GumKeys.Left).ShouldBeTrue();
        ((IInputReceiverKeyboard)keyboard).KeysTyped.ShouldContain(GumKeys.Left);
    }

    [Fact]
    public void KeyTyped_WithCustomDelayAndRate_ShouldUseThem()
    {
        Keyboard keyboard = CreateKeyboard().Object;
        IInputReceiverKeyboard asInterface = keyboard;
        asInterface.RepeatDelay = TimeSpan.FromMilliseconds(200);
        asInterface.RepeatRate = TimeSpan.FromMilliseconds(30);
        PressLeft(keyboard, 0);

        HoldLeft(keyboard, 0.15);
        keyboard.KeyTyped(GumKeys.Left).ShouldBeFalse();

        HoldLeft(keyboard, 0.21);
        keyboard.KeyTyped(GumKeys.Left).ShouldBeTrue();

        HoldLeft(keyboard, 0.23);
        keyboard.KeyTyped(GumKeys.Left).ShouldBeFalse();

        HoldLeft(keyboard, 0.25);
        keyboard.KeyTyped(GumKeys.Left).ShouldBeTrue();
    }

    [Fact]
    public void KeyTyped_KeyAlreadyHeldWhenFirstSeen_ShouldRepeatAfterRepeatDelay()
    {
        Keyboard keyboard = CreateKeyboard().Object;

        // The press frame was missed (or the key was held at startup): down, but never "pressed".
        HoldLeft(keyboard, 0);
        HoldLeft(keyboard, 0.4);
        keyboard.KeyTyped(GumKeys.Left).ShouldBeFalse();

        HoldLeft(keyboard, 0.51);
        keyboard.KeyTyped(GumKeys.Left).ShouldBeTrue();

        HoldLeft(keyboard, 0.59);
        keyboard.KeyTyped(GumKeys.Left).ShouldBeTrue();
    }

    [Fact]
    public void KeyTyped_WhenOsReportsARepeat_ShouldIgnoreItAndFollowGumTiming()
    {
        Keyboard keyboard = CreateKeyboard().Object;
        PressLeft(keyboard, 0);

        _leftDown = true;
        _leftPressed = false;
        _leftOsRepeat = true;
        keyboard.Activity(0.1);

        keyboard.KeyTyped(GumKeys.Left).ShouldBeFalse();
    }

    [Fact]
    public void KeyTyped_ReleasedAndPressedAgain_ShouldWaitForRepeatDelayAgain()
    {
        Keyboard keyboard = CreateKeyboard().Object;
        PressLeft(keyboard, 0);
        HoldLeft(keyboard, 0.6);

        _leftDown = false;
        _leftPressed = false;
        keyboard.Activity(0.7);
        PressLeft(keyboard, 0.8);
        HoldLeft(keyboard, 1.0);

        keyboard.KeyTyped(GumKeys.Left).ShouldBeFalse();
    }

    [Fact]
    public void GetStringTyped_ShouldNotDependOnRepeatTiming()
    {
        Mock<Keyboard> sut = CreateKeyboard();
        sut.Object.RepeatDelay = TimeSpan.FromSeconds(10);
        Queue<int> codepoints = new(new[] { 97, 0 });
        sut.Protected()
            .Setup<int>("GetCharPressed")
            .Returns(() => codepoints.Count > 0 ? codepoints.Dequeue() : 0);

        sut.Object.Activity(0.9);

        sut.Object.GetStringTyped().ShouldBe("a");
    }
}
