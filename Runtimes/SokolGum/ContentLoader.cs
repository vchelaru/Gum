using RenderingLibrary.Content;
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
        return default!;
    }

    private static Font? LoadFont(string fileName)
    {
        var key = Standardize(fileName);
        if (LoaderManager.Self.CacheTextures
            && LoaderManager.Self.GetDisposable(key) is ManagedFont cached)
        {
            return cached.Font;
        }

        if (!File.Exists(fileName))
            return null;

        var stash = (SystemManagers.Default
            ?? throw new InvalidOperationException(
                "SystemManagers.Default must be set before loading a Font."))
            .FontStash;
        if (stash == IntPtr.Zero)
            throw new InvalidOperationException(
                "SystemManagers.FontStash is not initialized; call SystemManagers.Initialize() first.");

        var bytes = File.ReadAllBytes(fileName);
        var font = Font.FromTrueTypeBytes(stash, Path.GetFileNameWithoutExtension(fileName), bytes);

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

        if (!File.Exists(fileName))
            return null;

        var tex = DecodeFromFile(fileName);
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

internal sealed class ManagedFont : IDisposable
{
    public Font Font { get; }
    private bool _disposed;

    public ManagedFont(Font font) { Font = font; }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Font.Dispose();
    }
}
