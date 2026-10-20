#nullable enable
using SkiaSharp;
using SkiaSharp.Skottie;
using System;
using System.Reflection;
using ToolsUtilities;
using SKSvg = Svg.Skia.SKSvg;

// This file is the one place that still talks to the obsolete SkiaResourceManager.
#pragma warning disable CS0618

namespace RenderingLibrary.Content;

/// <summary>
/// Skia implementation of <see cref="IContentLoader"/>. Loads bitmaps, SVGs, Lottie animations and
/// typefaces from disk, from <see cref="FileManager.CustomGetStreamFromFile"/> (a bundle or asset
/// store), or from embedded resources, and caches them. It is the Skia counterpart of the
/// MonoGame/KNI and raylib <c>ContentLoader</c>, so <c>GumService.ContentLoader</c> has the same
/// shape on every runtime. Skia has one global <c>SystemManagers</c> and no content pipeline, so
/// there is no <c>SystemManagers</c> or <c>XnaContentManager</c> property here.
/// </summary>
/// <remarks>
/// Sealed on purpose: nothing here is virtual, so deriving from it gains nothing. To customize
/// loading, implement <see cref="IContentLoader"/> and delegate to an instance of this class for
/// the content names you do not handle.
/// </remarks>
public sealed class ContentLoader : IContentLoader
{
    /// <summary>
    /// The assembly searched for embedded resources when a file is not found on disk or through
    /// <see cref="FileManager.CustomGetStreamFromFile"/>. Defaults to the SkiaGum assembly.
    /// </summary>
    public static Assembly? CustomResourceAssembly
    {
        get => global::SkiaGum.Content.SkiaResourceManager.CustomResourceAssembly;
        set => global::SkiaGum.Content.SkiaResourceManager.CustomResourceAssembly = value;
    }

    /// <summary>
    /// Maps a content name to the name used to look it up, for hosts whose embedded resource names
    /// differ from the names a project stores (for example MAUI prefixing a namespace).
    /// </summary>
    public static Func<string, string>? AdjustContentName
    {
        get => global::SkiaGum.Content.SkiaResourceManager.AdjustContentName;
        set => global::SkiaGum.Content.SkiaResourceManager.AdjustContentName = value;
    }

    /// <inheritdoc/>
    public T? LoadContent<T>(string contentName)
    {
        if (typeof(T) == typeof(SKTypeface))
        {
            return (T)(object)global::SkiaGum.Content.SkiaResourceManager.GetTypeface(Adjust(contentName));
        }
        else if (typeof(T) == typeof(SKSvg))
        {
            return (T)(object)global::SkiaGum.Content.SkiaResourceManager.GetSvg(Adjust(contentName));
        }
        else if (typeof(T) == typeof(SKBitmap))
        {
            return (T)(object)global::SkiaGum.Content.SkiaResourceManager.GetSKBitmap(Adjust(contentName));
        }
        else if (typeof(T) == typeof(Animation))
        {
            return (T?)(object?)global::SkiaGum.Content.SkiaResourceManager.GetLottieAnimation(Adjust(contentName));
        }
        else
        {
            return default(T);
        }
    }

    /// <inheritdoc/>
    public T? TryLoadContent<T>(string contentName)
    {
        try
        {
            return LoadContent<T>(contentName);
        }
        catch
        {
            return default(T);
        }
    }

    /// <summary>
    /// Returns <paramref name="fileName"/> standardized with its case preserved, prefixed with
    /// <see cref="FileManager.RelativeDirectory"/> when it is a relative path.
    /// </summary>
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

    /// <summary>
    /// Loads an SVG by its exact resource name, without <see cref="AdjustContentName"/>.
    /// </summary>
    internal static SKSvg LoadSvg(string resourceName) => global::SkiaGum.Content.SkiaResourceManager.GetSvg(resourceName);

    /// <summary>
    /// Marks the resource manager initialized, as <c>SystemManagers.Initialize</c> always has.
    /// </summary>
    internal static void MarkInitialized() => global::SkiaGum.Content.SkiaResourceManager.Initialize(null);

    private static string Adjust(string contentName) => AdjustContentName?.Invoke(contentName) ?? contentName;
}
