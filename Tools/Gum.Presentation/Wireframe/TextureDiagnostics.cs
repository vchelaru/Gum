using System;
using Gum.Diagnostics;

namespace Gum.Wireframe;

/// <summary>
/// Texture and content-cache diagnostics for the canvas, written through <see cref="ToolLog"/> (Debug builds
/// only, <c>%TEMP%\Gum\tool.log</c>). Lines start with <c>texture.drop</c>, <c>texture.note</c> or
/// <c>texture.drawfailure</c> so they are easy to search for.
/// </summary>
public static class TextureDiagnostics
{
    private static string? _lastDrop;

    public static void RecordDrop(string reason)
    {
        if (!ToolLog.IsEnabled)
        {
            return;
        }

        _lastDrop = reason + "\n" + Environment.StackTrace;
        ToolLog.Write("texture.drop", detail: _lastDrop);
    }

    public static void Note(string text)
    {
        if (ToolLog.IsEnabled)
        {
            ToolLog.Write("texture.note", detail: text);
        }
    }

    /// <summary>Name and disposed state of a backend texture object, read without referencing the backend type.</summary>
    public static string Describe(object? texture)
    {
        if (texture == null)
        {
            return "null";
        }

        string? name = texture.GetType().GetProperty("Name")?.GetValue(texture) as string;
        object? disposed = texture.GetType().GetProperty("IsDisposed")?.GetValue(texture);
        return $"{texture.GetType().Name} name='{name}' disposed={disposed}";
    }

    public static void RecordDrawFailure(Exception exception)
    {
        if (!ToolLog.IsEnabled)
        {
            return;
        }

        ToolLog.Write("texture.drawfailure",
            detail: $"{exception}\nMost recent drop: {_lastDrop ?? "none this session"}");
    }
}
