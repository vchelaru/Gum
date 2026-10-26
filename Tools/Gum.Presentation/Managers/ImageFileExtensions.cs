using System.Collections.Generic;

namespace Gum.Managers;

/// <summary>
/// The image file extensions (lowercase, without the leading dot) that the tool loads as textures.
/// Source File validation, file drops and file-change reactions all read this list so they stay in agreement.
/// </summary>
public static class ImageFileExtensions
{
    /// <summary>
    /// Every supported image extension.
    /// </summary>
    public static IReadOnlyList<string> All { get; } = new[] { "png", "jpg", "jpeg", "bmp", "gif" };

    /// <summary>
    /// Whether <paramref name="extension"/> (lowercase, without the leading dot) is a supported image extension.
    /// </summary>
    public static bool Contains(string extension) => ((ICollection<string>)All).Contains(extension);
}
