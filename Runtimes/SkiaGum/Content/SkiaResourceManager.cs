using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using SkiaSharp;
using SkiaSharp.Skottie;
using Svg.Skia;
using ToolsUtilities;

namespace SkiaGum.Content;

public static class SkiaResourceManager
{
    public static Assembly? CustomResourceAssembly { get; set; }

    // Even though this is used by EmbeddedResourceContentLoader, we put it here 
    // so projects could access both this and CustomResourceAssembly together.
    public static Func<string, string>? AdjustContentName;

    static Assembly ResourceAssembly => CustomResourceAssembly ?? Assembly.GetExecutingAssembly();

    public static bool IsInitialized { get; private set; }

    #region SVG caching

    private static ConcurrentDictionary<string, SKSvg> svgCache = new ConcurrentDictionary<string, SKSvg>();

    public static bool ContainsSvg(string name) => svgCache.ContainsKey(name);

    public static SKSvg GetSvg(string resourceName)
    {
        if (!svgCache.ContainsKey(resourceName))
            CacheSvg(resourceName);

        return svgCache[resourceName];
    }

    private static void CacheSvg(string resourceName)
    {
        using Stream? fileStream = TryOpenFile(resourceName);
        if (fileStream != null)
        {
            SKSvg svg = new SKSvg();
            svg.Load(fileStream);
            svgCache[resourceName] = svg;
        }
        else
        {
            using (Stream stream = GetManifestResourceStream(
                resourceName, ResourceAssembly))
            {
                SKSvg svg = new SKSvg();

                try
                {
                    svg.Load(stream);
                }
                catch (Exception e)
                {
                    throw new Exception($"Error loading SVG {resourceName}\n{e}");
                }
                svgCache[resourceName] = svg;
            }
        }
    }

    public static void CacheSvg(string resourceName, SKSvg svg)
    {
        // so that it can be replaced dynamically
        svgCache[resourceName] = svg;
    }
    #endregion

    #region Lottie Animation Caching

    private static ConcurrentDictionary<string, Animation?> animationCache = new ConcurrentDictionary<string, Animation?>();

    public static Animation? GetLottieAnimation(string resourceName)
    {
        if (!animationCache.ContainsKey(resourceName))
            CacheAnimation(resourceName);

        return animationCache[resourceName];
    }

    private static void CacheAnimation(string resourceName)
    {
        using (Stream stream = TryOpenFile(resourceName) ?? GetManifestResourceStream(
            resourceName, ResourceAssembly))
        {
            Animation? animation = null;

            try
            {
                animation = Animation.Create(stream);
            }
            catch (Exception e)
            {
                throw new Exception($"Error loading Animation {resourceName}\n{e}");
            }
            animationCache[resourceName] = animation;
        }
    }

    #endregion

    #region SKBitmap caching

    private static ConcurrentDictionary<string, SKBitmap> skBitmapCache = new ConcurrentDictionary<string, SKBitmap>();

    public static bool IsCached(string resourceName) => skBitmapCache.ContainsKey(resourceName);

    private static Stream GetUrlStream(string url)
    {
        using var client = new System.Net.Http.HttpClient();
        byte[] imageData = client.GetByteArrayAsync(url).GetAwaiter().GetResult();

        return new MemoryStream(imageData);
    }

    public static SKBitmap? GetSKBitmapFromUrl(string url)
    {
        // Even though GetSKBitmap does a cache check internally, we want to do a cache check before getting the url
        // so we'll wrap the check here:
        if (!skBitmapCache.ContainsKey(url))
        {
            try
            {
                using Stream urlStream = GetUrlStream(url);
                return GetSKBitmap(url, urlStream);
            }
            catch
            {
                // Vic says - I'm not sure which type of exception is being caught. Normally I'd catch specific exceptions
                // to indicate timeouts but I'm not sure what that is, so we'll just have to handle all and don't throw up:
            }
            return null;
        }
        else
        {
            return skBitmapCache[url];
        }
    }


