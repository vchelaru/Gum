using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using ImportFromGumxPlugin.ViewModels;
using System;
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
    /// <summary>
    /// Everything the import could not bring over (elements, behaviors, standards, animation files), by name.
    /// <see cref="SkippedReasons"/> says why. Nothing is dropped without an entry here.
    /// </summary>
    public List<string> SkippedElements { get; } = new();
    /// <summary>Why each <see cref="SkippedElements"/> entry was not imported, keyed by that entry.</summary>
    public Dictionary<string, string> SkippedReasons { get; } = new();

    /// <summary>Records that <paramref name="name"/> was not imported, and why. The first reason for a name wins.</summary>
    public void AddSkipped(string name, string reason)
    {
        if (!SkippedElements.Contains(name))
        {
            SkippedElements.Add(name);
        }
        SkippedReasons.TryAdd(name, reason);
    }

    /// <summary>Every file the import wrote before it finished (or failed), in order. Only files seen on disk.</summary>
    public List<string> WrittenFiles { get; } = new();

    /// <summary>
    /// Files of elements handed to <see cref="IImportLogic"/>, which writes them itself and reports a
    /// failed write only in a dialog. They join <see cref="WrittenFiles"/> once seen on disk.
    /// </summary>
    internal List<string> RegisteredFiles { get; } = new();

    /// <summary>
    /// The lines joined one per line, with all but the first <paramref name="max"/> collapsed into
    /// "(and N more)". Every list of skipped items shown to the user goes through this.
    /// </summary>
    public static string CapLines(IReadOnlyList<string> lines, int max = 5)
    {
        string text = string.Join("\n", lines.Take(max));
        return lines.Count > max ? text + $"\n(and {lines.Count - max} more)" : text;
    }

    /// <summary>One line per skipped item: its name, then its reason when known.</summary>
    public List<string> DescribeSkipped() => SkippedElements
        .Select(name => SkippedReasons.TryGetValue(name, out string? reason) ? $"{name}: {reason}" : name)
        .ToList();
    /// <summary>Elements whose destination files already exist. When non-empty the import was cancelled.</summary>
    public List<string> ConflictingElements { get; } = new();
    /// <summary>Why the import was refused because the source project is newer than the destination; null when it was not.</summary>
    public string? VersionRejection { get; set; }
    /// <summary>True when the imported files are on disk but the project could not be saved and reloaded, so the user must save and reopen it.</summary>
    public bool NeedsManualSave { get; set; }
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
    /// Which items the source lists but did not load (<see cref="GumxSourceProblems"/>) the import
    /// reports as skipped. Null reports all of them; a caller that imports only part of a source
    /// sets this to that part, and the rest is ignored.
    /// </summary>
    public Func<SourceProblem, bool>? ReportSourceProblem { get; init; }

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
        ReportSourceProblem = ReportSourceProblem,
    };
}

/// <summary>One source item: its kind and its name in the source project.</summary>
public readonly record struct ImportItemKey(ElementItemType Type, string Name);

/// <summary>
/// Which source items already exist in the destination project. An item in neither set is new.
/// </summary>
/// <param name="Differing">Items whose destination file exists with different content.</param>
/// <param name="Identical">Items whose destination file already equals what would be written.</param>
public record ImportDestinationStatus(IReadOnlySet<ImportItemKey> Differing, IReadOnlySet<ImportItemKey> Identical)
{
    /// <summary>No source item exists in the destination.</summary>
    public static ImportDestinationStatus None { get; } = new(new HashSet<ImportItemKey>(), new HashSet<ImportItemKey>());
}

/// <summary>
/// Orchestrates the actual import of elements from a source <see cref="GumProjectSave"/> into the
/// destination project.
/// </summary>
public interface IGumxImportService
{
    /// <summary>
    /// Reports, for every component, screen and behavior of <paramref name="source"/>, whether the
    /// destination project already has it (as <see cref="ImportAsync"/> would write it, with
    /// <paramref name="destinationSubfolder"/> applied) and whether that copy differs. Standards are
    /// not covered: an import always replaces them.
    /// </summary>
    ImportDestinationStatus GetDestinationStatus(GumProjectSave source, string destinationSubfolder);

    /// <summary>
    /// Imports all selections from the source project into the destination.
    /// Import order: standards → transitive components (topological) → behaviors → direct components → screens → assets → save/reload.
    /// </summary>
    /// <param name="beforeImport">
    /// Runs once the import is going ahead, before anything is written or registered, so the project
    /// can take on what the imported elements need. It does not run when the import is refused or
    /// stops at a conflict.
    /// </param>
    /// <param name="replaceStandardAnimations">
    /// Whether a selected Standard's animation file is replaced along with it (the default), or left
    /// as the project has it.
    /// </param>
    Task<ImportResult> ImportAsync(
        ImportSelections selections,
        GumProjectSave source,
        string sourceBase,
        string destinationSubfolder,
        ConflictResolution conflictResolution = ConflictResolution.Cancel,
        Action? beforeImport = null,
        bool replaceStandardAnimations = true);
}

/// <summary>
/// The import failed partway (for example a disk error), after <see cref="WrittenFiles"/> were
/// already written. The import writes in place, so those files are left behind.
/// </summary>
public class ImportFailedException : Exception
{
    public IReadOnlyList<string> WrittenFiles { get; }

    public ImportFailedException(Exception cause, IReadOnlyList<string> writtenFiles)
        : base(BuildMessage(cause, writtenFiles), cause)
    {
        WrittenFiles = writtenFiles;
    }

    private static string BuildMessage(Exception cause, IReadOnlyList<string> writtenFiles) =>
        writtenFiles.Count == 0
            ? $"{cause.Message.Split('\n')[0].Trim()}; nothing had been written yet."
            : $"{cause.Message.Split('\n')[0].Trim()}; {writtenFiles.Count} file(s) were already written:\n"
              + ImportResult.CapLines(writtenFiles);
}
