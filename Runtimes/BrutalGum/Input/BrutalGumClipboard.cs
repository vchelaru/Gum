using Gum.Forms.Controls;
using System;

namespace Gum.Input;

/// <summary>
/// <see cref="IGumClipboard"/> over GLFW's clipboard, which reads and writes the OS clipboard
/// synchronously, so the callback is ignored.
/// </summary>
internal sealed class BrutalGumClipboard : IGumClipboard
{
    private readonly IBrutalInput _input;

    public BrutalGumClipboard(IBrutalInput input)
    {
        _input = input;
    }

    public string? GetText(Action? callback) => _input.ClipboardText;

    public void SetText(string text) => _input.ClipboardText = text;
}
