using Gum.DataTypes;
using Gum.Managers;
using System.Collections.Generic;
using System.Linq;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Turns orphaned code files into GUM0005 rows. Shared by <see cref="HeadlessErrorChecker"/> (so
/// <c>gumcli check</c> and the tool's Errors tab say the same thing) and the tool's
/// <c>OrphanCodeFileReporter</c>, which adds the Delete File and Migrate actions.
/// </summary>
public static class OrphanCodeFileErrors
{
    /// <summary>Error code for an orphaned code file, registered in <see cref="ErrorDocsRegistry"/>.</summary>
    public const string ErrorCode = "GUM0005";

    /// <summary>
    /// One warning per orphan. Reads <see cref="ObjectFinder"/> to tell a file whose element was
    /// deleted from one left at an old path by a code settings change (its element still exists).
    /// </summary>
    public static List<ErrorResult> Create(IEnumerable<OrphanCodeFile> orphans) =>
        orphans.Select(Create).ToList();

    private static ErrorResult Create(OrphanCodeFile orphan)
    {
        bool elementExists = orphan.ElementName != null && ObjectFinder.Self.GetElementSave(orphan.ElementName) != null;
        string path = orphan.FilePath.FullPath;
        return new ErrorResult
        {
            Code = ErrorCode,
            Severity = ErrorSeverity.Warning,
            ElementName = orphan.ElementName ?? string.Empty,
            FilePath = path,
            Message = elementExists
                ? $"{Capitalized(GetKindDescription(orphan.Kind))} left at an old path by a code settings change: {path}"
                : $"Orphaned {GetKindDescription(orphan.Kind)}, no matching element in the project: {path}",
            Details = new OrphanCodeFileDetails { FilePath = path, Kind = orphan.Kind, ElementExists = elementExists }
        };
    }

    private static string Capitalized(string text) => char.ToUpperInvariant(text[0]) + text.Substring(1);

    private static string GetKindDescription(OrphanCodeFileKind kind) => kind switch
    {
        OrphanCodeFileKind.Generated => "generated code file",
        OrphanCodeFileKind.CustomCode => "custom code file",
        _ => "element code settings file"
    };
}
