using RenderingLibrary;
using RenderingLibrary.Content;
using Gum.Graphics.Animation;
using ToolsUtilities;
using static Sokol.StbImage;

namespace SokolGum;

/// <summary>
/// Implements Gum's <see cref="IContentLoader"/> against sokol_gfx + stb_image.
///
/// Decodes PNG/JPG/etc. via stbi_load_csharp into tightly-packed RGBA8 bytes,
/// then uploads to an sg_image wrapped in a <see cref="Texture2D"/>.
/// Caches results in Gum's LoaderManager (keyed by standardized path) so the
/// same file isn't decoded twice.
/// </summary>
public sealed class ContentLoader : IContentLoader
{
    public T LoadContent<T>(string contentName)
    {
        if (typeof(T) == typeof(Texture2D))
        {
            var tex = LoadTexture2D(contentName);
            return tex is null ? default! : (T)(object)tex;
        }
        if (typeof(T) == typeof(Font))
        {
            var font = LoadFont(contentName);
            return font is null ? default! : (T)(object)font;
        }
        if (typeof(T) == typeof(AnimationChainList))
        {
            var list = LoadAnimationChainList(contentName);
            return list is null ? default! : (T)(object)list;
        }
        throw new NotImplementedException(
            $"SokolGum.ContentLoader doesn't support {typeof(T).FullName} yet.");
    }

    public T TryLoadContent<T>(string contentName)
    {
        if (typeof(T) == typeof(Texture2D))
        {
            var tex = LoadTexture2D(contentName);
            return tex is null ? default! : (T)(object)tex;
        }
        if (typeof(T) == typeof(Font))
        {
            var font = LoadFont(contentName);
            return font is null ? default! : (T)(object)font;
        }
        if (typeof(T) == typeof(AnimationChainList))
        {
            var list = LoadAnimationChainList(contentName);
            return list is null ? default! : (T)(object)list;
        }
        return default!;
    }

    /// <summary>
    /// Loads a <c>.achx</c> file into an <see cref="AnimationChainList"/>,
    /// resolving every frame's texture along the way. Cached by standardized
    /// path so repeated loads share both the chain list and the referenced
    /// textures (textures self-cache inside <see cref="LoadTexture2D"/>).
    ///
    /// Delegates to the shared <c>AnimationChainListSave</c> XML
    /// deserializer and the <c>ToAnimationChainList()</c> conversion
    /// extension. Texture loading inside <c>ToAnimationFrame</c> is
    /// gated by our <c>#elif SOKOL</c> branch which calls back into
    /// this ContentLoader via <c>LoaderManager.Self.LoadContent&lt;Texture2D&gt;</c>.
    /// </summary>
    private static AnimationChainList? LoadAnimationChainList(string fileName)
    {
        var key = Standardize(fileName);
        if (LoaderManager.Self.CacheTextures
            && LoaderManager.Self.GetDisposable(key) is ManagedAnimationChainList cached)
        {
            return cached.List;
        }

        if (!File.Exists(key)) return null;

        var save = Gum.Content.AnimationChain.AnimationChainListSave.FromFile(key);
        var list = save.ToAnimationChainList();

        if (LoaderManager.Self.CacheTextures)
            LoaderManager.Self.AddDisposable(key, new ManagedAnimationChainList(list));

        return list;
    }

    private static Font? LoadFont(string fileName)
    {
        var key = Standardize(fileName);
        if (LoaderManager.Self.CacheTextures
            && LoaderManager.Self.GetDisposable(key) is ManagedFont cached)
        {
            return cached.Font;
        }

        // Use `key` (the standardized, absolute path) rather than the caller's
        // `fileName` — otherwise a relative input with a different working
        // directory could miss here while still hitting the cache above.
        if (!File.Exists(key))
            return null;

        var atlas = (SystemManagers.Default
            ?? throw new InvalidOperationException(
                "SystemManagers.Default must be set before loading a Font."))
            .Fonts
            ?? throw new InvalidOperationException(
                "SystemManagers.Fonts is not initialized; call SystemManagers.Initialize() first.");

        var bytes = File.ReadAllBytes(key);
        var font = Font.FromTrueTypeBytes(atlas, Path.GetFileNameWithoutExtension(key), bytes);

        if (LoaderManager.Self.CacheTextures)
            LoaderManager.Self.AddDisposable(key, new ManagedFont(font));

        return font;
    }

    private static Texture2D? LoadTexture2D(string fileName)
    {
        var key = Standardize(fileName);

        if (LoaderManager.Self.CacheTextures
            && LoaderManager.Self.GetDisposable(key) is ManagedTexture cached)
        {
            return cached.Texture;
        }

        // Same rationale as LoadFont: use the standardized key for existence
        // + read so the path probed for presence matches the one we just
        // missed the cache lookup on.
        if (!File.Exists(key))
            return null;

        var tex = DecodeFromFile(key);
        if (tex is not null && LoaderManager.Self.CacheTextures)
            LoaderManager.Self.AddDisposable(key, new ManagedTexture(tex));

        return tex;
    }

    private static unsafe Texture2D? DecodeFromFile(string fileName)
    {
        var bytes = File.ReadAllBytes(fileName);
        int width = 0, height = 0, channels = 0;
        byte* rgba;

        fixed (byte* pBytes = bytes)
        {
            // desired_channels=4 forces RGBA output regardless of source format.
            rgba = stbi_load_csharp(in pBytes[0], bytes.Length,
                ref width, ref height, ref channels, desired_channels: 4);
        }

        if (rgba is null)
            return null;

        try
        {
            var pixels = new ReadOnlySpan<byte>(rgba, width * height * 4);
            return Texture2D.FromRgba8(pixels, width, height, Path.GetFileName(fileName));
        }
        finally
        {
            stbi_image_free_csharp(rgba);
        }
    }

    private static string Standardize(string fileName)
    {
        var normalized = FileManager.Standardize(fileName, preserveCase: true, makeAbsolute: false);
        if (FileManager.IsRelative(normalized) && !FileManager.IsUrl(fileName))
        {
            normalized = FileManager.RelativeDirectory + normalized;
            normalized = FileManager.RemoveDotDotSlash(normalized);
        }
        return normalized;
    }
}

/// <summary>
/// Disposable cache entry for Gum's LoaderManager.
/// </summary>
internal sealed class ManagedTexture : IDisposable
{
    public Texture2D Texture { get; }
    private bool _disposed;

    public ManagedTexture(Texture2D texture) { Texture = texture; }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Texture.Dispose();
    }
}

/// <summary>
/// LoaderManager keys its cache on IDisposable so every cache entry has to
/// nominally implement it. Font itself isn't disposable (see Font.cs for
/// why), so ManagedFont's Dispose is a no-op — the underlying buffer and
/// fontstash context are released by <see cref="FontAtlas.Dispose"/>.
/// </summary>
internal sealed class ManagedFont : IDisposable
{
    public Font Font { get; }
    public ManagedFont(Font font) { Font = font; }
    public void Dispose() { /* nothing to release per-font */ }
}

/// <summary>
/// Cache entry for <see cref="AnimationChainList"/>. The list itself isn't
/// IDisposable (its frames just reference cached Texture2Ds which the
/// texture cache owns), so this wrapper is purely so LoaderManager can
/// store the list under its IDisposable-keyed API.
/// </summary>
internal sealed class ManagedAnimationChainList : IDisposable
{
    public AnimationChainList List { get; }
    public ManagedAnimationChainList(AnimationChainList list) { List = list; }
    public void Dispose() { /* textures are managed by the Texture2D cache */ }
}
