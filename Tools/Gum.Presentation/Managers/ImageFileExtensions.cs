using System;
using System.Collections.Generic;
using System.Linq;
using RenderingLibrary.Content;

namespace Gum.Managers;

/// <summary>
/// The image file extensions (lowercase, without the leading dot) that the tool loads as textures.
/// Source File validation, file drops and file-change reactions all read this list so they stay in agreement.
/// </summary>
public static class ImageFileExtensions
{
    private static readonly string[] BuiltIn = { "png", "jpg", "jpeg", "bmp", "gif" };

    /// <summary>
    /// Every supported image extension: the built-in ones plus those declared by decoders a plugin
    /// registered in <see cref="LoaderManager.ImageDecoders"/>.
    /// </summary>
    public static IReadOnlyList<string> All =>
        BuiltIn.Concat(LoaderManager.Self.ImageDecoders.DeclaredExtensions).Distinct().ToArray();

    /// <summary>
    /// Every extension the tool accepts as a texture-like Source File: the image extensions plus svg and the
    /// animation chain formats (achx, achj). File drops and the import from another .gumx read this list.
    /// </summary>
    public static IReadOnlyList<string> AllSourceFileFormats =>
        All.Concat(new[] { "svg", "achx", "achj" }).Distinct().ToArray();

    /// <summary>
    /// Whether <paramref name="extension"/> (without the leading dot, any case) is a supported image extension.
    /// </summary>
    public static bool Contains(string extension) =>
        All.Contains(extension, StringComparer.OrdinalIgnoreCase);
}
