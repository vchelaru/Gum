using SDL;
using ToolsUtilities;
using static SDL.SDL3;

namespace Sdl3GumSample.Backend;

/// <summary>
/// A texture uploaded to the SDL renderer, plus its size. Gum's layout needs the size for
/// "percentage of source file" dimensions, so every backend has to keep it next to the handle.
/// </summary>
public sealed unsafe class SdlTexture
{
    private static readonly Dictionary<string, SdlTexture> _cache = new Dictionary<string, SdlTexture>(StringComparer.OrdinalIgnoreCase);

    /// <summary>The renderer textures are uploaded to. Set once by <see cref="SdlGum.Initialize"/>.</summary>
    internal static SDL_Renderer* Renderer;

    public SDL_Texture* Handle { get; }
    public int Width { get; }
    public int Height { get; }

    private SdlTexture(SDL_Texture* handle, int width, int height)
    {
        Handle = handle;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Loads (once) a PNG. A relative path resolves against <see cref="FileManager.RelativeDirectory"/>,
    /// which is how a .gumx project's SourceFile values are written. Returns null when the file is missing,
    /// which draws nothing, the same as Gum's other runtimes.
    /// </summary>
    public static SdlTexture? Load(string? file)
    {
        if (string.IsNullOrEmpty(file))
        {
            return null;
        }

        string absolute = FileManager.IsRelative(file) ? FileManager.MakeAbsolute(file) : file;
        if (_cache.TryGetValue(absolute, out SdlTexture? cached))
        {
            return cached;
        }

        SDL_Surface* surface = SDL_LoadPNG(absolute);
        if (surface == null)
        {
            return null;
        }

        SDL_Texture* handle = SDL_CreateTextureFromSurface(Renderer, surface);
        // Pixel art in the sample; a project's TextureFilter could choose linear instead.
        SDL_SetTextureScaleMode(handle, SDL_ScaleMode.SDL_SCALEMODE_NEAREST);
        SdlTexture texture = new SdlTexture(handle, surface->w, surface->h);
        SDL_DestroySurface(surface);

        _cache[absolute] = texture;
        return texture;
    }

    /// <summary>Destroys every loaded texture. Call before destroying the renderer.</summary>
    public static void DestroyAll()
    {
        foreach (SdlTexture texture in _cache.Values)
        {
            SDL_DestroyTexture(texture.Handle);
        }
        _cache.Clear();
    }
}
