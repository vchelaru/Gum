using Gum.Forms.Controls;
using Silk.NET.Input;
using System;

namespace Gum.Input;

/// <summary>
/// Silk.NET.Input implementation of <see cref="IGumClipboard"/>. Wraps
/// <see cref="IKeyboard.ClipboardText"/>, which reads/writes the real OS clipboard synchronously
/// on every desktop Silk backend. Registered onto <c>GumService.Clipboard</c> from
/// <c>GumService.Silk.cs</c>'s <c>AssignClipboard</c> seam.
/// </summary>
internal sealed class SilkGumClipboard : IGumClipboard
{
    private readonly IInputContext _inputContext;

    public SilkGumClipboard(IInputContext inputContext)
    {
        _inputContext = inputContext;
    }

    private IKeyboard? Keyboard => _inputContext.Keyboards.Count > 0 ? _inputContext.Keyboards[0] : null;

    public string? GetText(Action? callback) => Keyboard?.ClipboardText;

    public void SetText(string text)
    {
        var keyboard = Keyboard;
        if (keyboard != null)
        {
            keyboard.ClipboardText = text;
        }
    }
}
