using Gum.Input;
using Moq;
using Shouldly;
using Stride.Core.Collections;
using Stride.Input;
using System.Collections.Generic;
using GumKeys = Gum.Forms.Input.Keys;
using StrideKeys = Stride.Input.Keys;

namespace StrideGum.Tests;

/// <summary>
/// Unit tests for the Stride keyboard: the GumKeys->Stride Keys translation table, held-key repeat
/// timing, and TextInputEvent-driven typed-text buffering. Unlike Silk (which only exposes a live
/// down-poll and derives push/release edges itself), Stride's <see cref="IKeyboardDevice"/> already
/// reports per-frame Pressed/Released/Down sets directly, so the mock supplies those three sets per
/// simulated frame instead of a single continuous down-state.
/// </summary>
public class KeyboardStrideTests
{
    private static (Keyboard keyboard, Mock<IKeyboardDevice> device) CreateAttachedKeyboard()
    {
        var device = new Mock<IKeyboardDevice>();
        SetFrameState(device);
        var keyboard = new Keyboard(device.Object, new InputManager());
        return (keyboard, device);
    }

    private static void SetFrameState(
        Mock<IKeyboardDevice> device,
        IEnumerable<StrideKeys>? pressed = null,
        IEnumerable<StrideKeys>? released = null,
        IEnumerable<StrideKeys>? down = null)
    {
        device.SetupGet(d => d.PressedKeys).Returns(new ReadOnlySet<StrideKeys>(new HashSet<StrideKeys>(pressed ?? new StrideKeys[0])));
        device.SetupGet(d => d.ReleasedKeys).Returns(new ReadOnlySet<StrideKeys>(new HashSet<StrideKeys>(released ?? new StrideKeys[0])));
        device.SetupGet(d => d.DownKeys).Returns(new ReadOnlySet<StrideKeys>(new HashSet<StrideKeys>(down ?? new StrideKeys[0])));
    }

    [Fact]
    public void Activity_TranslatesStrideKeyToGumKey_ForDownState()
    {
        (Keyboard keyboard, Mock<IKeyboardDevice> device) = CreateAttachedKeyboard();
        SetFrameState(device, down: new[] { StrideKeys.Left });

        keyboard.Activity(0);

        keyboard.KeyDown(GumKeys.Left).ShouldBeTrue();
        keyboard.KeyDown(GumKeys.Right).ShouldBeFalse();
    }

    [Fact]
    public void GetStringTyped_ReturnsCharactersFromTextInputEvents()
    {
        (Keyboard keyboard, _) = CreateAttachedKeyboard();

        ((IInputEventListener<TextInputEvent>)keyboard).ProcessEvent(new TextInputEvent { Text = "h", Type = TextInputEventType.Input });
        ((IInputEventListener<TextInputEvent>)keyboard).ProcessEvent(new TextInputEvent { Text = "i", Type = TextInputEventType.Input });
        keyboard.Activity(0);

        keyboard.GetStringTyped().ShouldBe("hi");
    }

    [Fact]
    public void GetStringTyped_IgnoresCompositionEvents()
    {
        (Keyboard keyboard, _) = CreateAttachedKeyboard();

        ((IInputEventListener<TextInputEvent>)keyboard).ProcessEvent(new TextInputEvent { Text = "IME draft", Type = TextInputEventType.Composition });
        keyboard.Activity(0);

        keyboard.GetStringTyped().ShouldBe("");
    }

    [Fact]
    public void KeyTyped_DoesNotRepeatBeforeRepeatDelayElapses()
    {
        (Keyboard keyboard, Mock<IKeyboardDevice> device) = CreateAttachedKeyboard();
        SetFrameState(device, pressed: new[] { StrideKeys.A }, down: new[] { StrideKeys.A });

        keyboard.Activity(0);
        keyboard.KeyTyped(GumKeys.A).ShouldBeTrue(); // initial push

        SetFrameState(device, down: new[] { StrideKeys.A });
        keyboard.Activity(keyboard.RepeatDelay.TotalSeconds - 0.01);
        keyboard.KeyTyped(GumKeys.A).ShouldBeFalse();
    }

    [Fact]
    public void KeyTyped_RepeatsAtRepeatRateAfterRepeatDelayElapses()
    {
        (Keyboard keyboard, Mock<IKeyboardDevice> device) = CreateAttachedKeyboard();
        SetFrameState(device, pressed: new[] { StrideKeys.A }, down: new[] { StrideKeys.A });

        keyboard.Activity(0);
        keyboard.KeyTyped(GumKeys.A).ShouldBeTrue(); // initial push

        SetFrameState(device, down: new[] { StrideKeys.A });
        keyboard.Activity(keyboard.RepeatDelay.TotalSeconds);
        keyboard.KeyTyped(GumKeys.A).ShouldBeTrue(); // first repeat

        keyboard.Activity(keyboard.RepeatDelay.TotalSeconds + keyboard.RepeatRate.TotalSeconds - 0.01);
        keyboard.KeyTyped(GumKeys.A).ShouldBeFalse(); // too soon for the next repeat

        keyboard.Activity(keyboard.RepeatDelay.TotalSeconds + keyboard.RepeatRate.TotalSeconds);
        keyboard.KeyTyped(GumKeys.A).ShouldBeTrue(); // second repeat
    }

    [Fact]
    public void KeyPushed_IsTrueOnlyOnTheFrameOfInitialPress()
    {
        (Keyboard keyboard, Mock<IKeyboardDevice> device) = CreateAttachedKeyboard();
        SetFrameState(device, pressed: new[] { StrideKeys.A }, down: new[] { StrideKeys.A });

        keyboard.Activity(0);
        keyboard.KeyPushed(GumKeys.A).ShouldBeTrue();

        // Still held on the next frame -- Stride no longer reports it in PressedKeys.
        SetFrameState(device, down: new[] { StrideKeys.A });
        keyboard.Activity(1);
        keyboard.KeyPushed(GumKeys.A).ShouldBeFalse();
        keyboard.KeyDown(GumKeys.A).ShouldBeTrue();
    }

    [Fact]
    public void KeyReleased_IsTrueOnTheFrameOfRelease()
    {
        (Keyboard keyboard, Mock<IKeyboardDevice> device) = CreateAttachedKeyboard();
        SetFrameState(device, pressed: new[] { StrideKeys.Space }, down: new[] { StrideKeys.Space });
        keyboard.Activity(0);

        SetFrameState(device, released: new[] { StrideKeys.Space });
        keyboard.Activity(1);

        keyboard.KeyReleased(GumKeys.Space).ShouldBeTrue();
        keyboard.KeyDown(GumKeys.Space).ShouldBeFalse();
    }

    [Fact]
    public void DeviceLessKeyboard_DoesNotThrow_OnActivityOrQueries()
    {
        var keyboard = new Gum.Input.Keyboard();

        Should.NotThrow(() => keyboard.Activity(0));
        keyboard.KeyDown(GumKeys.A).ShouldBeFalse();
        keyboard.GetStringTyped().ShouldBe("");
    }
}
