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
/// Outcome of an import operation, including which asset files were copied or could not be found.
/// </summary>
public class ImportResult
{
    public List<string> CopiedAssets { get; } = new();
    /// <summary>Elements that were not imported because one or more of their required asset files could not be found.</summary>
    public List<string> SkippedElements { get; } = new();
    /// <summary>Elements whose destination files already exist. When non-empty the import was cancelled.</summary>
    public List<string> ConflictingElements { get; } = new();
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

    private static readonly HashSet<string> _assetExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tga", ".achx", ".ttf", ".otf", ".fnt" };

    /// <summary>
    /// Imports all selections from the source project into the destination.
    /// Import order: standards → transitive components (topological) → behaviors → direct components → screens → assets → save/reload.
    /// </summary>
    public async Task<ImportResult> ImportAsync(
        ImportSelections selections,
        GumProjectSave source,
        string sourceBase,
        string destinationSubfolder)
    {
        string projectDir = _projectState.ProjectDirectory
            ?? throw new InvalidOperationException("No project is loaded");

        var result = new ImportResult();
        var nameMap = BuildNameMap(selections, destinationSubfolder);

        // Cancel early if any destination files already exist
        var conflicts = FindConflictingElements(selections, nameMap, projectDir);
        if (conflicts.Count > 0)
        {
            result.ConflictingElements.AddRange(conflicts);
            return result;
        }

        // Pre-fetch all assets for all candidate elements in parallel, so we can gate each
        // element's import on whether its required assets are actually available.
        var allCandidateElements = selections.Standards.Cast<ElementSave>()
            .Concat(selections.TransitiveComponents)
            .Concat(selections.DirectComponents)
            .Concat(selections.DirectScreens)
            .ToList();

        var assetsByElement = allCandidateElements.ToDictionary(
            e => e,
            e => CollectReferencedAssetPaths(new[] { e }));

        var allAssetPaths = assetsByElement.Values
            .SelectMany(p => p)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var fetchTasks = allAssetPaths
            .Select(async p => (path: p, bytes: await _sourceService.FetchBinaryAsync(p.Replace('\\', '/'), sourceBase)))
            .ToList();
        var assetCache = (await Task.WhenAll(fetchTasks))
            .ToDictionary(t => t.path, t => t.bytes, StringComparer.OrdinalIgnoreCase);

        var skippedElements = allCandidateElements
            .Where(e => assetsByElement[e].Any(p => assetCache.TryGetValue(p, out var b) ? b == null : false))
            .ToHashSet();

        foreach (var element in skippedElements)
            result.SkippedElements.Add(element.Name);

        // 1. Standards
        foreach (var standard in selections.Standards.Where(s => !skippedElements.Contains(s)))
            await WriteAndImportStandardAsync(standard, source, sourceBase, projectDir);

        // 2. Transitive components — topological order (already sorted by GumxDependencyResolver)
        foreach (var component in selections.TransitiveComponents.Where(c => !skippedElements.Contains(c)))
            await WriteAndImportComponentAsync(component, source, sourceBase, projectDir, nameMap);

        // 3. Behaviors (no asset dependencies)
        foreach (var behavior in selections.Behaviors)
            await WriteAndImportBehaviorAsync(behavior, source, sourceBase, projectDir);

        // 4. Direct components
        foreach (var component in selections.DirectComponents.Where(c => !skippedElements.Contains(c)))
            await WriteAndImportComponentAsync(component, source, sourceBase, projectDir, nameMap);

        // 5. Screens
        foreach (var screen in selections.DirectScreens.Where(s => !skippedElements.Contains(s)))
            await WriteAndImportScreenAsync(screen, source, sourceBase, projectDir, nameMap);

        // 6. Write pre-fetched asset files to disk for imported elements
        var importedElements = allCandidateElements.Where(e => !skippedElements.Contains(e));
        await CopyCachedAssetsAsync(importedElements, assetCache, projectDir, result);

        // 7. Copy sibling .ganx (state animation) files for each imported element
        var allComponents = selections.TransitiveComponents.Cast<ElementSave>()
            .Concat(selections.DirectComponents)
            .Where(e => !skippedElements.Contains(e));
        await CopyGanxFilesAsync(allComponents, "Components", nameMap, sourceBase, projectDir);
        await CopyGanxFilesAsync(
            selections.DirectScreens.Cast<ElementSave>().Where(e => !skippedElements.Contains(e)),
            "Screens", nameMap, sourceBase, projectDir);

        // 8. Save project then reload (standards take effect only after reload)
        var fileName = _projectState.GumProjectSave.FullFileName;
        bool wasSaved = _fileCommands.TryAutoSaveProject();
        if (wasSaved)
            _fileCommands.LoadProject(fileName);

        return result;
    }

