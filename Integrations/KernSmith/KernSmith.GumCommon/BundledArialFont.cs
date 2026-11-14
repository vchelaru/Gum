using System.Reflection;
using KernSmith;

namespace KernSmith.Gum;

/// <summary>
/// Liberation Sans, shipped inside this package, registered under "Arial" the first time a system-font
/// build of "Arial" fails (#8044). Default Text asks for "Arial"; Linux without the Microsoft core fonts
/// and the browser have none, so without this an oversampled (zoomed) build of the default font fails.
/// Registering only on failure leaves a real installed Arial, and any Arial the game registered itself,
/// untouched. Liberation Sans is SIL OFL 1.1; the license ships next to the font (Fonts/).
/// </summary>
internal static class BundledArialFont
{
    private const string RegularResourceName = "KernSmith.Gum.Fonts.LiberationSans-Regular.ttf";

    private static bool _attempted;
    private static readonly object _lock = new();

    /// <summary>Test seam: the family name the bundled font is registered under.</summary>
    internal static string FamilyName = "Arial";

    /// <summary>Test seam: registers font bytes with KernSmith.</summary>
    internal static Action<string, byte[], string?> RegisterFont =
        (family, bytes, style) => BmFont.RegisterFont(family, bytes, style);

    /// <summary>Test seam: forgets that registration was attempted.</summary>
    internal static void Reset()
    {
        lock (_lock)
        {
            _attempted = false;
        }
    }

    /// <summary>
    /// Registers the bundled font when <paramref name="fontName"/> is the default family and this has not
    /// been tried yet. Returns true when the caller should retry its build.
    /// </summary>
    internal static bool TryRegisterFor(string? fontName)
    {
        if (!string.Equals(fontName, FamilyName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        lock (_lock)
        {
            if (_attempted)
            {
                return false;
            }
            _attempted = true;
        }

        using Stream? stream = typeof(BundledArialFont).Assembly.GetManifestResourceStream(RegularResourceName);
        if (stream == null)
        {
            return false;
        }

        using MemoryStream bytes = new();
        stream.CopyTo(bytes);
        RegisterFont(FamilyName, bytes.ToArray(), null);
        return true;
    }
}
