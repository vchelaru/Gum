#if MONOGAME || KNI || XNA4 || FNA
#define XNALIKE
#endif
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using ToolsUtilities;

namespace RenderingLibrary.Content;

/// <summary>
/// Provides a simple implementation of IContentLoader for applications
/// using the LoaderManager and not specifying their own custom ContentLoader.
/// This content loader uses the default SystemManagers internally.
/// </summary>
/// <remarks>
/// Sealed on purpose: nothing here is virtual, so deriving from it gains nothing. To customize
/// loading, implement <see cref="IContentLoader"/> and delegate to an instance of this class for
/// the content names you do not handle — see the "Custom content loader" section of the file
/// loading docs.
/// </remarks>
public sealed class ContentLoader : IContentLoader
{
    public SystemManagers? SystemManagers { get; set; }

#if XNALIKE && !FRB
    /// <summary>
    /// The ContentManager to use when loading files processed by the content pipeline.
    /// </summary>
    public Microsoft.Xna.Framework.Content.ContentManager? XnaContentManager { get; set; }
#endif

    /// <inheritdoc/>
    public T? LoadContent<T>(string contentName)
    {
        if(typeof(T) == typeof(Texture2D))
        {
            var texture = LoadTexture2D(contentName, SystemManagers);
            return (T?)(object?)texture;
        }
        else
        {
            throw new NotImplementedException($"Error attempting to load {contentName} of type {typeof(T).AssemblyQualifiedName}");
        }
    }

    /// <inheritdoc/>
    public T? TryLoadContent<T>( string contentName)
    {
        bool knownType = false;
        try
        {
            if (typeof(T) == typeof(Texture2D))
            {
                knownType = true;
                var texture = LoadTexture2D(contentName, SystemManagers);
                return (T?)(object?)texture;
            }
        }
        catch
        { 
            return default( T );
        }

        if(!knownType)
        {
            throw new NotImplementedException($"Could not load {contentName} of type {typeof(T).AssemblyQualifiedName}");
        }
        else
        {
            return default(T);
        }
    }

    private Texture2D? LoadTexture2D(string fileName, SystemManagers? managers)
    {
        string fileNameStandardized = StandardizeCaseSensitive(fileName);

        Texture2D? toReturn = null;

        if (LoaderManager.Self.CacheTextures)
        {
            var cached = LoaderManager.Self.GetDisposable(fileNameStandardized) as Texture2D;
            if (cached != null)
            {
                return cached;
            }
        }

        bool isFromContentManager = false;
        bool isProcessedOnLoad;
        if (FileManager.IsUrl(fileName))
        {
            toReturn = LoadTextureFromUrl(fileName, managers, out isProcessedOnLoad);
        }
        else
        {
            toReturn = LoadTextureFromFile(fileName, out isFromContentManager, out isProcessedOnLoad, managers);
        }

        // A ContentManager texture is that manager's cached instance, shared with the game, and the
        // content pipeline premultiplies by default. Bleeding it would corrupt the game's own draws.
        if (!isFromContentManager && !isProcessedOnLoad)
        {
            ProcessLoadedTexture(toReturn);
        }

        if (LoaderManager.Self.CacheTextures && toReturn != null)
        {
            LoaderManager.Self.AddDisposable(fileNameStandardized, toReturn);
        }
        return toReturn;

    }

    /// <summary>
    /// Applies <see cref="ProcessLoadedPixels"/> to a texture through a GetData/SetData round trip.
    /// Only applies to uncompressed <see cref="SurfaceFormat.Color"/> textures. Callers skip
    /// ContentManager textures, which can also be <see cref="SurfaceFormat.Color"/>.
    /// </summary>
    private static void ProcessLoadedTexture(Texture2D? texture)
    {
        bool usePremultipliedAlpha = Graphics.Renderer.UsePremultipliedAlpha;
        bool bleedTransparentEdges = Graphics.Renderer.BleedTransparentTextureEdgesOnLoad;
        if (texture == null
            || texture.Format != SurfaceFormat.Color
            || (!usePremultipliedAlpha && !bleedTransparentEdges))
        {
            return;
        }

        var pixels = new Microsoft.Xna.Framework.Color[texture.Width * texture.Height];
        texture.GetData(pixels);
        ProcessLoadedPixels(System.Runtime.InteropServices.MemoryMarshal.AsBytes(pixels.AsSpan()),
            texture.Width, texture.Height, usePremultipliedAlpha, bleedTransparentEdges);
        texture.SetData(pixels);
    }

