using System.Reflection;
using KernSmith;

namespace KernSmith.Gum;

/// <summary>
/// Liberation Sans, shipped inside this package, registered under "Arial" or "Liberation Sans" the first time a
/// system-font build of that family fails (#8044). Default Text asks for "Arial" and code-only V3 text for
/// "Liberation Sans"; Linux without the Microsoft core fonts, the browser and most Windows machines lack one of
/// them, so without this an oversampled (zoomed) or resized build of the default font fails.
/// Registering only on failure leaves a real installed font, and any font the game registered itself,
/// untouched. Liberation Sans is SIL OFL 1.1; the license ships next to the font (Fonts/).
/// </summary>
internal static class BundledArialFont
{
    private const string RegularResourceName = "KernSmith.Gum.Fonts.LiberationSans-Regular.ttf";

    private static readonly Dictionary<string, bool> _registeredByFamily = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object _lock = new();

    /// <summary>Test seam: the family names the bundled font is registered under.</summary>
    internal static string[] FamilyNames = { "Arial", "Liberation Sans" };

    /// <summary>Test seam: registers font bytes with KernSmith.</summary>
    internal static Action<string, byte[], string?> RegisterFont =
        (family, bytes, style) => BmFont.RegisterFont(family, bytes, style);

    /// <summary>Test seam: forgets that registration was attempted.</summary>
    internal static void Reset()
    {
        lock (_lock)
        {
            _registeredByFamily.Clear();
        }
    }

    /// <summary>
    /// Registers the bundled font under <paramref name="fontName"/> when it is one of <see cref="FamilyNames"/> and
    /// this has not been tried for it yet. Returns true when the caller should retry its build.
    /// </summary>
    internal static bool TryRegisterFor(string? fontName)
    {
        string? family = FamilyNames.FirstOrDefault(name => string.Equals(name, fontName, StringComparison.OrdinalIgnoreCase));
        if (family == null)
        {
            return false;
        }

        // Registration runs inside the lock, so a second thread that failed at the same moment waits for
        // it and then retries against the registered font instead of reporting a failure (#8035 memoizes one).
        lock (_lock)
        {
            if (!_registeredByFamily.TryGetValue(family, out bool registered))
            {
                using Stream? stream = typeof(BundledArialFont).Assembly.GetManifestResourceStream(RegularResourceName);
                if (stream != null)
                {
                    using MemoryStream bytes = new();
                    stream.CopyTo(bytes);
                    RegisterFont(family, bytes.ToArray(), null);
                    registered = true;
                }
                _registeredByFamily[family] = registered;
            }
            return registered;
        }
    }
}
