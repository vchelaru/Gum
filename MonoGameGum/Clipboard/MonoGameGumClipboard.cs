using Gum.Forms.Controls;
using System;

namespace Gum.Clipboard;

/// <summary>
/// MonoGame / KNI / FNA / Raylib / Sokol implementation of <see cref="IGumClipboard"/>.
/// Wraps the existing static <see cref="ClipboardImplementation"/> helper so the
/// platform-agnostic Forms controls in GumCommon can reach clipboard text through
/// the <see cref="IGumService.Clipboard"/> abstraction. Registered onto
/// <c>GumService.Clipboard</c> during initialization.
/// </summary>
/// <remarks>
/// On iOS the underlying <see cref="ClipboardImplementation"/> stubs out both
/// directions (returns empty / no-op), so iOS callers still go through this path
/// rather than seeing a null clipboard.
/// </remarks>
internal sealed class MonoGameGumClipboard : IGumClipboard
{
    public string? GetText(Action? callback) => ClipboardImplementation.GetText(callback);

    public void SetText(string text) => ClipboardImplementation.PushStringToClipboard(text);
}
