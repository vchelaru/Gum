using Gum.Input;
using Moq;
using Silk.NET.Input;
using System.Collections.Generic;
using GumKeys = Gum.Forms.Input.Keys;

namespace SilkNetGum.Tests;

/// <summary>
/// Unit tests for the Silk keyboard: down state translated from <see cref="IKeyboard.IsKeyPressed"/>,
/// frame-over-frame push/release edge detection, and KeyChar-driven typed-text buffering. The cases
/// live in <see cref="SkiaHostTests.PolledKeyboardTestsBase"/>; this class supplies a keyboard over a
/// mocked Silk device (a poll of the down state, and the real <see cref="IKeyboard.KeyChar"/> event).
/// </summary>
public class KeyboardSilkTests : SkiaHostTests.PolledKeyboardTestsBase
{
    private sealed class SilkKeyboardRig : SkiaHostTests.IHostKeyboardRig
    {
        private readonly Mock<IKeyboard> _device = new();
        private readonly HashSet<Key> _held = new();

        public SilkKeyboardRig()
        {
            _device.Setup(k => k.IsKeyPressed(It.IsAny<Key>())).Returns((Key key) => _held.Contains(key));
            Keyboard = new Keyboard(_device.Object);
        }

        public PolledKeyboard Keyboard { get; }

        public void SetHeld(params GumKeys[] keys)
        {
            _held.Clear();
            foreach (GumKeys key in keys)
            {
                _held.Add(ToSilk(key));
            }
        }

        public void BeginFrame()
        {
            // Silk only exposes the live down state; there are no per-frame sets to drop.
        }

        public void TypeText(string text)
        {
            foreach (char character in text)
            {
                _device.Raise(k => k.KeyChar += null, _device.Object, character);
            }
        }

        private static Key ToSilk(GumKeys key) => key switch
        {
            GumKeys.A => Key.A,
            GumKeys.Left => Key.Left,
            GumKeys.Right => Key.Right,
            GumKeys.Space => Key.Space,
            _ => throw new System.NotSupportedException(key.ToString()),
        };
    }

    protected override SkiaHostTests.IHostKeyboardRig CreateRig() => new SilkKeyboardRig();
}