    /// <summary>
    /// Scans all states of each element for VariableSave entries whose value looks like
    /// an asset file path (known extension). Excludes paths inside a FontCache folder.
    /// Returns each unique path once.
    /// </summary>
    private static List<string> CollectReferencedAssetPaths(IEnumerable<ElementSave> elements)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();
        foreach (var element in elements)
        {
            foreach (var state in element.AllStates)
            {
                foreach (var variable in state.Variables)
                {
                    if (variable.Value is string stringValue && !string.IsNullOrEmpty(stringValue))
                    {
                        if (_assetExtensions.Contains(Path.GetExtension(stringValue))
                            && !IsFontCachePath(stringValue)
                            && seen.Add(stringValue))
                        {
                            result.Add(stringValue);
                        }
                    }
                }
            }
        }
        return result;
    }

    private static bool IsFontCachePath(string relativePath) =>
        relativePath.IndexOf("FontCache/", StringComparison.OrdinalIgnoreCase) >= 0
        || relativePath.IndexOf("FontCache\\", StringComparison.OrdinalIgnoreCase) >= 0;

    /// <summary>
    /// Writes pre-fetched asset bytes to disk for each imported element.
    /// Assets were already fetched and cached before import; elements with missing assets
    /// were excluded from importedElements, so every path here should have cached bytes.
    /// </summary>
    private async Task CopyCachedAssetsAsync(
        IEnumerable<ElementSave> importedElements,
        Dictionary<string, byte[]?> assetCache,
        string projectDir,
        ImportResult result)
    {
        var assetPaths = CollectReferencedAssetPaths(importedElements);
        foreach (var relativePath in assetPaths)
        {
            string normalized = relativePath
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
            string destPath = Path.Combine(projectDir, normalized);

            if (File.Exists(destPath))
            {
                result.CopiedAssets.Add(relativePath);
                continue;
            }

            if (assetCache.TryGetValue(relativePath, out var bytes) && bytes != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                await File.WriteAllBytesAsync(destPath, bytes);
                result.CopiedAssets.Add(relativePath);
            }
        }
    }

    /// <summary>
    /// Copies the sibling .ganx (state animation) file for each element, if one exists in the source.
    /// The naming convention is {elementName}Animations.ganx, located in the same subfolder
    /// as the element file (e.g. Components/Controls/ButtonAnimations.ganx).
    /// Silently skips elements that have no .ganx file.
    /// </summary>
    private async Task CopyGanxFilesAsync(
        IEnumerable<ElementSave> elements,
        string elementSubfolder,
        Dictionary<string, string> nameMap,
        string sourceBase,
        string projectDir)
    {
        foreach (var element in elements)
        {
            string sourceName = element.Name;
            string destName = nameMap.TryGetValue(sourceName, out var mapped) ? mapped : sourceName;

            string relativeSourcePath = $"{elementSubfolder}/{sourceName}Animations.ganx";
            string destPath = Path.Combine(projectDir, elementSubfolder, $"{destName}Animations.ganx");

            byte[]? bytes = await _sourceService.FetchBinaryAsync(relativeSourcePath, sourceBase);
            if (bytes != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                await File.WriteAllBytesAsync(destPath, bytes);
            }
        }
    }

    private static List<string> FindConflictingElements(
        ImportSelections selections,
        Dictionary<string, string> nameMap,
        string projectDir)
    {
        var conflicts = new List<string>();

        foreach (var component in selections.TransitiveComponents.Concat(selections.DirectComponents))
        {
            string destName = nameMap.TryGetValue(component.Name, out var mapped) ? mapped : component.Name;
            string destPath = Path.Combine(projectDir, "Components", $"{destName}.{GumProjectSave.ComponentExtension}");
            if (File.Exists(destPath))
                conflicts.Add(destName);
        }

        foreach (var screen in selections.DirectScreens)
        {
            string destName = nameMap.TryGetValue(screen.Name, out var mapped) ? mapped : screen.Name;
            string destPath = Path.Combine(projectDir, "Screens", $"{destName}.{GumProjectSave.ScreenExtension}");
            if (File.Exists(destPath))
                conflicts.Add(destName);
        }

        foreach (var behavior in selections.Behaviors)
        {
            string destPath = Path.Combine(projectDir, "Behaviors", $"{behavior.Name}.{BehaviorReference.Extension}");
            if (File.Exists(destPath))
                conflicts.Add(behavior.Name);
        }

        return conflicts;
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
