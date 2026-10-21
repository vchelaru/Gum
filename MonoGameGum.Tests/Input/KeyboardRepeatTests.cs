using Gum.Input;
using Moq;
using Shouldly;
using System;
using Xunit;
using GumKeys = Gum.Forms.Input.Keys;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace MonoGameGum.Tests.Input;

/// <summary>
/// Pins the held-key repeat timing of the MonoGame/KNI/FNA <see cref="Keyboard"/>.
/// </summary>
public class KeyboardRepeatTests
{
    private bool _leftDown;
    private bool _leftPushed;

    private Keyboard CreateKeyboard()
    {
        Mock<KeyboardStateProcessor> processor = new Mock<KeyboardStateProcessor>();
        processor.Setup(x => x.IsKeyDown(XnaKeys.Left)).Returns(() => _leftDown);
        processor.Setup(x => x.KeyPushed(XnaKeys.Left)).Returns(() => _leftPushed);

        Keyboard keyboard = new Keyboard();
        keyboard.KeyboardStateProcessor = processor.Object;
        return keyboard;
    }

    private void PressLeft(Keyboard keyboard, double time)
    {
        _leftDown = true;
        _leftPushed = true;
        keyboard.Activity(time);
    }

    private void HoldLeft(Keyboard keyboard, double time)
    {
        _leftDown = true;
        _leftPushed = false;
        keyboard.Activity(time);
    }

    [Fact]
    public void RepeatDelayAndRate_ByDefault_ShouldBe500And70Milliseconds()
    {
        Keyboard keyboard = new Keyboard();

        keyboard.RepeatDelay.ShouldBe(TimeSpan.FromMilliseconds(500));
        keyboard.RepeatRate.ShouldBe(TimeSpan.FromMilliseconds(70));
    }

    [Fact]
    public void KeyTyped_OnPush_ShouldBeTrue()
    {
        Keyboard keyboard = CreateKeyboard();

        PressLeft(keyboard, 0);

        keyboard.KeyTyped(GumKeys.Left).ShouldBeTrue();
    }

    [Fact]
    public void KeyTyped_HeldBeforeRepeatDelay_ShouldBeFalse()
    {
        Keyboard keyboard = CreateKeyboard();
        PressLeft(keyboard, 0);

        HoldLeft(keyboard, 0.4);

        keyboard.KeyTyped(GumKeys.Left).ShouldBeFalse();
    }

    [Fact]
    public void KeyTyped_HeldPastRepeatDelay_ShouldBeTrueThenRepeatAtRepeatRate()
    {
        Keyboard keyboard = CreateKeyboard();
        PressLeft(keyboard, 0);

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
        Keyboard keyboard = CreateKeyboard();
        PressLeft(keyboard, 0);
        HoldLeft(keyboard, 0.51);

        keyboard.KeyTyped(GumKeys.Left).ShouldBeTrue();
        keyboard.KeyTyped(GumKeys.Left).ShouldBeTrue();
    }

    [Fact]
    public void KeyTyped_WithCustomDelayAndRate_ShouldUseThem()
    {
        Keyboard keyboard = CreateKeyboard();
        keyboard.RepeatDelay = TimeSpan.FromMilliseconds(200);
        keyboard.RepeatRate = TimeSpan.FromMilliseconds(30);
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
}