    /// <summary>
    /// The load-time fix-up for straight-alpha RGBA texels: premultiplies them (issue #6154) when
    /// <paramref name="usePremultipliedAlpha"/> is set, otherwise bleeds edge color into
    /// fully-transparent texels (issue #3691) when <paramref name="bleedTransparentEdges"/> is set,
    /// so a non-premultiplied Linear pipeline does not darken anti-aliased edges toward black.
    /// </summary>
    internal static void ProcessLoadedPixels(Span<byte> rgba, int width, int height,
        bool usePremultipliedAlpha, bool bleedTransparentEdges)
    {
        if (usePremultipliedAlpha)
        {
            TexturePremultiply.PremultiplyRgba(rgba);
        }
        else if (bleedTransparentEdges)
        {
            TextureEdgeBleed.BleedRgba(rgba, width, height);
        }
    }

    /// <summary>
    /// The load-time fix-up for pixels an <see cref="IImageDecoder"/> produced. Straight pixels get
    /// <see cref="ProcessLoadedPixels"/>. Premultiplied pixels are left alone when the renderer is
    /// premultiplied, otherwise un-premultiplied (then edge-bled when that is on).
    /// </summary>
    /// <returns>True when the pixels were un-premultiplied, which the caller reports as a warning.</returns>
    internal static bool PrepareDecodedPixels(Span<byte> rgba, int width, int height, bool isPremultiplied,
        bool usePremultipliedAlpha, bool bleedTransparentEdges)
    {
        if (!isPremultiplied)
        {
            ProcessLoadedPixels(rgba, width, height, usePremultipliedAlpha, bleedTransparentEdges);
            return false;
        }

        if (usePremultipliedAlpha)
        {
            return false;
        }

        TexturePremultiply.UnpremultiplyRgba(rgba);
        if (bleedTransparentEdges)
        {
            TextureEdgeBleed.BleedRgba(rgba, width, height);
        }
        return true;
    }

    /// <summary>
    /// Creates a texture from an encoded image stream: the registered
    /// <see cref="LoaderManager.ImageDecoders"/> first, then the built-in decoding.
    /// <paramref name="isProcessedOnLoad"/> is true when the pixels already went through the load-time fix-up.
    /// </summary>
    private static Texture2D CreateTextureFromStream(GraphicsDevice device, string fileName, System.IO.Stream stream,
        out bool isProcessedOnLoad)
    {
        bool usePremultipliedAlpha = Graphics.Renderer.UsePremultipliedAlpha;
        bool bleedTransparentEdges = Graphics.Renderer.BleedTransparentTextureEdgesOnLoad;
#if !FRB
        System.IO.Stream original = stream;
        try
        {
            DecodedImage? decoded = LoaderManager.Self.ImageDecoders.TryDecode(fileName, ref stream);
            if (decoded != null)
            {
                bool unpremultiplied = PrepareDecodedPixels(decoded.Rgba, decoded.Width, decoded.Height,
                    decoded.IsPremultiplied, usePremultipliedAlpha, bleedTransparentEdges);
                if (unpremultiplied)
                {
                    System.Diagnostics.Trace.TraceWarning(
                        $"Gum: {fileName} was decoded as premultiplied alpha but Renderer.UsePremultipliedAlpha " +
                        "is false, so it was un-premultiplied on load, which loses precision at low alpha.");
                }
                isProcessedOnLoad = true;
                return CreateTextureFromRgba(device, decoded.Rgba, decoded.Width, decoded.Height);
            }

            return CreateTextureFromEncodedStream(device, fileName, stream, usePremultipliedAlpha,
                bleedTransparentEdges, out isProcessedOnLoad);
        }
        finally
        {
            // TryDecode replaces a non-seekable stream with an in-memory copy; the caller disposes the original.
            if (!ReferenceEquals(stream, original))
            {
                stream.Dispose();
            }
        }
#else
        return CreateTextureFromEncodedStream(device, fileName, stream, usePremultipliedAlpha,
            bleedTransparentEdges, out isProcessedOnLoad);
#endif
    }

