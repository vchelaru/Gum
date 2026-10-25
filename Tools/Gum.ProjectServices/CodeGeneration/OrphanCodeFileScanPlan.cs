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

    /// <summary>
    /// Other folders to walk for generated files: a previous code root a settings edit moved away
    /// from. Never one inside <see cref="CodeRoot"/>, which is walked already.
    /// </summary>
    public IReadOnlyList<string> AdditionalCodeRoots { get; init; } = new List<string>();

    /// <summary>Where custom code sits relative to a generated file under the current settings; null means beside it.</summary>
    public CustomCodeMapping? CurrentMapping { get; init; }

    /// <summary>The mapping before the settings edit this scan follows, so custom files left in an old custom folder are found.</summary>
    public CustomCodeMapping? PreviousMapping { get; init; }

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

/// <summary>
/// Maps a generated file path to the custom (.cs) path that pairs with it when custom code lives in
/// its own folder: the part of the path under the generated folder is mirrored under the custom folder.
/// </summary>
public sealed class CustomCodeMapping
{
    private const string GeneratedSuffix = ".Generated.cs";

    /// <summary>Absolute generated code folder, with a trailing separator.</summary>
    public string GeneratedFolder { get; }

    /// <summary>Absolute custom code folder, with a trailing separator.</summary>
    public string CustomFolder { get; }

    public CustomCodeMapping(string generatedFolder, string customFolder)
    {
        GeneratedFolder = generatedFolder;
        CustomFolder = customFolder;
    }

    /// <summary>True when custom code is in a different folder than generated code.</summary>
    public bool IsSplit => !string.Equals(GeneratedFolder, CustomFolder, System.StringComparison.OrdinalIgnoreCase);

    /// <summary>The custom file path that pairs with <paramref name="generated"/> under this mapping.</summary>
    public FilePath CustomFor(FilePath generated)
    {
        string fullPath = generated.FullPath;
        string sibling = fullPath.Substring(0, fullPath.Length - GeneratedSuffix.Length) + ".cs";
        if (IsSplit && fullPath.StartsWith(GeneratedFolder, System.StringComparison.OrdinalIgnoreCase))
        {
            string relative = sibling.Substring(GeneratedFolder.Length);
            return System.IO.Path.GetFullPath(System.IO.Path.Combine(CustomFolder, relative));
        }
        return sibling;
    }
}
