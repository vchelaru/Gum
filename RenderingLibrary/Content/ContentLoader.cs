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
public class ContentLoader : IContentLoader
{
    public SystemManagers SystemManagers { get; set; }

#if XNALIKE && !FRB
    /// <summary>
    /// The ContentManager to use when loading files processed by the content pipeline.
    /// </summary>
    public Microsoft.Xna.Framework.Content.ContentManager? XnaContentManager { get; set; }
#endif

    //List<Atlas> atlases = new List<Atlas>();

    /// <inheritdoc/>
    public T LoadContent<T>(string contentName)
    {
        if(typeof(T) == typeof(Texture2D))
        {
            var texture = LoadTexture2D(contentName, SystemManagers);
            return (T)(object)texture;
        }
        //else if(typeof(T) == (typeof(AtlasedTexture)))
        //{
        //    foreach(var atlas in atlases)
        //    {
        //        if(atlas.Contains(contentName))
        //        {
        //            var asObject = (object)atlas.Get(contentName);
        //            return (T)(asObject);
        //        }
        //    }

        //    return default(T);
        //}
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
                return (T)(object)texture;
            }
            else if (typeof(T) == (typeof(AtlasedTexture)))
            {
                knownType = true;

                //foreach (var atlas in atlases)
                //{
                //    if (atlas.Contains(contentName))
                //    {
                //        var asObject = (object)atlas.Get(contentName);
                //        return (T)(asObject);
                //    }
                //}

                return default(T);
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

    private Texture2D? LoadTexture2D(string fileName, SystemManagers managers)
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

        if (FileManager.IsUrl(fileName))
        {
            toReturn = LoadTextureFromUrl(fileName, managers);
        }
        else
        {
            toReturn = LoadTextureFromFile(fileName, managers);
        }

        ApplyEdgeBleed(toReturn);

        if (LoaderManager.Self.CacheTextures && toReturn != null)
        {
            LoaderManager.Self.AddDisposable(fileNameStandardized, toReturn);
        }
        return toReturn;

    }

    /// <summary>
    /// Bleeds edge color into fully-transparent texels so a non-premultiplied Linear pipeline does
    /// not darken anti-aliased edges toward black (issue #3691). No-op unless
    /// <see cref="Graphics.Renderer.BleedTransparentTextureEdgesOnLoad"/> is set, and only applies to
    /// uncompressed <see cref="SurfaceFormat.Color"/> textures (what <c>Texture2D.FromStream</c>
    /// produces) — compressed / content-pipeline formats are left untouched.
    /// </summary>
    private static void ApplyEdgeBleed(Texture2D? texture)
    {
        if (texture == null
            || !Graphics.Renderer.BleedTransparentTextureEdgesOnLoad
            || texture.Format != SurfaceFormat.Color)
        {
            return;
        }

        var pixels = new Microsoft.Xna.Framework.Color[texture.Width * texture.Height];
        texture.GetData(pixels);
        TextureEdgeBleed.Bleed(pixels, texture.Width, texture.Height);
        texture.SetData(pixels);
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

    private Texture2D LoadTextureFromUrl(string fileName, SystemManagers managers)
    {

        Renderer renderer = Renderer.Self ?? managers.Renderer;

        string fileNameStandardized = FileManager.Standardize(fileName, false, false);

        Texture2D texture;
        using (var stream = GetUrlStream(fileName))
        {
            texture = Texture2D.FromStream(renderer.GraphicsDevice,
                stream);

            texture.Name = fileNameStandardized;
        }
        return texture;
    }

    private static System.IO.Stream GetUrlStream(string url)
    {
        byte[] imageData = null;

        using (var wc = new System.Net.WebClient())
            imageData = wc.DownloadData(url);

        return new System.IO.MemoryStream(imageData);
    }


    /// <summary>
    /// Performs a no-caching load of the texture. This will always go to disk to access a file and 
    /// will always return a unique Texture2D. This should not be used in most cases, as caching is preferred
    /// </summary>
    /// <param name="fileName">The filename to load</param>
    /// <param name="managers">The optional SystemManagers to use when loading the file to obtain a GraphicsDevice</param>
    /// <returns>The loaded Texture2D</returns>
    private Texture2D? LoadTextureFromFile(string fileName, SystemManagers? managers = null)
    {
        string fileNameStandardized = FileManager.Standardize(fileName, true, false);

        if (FileManager.IsRelative(fileNameStandardized))
        {
            fileNameStandardized = FileManager.RelativeDirectory + fileNameStandardized;

            fileNameStandardized = FileManager.RemoveDotDotSlash(fileNameStandardized);
        }

#if XNALIKE && !FRB
        // On desktop OSes File.Exists is authoritative, so we can detect a missing file up front and
        // return null instead of falling into File.OpenRead, which throws (and is caught) once per
        // missing file on every wireframe rebuild. Those throws are cheap at runtime, but they produce
        // a first-chance exception per missing file; with a debugger attached that makes selecting a
        // screen full of missing files take tens of seconds in the Gum tool (issue #3075). We skip the
        // shortcut when an XnaContentManager is present (it can alias a content-pipeline asset that has
        // no loose file on disk) and on non-desktop platforms (web/mobile resolve files through
        // TitleContainer or a host stream hook, where File.Exists is not authoritative). Mirrors the
        // desktop guard in Renderer.Initialize.
        bool canCheckFileExists = OperatingSystem.IsWindows()
            || OperatingSystem.IsLinux()
            || OperatingSystem.IsMacOS();

        bool existsOnDisk = System.IO.File.Exists(fileNameStandardized);
        if (!existsOnDisk && OperatingSystem.IsMacOS())
        {
            // In a macOS .app bundle, content ships in Contents/Resources/ rather than next to the
            // executable, so a file missing at the exe-relative path may still exist there. Don't bail
            // early in that case; GetStreamForFile below performs the same rebase when opening (issue #731).
            string? resourcesPath = FileManager.GetMacOSBundleResourcesPath(fileNameStandardized, FileManager.ExeLocation);
            existsOnDisk = resourcesPath != null && System.IO.File.Exists(resourcesPath);
        }

        if (canCheckFileExists && XnaContentManager == null && !existsOnDisk)
        {
            return null;
        }
#endif

        Texture2D toReturn;
        string extension = FileManager.GetExtension(fileName);
        Renderer renderer = null;
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
        }
#endif


        else
        {
            // This file could be a reference from a .fnt file (with extension), 
            // but the file could be a content pipeline file. If so, we need to tolerate
            // missing streams if we have an XnaContentManager, and if so we should try to 
            // load from there.
            try
            {
                using var stream = FileManager.GetStreamForFile(fileNameStandardized);

                Texture2D texture = null;

                texture = Texture2D.FromStream(renderer.GraphicsDevice,
                    stream);

                texture.Name = fileNameStandardized;

                toReturn = texture;
            }
            catch(Exception e)
            {
#if XNALIKE && !FRB
                if (XnaContentManager != null)
                {
                    var noExtension = FileManager.RemoveExtension(fileNameStandardized);

                    toReturn = LoadFromContentManager(fileName, noExtension);
                    if(toReturn is Texture2D asTexture2D)
                    {
                        // Since we asked with extension, include the extension:
                        asTexture2D.Name = fileNameStandardized;
                    }

                }
                else
                {
                    throw;
                }
#else
                throw;      
#endif
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
