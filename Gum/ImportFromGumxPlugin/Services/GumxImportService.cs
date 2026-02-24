using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Commands;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Plugins.ImportPlugin.Services;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ToolsUtilities;

namespace ImportFromGumxPlugin.Services;

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
}

/// <summary>
/// Orchestrates the actual import of elements from a source GumProjectSave into the destination
/// project. Writes files to disk in import order, then registers each via IImportLogic.
/// </summary>
public class GumxImportService
{
    private readonly IImportLogic _importLogic;
    private readonly IProjectState _projectState;
    private readonly IFileCommands _fileCommands;
    private readonly GumxSourceService _sourceService;

    public GumxImportService(
        IImportLogic importLogic,
        IProjectState projectState,
        IFileCommands fileCommands,
        GumxSourceService sourceService)
    {
        _importLogic = importLogic;
        _projectState = projectState;
        _fileCommands = fileCommands;
        _sourceService = sourceService;
    }

    /// <summary>
    /// Imports all selections from the source project into the destination.
    /// Import order: standards → transitive components (topological) → behaviors → direct components → screens → save/reload.
    /// </summary>
    public async Task ImportAsync(
        ImportSelections selections,
        GumProjectSave source,
        string sourceBase,
        string destinationSubfolder)
    {
        string projectDir = _projectState.ProjectDirectory
            ?? throw new InvalidOperationException("No project is loaded");

        // Build name-mapping from source names to destination names (with subfolder prefix if any)
        var nameMap = BuildNameMap(selections, destinationSubfolder);

        // 1. Standards — write to disk; they are picked up on project reload
        foreach (var standard in selections.Standards)
        {
            await WriteAndImportStandardAsync(standard, source, sourceBase, projectDir);
        }

        // 2. Transitive components — topological order (already sorted by GumxDependencyResolver)
        foreach (var component in selections.TransitiveComponents)
        {
            await WriteAndImportComponentAsync(component, source, sourceBase, projectDir, nameMap);
        }

        // 3. Behaviors
        foreach (var behavior in selections.Behaviors)
        {
            await WriteAndImportBehaviorAsync(behavior, source, sourceBase, projectDir);
        }

        // 4. Direct components
        foreach (var component in selections.DirectComponents)
        {
            await WriteAndImportComponentAsync(component, source, sourceBase, projectDir, nameMap);
        }

        // 5. Screens
        foreach (var screen in selections.DirectScreens)
        {
            await WriteAndImportScreenAsync(screen, source, sourceBase, projectDir, nameMap);
        }

        // 6. Save project then reload (standards take effect only after reload)
        var fileName = _projectState.GumProjectSave.FullFileName;
        bool wasSaved = _fileCommands.TryAutoSaveProject();
        if (wasSaved)
        {
            _fileCommands.LoadProject(fileName);
        }
    }

    private static Dictionary<string, string> BuildNameMap(
        ImportSelections selections,
        string destinationSubfolder)
    {
        var map = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(destinationSubfolder))
        {
            return map; // No remapping needed
        }

        var allComponents = selections.DirectComponents
            .Concat(selections.TransitiveComponents);

        foreach (var component in allComponents)
        {
            string newName = destinationSubfolder.TrimEnd('/') + "/" + component.Name;
            map[component.Name] = newName;
        }

        foreach (var screen in selections.DirectScreens)
        {
            string newName = destinationSubfolder.TrimEnd('/') + "/" + screen.Name;
            map[screen.Name] = newName;
        }

