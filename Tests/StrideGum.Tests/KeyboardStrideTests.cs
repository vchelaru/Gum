using Gum.Input;
using Moq;
using Shouldly;
using Stride.Core.Collections;
using Stride.Input;
using System.Collections.Generic;
using System.Linq;
using GumKeys = Gum.Forms.Input.Keys;
using StrideKeys = Stride.Input.Keys;

namespace StrideGum.Tests;

/// <summary>
/// Unit tests for the Stride keyboard: held-key repeat timing and TextInputEvent-driven typed-text
/// buffering. The cases every polled keyboard shares live in
/// <see cref="SkiaHostTests.PolledKeyboardTestsBase"/>. Unlike Silk (which only exposes a live
/// down-poll and derives push/release edges itself), Stride's <see cref="IKeyboardDevice"/> already
/// reports per-frame Pressed/Released/Down sets directly, so the rig supplies those three sets,
/// derived from the held keys, for each simulated frame.
/// </summary>
public class KeyboardStrideTests : SkiaHostTests.PolledKeyboardTestsBase
{
    private sealed class StrideKeyboardRig : SkiaHostTests.IHostKeyboardRig
    {
        private readonly Mock<IKeyboardDevice> _device = new();
        private HashSet<StrideKeys> _held = new();
        private bool _changedThisFrame;

        public StrideKeyboardRig()
        {
            Publish(new HashSet<StrideKeys>(), new HashSet<StrideKeys>(), _held);
            Keyboard = new Keyboard(_device.Object, new InputManager());
        }

        public PolledKeyboard Keyboard { get; }

        public void SetHeld(params GumKeys[] keys)
        {
            var next = new HashSet<StrideKeys>(keys.Select(ToStride));
            Publish(
                pressed: new HashSet<StrideKeys>(next.Where(k => !_held.Contains(k))),
                released: new HashSet<StrideKeys>(_held.Where(k => !next.Contains(k))),
                down: next);
            _held = next;
            _changedThisFrame = true;
        }

        public void BeginFrame()
        {
            if (_changedThisFrame)
            {
                _changedThisFrame = false;
                return;
            }

            // Stride reports a key as pressed or released only for the frame it changed.
            Publish(new HashSet<StrideKeys>(), new HashSet<StrideKeys>(), _held);
        }

        public void TypeText(string text)
        {
            foreach (char character in text)
            {
                ((IInputEventListener<TextInputEvent>)Keyboard).ProcessEvent(
                    new TextInputEvent { Text = character.ToString(), Type = TextInputEventType.Input });
            }
        }

        private void Publish(HashSet<StrideKeys> pressed, HashSet<StrideKeys> released, HashSet<StrideKeys> down)
        {
            _device.SetupGet(d => d.PressedKeys).Returns(new ReadOnlySet<StrideKeys>(pressed));
            _device.SetupGet(d => d.ReleasedKeys).Returns(new ReadOnlySet<StrideKeys>(released));
            _device.SetupGet(d => d.DownKeys).Returns(new ReadOnlySet<StrideKeys>(down));
        }

        private static StrideKeys ToStride(GumKeys key) => key switch
        {
            GumKeys.A => StrideKeys.A,
            GumKeys.Left => StrideKeys.Left,
            GumKeys.Right => StrideKeys.Right,
            GumKeys.Space => StrideKeys.Space,
            _ => throw new System.NotSupportedException(key.ToString()),
        };
    }

    protected override SkiaHostTests.IHostKeyboardRig CreateRig() => new StrideKeyboardRig();

    [Fact]
    public void GetStringTyped_IgnoresCompositionEvents()
    {
        var rig = (StrideKeyboardRig)CreateRig();

        ((IInputEventListener<TextInputEvent>)rig.Keyboard).ProcessEvent(new TextInputEvent { Text = "IME draft", Type = TextInputEventType.Composition });
        rig.Keyboard.Activity(0);

        rig.Keyboard.GetStringTyped().ShouldBe("");
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