    /// <summary>
    /// Returns the cached bitmap for <paramref name="resourceName"/>, loading it on first use.
    /// When <paramref name="stream"/> is given, the bitmap is decoded from it; otherwise it is
    /// loaded through <see cref="FileManager.GetStreamForFile"/> (disk or the host's stream hook) or,
    /// failing that, from an embedded resource.
    /// </summary>
    /// <exception cref="InvalidDataException"><paramref name="stream"/> does not hold a decodable image.</exception>
    public static SKBitmap GetSKBitmap(string resourceName, Stream? stream = null)
    {
        if (!skBitmapCache.ContainsKey(resourceName))
        {
            if (stream != null)
            {
                // Don't cache a failed decode, so a later call (e.g. a retried download) can succeed.
                skBitmapCache[resourceName] = SKBitmap.Decode(stream)
                    ?? throw new InvalidDataException($"Could not decode an image for {resourceName}.");
            }
            else
            {
                CacheSKImage(resourceName);
            }
        }

        return skBitmapCache[resourceName];
    }

    /// <summary>
    /// The on-disk path tried for <paramref name="resourceName"/> before falling back to embedded
    /// resources, with native separators. A project saved on Windows stores SourceFile values with
    /// backslashes, which macOS/Linux read as part of the file name.
    /// </summary>
    internal static string GetAbsoluteFilePath(string resourceName)
    {
        // Only prepend RelativeDirectory when the caller passed a relative name.
        // AnimationFrame.ToAnimationFrame already pre-prefixes with RelativeDirectory
        // before calling LoadContent, so unconditionally concatenating here used to
        // produce a doubled path ("C:\...\dir\" + "C:\...\dir\file.png") that
        // File.Exists couldn't find — the loader then silently fell through to the
        // embedded-resource branch and threw on the mangled name.
        var absoluteFile = FileManager.IsRelative(resourceName)
            ? FileManager.RelativeDirectory + resourceName
            : resourceName;
        return FileManager.Standardize(absoluteFile, preserveCase: true);
    }

    private static void CacheSKImage(string resourceName)
    {
        using var stream = TryOpenFile(resourceName) ?? GetManifestResourceStream(
            resourceName, ResourceAssembly);
        skBitmapCache[resourceName] = SKBitmap.Decode(stream);
    }

    /// <summary>
    /// Opens <paramref name="resourceName"/> through <see cref="FileManager.GetStreamForFile"/>, the
    /// seam the MonoGame and raylib loaders read through, so a host's
    /// <see cref="FileManager.CustomGetStreamFromFile"/> (a .gumpkg bundle, a zip, a mobile asset
    /// store) is honored. The hook wins over a loose file at the same path, so a bundle overrides
    /// stale loose copies next to it; disk is the fallback. Returns null when neither has the file,
    /// leaving the caller to fall back to embedded resources.
    /// </summary>
    private static Stream? TryOpenFile(string resourceName)
    {
        var absoluteFile = GetAbsoluteFilePath(resourceName);
        // FileExists probes disk, the macOS .app Resources folder and the hook, so a missing file
        // costs no first-chance exception.
        return FileManager.FileExists(absoluteFile)
            ? FileManager.GetStreamForFile(absoluteFile)
            : null;
    }



    #endregion

    #region Font caching

    private static ConcurrentDictionary<int, SKTypeface> typefaceCache = new ConcurrentDictionary<int, SKTypeface>();

    public enum TypefaceType
    {
        Leelawadee,
        ArialRoundedMTBold,
        RobotoRegular,
        RobotoMedium,
        RobotoMediumItalic,
        RobotoBold
    }

    public static SKTypeface GetTypeface(TypefaceType type) => typefaceCache[(int)type];

    public static SKTypeface GetTypeface(string typefaceName)
    {
        //Do not enclose this stream in a using block: https://stackoverflow.com/questions/48061401/drawtext-in-canvas-skiasharp-text-does-not-display
        Stream stream = TryOpenFile(typefaceName) ?? GetManifestResourceStream(
            typefaceName, ResourceAssembly);

        var typeface = SKTypeface.FromStream(stream);
        //typefaceCache.Add(type, typeface);
        // todo - cache it:
        return typeface;
    }