        return map;
    }

    private async Task WriteAndImportStandardAsync(
        StandardElementSave standard,
        GumProjectSave source,
        string sourceBase,
        string projectDir)
    {
        string relativeSrc = $"Standards/{standard.Name}.{GumProjectSave.StandardExtension}";
        string destPath = Path.Combine(projectDir, "Standards", $"{standard.Name}.{GumProjectSave.StandardExtension}");

        if (await CopyElementFileAsync(relativeSrc, sourceBase, destPath))
        {
            // Standards are not imported via IImportLogic — they are loaded on project reload.
            // Ensure the destination directory exists (done inside CopyElementFileAsync).
        }
    }

    private async Task WriteAndImportComponentAsync(
        ComponentSave component,
        GumProjectSave source,
        string sourceBase,
        string projectDir,
        Dictionary<string, string> nameMap)
    {
        string sourceName = component.Name;
        string destName = nameMap.TryGetValue(sourceName, out var mapped) ? mapped : sourceName;

        string relativeSrc = $"Components/{sourceName}.{GumProjectSave.ComponentExtension}";
        string destPath = Path.Combine(projectDir, "Components", $"{destName}.{GumProjectSave.ComponentExtension}");

        bool wrote = await CopyElementFileWithRemapAsync(
            relativeSrc, sourceBase, destPath, sourceName, destName, nameMap);

        if (wrote)
        {
            _importLogic.ImportComponent(new FilePath(destPath), saveProject: false);
        }
    }

    private async Task WriteAndImportBehaviorAsync(
        BehaviorSave behavior,
        GumProjectSave source,
        string sourceBase,
        string projectDir)
    {
        string relativeSrc = $"Behaviors/{behavior.Name}.{BehaviorReference.Extension}";
        string destPath = Path.Combine(projectDir, "Behaviors", $"{behavior.Name}.{BehaviorReference.Extension}");

        if (await CopyElementFileAsync(relativeSrc, sourceBase, destPath))
        {
            _importLogic.ImportBehavior(new FilePath(destPath), saveProject: false);
        }
    }

    private async Task WriteAndImportScreenAsync(
        ScreenSave screen,
        GumProjectSave source,
        string sourceBase,
        string projectDir,
        Dictionary<string, string> nameMap)
    {
        string sourceName = screen.Name;
        string destName = nameMap.TryGetValue(sourceName, out var mapped) ? mapped : sourceName;

        string relativeSrc = $"Screens/{sourceName}.{GumProjectSave.ScreenExtension}";
        string destPath = Path.Combine(projectDir, "Screens", $"{destName}.{GumProjectSave.ScreenExtension}");

        bool wrote = await CopyElementFileWithRemapAsync(
            relativeSrc, sourceBase, destPath, sourceName, destName, nameMap);

        if (wrote)
        {
            _importLogic.ImportScreen(new FilePath(destPath), saveProject: false);
        }
    }

    /// <summary>
    /// Copies an element file from source (local or URL) to the destination path.
    /// Returns true if the file was successfully written.
    /// </summary>
    private async Task<bool> CopyElementFileAsync(string relativeSourcePath, string sourceBase, string destPath)
    {
        string? content = await _sourceService.FetchElementTextAsync(relativeSourcePath, sourceBase);
        if (content == null) return false;

        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
        await File.WriteAllTextAsync(destPath, content);
        return true;
    }

    /// <summary>
    /// Copies an element file, remapping the element's Name and any BaseType references that
    /// point to other imported elements (from the name map). Returns true if file was written.
    /// </summary>
    private async Task<bool> CopyElementFileWithRemapAsync(
        string relativeSourcePath,
        string sourceBase,
        string destPath,
        string sourceName,
        string destName,
        Dictionary<string, string> nameMap)
    {
        string? content = await _sourceService.FetchElementTextAsync(relativeSourcePath, sourceBase);
        if (content == null) return false;

        // If there are name remappings, apply them to the file content before writing
        if (nameMap.Count > 0)
        {
            content = ApplyNameRemappings(content, sourceName, destName, nameMap);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
        await File.WriteAllTextAsync(destPath, content);
        return true;
    }

    /// <summary>
    /// Applies Name and BaseType remappings to the raw XML content of an element file.
    /// Uses simple string replacement on the serialized XML to avoid full deserialization.
    /// </summary>
    private static string ApplyNameRemappings(
        string content,
        string sourceName,
        string destName,
        Dictionary<string, string> nameMap)
    {
        // Remap the element's own name
        if (sourceName != destName)
        {
            // Match the Name element/attribute in the XML
            content = content.Replace($"<Name>{sourceName}</Name>", $"<Name>{destName}</Name>");
            content = content.Replace($" Name=\"{sourceName}\"", $" Name=\"{destName}\"");
        }

        // Remap BaseType references to other imported components
        foreach (var (oldName, newName) in nameMap)
        {
            if (oldName == sourceName) continue; // Already handled above

            content = content.Replace($"<BaseType>{oldName}</BaseType>", $"<BaseType>{newName}</BaseType>");
            content = content.Replace($" BaseType=\"{oldName}\"", $" BaseType=\"{newName}\"");
        }

        return content;
    }
}
