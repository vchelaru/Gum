using Apos.Shapes;
using Gum.Wireframe;
using RenderingLibrary.Content;
using System;
using System.IO;
using ToolsUtilities;

namespace Gum.Content;

/// <summary>
/// Cache entry for <see cref="ShapeSvg"/>. The document isn't IDisposable — it's parsed geometry
/// with no GPU or native resource behind it — so this wrapper exists purely so
/// <see cref="LoaderManager"/> can hold it under its IDisposable-keyed cache API. Mirrors
/// <c>ManagedAnimationChainList</c> / <c>ManagedFont</c> in <c>Runtimes/SokolGum/ContentLoader.cs</c>
/// and raylib's <c>ManagedTexture</c>.
/// </summary>
internal sealed class ManagedShapeSvg : IDisposable
{
    public ShapeSvg Svg { get; }
    public ManagedShapeSvg(ShapeSvg svg) { Svg = svg; }
    public void Dispose() { /* nothing to release - the document is plain managed geometry */ }
}

/// <summary>
/// Loads and caches <see cref="ShapeSvg"/> documents for the Apos.Shapes-backed
/// <see cref="Gum.GueDeriving.SvgRuntime"/>.
/// </summary>
/// <remarks>
/// Apos.Shapes does no caching of its own — <see cref="ShapeSvg"/>'s own documentation says
/// "loading is the expensive part, so load a drawing once and keep it," and one document can back
/// any number of batches. Documents are cached in <see cref="LoaderManager"/> alongside textures
/// so they inherit its lifetime: <c>CacheTextures = false</c> and
/// <c>GumService.Uninitialize()</c>'s <see cref="LoaderManager.DisposeAndClear"/> both evict them
/// with no extra teardown hook.
///
/// This bypasses <see cref="IContentLoader.LoadContent{T}"/> on purpose. The XNA family's
/// <see cref="ContentLoader"/> lives in <c>RenderingLibrary/</c>, which is source-shared into
/// GumCommon <i>and</i> FlatRedBall, so giving it a <see cref="ShapeSvg"/> branch would pull an
/// Apos.Shapes reference into core. SokolGum can add loader branches only because it owns its own
/// <see cref="IContentLoader"/> implementation.
/// </remarks>
public static class ShapeSvgLoader
{
    /// <summary>
    /// Returns the <see cref="ShapeSvg"/> for <paramref name="fileName"/>, loading and caching it
    /// on first request. A file that is missing or isn't SVG this can read follows
    /// <see cref="GraphicalUiElement.MissingFileBehavior"/> like a Sprite's image does:
    /// <c>ThrowException</c> throws a <see cref="FileNotFoundException"/> naming the file, otherwise
    /// this returns <c>null</c> and raises <c>PropertyAssignmentError</c>. A failure is never cached,
    /// so a file fixed on disk loads on the next call.
    /// </summary>
    public static ShapeSvg? Load(string fileName) => Load(fileName, owner: null);

    /// <summary>
    /// <see cref="Load(string)"/> that also names <paramref name="owner"/> (by its Tag) in a failure message.
    /// </summary>
    internal static ShapeSvg? Load(string fileName, GraphicalUiElement? owner)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return null;
        }

        // Same key derivation the texture cache uses, so a relative path resolves against
        // FileManager.RelativeDirectory and two spellings of one file share a cache entry.
        var key = ContentLoader.StandardizeCaseSensitive(fileName);

        if (LoaderManager.Self.CacheTextures
            && LoaderManager.Self.GetDisposable(key) is ManagedShapeSvg cached)
        {
            return cached.Svg;
        }

        // The key has its slashes flipped to the OS separator, which breaks a URL, so download
        // from the original string.
        var loaded = LoadFromFile(FileManager.IsUrl(fileName) ? fileName : key, out Exception? failure);

        if (loaded == null)
        {
            ReportLoadFailure(fileName, owner, failure);
            return null;
        }

        if (LoaderManager.Self.CacheTextures)
        {
            LoaderManager.Self.AddDisposable(key, new ManagedShapeSvg(loaded),
                LoaderManager.ExistingContentBehavior.Replace);
        }

        return loaded;
    }

    private static ShapeSvg? LoadFromFile(string standardizedFileName, out Exception? failure)
    {
        failure = null;

        // Routed through FileManager.GetStreamForFile rather than File.OpenRead so the
        // FileManager.CustomGetStreamFromFile hook is honored — .gumpkg bundles, the
        // GumFromZipFile sample, mobile TitleContainer redirection, and any in-memory asset store
        // serve files that have no loose copy on disk (the same reason raylib's loader routes
        // through it — issue #3033). That rules out a File.Exists fast path, so a missing file
        // costs one first-chance exception; unlike texture loads this happens per SourceFile
        // assignment rather than per wireframe rebuild, so it isn't on a hot path.
        try
        {
            using var stream = FileManager.IsUrl(standardizedFileName)
                ? DownloadUrl(standardizedFileName)
                : FileManager.GetStreamForFile(standardizedFileName);

            return ShapeSvg.TryLoad(stream, out var svg) ? svg : null;
        }
        catch (Exception ex)
        {
            failure = ex;
            return null;
        }
    }

    // Mirrors the Sprite SourceFile failure in Gum/Wireframe/CustomSetPropertyOnRenderable.cs.
    private static void ReportLoadFailure(string fileName, GraphicalUiElement? owner, Exception? failure)
    {
        string message = "Error setting SourceFile on Svg";
        if (owner?.Tag != null)
        {
            message += $" in {owner.Tag}";
        }
        message += $"\n{fileName}";
        message += "\nThe file is missing or is not an SVG that Gum can read.";
        message += "\nCheck if the file exists. If necessary, set FileManager.RelativeDirectory";
        message += "\nThe current relative directory is:\n" + FileManager.RelativeDirectory;

        if (GraphicalUiElement.MissingFileBehavior == MissingFileBehavior.ThrowException)
        {
            throw new FileNotFoundException(message, failure);
        }

        Gum.Wireframe.CustomSetPropertyOnRenderable.RaisePropertyAssignmentError(
            failure != null ? message + "\n" + failure : message);
    }

    // Blocking, like the texture loader's URL path. The URL needs no .svg extension; the content
    // is what gets parsed. Some hosts (Wikimedia) refuse a request with no User-Agent.
    private static Stream DownloadUrl(string url)
    {
        using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("GumUI (https://github.com/vchelaru/Gum)");
        byte[] data = client.GetByteArrayAsync(url).GetAwaiter().GetResult();

        return new MemoryStream(data);
    }
}
