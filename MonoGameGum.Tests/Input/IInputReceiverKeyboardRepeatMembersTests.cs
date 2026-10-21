using Gum.Input;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;
using GumKeys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Input;

/// <summary>
/// Tests the <see cref="IInputReceiverKeyboard.RepeatDelay"/> and <see cref="IInputReceiverKeyboard.RepeatRate"/>
/// default interface members and that the concrete keyboards implement them with their own values.
/// </summary>
public class IInputReceiverKeyboardRepeatMembersTests
{
    private sealed class FakePolledKeyboard : PolledKeyboard
    {
        public readonly HashSet<GumKeys> DeviceDown = new();

        protected override IEnumerable<GumKeys> SupportedKeys => new[] { GumKeys.Left };

        protected override bool IsDeviceKeyDown(GumKeys key) => DeviceDown.Contains(key);
    }

    private static Mock<IInputReceiverKeyboard> CreateMinimalKeyboard()
    {
        // CallBase makes Moq run the interface's default implementations, as a third-party
        // implementer that predates the members would get.
        return new Mock<IInputReceiverKeyboard> { CallBase = true };
    }

    [Fact]
    public void RepeatDelayAndRate_OnImplementerWithoutThem_ShouldDefaultTo500And70Milliseconds()
    {
        Mock<IInputReceiverKeyboard> keyboard = CreateMinimalKeyboard();

        keyboard.Object.RepeatDelay.ShouldBe(TimeSpan.FromMilliseconds(500));
        keyboard.Object.RepeatRate.ShouldBe(TimeSpan.FromMilliseconds(70));
    }

    [Fact]
    public void SettingRepeatDelayAndRate_OnImplementerWithoutThem_ShouldNotThrowAndKeepDefaults()
    {
        Mock<IInputReceiverKeyboard> keyboard = CreateMinimalKeyboard();

        keyboard.Object.RepeatDelay = TimeSpan.FromMilliseconds(200);
        keyboard.Object.RepeatRate = TimeSpan.FromMilliseconds(30);

        keyboard.Object.RepeatDelay.ShouldBe(TimeSpan.FromMilliseconds(500));
        keyboard.Object.RepeatRate.ShouldBe(TimeSpan.FromMilliseconds(70));
    }

    [Fact]
    public void Keyboard_ThroughTheInterface_ShouldReadAndWriteItsOwnValues()
    {
        Keyboard keyboard = new Keyboard();
        IInputReceiverKeyboard asInterface = keyboard;

        asInterface.RepeatDelay.ShouldBe(TimeSpan.FromMilliseconds(500));
        asInterface.RepeatRate.ShouldBe(TimeSpan.FromMilliseconds(70));

        asInterface.RepeatDelay = TimeSpan.FromMilliseconds(200);
        asInterface.RepeatRate = TimeSpan.FromMilliseconds(30);

        keyboard.RepeatDelay.ShouldBe(TimeSpan.FromMilliseconds(200));
        keyboard.RepeatRate.ShouldBe(TimeSpan.FromMilliseconds(30));
    }

    [Fact]
    public void PolledKeyboard_ThroughTheInterface_ShouldReadAndWriteItsOwnValues()
    {
        FakePolledKeyboard keyboard = new FakePolledKeyboard();
        IInputReceiverKeyboard asInterface = keyboard;

        asInterface.RepeatDelay.ShouldBe(TimeSpan.FromMilliseconds(500));
        asInterface.RepeatRate.ShouldBe(TimeSpan.FromMilliseconds(70));

        asInterface.RepeatDelay = TimeSpan.FromMilliseconds(200);
        asInterface.RepeatRate = TimeSpan.FromMilliseconds(30);

        keyboard.RepeatDelay.ShouldBe(TimeSpan.FromMilliseconds(200));
        keyboard.RepeatRate.ShouldBe(TimeSpan.FromMilliseconds(30));
    }

    [Fact]
    public void PolledKeyboard_SetThroughTheInterface_ShouldDriveKeyTypedRepeat()
    {
        FakePolledKeyboard keyboard = new FakePolledKeyboard();
        IInputReceiverKeyboard asInterface = keyboard;
        asInterface.RepeatDelay = TimeSpan.FromMilliseconds(200);
        keyboard.DeviceDown.Add(GumKeys.Left);
        keyboard.Activity(0);

        keyboard.Activity(0.25);

        keyboard.KeyTyped(GumKeys.Left).ShouldBeTrue();
    }

    [Fact]
    public void PolledKeyboard_KeyTypedAskedTwiceInTheSameRepeatFrame_ShouldAnswerTheSame()
    {
        FakePolledKeyboard keyboard = new FakePolledKeyboard();
        keyboard.DeviceDown.Add(GumKeys.Left);
        keyboard.Activity(0);
        keyboard.Activity(0.51);

        keyboard.KeyTyped(GumKeys.Left).ShouldBeTrue();
        keyboard.KeyTyped(GumKeys.Left).ShouldBeTrue();
    }
}