    //private static void CacheTypeface(int type)
    //{
    //    SKTypeface result = SKTypeface.Default;
    //    string fullFontName = "Namespace.Resources.Fonts.";

    //    switch (type)
    //    {
    //        case (int)TypefaceType.Leelawadee:
    //            fullFontName += Fonts.CustomFont.Leelawadee.IosName;
    //            break;

    //        case (int)TypefaceType.ArialRoundedMTBold:
    //            fullFontName += Fonts.CustomFont.ArialRoundedMTBold.IosName;
    //            break;
    //        case (int)TypefaceType.RobotoRegular:
    //            fullFontName += Fonts.CustomFont.RobotoRegular.IosName;
    //            break;
    //        case (int)TypefaceType.RobotoMedium:
    //            fullFontName += Fonts.CustomFont.RobotoMedium.IosName;
    //            break;
    //        case (int)TypefaceType.RobotoMediumItalic:
    //            fullFontName += Fonts.CustomFont.RobotoMediumItalic.IosName;
    //            break;
    //        case (int)TypefaceType.RobotoBold:
    //            fullFontName += Fonts.CustomFont.RobotoBold.IosName;
    //            break;
    //    }

    //    fullFontName += ".ttf";

    //    //Do not enclose this stream in a using block: https://stackoverflow.com/questions/48061401/drawtext-in-canvas-skiasharp-text-does-not-display
    //    Stream stream = GetManifestResourceStream(
    //        fullFontName, ResourceAssembly);

    //    var typeface = SKTypeface.FromStream(stream);
    //    typefaceCache.Add(type, typeface);
    //}
    #endregion

    public static void Initialize(Assembly? resourceAssembly)
    {
        if(IsInitialized == false)
        {
            IsInitialized = true;

            foreach (int i in Enum.GetValues(typeof(TypefaceType)))
            {
                // don't do anything, this isn't standardized...
                //CacheTypeface(i);
            }
        }

        // Only if the user explicitly changed the resource:
        if(resourceAssembly != null)
        {
            CustomResourceAssembly = resourceAssembly;
        }
    }


    private static Stream GetManifestResourceStream(string path, Assembly assembly)
    {
        var replacedPath = path
            .Replace('\\', '.')
            .Replace('/', '.');

        Stream? s = assembly.GetManifestResourceStream(replacedPath);

        if (s == null)
        {
            // try to match lower-case names


            string message = $"Looking for file {replacedPath} in assembly {assembly} but could not find it.";

            var resourceNames = assembly.GetManifestResourceNames();
            if (resourceNames.Length > 0)
            {
                message += "\nFound names\n";
                foreach (var name in resourceNames)
                {
                    if (name.ToLowerInvariant() == path.ToLowerInvariant())
                    {
                        s = assembly.GetManifestResourceStream(name);
                        break;
                    }

                    message += name + "\n";
                }
            }
            else
            {
                message += "\nThis assembly contains no embedded resource files.";
            }

            // TryOpenFile already tried FileManager.RelativeDirectory + name (or the bare
            // absolute name) on disk and through FileManager.CustomGetStreamFromFile; landing
            // here means neither had it. Surface both possibilities so the caller can fix
            // whichever fits their setup.
            message +=
                $"\n\nThe file was also looked for on disk and through FileManager.CustomGetStreamFromFile before falling through to this assembly. " +
                $"At this call, FileManager.RelativeDirectory was \"{FileManager.RelativeDirectory}\". " +
                $"Two ways to fix it:" +
                $"\n  - File: confirm the file exists (or your CustomGetStreamFromFile hook serves it) at FileManager.RelativeDirectory + name, " +
                $"or at the absolute path if you passed one. Loose-file content is the typical setup " +
                $"for the SilkNet / standalone Skia samples." +
                $"\n  - Embedded resource: mark the file as <EmbeddedResource> in the assembly that " +
                $"owns it and (if it isn't this assembly) set SkiaResourceManager.CustomResourceAssembly " +
                $"to that assembly.";


            // See if it's still null
            if (s == null)
            {
                throw new ArgumentException(message);

            }

        }

        return s;
    }
}
