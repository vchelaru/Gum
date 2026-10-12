using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ImportFromGumxPlugin.Services;

/// <summary>
/// Outcome of an import operation, including which asset files were copied or could not be found.
/// </summary>
public class ImportResult
{
    public List<string> CopiedAssets { get; } = new();
    /// <summary>Elements that were not imported because one or more of their required asset files could not be found.</summary>
    public List<string> SkippedElements { get; } = new();
    /// <summary>Elements whose destination files already exist. When non-empty the import was cancelled.</summary>
    public List<string> ConflictingElements { get; } = new();
    /// <summary>Why the import was refused because the source project is newer than the destination; null when it was not.</summary>
    public string? VersionRejection { get; set; }
}

/// <summary>
/// How the import service should treat selected elements whose destination file already exists.
/// </summary>
public enum ConflictResolution
{
    /// <summary>Default: abort the entire import and surface the conflict list.</summary>
    Cancel,
    /// <summary>Leave each conflicting destination file untouched; import only non-conflicting elements.</summary>
    Skip,
    /// <summary>Replace conflicting destination files with the source content.</summary>
    Overwrite,
}

/// <summary>
/// The user's explicit selections from the import preview dialog.
/// </summary>
public class ImportSelections
{
    public List<ComponentSave> DirectComponents { get; init; } = new();
    public List<ScreenSave> DirectScreens { get; init; } = new();
    public List<ComponentSave> TransitiveComponents { get; init; } = new();
    public List<BehaviorSave> Behaviors { get; init; } = new();
    public List<StandardElementSave> Standards { get; init; } = new();

    /// <summary>
    /// A copy without the given components, screens and behaviors (matched by reference);
    /// Standards are always kept.
    /// </summary>
    internal ImportSelections WithoutUnchanged(IReadOnlySet<object> unchanged) => new()
    {
        DirectComponents = DirectComponents.Where(c => !unchanged.Contains(c)).ToList(),
        DirectScreens = DirectScreens.Where(s => !unchanged.Contains(s)).ToList(),
        TransitiveComponents = TransitiveComponents.Where(c => !unchanged.Contains(c)).ToList(),
        Behaviors = Behaviors.Where(b => !unchanged.Contains(b)).ToList(),
        Standards = Standards,
    };
}

/// <summary>
/// Orchestrates the actual import of elements from a source <see cref="GumProjectSave"/> into the
/// destination project.
/// </summary>
public interface IGumxImportService
{
    /// <summary>
    /// Imports all selections from the source project into the destination.
    /// Import order: standards → transitive components (topological) → behaviors → direct components → screens → assets → save/reload.
    /// </summary>
    Task<ImportResult> ImportAsync(
        ImportSelections selections,
        GumProjectSave source,
        string sourceBase,
        string destinationSubfolder,
        ConflictResolution conflictResolution = ConflictResolution.Cancel);
}
