using Gum.ProjectServices;

namespace Gum.Plugins.Errors;

/// <summary>
/// Resolves a GUM0008 row: changes the reference to the spelling the file has on disk.
/// </summary>
public interface IFileCaseMismatchFixer
{
    /// <summary>
    /// True when the project stores the referenced path somewhere that can be rewritten (a file variable, the
    /// single pixel texture setting, a localization file). False for a reference inside another file or an
    /// element's own file name, which the row can only point at.
    /// </summary>
    bool CanFix(FileNameCaseMismatchDetails details);

    /// <summary>
    /// Replaces the reference through the tool's undo, save and plugin notification path. Tells the user
    /// instead when two files differ only by case, since the on-disk spelling is then ambiguous.
    /// </summary>
    void Fix(FileNameCaseMismatchDetails details);
}
