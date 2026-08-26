using Apos.Shapes;
using RenderingLibrary.Content;
using System;
using System.IO;
using ToolsUtilities;

namespace MonoGameAndGum.Content;

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
    /// on first request. Returns <c>null</c> when the file is missing or isn't SVG this can read,
    /// so a bad <c>SourceFile</c> renders nothing rather than throwing mid-layout — matching how
    /// <see cref="ContentLoader"/> returns a null texture for a missing image.
    /// </summary>
    public static ShapeSvg? Load(string fileName)
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

        var loaded = LoadFromFile(key);

        if (loaded != null && LoaderManager.Self.CacheTextures)
        {
            LoaderManager.Self.AddDisposable(key, new ManagedShapeSvg(loaded),
                LoaderManager.ExistingContentBehavior.Replace);
        }

        return loaded;
    }

    private static ShapeSvg? LoadFromFile(string standardizedFileName)
    {
        // Routed through FileManager.GetStreamForFile rather than File.OpenRead so the
        // FileManager.CustomGetStreamFromFile hook is honored — .gumpkg bundles, the
        // GumFromZipFile sample, mobile TitleContainer redirection, and any in-memory asset store
        // serve files that have no loose copy on disk (the same reason raylib's loader routes
        // through it — issue #3033). That rules out a File.Exists fast path, so a missing file
        // costs one first-chance exception; unlike texture loads this happens per SourceFile
        // assignment rather than per wireframe rebuild, so it isn't on a hot path.
        try
        {
            using var stream = FileManager.GetStreamForFile(standardizedFileName);

            return ShapeSvg.TryLoad(stream, out var svg) ? svg : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
