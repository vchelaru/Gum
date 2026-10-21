using Gum.Input;
using Gum.Wireframe;
using Shouldly;
using System.Collections.Generic;
using Xunit;
using GumKeys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Input;

/// <summary>
/// Tests for <see cref="PolledKeyboard"/>, the base the Silk.NET and Stride keyboards share. A fake
/// host sets which keys its device reports as down; the base derives edges, repeat and typed text.
/// </summary>
public class PolledKeyboardTests
{
    private sealed class FakeHostKeyboard : PolledKeyboard
    {
        public readonly HashSet<GumKeys> DeviceDown = new();

        protected override IEnumerable<GumKeys> SupportedKeys => new[] { GumKeys.A, GumKeys.B, GumKeys.Left };

        protected override bool IsDeviceKeyDown(GumKeys key)
        {
            KeyQueriesSinceBeginPoll++;
            return DeviceDown.Contains(key);
        }

        public void Type(string text) => AppendTypedText(text);

        public void Type(char character) => AppendTypedText(character);

        public int BeginPollCount;

        public int KeyQueriesSinceBeginPoll;

        protected override void BeginPoll()
        {
            BeginPollCount++;
            KeyQueriesSinceBeginPoll = 0;
        }
    }

    [Fact]
    public void Activity_DerivesPushAndReleaseEdgesFromDownState()
    {
        var keyboard = new FakeHostKeyboard();

        keyboard.DeviceDown.Add(GumKeys.A);
        keyboard.Activity(0);
        keyboard.KeyPushed(GumKeys.A).ShouldBeTrue();
        keyboard.KeyDown(GumKeys.A).ShouldBeTrue();

        keyboard.Activity(0.1);
        keyboard.KeyPushed(GumKeys.A).ShouldBeFalse();
        keyboard.KeyDown(GumKeys.A).ShouldBeTrue();

        keyboard.DeviceDown.Clear();
        keyboard.Activity(0.2);
        keyboard.KeyReleased(GumKeys.A).ShouldBeTrue();
        keyboard.KeyDown(GumKeys.A).ShouldBeFalse();
    }

    [Fact]
    public void KeysTyped_RepeatsHeldKeyAfterRepeatDelay()
    {
        var keyboard = new FakeHostKeyboard();
        IInputReceiverKeyboard asInterface = keyboard;
        keyboard.DeviceDown.Add(GumKeys.Left);

        keyboard.Activity(0);
        asInterface.KeysTyped.ShouldBe(new[] { GumKeys.Left });

        keyboard.Activity(keyboard.RepeatDelay.TotalSeconds - 0.01);
        asInterface.KeysTyped.ShouldBeEmpty();

        keyboard.Activity(keyboard.RepeatDelay.TotalSeconds + 0.01);
        asInterface.KeysTyped.ShouldBe(new[] { GumKeys.Left });
    }

    [Fact]
    public void GetStringTyped_ReturnsTextAppendedBeforeThisFrameOnly()
    {
        var keyboard = new FakeHostKeyboard();

        keyboard.Type("h");
        keyboard.Type("i");
        keyboard.Activity(0);
        keyboard.GetStringTyped().ShouldBe("hi");

        keyboard.Activity(0.1);
        keyboard.GetStringTyped().ShouldBe("");
    }

    [Fact]
    public void GetStringTyped_DropsControlCharactersAndKeepsPrintableOnes()
    {
        var keyboard = new FakeHostKeyboard();

        // Backspace, Enter and Ctrl+A are reported as characters by some hosts. TextBox handles
        // those through the keys, so they must not land in the text.
        keyboard.Type('h');
        keyboard.Type('\b');
        keyboard.Type('\r');
        keyboard.Type((char)1);
        keyboard.Type("i\u001d ");
        keyboard.Activity(0);

        keyboard.GetStringTyped().ShouldBe("hi ");
    }

    [Fact]
    public void Activity_CallsBeginPollOncePerFrameBeforeQueryingKeys()
    {
        var keyboard = new FakeHostKeyboard();

        keyboard.Activity(0);

        keyboard.BeginPollCount.ShouldBe(1);
        // All three supported keys were queried after the single BeginPoll, not before it.
        keyboard.KeyQueriesSinceBeginPoll.ShouldBe(3);

        keyboard.Activity(0.1);

        keyboard.BeginPollCount.ShouldBe(2);
    }

    [Fact]
    public void RepeatDelayAndRate_ByDefault_ShouldBe500And70Milliseconds()
    {
        var keyboard = new FakeHostKeyboard();

        keyboard.RepeatDelay.ShouldBe(System.TimeSpan.FromMilliseconds(500));
        keyboard.RepeatRate.ShouldBe(System.TimeSpan.FromMilliseconds(70));
    }

    [Fact]
    public void KeyTyped_HeldPastRepeatDelay_ShouldRepeatAtRepeatRate()
    {
        var keyboard = new FakeHostKeyboard();
        keyboard.DeviceDown.Add(GumKeys.Left);
        keyboard.Activity(0);
        keyboard.KeyTyped(GumKeys.Left).ShouldBeTrue();

        keyboard.Activity(0.4);
        keyboard.KeyTyped(GumKeys.Left).ShouldBeFalse();

        keyboard.Activity(0.51);
        keyboard.KeyTyped(GumKeys.Left).ShouldBeTrue();

        keyboard.Activity(0.55);
        keyboard.KeyTyped(GumKeys.Left).ShouldBeFalse();

        keyboard.Activity(0.59);
        keyboard.KeyTyped(GumKeys.Left).ShouldBeTrue();
    }

    [Fact]
    public void KeyTyped_WithCustomDelayAndRate_ShouldUseThem()
    {
        var keyboard = new FakeHostKeyboard();
        keyboard.RepeatDelay = System.TimeSpan.FromMilliseconds(200);
        keyboard.RepeatRate = System.TimeSpan.FromMilliseconds(30);
        keyboard.DeviceDown.Add(GumKeys.Left);
        keyboard.Activity(0);

        keyboard.Activity(0.15);
        keyboard.KeyTyped(GumKeys.Left).ShouldBeFalse();

        keyboard.Activity(0.21);
        keyboard.KeyTyped(GumKeys.Left).ShouldBeTrue();

        keyboard.Activity(0.23);
        keyboard.KeyTyped(GumKeys.Left).ShouldBeFalse();

        keyboard.Activity(0.25);
        keyboard.KeyTyped(GumKeys.Left).ShouldBeTrue();
    }

    [Fact]
    public void KeyTyped_ReleasedAndPressedAgain_ShouldWaitForRepeatDelayAgain()
    {
        var keyboard = new FakeHostKeyboard();
        keyboard.DeviceDown.Add(GumKeys.Left);
        keyboard.Activity(0);
        keyboard.Activity(0.6);

        keyboard.DeviceDown.Clear();
        keyboard.Activity(0.7);
        keyboard.DeviceDown.Add(GumKeys.Left);
        keyboard.Activity(0.8);
        keyboard.KeyTyped(GumKeys.Left).ShouldBeTrue();

        keyboard.Activity(1.0);
        keyboard.KeyTyped(GumKeys.Left).ShouldBeFalse();
    }
}
