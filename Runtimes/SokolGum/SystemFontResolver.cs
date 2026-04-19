using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SokolGum;

/// <summary>
/// Maps a logical font identity (family name + bold/italic flags) to a TTF
/// path on the local system. Windows-only in this first pass — Linux/macOS
/// paths are a TODO. Used by <see cref="FontAtlas.GetOrLoadFont"/> to
/// resolve `.gumx` assignments like <c>Font="Arial"</c> + <c>IsBold=true</c>
/// into bytes that fontstash can consume.
/// </summary>
internal static class SystemFontResolver
{
    /// <summary>
    /// Resolves (family, bold, italic) to an absolute TTF/OTF path on disk,
    /// or null if no matching file is installed. Caller is responsible for
    /// fallback behaviour when null is returned (TODO: ship a bundled
    /// fallback TTF so text always draws something).
    /// </summary>
    public static string? Resolve(string family, bool bold, bool italic)
    {
        if (string.IsNullOrEmpty(family))
        {
            return null;
        }
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return null;
        }

        // The registry maps display name -> filename:
        //   HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts
        //   "Arial (TrueType)" = "arial.ttf"
        //   "Arial Bold (TrueType)" = "arialbd.ttf"
        //   "Arial Italic (TrueType)" = "ariali.ttf"
        //   "Arial Bold Italic (TrueType)" = "arialbi.ttf"
        // We scan for a case-insensitive display-name match.
        const string fontsSubKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts";
        using var key = Registry.LocalMachine.OpenSubKey(fontsSubKey);
        if (key is null)
        {
            return null;
        }

        var styleWords = (bold, italic) switch
        {
            (true, true) => " bold italic",
            (true, false) => " bold",
            (false, true) => " italic",
            _ => "",
        };
        var wanted = family + styleWords;

        string? bestMatch = null;
        foreach (var name in key.GetValueNames())
        {
            // Display name is like "Arial (TrueType)" or "Arial Bold (TrueType)".
            // Strip the " (TrueType)" / " (OpenType)" trailer before matching.
            var displayName = name;
            var paren = displayName.IndexOf('(');
            if (paren > 0)
            {
                displayName = displayName.Substring(0, paren).TrimEnd();
            }

            if (!string.Equals(displayName, wanted, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (key.GetValue(name) is string file)
            {
                bestMatch = file;
                break;
            }
        }

        if (bestMatch is null)
        {
            return null;
        }

        // Registry values are usually bare filenames; prepend %WINDIR%\Fonts
        // when the stored value isn't already absolute.
        if (!Path.IsPathRooted(bestMatch))
        {
            var windir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            bestMatch = Path.Combine(windir, bestMatch);
        }
        return File.Exists(bestMatch) ? bestMatch : null;
    }
}
