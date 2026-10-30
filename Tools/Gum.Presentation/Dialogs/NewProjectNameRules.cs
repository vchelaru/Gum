using System;
using System.Linq;

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
