using Gum.Forms.Controls;
using System;

namespace Gum.Input;

/// <summary>
/// Clipboard implementation for the Stride prototype. Stride's runtime (as opposed to its WPF
/// GameStudio editor) exposes no system-clipboard API, so this wraps the cross-platform TextCopy
/// package instead -- matches the fallback issue #4600 called out. Registered onto
/// <c>GumService.Clipboard</c> from <c>GumService.Stride.cs</c>'s <c>Initialize</c>.
/// </summary>
internal sealed class StrideGumClipboard : IGumClipboard
{
    // TextCopy resolves synchronously, so callback (only meaningful for an async clipboard path)
    // is ignored, matching SilkGumClipboard.
    public string? GetText(Action? callback) => TextCopy.ClipboardService.GetText();

    public void SetText(string text) => TextCopy.ClipboardService.SetText(text);
}
