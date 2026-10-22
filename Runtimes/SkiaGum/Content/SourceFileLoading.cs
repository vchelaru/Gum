#nullable enable
using RenderingLibrary.Content;

namespace SkiaGum.Content;

internal static class SourceFileLoading
{
    /// <summary>
    /// Loads the document at <paramref name="path"/> through the <see cref="LoaderManager"/>, or returns
    /// <c>null</c> for a null or empty path (the runtime's "no file" value).
    /// </summary>
    internal static T? LoadOrNull<T>(string? path) where T : class =>
        string.IsNullOrEmpty(path) ? null : LoaderManager.Self.LoadContent<T>(path);
}
