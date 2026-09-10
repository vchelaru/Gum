using System;
using Gum.DataTypes;

namespace Gum.ProjectServices.FontGeneration;

/// <summary>
/// Turns a project's requested <see cref="FontGeneratorType"/> into the one that can actually run
/// on this machine. <see cref="FontGeneratorType.BmFont"/> shells out to <c>bmfont.exe</c>, which
/// exists only on Windows, so off Windows it resolves to <see cref="FontGeneratorType.KernSmith"/>.
/// </summary>
public static class FontGeneratorResolver
{
    /// <summary>Whether the bmfont.exe backend can run on the current operating system.</summary>
    public static bool IsBmFontSupported => OperatingSystem.IsWindows();

    /// <summary>Resolves against the current operating system.</summary>
    public static FontGeneratorType Resolve(FontGeneratorType requested) =>
        Resolve(requested, IsBmFontSupported);

    /// <summary>
    /// Resolves against an explicit capability, so callers and tests can reason about the
    /// substitution without depending on the machine they run on.
    /// </summary>
    public static FontGeneratorType Resolve(FontGeneratorType requested, bool isBmFontSupported)
    {
        if (requested == FontGeneratorType.BmFont && !isBmFontSupported)
        {
            return FontGeneratorType.KernSmith;
        }

        return requested;
    }

    /// <summary>True when <see cref="Resolve(FontGeneratorType, bool)"/> would change the request.</summary>
    public static bool IsSubstituted(FontGeneratorType requested, bool isBmFontSupported) =>
        Resolve(requested, isBmFontSupported) != requested;
}
