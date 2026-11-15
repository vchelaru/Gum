using System;
using System.Collections.Generic;
using System.Linq;
using GumKeys = Gum.Forms.Input.Keys;

namespace Gum.Input;

/// <summary>
/// Keyboard for BRUTAL hosts. Down state is polled from <see cref="IBrutalInput.IsKeyDown"/> and
/// <see cref="PolledKeyboard"/> derives push/release edges and key repeat from it. Typed text comes from
/// <see cref="IBrutalInput.CharTyped"/>.
/// </summary>
public class Keyboard : PolledKeyboard
{
    // Every key is asked of the host each frame; a key the host has no equivalent for reads as not held.
    private static readonly GumKeys[] _polledKeys = Enum.GetValues<GumKeys>().Distinct().ToArray();

    private readonly IBrutalInput? _input;

    internal Keyboard(IBrutalInput input)
    {
        _input = input;
        _input.CharTyped += HandleChar;
        OnDispose(() => input.CharTyped -= HandleChar);
    }

    /// <summary>
    /// Device-less keyboard, so the Forms input pump still has a non-null keyboard to tick when the
    /// service has no window (tests).
    /// </summary>
    internal Keyboard()
    {
    }

    private void HandleChar(uint codepoint)
    {
        // Lone surrogates and out-of-range values cannot be converted; drop them.
        if (codepoint <= 0x10FFFF && (codepoint < 0xD800 || codepoint > 0xDFFF))
        {
            AppendTypedText(char.ConvertFromUtf32((int)codepoint));
        }
    }

    /// <inheritdoc/>
    protected override IEnumerable<GumKeys> SupportedKeys => _polledKeys;

    /// <inheritdoc/>
    protected override bool IsDeviceKeyDown(GumKeys key) => _input?.IsKeyDown(key) == true;
}
