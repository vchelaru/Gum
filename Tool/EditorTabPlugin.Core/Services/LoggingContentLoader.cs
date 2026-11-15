using System;
using Gum.Diagnostics;
using RenderingLibrary.Content;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

/// <summary>
/// Writes a <c>content.load</c> line to <see cref="ToolLog"/> before and after every texture load, so a
/// native crash in the decoder or the GL upload (which leaves no managed exception) shows which file
/// it died on. Delegates everything to the wrapped loader.
/// </summary>
internal sealed class LoggingContentLoader : IContentLoader
{
    private readonly IContentLoader _inner;

    public LoggingContentLoader(IContentLoader inner)
    {
        _inner = inner;
    }

    public T? LoadContent<T>(string contentName)
    {
        ToolLog.Write("content.load.begin", detail: contentName);
        T? result = _inner.LoadContent<T>(contentName);
        ToolLog.Write("content.load.end", detail: contentName);
        return result;
    }

    public T? TryLoadContent<T>(string contentName)
    {
        ToolLog.Write("content.load.begin", detail: contentName);
        T? result = _inner.TryLoadContent<T>(contentName);
        ToolLog.Write("content.load.end", detail: contentName);
        return result;
    }
}
