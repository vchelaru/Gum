using System.Collections.Generic;
using ToolsUtilities;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Everything an orphan scan needs from the project, captured up front by
/// <see cref="IOrphanCodeFileScanService.CreatePlan"/> so that
/// <see cref="IOrphanCodeFileScanService.Execute"/> can walk the disk on a worker thread without
/// touching the live <see cref="Gum.DataTypes.GumProjectSave"/>.
/// </summary>
public class OrphanCodeFileScanPlan
{
    /// <summary>
    /// The resolved code output folder to walk for generated files, or null when none is
    /// configured or it does not exist.
    /// </summary>
    public string? CodeRoot { get; }

    /// <summary>Generated file paths that belong to an element in the project.</summary>
    public IReadOnlySet<FilePath> ExpectedGenerated { get; }

    /// <summary>Custom code file paths that belong to an element in the project.</summary>
    public IReadOnlySet<FilePath> ExpectedCustom { get; }

    /// <summary>Folders holding per-element <c>.codsj</c> settings files.</summary>
    public IReadOnlyList<string> ElementSettingsDirectories { get; }

    /// <summary><c>.codsj</c> settings file paths that belong to an element in the project.</summary>
    public IReadOnlySet<FilePath> ExpectedElementSettings { get; }

    public OrphanCodeFileScanPlan(
        string? codeRoot,
        IReadOnlySet<FilePath> expectedGenerated,
        IReadOnlySet<FilePath> expectedCustom,
        IReadOnlyList<string> elementSettingsDirectories,
        IReadOnlySet<FilePath> expectedElementSettings)
    {
        CodeRoot = codeRoot;
        ExpectedGenerated = expectedGenerated;
        ExpectedCustom = expectedCustom;
        ElementSettingsDirectories = elementSettingsDirectories;
        ExpectedElementSettings = expectedElementSettings;
    }
}

/// <summary>
/// The outcome of <see cref="IOrphanCodeFileScanService.Execute"/>.
/// </summary>
public class OrphanCodeFileScanResult
{
    /// <summary>The orphaned files found.</summary>
    public IReadOnlyList<OrphanCodeFile> Orphans { get; }

    /// <summary>
    /// True when the walk of <see cref="CodeRoot"/> stopped at its folder limit, so
    /// <see cref="Orphans"/> may be incomplete. Usually means the code root points at the wrong folder.
    /// </summary>
    public bool IsTruncated { get; }

    /// <summary>The code output folder that was walked, or null when there was none.</summary>
    public string? CodeRoot { get; }

    public OrphanCodeFileScanResult(IReadOnlyList<OrphanCodeFile> orphans, bool isTruncated, string? codeRoot)
    {
        Orphans = orphans;
        IsTruncated = isTruncated;
        CodeRoot = codeRoot;
    }
}
