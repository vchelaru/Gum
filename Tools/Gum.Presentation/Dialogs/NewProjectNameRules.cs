using System;
using System.Linq;
using System.Text;

namespace Gum.Dialogs;

/// <summary>
/// What a project name may be. The same on every operating system, because a project made on one is opened on
/// another: the characters Windows forbids, control characters, a trailing dot or space, and the Windows device names.
/// </summary>
public static class NewProjectNameRules
{
    private const string ForbiddenCharacters = "<>:\"/\\|?*";

    private static readonly string[] ReservedNames =
        new[] { "CON", "PRN", "AUX", "NUL" }
            .Concat(Enumerable.Range(1, 9).Select(number => "COM" + number))
            .Concat(Enumerable.Range(1, 9).Select(number => "LPT" + number))
            .ToArray();

    private const int MaxFileNameBytes = 255;
    private const int GumExtensionLength = 5;
    private const int CsprojExtensionLength = 7;

    /// <summary>
    /// Whether <paramref name="name"/> (already trimmed) is too long for the longest file name creating the project
    /// writes: the name plus ".csproj" with a C# project, otherwise plus ".gumj"/".gumx". Counted in UTF-8 bytes,
    /// the strictest limit among the operating systems.
    /// </summary>
    public static bool IsTooLong(string name, bool includeCodeProject)
    {
        int extensionLength = includeCodeProject ? CsprojExtensionLength : GumExtensionLength;
        return Encoding.UTF8.GetByteCount(name) + extensionLength > MaxFileNameBytes;
    }

    /// <summary>Whether <paramref name="name"/> (already trimmed) can name the project folder and file on every OS.</summary>
    public static bool IsValidFolderName(string name)
    {
        if (name.Length == 0 || name.EndsWith('.') || name.EndsWith(' '))
        {
            return false;
        }
        if (name.Any(character => character < ' ' || ForbiddenCharacters.Contains(character)))
        {
            return false;
        }

        // A device name is reserved with any extension too ("CON.txt").
        int dot = name.IndexOf('.');
        string baseName = dot < 0 ? name : name.Substring(0, dot);
        return !ReservedNames.Contains(baseName.TrimEnd(' '), StringComparer.OrdinalIgnoreCase);
    }
}
