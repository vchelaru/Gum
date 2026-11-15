using Gum.DataTypes;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Supplies the orphaned code files of a project to <see cref="HeadlessErrorChecker"/>, which turns them into
/// GUM0005 rows. A headless caller scans the disk (<see cref="HeadlessOrphanCodeFileProvider"/>).
/// </summary>
public interface IOrphanCodeFileProvider
{
    /// <summary>The orphans in <paramref name="project"/>, or null when the project has none to find.</summary>
    OrphanCodeFileScanResult? GetOrphans(GumProjectSave project);
}