    private static Texture2D CreateTextureFromEncodedStream(GraphicsDevice device, string fileName,
        System.IO.Stream stream, bool usePremultipliedAlpha, bool bleedTransparentEdges, out bool isProcessedOnLoad)
    {
#if KNI && !FRB
        if (OperatingSystem.IsBrowser())
        {
            // KNI BlazorGL: decode and fix up the bytes here, then upload once, so the fix-up needs no
            // GetData readback. Switch to Texture2D.FromStream with a color processor once a KNI
            // release has it: https://github.com/kniEngine/kni/pull/2740
            byte[] rgba = StbTextureDecoder.Decode(stream, out int width, out int height);
            ProcessLoadedPixels(rgba, width, height, usePremultipliedAlpha, bleedTransparentEdges);
            isProcessedOnLoad = true;
            return CreateTextureFromRgba(device, rgba, width, height);
        }
#endif
        isProcessedOnLoad = false;
        try
        {
            return Texture2D.FromStream(device, stream);
        }
        catch (Exception exception) when (!IsBuiltInImageExtension(FileManager.GetExtension(fileName)))
        {
            string extension = FileManager.GetExtension(fileName);
            throw new InvalidOperationException(
                $"Could not load {fileName}: no image decoder handled the .{extension} format, and Gum does not " +
                $"load it on its own. Add an IImageDecoder for .{extension} to LoaderManager.Self.ImageDecoders " +
                "before GumService.Initialize loads the project.", exception);
        }
    }

    // The formats Texture2D.FromStream loads on every XNA-like backend.
    private static bool IsBuiltInImageExtension(string extension) =>
        extension is "png" or "jpg" or "jpeg" or "bmp" or "gif" or "tga" or "tif" or "tiff" or "dds";

    private static Texture2D CreateTextureFromRgba(GraphicsDevice device, byte[] rgba, int width, int height)
    {
        var texture = new Texture2D(device, width, height, false, SurfaceFormat.Color);
        texture.SetData(rgba);
        return texture;
    }

    public static string StandardizeCaseSensitive(string fileName)
    {
        const bool preserveCase = true;

        string fileNameStandardized = FileManager.Standardize(fileName, preserveCase, false);

        if (FileManager.IsRelative(fileNameStandardized) && FileManager.IsUrl(fileName) == false)
        {
            fileNameStandardized = FileManager.RelativeDirectory + fileNameStandardized;

            fileNameStandardized = FileManager.RemoveDotDotSlash(fileNameStandardized);
        }

        return fileNameStandardized;
    }

    private Texture2D LoadTextureFromUrl(string fileName, SystemManagers? managers, out bool isProcessedOnLoad)
    {

        Renderer renderer = Renderer.Self ?? managers!.Renderer;

        string fileNameStandardized = FileManager.Standardize(fileName, preserveCase: true, makeAbsolute: false);

        Texture2D texture;
        using (var stream = GetUrlStream(fileName))
        {
            // Headless (no device): FromStream throws, and TryLoadContent callers catch it.
            texture = CreateTextureFromStream(renderer.GraphicsDevice!, fileName, stream, out isProcessedOnLoad);

            texture.Name = fileNameStandardized;
        }
        return texture;
    }

    private static System.IO.Stream GetUrlStream(string url)
    {
        byte[] imageData;

#pragma warning disable SYSLIB0014 // obsolete but still supported; kept for its synchronous DownloadData
        using (var wc = new System.Net.WebClient())
#pragma warning restore SYSLIB0014
            imageData = wc.DownloadData(url);

        return new System.IO.MemoryStream(imageData);
    }


