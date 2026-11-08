#nullable enable

using Gum.Forms.Controls;
using System;

namespace Gum.Input;

/// <summary>
/// <see cref="IGumClipboard"/> over a host engine's synchronous clipboard, given as a getter and a
/// setter. Compiled into each interactive Skia host through SkiaInputHost.props.
/// </summary>
/// <remarks>
/// The get callback exists for clipboards that answer later; these answer immediately, so
/// <see cref="GetText"/> never invokes it.
/// </remarks>
internal sealed class DelegateClipboard : IGumClipboard
{
    private readonly Func<string?> _get;
    private readonly Action<string> _set;

    public DelegateClipboard(Func<string?> get, Action<string> set)
    {
        _get = get;
        _set = set;
    }

    public string? GetText(Action? callback) => _get();

    public void SetText(string text) => _set(text);
}
