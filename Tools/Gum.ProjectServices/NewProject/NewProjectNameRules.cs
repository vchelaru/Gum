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
    /// <summary>Shown when <see cref="IsValidFolderName"/> is false. Says what to change.</summary>
    public const string InvalidFolderNameMessage =
        "The name can't be a folder name: avoid < > : \" / \\ | ? *, a trailing dot or space, and Windows device names such as CON, NUL or COM1.";

    /// <summary>Shown when <see cref="IsTooLong"/> is true.</summary>
    public const string NameTooLongMessage = "The name is too long.";

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
    /// The message for the first rule <paramref name="name"/> (already trimmed, not empty) breaks, or <see langword="null"/>
    /// when it can name the project.
    /// </summary>
    public static string? GetProblem(string name, bool includeCodeProject)
    {
        if (!IsValidFolderName(name))
        {
            return InvalidFolderNameMessage;
        }
        return IsTooLong(name, includeCodeProject) ? NameTooLongMessage : null;
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