    /// <summary>
    /// Performs a no-caching load of the texture. This will always go to disk to access a file and 
    /// will always return a unique Texture2D. This should not be used in most cases, as caching is preferred
    /// </summary>
    /// <param name="fileName">The filename to load</param>
    /// <param name="isFromContentManager">True when the texture came from <see cref="XnaContentManager"/>, which
    /// owns and caches it, rather than being created by this loader.</param>
    /// <param name="isProcessedOnLoad">True when the pixels already went through <see cref="ProcessLoadedPixels"/>.</param>
    /// <param name="managers">The optional SystemManagers to use when loading the file to obtain a GraphicsDevice</param>
    /// <returns>The loaded Texture2D</returns>
    private Texture2D? LoadTextureFromFile(string fileName, out bool isFromContentManager, out bool isProcessedOnLoad, SystemManagers? managers = null)
    {
        isFromContentManager = false;
        isProcessedOnLoad = false;
        string fileNameStandardized = FileManager.Standardize(fileName, true, false);

        if (FileManager.IsRelative(fileNameStandardized))
        {
            fileNameStandardized = FileManager.RelativeDirectory + fileNameStandardized;

            fileNameStandardized = FileManager.RemoveDotDotSlash(fileNameStandardized);
        }

#if XNALIKE && !FRB
        // On desktop OSes we can detect a missing file up front and return null instead of falling
        // into GetStreamForFile, which throws (and is caught) once per missing file on every wireframe
        // rebuild. Those throws are cheap at runtime, but they produce a first-chance exception per
        // missing file; with a debugger attached that makes selecting a screen full of missing files
        // take tens of seconds in the Gum tool (issue #3075). FileManager.FileExists checks the same
        // places GetStreamForFile reads from: disk, the macOS .app Resources folder (#731) and
        // FileManager.CustomGetStreamFromFile, so a file served only by a .gumpkg bundle or a host's
        // asset zip still loads (#5225). We skip the shortcut when an XnaContentManager is
        // present (it can alias a content-pipeline asset that has no loose file on disk) and on
        // non-desktop platforms, where GetStreamForFile goes straight to the hook. Mirrors the desktop
        // guard in Renderer.Initialize.
        bool canCheckFileExists = OperatingSystem.IsWindows()
            || OperatingSystem.IsLinux()
            || OperatingSystem.IsMacOS();

        if (canCheckFileExists && XnaContentManager == null && !FileManager.FileExists(fileNameStandardized))
        {
            return null;
        }
#endif

        Texture2D toReturn;
        string extension = FileManager.GetExtension(fileName);
        Renderer renderer;
        if (managers == null)
        {
            renderer = Renderer.Self;
        }
        else
        {
            renderer = managers.Renderer;
        }
        
        if (extension == "tga")
        {
#if RENDERING_LIB_SUPPORTS_TGA
                if (renderer.GraphicsDevice == null)
                {
                    throw new Exception("The renderer is null - did you forget to call Initialize?");
                }

                Paloma.TargaImage tgaImage = new Paloma.TargaImage(fileName);
                using (MemoryStream stream = new MemoryStream())
                {
                    tgaImage.Image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    stream.Seek(0, SeekOrigin.Begin); //must do this, or error is thrown in next line
                    toReturn = Texture2D.FromStream(renderer.GraphicsDevice, stream);
                    toReturn.Name = fileName;
                }
#else
            throw new NotImplementedException();
#endif
        }
#if HAS_SYSTEM_DRAWING_IMAGE

        else if (extension == "bmp")
        {
            var image = System.Drawing.Image.FromFile(fileNameStandardized);
            var bitmap = new System.Drawing.Bitmap(image);
            
            var texture = new Texture2D(renderer.GraphicsDevice, bitmap.Width, bitmap.Height, false, SurfaceFormat.Color);
            var pixels = new Microsoft.Xna.Framework.Color[bitmap.Width * bitmap.Height];
            var index = 0;
            for (var y = 0; y < bitmap.Height; y++)
            {
                for (var x = 0; x < bitmap.Width; x++)
                {
                    var color = bitmap.GetPixel(x, y);
                    var r = color.R;
                    var g = color.G;
                    var b = color.B;
                    var a = color.A;
                    pixels[index] = new Microsoft.Xna.Framework.Color(r, g, b, a);
                    index++;
                }
            }
            
            texture.SetData(pixels);
            texture.Name = fileNameStandardized;

            toReturn = texture;
        }
#endif

#if XNALIKE && !FRB
        else if (string.IsNullOrEmpty(extension) && XnaContentManager != null)
        {
            toReturn = LoadFromContentManager(fileName, fileNameStandardized);
            isFromContentManager = true;
        }
#endif


        else
        {
            // This file could be a reference from a .fnt file (with extension),
            // but the file could be a content pipeline file. If so, we need to tolerate
            // missing streams if we have an XnaContentManager, and if so we should try to
            // load from there. Only a missing file falls back: any other failure (a corrupt
            // image, a backend limitation, a failing stream hook) is the real error and is
            // rethrown, rather than hidden behind the ContentManager's ContentLoadException.
            System.IO.Stream stream;
            try
            {
                stream = FileManager.GetStreamForFile(fileNameStandardized);
            }
            catch(Exception exception) when (IsMissingFileException(exception))
            {
#if XNALIKE && !FRB
                if (XnaContentManager != null)
                {
                    var noExtension = FileManager.RemoveExtension(fileNameStandardized);

                    toReturn = LoadFromContentManager(fileName, noExtension);
                    isFromContentManager = true;
                    if(toReturn is Texture2D asTexture2D)
                    {
                        // Since we asked with extension, include the extension:
                        asTexture2D.Name = fileNameStandardized;
                    }
                    return toReturn;
                }
#endif
                throw;
            }

            using (stream)
            {
                // Headless (no device): FromStream throws, and TryLoadContent callers catch it.
                Texture2D texture = CreateTextureFromStream(renderer.GraphicsDevice!, fileNameStandardized, stream, out isProcessedOnLoad);

                texture.Name = fileNameStandardized;

                toReturn = texture;
            }
        }

        return toReturn;
#if XNALIKE && !FRB

        Texture2D LoadFromContentManager(string fileName, string fileNameStandardized)
        {
            // the file name should be absolute at this point, but we want to make
            // it relative to the XNA content manager since that's what's doing the loading.
            var relativeFileName = ResolveContentManagerAssetName(
                XnaContentManager.RootDirectory,
                fileNameStandardized);
            Texture2D texture = XnaContentManager.Load<Texture2D>(relativeFileName);
            texture.Name = fileNameStandardized;
            return texture;
        }
#endif
    }

