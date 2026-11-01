using System;
using System.Linq;
using System.Text;

namespace Gum.ProjectServices;

/// <summary>
/// What a project name may be. The same on every operating system, because a project made on one is opened on
/// another: the characters Windows forbids, control characters, a trailing dot or space, and the Windows device names.
/// The one validator behind the New Project dialog, <see cref="NewProjectService"/> and <c>gumcli new</c>.
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

    /// <summary>
    /// A short sentence naming the first rule <paramref name="name"/> (already trimmed, not empty) breaks, or
    /// <see langword="null"/> when it can name the project.
    /// </summary>
    public static string? GetProblem(string name, bool includeCodeProject)
    {
        if (name.Any(character => character < ' ' || ForbiddenCharacters.Contains(character)))
        {
            return "The name contains characters a folder name cannot have: " + string.Join(' ', ForbiddenCharacters.Select(character => character.ToString()));
        }
        if (name.EndsWith('.') || name.EndsWith(' '))
        {
            return "A folder name cannot end with a dot or a space.";
        }
        string? reserved = GetReservedBaseName(name);
        if (reserved != null)
        {
            return $"{reserved} is a reserved Windows name. Choose another name.";
        }
        return IsTooLong(name, includeCodeProject)
            ? $"The name is too long: {MaxFileNameBytes - (includeCodeProject ? CsprojExtensionLength : GumExtensionLength)} bytes at most."
            : null;
    }

    // A device name is reserved with any extension too ("CON.txt"); returns it as typed, or null.
    private static string? GetReservedBaseName(string name)
    {
        int dot = name.IndexOf('.');
        string baseName = (dot < 0 ? name : name.Substring(0, dot)).TrimEnd(' ');
        return ReservedNames.Contains(baseName, StringComparer.OrdinalIgnoreCase) ? baseName : null;
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

        return GetReservedBaseName(name) == null;
    }
}
