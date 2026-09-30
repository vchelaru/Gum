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

        protected override bool IsDeviceKeyDown(GumKeys key) => DeviceDown.Contains(key);

        public void Type(string text) => AppendTypedText(text);
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
}
