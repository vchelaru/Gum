using Gum.Input;
using Moq;
using Shouldly;
using Silk.NET.Input;
using System.Collections.Generic;
using GumKeys = Gum.Forms.Input.Keys;

namespace SilkNetGum.Tests;

/// <summary>
/// Unit tests for the Silk keyboard: the GumKeys->Silk Key translation table, frame-over-frame
/// push/release edge detection, and KeyChar-driven typed-text buffering. Edge/translation tests
/// drive the <c>IsKeyPressed</c> seam via <see cref="TestableKeyboard"/> so no live Silk device is
/// needed; the typed-text test raises the real <see cref="IKeyboard.KeyChar"/> event through Moq.
/// </summary>
public class KeyboardSilkTests
{
    /// <summary>Exposes the protected <c>IsKeyPressed</c> seam so tests set the polled down-state.</summary>
    private sealed class TestableKeyboard : Keyboard
    {
        public readonly HashSet<Key> PressedKeys = new();
        protected override bool IsKeyPressed(Key key) => PressedKeys.Contains(key);
    }

    [Fact]
    public void Activity_TranslatesSilkKeyToGumKey_ForDownState()
    {
        var keyboard = new TestableKeyboard();
        keyboard.PressedKeys.Add(Key.Left);

        keyboard.Activity(0);

        keyboard.KeyDown(GumKeys.Left).ShouldBeTrue();
        keyboard.KeyDown(GumKeys.Right).ShouldBeFalse();
    }

    [Fact]
    public void GetStringTyped_ReturnsCharactersFromKeyCharEvent()
    {
        var device = new Mock<IKeyboard>();
        var keyboard = new Keyboard(device.Object);

        device.Raise(k => k.KeyChar += null, device.Object, 'h');
        device.Raise(k => k.KeyChar += null, device.Object, 'i');
        keyboard.Activity(0);

        keyboard.GetStringTyped().ShouldBe("hi");
    }

    [Fact]
    public void KeyTyped_DoesNotRepeatBeforeRepeatDelayElapses()
    {
        var keyboard = new TestableKeyboard();
        keyboard.PressedKeys.Add(Key.A);

        keyboard.Activity(0);
        keyboard.KeyTyped(GumKeys.A).ShouldBeTrue(); // initial push

        keyboard.Activity(keyboard.RepeatDelay.TotalSeconds - 0.01);
        keyboard.KeyTyped(GumKeys.A).ShouldBeFalse();
    }

    [Fact]
    public void KeyTyped_RepeatsAtRepeatRateAfterRepeatDelayElapses()
    {
        var keyboard = new TestableKeyboard();
        keyboard.PressedKeys.Add(Key.A);

        keyboard.Activity(0);
        keyboard.KeyTyped(GumKeys.A).ShouldBeTrue(); // initial push

        keyboard.Activity(keyboard.RepeatDelay.TotalSeconds);
        keyboard.KeyTyped(GumKeys.A).ShouldBeTrue(); // first repeat

        keyboard.Activity(keyboard.RepeatDelay.TotalSeconds + keyboard.RepeatRate.TotalSeconds - 0.01);
        keyboard.KeyTyped(GumKeys.A).ShouldBeFalse(); // too soon for the next repeat

        keyboard.Activity(keyboard.RepeatDelay.TotalSeconds + keyboard.RepeatRate.TotalSeconds);
        keyboard.KeyTyped(GumKeys.A).ShouldBeTrue(); // second repeat
    }

    [Fact]
    public void KeyTyped_ResetsRepeatTimingAfterKeyIsReleasedAndPressedAgain()
    {
        var keyboard = new TestableKeyboard();
        keyboard.PressedKeys.Add(Key.A);
        keyboard.Activity(0);
        keyboard.Activity(keyboard.RepeatDelay.TotalSeconds); // would repeat if still held

        keyboard.PressedKeys.Remove(Key.A);
        keyboard.Activity(keyboard.RepeatDelay.TotalSeconds + 0.01);

        keyboard.PressedKeys.Add(Key.A);
        keyboard.Activity(keyboard.RepeatDelay.TotalSeconds + 0.02);

        keyboard.KeyTyped(GumKeys.A).ShouldBeTrue(); // fresh push, not a stale repeat
    }

    [Fact]
    public void KeyPushed_IsTrueOnlyOnTheFrameOfInitialPress()
    {
        var keyboard = new TestableKeyboard();
        keyboard.PressedKeys.Add(Key.A);

        keyboard.Activity(0);
        keyboard.KeyPushed(GumKeys.A).ShouldBeTrue();

        // Still held on the next frame -> no longer a fresh push.
        keyboard.Activity(1);
        keyboard.KeyPushed(GumKeys.A).ShouldBeFalse();
        keyboard.KeyDown(GumKeys.A).ShouldBeTrue();
    }

    [Fact]
    public void KeyReleased_IsTrueOnTheFrameAfterRelease()
    {
        var keyboard = new TestableKeyboard();
        keyboard.PressedKeys.Add(Key.Space);
        keyboard.Activity(0);

        keyboard.PressedKeys.Remove(Key.Space);
        keyboard.Activity(1);

        keyboard.KeyReleased(GumKeys.Space).ShouldBeTrue();
        keyboard.KeyDown(GumKeys.Space).ShouldBeFalse();
    }
}
