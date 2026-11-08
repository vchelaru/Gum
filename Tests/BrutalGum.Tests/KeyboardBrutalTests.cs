using Brutal.GlfwApi;
using Gum.Input;
using Shouldly;
using GumKeys = Gum.Forms.Input.Keys;

namespace BrutalGum.Tests;

/// <summary>Down state from the window, edge detection, and typed text from the character callback.</summary>
public class KeyboardBrutalTests : SkiaHostTests.PolledKeyboardTestsBase
{
    private sealed class BrutalKeyboardRig : SkiaHostTests.IHostKeyboardRig
    {
        private readonly FakeBrutalInput _input = new();

        public BrutalKeyboardRig()
        {
            Keyboard = new Gum.Input.Keyboard(_input);
        }

        public PolledKeyboard Keyboard { get; }

        public void SetHeld(params GumKeys[] keys)
        {
            _input.HeldKeys.Clear();
            foreach (GumKeys key in keys)
            {
                _input.HeldKeys.Add(Gum.Input.Keyboard.GumToGlfw[key]);
            }
        }

        public void BeginFrame()
        {
            // GLFW only exposes the live down state; there are no per-frame sets to drop.
        }

        public void TypeText(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                uint codepoint = (uint)char.ConvertToUtf32(text, i);
                if (char.IsHighSurrogate(text[i]))
                {
                    i++;
                }
                _input.RaiseChar(codepoint);
            }
        }
    }

    protected override SkiaHostTests.IHostKeyboardRig CreateRig() => new BrutalKeyboardRig();

    [Fact]
    public void TypedText_SupplementaryCodePoint_ArrivesAsSurrogatePair()
    {
        FakeBrutalInput input = new();
        Gum.Input.Keyboard keyboard = new(input);

        input.RaiseChar(0x1F600);
        keyboard.Activity(0);

        keyboard.GetStringTyped().ShouldBe("\U0001F600");
    }

    [Fact]
    public void TypedText_LoneSurrogate_IsDropped()
    {
        FakeBrutalInput input = new();
        Gum.Input.Keyboard keyboard = new(input);

        input.RaiseChar(0xD800);
        keyboard.Activity(0);

        keyboard.GetStringTyped().ShouldBeEmpty();
    }

    [Fact]
    public void Dispose_UnsubscribesFromChar()
    {
        FakeBrutalInput input = new();
        Gum.Input.Keyboard keyboard = new(input);

        keyboard.Dispose();

        input.CharSubscribers.ShouldBe(0);
    }
}
