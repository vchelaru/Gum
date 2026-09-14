using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace Gum.Services.Fonts;

/// <summary>Lists the font families installed on the machine, for the Font variable's drop-down.</summary>
public interface IInstalledFontProvider
{
    /// <summary>The installed family names, sorted and without duplicates.</summary>
    IReadOnlyList<string> GetInstalledFontFamilyNames();
}

/// <summary>
/// <see cref="IInstalledFontProvider"/> over SkiaSharp's font manager, which enumerates the OS font
/// collection on Windows, macOS, and Linux (GDI+'s <c>FontFamily.Families</c> is Windows-only).
/// </summary>
public class SkiaInstalledFontProvider : IInstalledFontProvider
{
    /// <inheritdoc/>
    public IReadOnlyList<string> GetInstalledFontFamilyNames()
    {
        return SKFontManager.Default.FontFamilies
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .OrderBy(name => name, System.StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