    // GetStreamForFile wraps every failure in an IOException, so the cause is the inner exception.
    private static bool IsMissingFileException(Exception exception) =>
        exception.InnerException is System.IO.FileNotFoundException or System.IO.DirectoryNotFoundException;

#if XNALIKE && !FRB
    /// <summary>
    /// Computes the asset name to pass to <see cref="Microsoft.Xna.Framework.Content.ContentManager.Load{T}(string)"/>
    /// given the standardized file path and the content manager's configured root.
    /// </summary>
    /// <remarks>
    /// On desktop, <paramref name="fileNameStandardized"/> is typically an absolute path that contains the
    /// content root segment (e.g. "C:/app/bin/Debug/Content/images/atlas"). On mobile platforms such as
    /// Android, <see cref="FileManager.ExeLocation"/> is empty or "/", so a previous implementation
    /// that pivoted MakeRelative on ExeLocation + content root produced asset names like
    /// "./Content/images/atlas". The XNA content manager would then prepend its RootDirectory a second
    /// time, producing paths like "Content/./Content/images/atlas.xnb" that fail to load. This helper
    /// strips the content root segment directly, regardless of whether the input is absolute or relative.
    /// </remarks>
    internal static string ResolveContentManagerAssetName(
        string contentRootDirectory,
        string fileNameStandardized)
    {
        // Normalize separators to forward slashes so we can match content root regardless of platform.
        string normalizedAsset = (fileNameStandardized ?? string.Empty).Replace('\\', '/');
        string normalizedRoot = (contentRootDirectory ?? string.Empty).Replace('\\', '/').Trim('/');

        if (!string.IsNullOrEmpty(normalizedRoot))
        {
            string rootWithSlash = normalizedRoot + "/";
            int rootIndex = normalizedAsset.IndexOf(rootWithSlash, StringComparison.OrdinalIgnoreCase);
            if (rootIndex >= 0)
            {
                return normalizedAsset.Substring(rootIndex + rootWithSlash.Length);
            }
        }

        // Fallback: strip a leading "./" (left over from MakeRelative on platforms without an exe path).
        if (normalizedAsset.StartsWith("./", StringComparison.Ordinal))
        {
            normalizedAsset = normalizedAsset.Substring(2);
        }
        return normalizedAsset;
    }
#endif
}
