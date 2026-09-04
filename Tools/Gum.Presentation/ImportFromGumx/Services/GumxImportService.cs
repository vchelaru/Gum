using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.DataTypes.Serialization;
using Gum.DataTypes.Serialization.Json;
using Gum.StateAnimation.SaveClasses;
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
/// Orchestrates the actual import of elements from a source GumProjectSave into the destination
/// project. Writes files to disk in import order, then registers each via IImportLogic.
/// </summary>
public class GumxImportService : IGumxImportService
{
    private readonly IImportLogic _importLogic;
    private readonly IProjectState _projectState;
    private readonly IFileCommands _fileCommands;
    private readonly IGumxSourceService _sourceService;

    public GumxImportService(
        IImportLogic importLogic,
        IProjectState projectState,
        IFileCommands fileCommands,
        IGumxSourceService sourceService)
    {
        _importLogic = importLogic;
        _projectState = projectState;
        _fileCommands = fileCommands;
        _sourceService = sourceService;
    }

    private static readonly HashSet<string> _assetExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tga", ".achx", ".achj", ".ttf", ".otf", ".fnt" };

    /// <inheritdoc/>
    public async Task<ImportResult> ImportAsync(
        ImportSelections selections,
        GumProjectSave source,
        string sourceBase,
        string destinationSubfolder,
        ConflictResolution conflictResolution = ConflictResolution.Cancel)
    {
        string projectDir = _projectState.ProjectDirectory
            ?? throw new InvalidOperationException("No project is loaded");

        var result = new ImportResult();
        var nameMap = BuildNameMap(selections, destinationSubfolder);
        var qualifiedNameMap = BuildQualifiedNameMap(selections, destinationSubfolder);

        // Conflicts are file-level: a destination element file already exists at the same path.
        // The set of names is needed downstream regardless of resolution so writers can decide
        // whether to skip the file write (Skip) or skip the IImportLogic registration (Overwrite,
        // since the element is already known in-memory and will refresh on the post-import reload).
        var conflicts = FindConflictingElements(selections, nameMap, projectDir, IsDestinationJsonFormat);
        if (conflicts.Count > 0 && conflictResolution == ConflictResolution.Cancel)
        {
            result.ConflictingElements.AddRange(conflicts);
            return result;
        }
        var conflictNameSet = new HashSet<string>(conflicts, StringComparer.Ordinal);

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

        // 1. Standards (always overwritten by design — see GumxImportServiceTests on standards)
        foreach (var standard in selections.Standards.Where(s => !skippedElements.Contains(s)))
            await WriteAndImportStandardAsync(standard, source, sourceBase, projectDir);

        // 2. Transitive components — topological order (already sorted by GumxDependencyResolver)
        foreach (var component in selections.TransitiveComponents.Where(c => !skippedElements.Contains(c)))
            ImportComponentInMemory(component, projectDir, nameMap, qualifiedNameMap, conflictNameSet, conflictResolution);

        // 3. Behaviors (no asset dependencies)
        foreach (var behavior in selections.Behaviors)
            await WriteAndImportBehaviorAsync(behavior, source, sourceBase, projectDir, conflictNameSet, conflictResolution);

        // 4. Direct components
        foreach (var component in selections.DirectComponents.Where(c => !skippedElements.Contains(c)))
            ImportComponentInMemory(component, projectDir, nameMap, qualifiedNameMap, conflictNameSet, conflictResolution);

        // 5. Screens
        foreach (var screen in selections.DirectScreens.Where(s => !skippedElements.Contains(s)))
            ImportScreenInMemory(screen, projectDir, nameMap, qualifiedNameMap, conflictNameSet, conflictResolution);

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
    /// Copies the sibling state-animation file for each element, if one exists in the source. The
    /// naming convention is {elementName}Animations.ganx/.ganj, located in the same subfolder as
    /// the element file (e.g. Components/Controls/ButtonAnimations.ganx). Both source extensions
    /// are attempted, and whichever is found is re-serialized into the destination project's own
    /// format - a byte copy would put XML inside a .ganj the runtime then fails to parse
    /// (issue #4595). Silently skips elements that have neither file.
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

            string destPath = Path.Combine(projectDir, elementSubfolder,
                destName + ElementAnimationsSave.GetFileNameSuffix(IsDestinationJsonFormat));

            foreach (bool isSourceJson in new[] { false, true })
            {
                string relativeSourcePath = $"{elementSubfolder}/{sourceName}" +
                    ElementAnimationsSave.GetFileNameSuffix(isSourceJson);

                byte[]? bytes = await _sourceService.FetchBinaryAsync(relativeSourcePath, sourceBase);
                if (bytes == null)
                {
                    continue;
                }

                string content = System.Text.Encoding.UTF8.GetString(bytes).TrimStart('﻿');
                ElementAnimationsSave animations = isSourceJson
                    ? GumAnimationJsonFileSerializer.DeserializeElementAnimations(content)
                    : FileManager.XmlDeserializeFromStream<ElementAnimationsSave>(new MemoryStream(bytes));

                Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                animations.Save(destPath);
                break;
            }
        }
    }

    /// <summary>
    /// A conflict is a destination file that already exists, so every path here uses the
    /// destination project's own format - in a .gumj project the existing file is a .gucj, and
    /// probing for .gucx would report no conflict and then silently overwrite (issue #4595).
    /// </summary>
    private static List<string> FindConflictingElements(
        ImportSelections selections,
        Dictionary<string, string> nameMap,
        string projectDir,
        bool isJsonFormat)
    {
        var conflicts = new List<string>();

        foreach (var component in selections.TransitiveComponents.Concat(selections.DirectComponents))
        {
            string destName = nameMap.TryGetValue(component.Name, out var mapped) ? mapped : component.Name;
            string destPath = Path.Combine(projectDir, "Components", destName + "." + ComponentExtension(isJsonFormat));
            if (File.Exists(destPath))
                conflicts.Add(destName);
        }

        foreach (var screen in selections.DirectScreens)
        {
            string destName = nameMap.TryGetValue(screen.Name, out var mapped) ? mapped : screen.Name;
            string destPath = Path.Combine(projectDir, "Screens", destName + "." + ScreenExtension(isJsonFormat));
            if (File.Exists(destPath))
                conflicts.Add(destName);
        }

        foreach (var behavior in selections.Behaviors)
        {
            string destPath = Path.Combine(projectDir, "Behaviors", behavior.Name + "." + BehaviorExtension(isJsonFormat));
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

    /// <summary>
    /// Builds a map of qualified element names (e.g. "Components/Styles" → "Components/Theme/Styles")
    /// for use in rewriting VariableReferences entries whose right-hand sides reference other
    /// simultaneously-imported elements by qualified name.
    /// </summary>
    private static Dictionary<string, string> BuildQualifiedNameMap(
        ImportSelections selections,
        string destinationSubfolder)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(destinationSubfolder))
        {
            return map;
        }

        string subfolder = destinationSubfolder.TrimEnd('/');

        foreach (var component in selections.DirectComponents.Concat(selections.TransitiveComponents))
        {
            map[$"Components/{component.Name}"] = $"Components/{subfolder}/{component.Name}";
        }

        foreach (var screen in selections.DirectScreens)
        {
            map[$"Screens/{screen.Name}"] = $"Screens/{subfolder}/{screen.Name}";
        }

        return map;
    }

    private async Task WriteAndImportStandardAsync(
        StandardElementSave standard,
        GumProjectSave source,
        string sourceBase,
        string projectDir)
    {
        // Source extension follows the SOURCE project's format, destination the destination's -
        // the two are independent, so this is a re-serialize rather than a copy (issue #4595).
        bool isSourceJson = GumProjectSave.IsJsonFormat(source.FullFileName ?? "");
        string relativeSrc = $"Standards/{standard.Name}." + StandardExtension(isSourceJson);
        string destPath = Path.Combine(projectDir, "Standards",
            standard.Name + "." + StandardExtension(IsDestinationJsonFormat));

        if (await CopyElementFileAsync<StandardElementSave>(relativeSrc, sourceBase, destPath))
        {
            // Standards are not imported via IImportLogic — they are loaded on project reload.
            // Ensure the destination directory exists (done inside CopyElementFileAsync).
        }
    }

    /// <summary>
    /// Component path (issue #2839 refactor): clone the source ComponentSave in memory,
    /// mutate Name / BaseType / VariableReferences in-place on the clone, then hand it to
    /// IImportLogic which writes it once to its canonical destination. Replaces the prior
    /// fetch-XML-text → string-replace → write → deserialize → re-save dance, which raced
    /// the in-memory model and silently lost VariableReferences rewrites.
    /// </summary>
    private void ImportComponentInMemory(
        ComponentSave sourceComponent,
        string projectDir,
        Dictionary<string, string> nameMap,
        Dictionary<string, string> qualifiedNameMap,
        HashSet<string> conflictNameSet,
        ConflictResolution conflictResolution)
    {
        string sourceName = sourceComponent.Name;
        string destName = nameMap.TryGetValue(sourceName, out var mapped) ? mapped : sourceName;

        bool isConflict = conflictNameSet.Contains(destName);
        if (isConflict && conflictResolution == ConflictResolution.Skip) { return; }

        ComponentSave clone = CloneElement(sourceComponent);
        clone.Name = destName;
        RemapBaseTypes(clone, nameMap);
        RemapVariableReferences(clone, qualifiedNameMap);

        if (isConflict && conflictResolution == ConflictResolution.Overwrite)
        {
            // Element is already in the destination project; bypass IImportLogic registration
            // and write the file directly. The post-import save+reload picks up the new content.
            string destPath = Path.Combine(projectDir, "Components",
                destName + "." + ComponentExtension(IsDestinationJsonFormat));
            WriteElementToDisk(clone, destPath);
        }
        else
        {
            _importLogic.ImportComponent(clone, saveProject: false);
        }
    }

    /// <summary>
    /// Screen analogue of <see cref="ImportComponentInMemory"/>. Same shape — clone, mutate,
    /// hand to IImportLogic (or write directly on Overwrite-conflict).
    /// </summary>
    private void ImportScreenInMemory(
        ScreenSave sourceScreen,
        string projectDir,
        Dictionary<string, string> nameMap,
        Dictionary<string, string> qualifiedNameMap,
        HashSet<string> conflictNameSet,
        ConflictResolution conflictResolution)
    {
        string sourceName = sourceScreen.Name;
        string destName = nameMap.TryGetValue(sourceName, out var mapped) ? mapped : sourceName;

        bool isConflict = conflictNameSet.Contains(destName);
        if (isConflict && conflictResolution == ConflictResolution.Skip) { return; }

        ScreenSave clone = CloneElement(sourceScreen);
        clone.Name = destName;
        RemapBaseTypes(clone, nameMap);
        RemapVariableReferences(clone, qualifiedNameMap);

        if (isConflict && conflictResolution == ConflictResolution.Overwrite)
        {
            string destPath = Path.Combine(projectDir, "Screens",
                destName + "." + ScreenExtension(IsDestinationJsonFormat));
            WriteElementToDisk(clone, destPath);
        }
        else
        {
            _importLogic.ImportScreen(clone, saveProject: false);
        }
    }

    /// <summary>
    /// Clones an ElementSave via a serializer round-trip. Round-tripping (rather than mutating
    /// the source instance) protects the source GumProjectSave's in-memory copy, which may still
    /// be visible to the import preview dialog. <see cref="StateSave.FixEnumerations"/> is called
    /// after the round-trip for the same reason GumxSourceService applies it on load (issue #2810):
    /// keep int-on-disk enum values typed in-memory before downstream comparers see them.
    /// </summary>
    private static T CloneElement<T>(T source) where T : ElementSave, new()
    {
        var serializer = GumFileSerializer.GetCompactSerializer(typeof(T));
        using var writer = new StringWriter();
        serializer.Serialize(writer, source);
        using var reader = new StringReader(writer.ToString());
        var clone = (T)serializer.Deserialize(reader)!;

        foreach (var state in clone.AllStates)
        {
            state.FixEnumerations();
        }
        return clone;
    }

    /// <summary>
    /// Mirrors the BaseType column of the prior string-replace logic, but on the in-memory model:
    /// rewrites the element's own BaseType and every instance's BaseType when they point at
    /// another simultaneously-imported element.
    /// </summary>
    private static void RemapBaseTypes(ElementSave element, Dictionary<string, string> nameMap)
    {
        if (nameMap.Count == 0) { return; }

        if (element.BaseType != null && nameMap.TryGetValue(element.BaseType, out var newBase))
        {
            element.BaseType = newBase;
        }

        if (element.Instances != null)
        {
            foreach (var instance in element.Instances)
            {
                if (instance.BaseType != null && nameMap.TryGetValue(instance.BaseType, out var newInstanceBase))
                {
                    instance.BaseType = newInstanceBase;
                }
            }
        }
    }

    /// <summary>
    /// Issue #2839: rewrite the right-hand side of every VariableReferences entry whose qualified
    /// prefix (e.g. "Components/Styles") points at a simultaneously-imported element that is
    /// gaining the destination subfolder. Pattern mirrors RenameLogic.ApplyElementReferences.
    /// </summary>
    private static void RemapVariableReferences(ElementSave element, Dictionary<string, string> qualifiedNameMap)
    {
        if (qualifiedNameMap.Count == 0) { return; }

        foreach (var state in element.AllStates)
        {
            foreach (var variableList in state.VariableLists)
            {
                if (variableList.GetRootName() != "VariableReferences") { continue; }

                var values = variableList.ValueAsIList;
                for (int i = 0; i < values.Count; i++)
                {
                    if (values[i] is not string line) { continue; }

                    int eqIndex = line.IndexOf('=');
                    if (eqIndex < 0) { continue; }

                    string left = line.Substring(0, eqIndex).TrimEnd();
                    string right = line.Substring(eqIndex + 1).TrimStart();

                    foreach (var (oldQualified, newQualified) in qualifiedNameMap)
                    {
                        if (right.StartsWith(oldQualified + ".", StringComparison.Ordinal))
                        {
                            right = newQualified + right.Substring(oldQualified.Length);
                            break;
                        }
                    }

                    values[i] = $"{left} = {right}";
                }
            }
        }
    }

    /// <summary>
    /// Writes a cloned element to a specific destination path (used for Overwrite conflicts
    /// where IImportLogic registration is skipped because the element is already in the project).
    /// </summary>
    private void WriteElementToDisk(ElementSave element, string destPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
        bool useCompact = _projectState.GumProjectSave?.Version
            >= (int)GumProjectSave.GumxVersions.AttributeVersion;
        element.Save(destPath, useCompact);
    }

    private async Task WriteAndImportBehaviorAsync(
        BehaviorSave behavior,
        GumProjectSave source,
        string sourceBase,
        string projectDir,
        HashSet<string> conflictNameSet,
        ConflictResolution conflictResolution)
    {
        bool isConflict = conflictNameSet.Contains(behavior.Name);
        if (isConflict && conflictResolution == ConflictResolution.Skip) { return; }

        // Source extension follows the SOURCE project's format, destination the destination's
        // (issue #4595).
        bool isSourceJson = GumProjectSave.IsJsonFormat(source.FullFileName ?? "");
        var matchingReference = source.BehaviorReferences?.FirstOrDefault(item => item.Name == behavior.Name);
        string relativeSrc = matchingReference != null
            ? matchingReference.GetRelativeFilePath(isSourceJson)
            : $"Behaviors/{behavior.Name}." + BehaviorExtension(isSourceJson);
        string destPath = Path.Combine(projectDir, "Behaviors",
            behavior.Name + "." + BehaviorExtension(IsDestinationJsonFormat));

        if (await CopyBehaviorFileAsync(relativeSrc, sourceBase, destPath) && !isConflict)
        {
            _importLogic.ImportBehavior(new FilePath(destPath), saveProject: false);
        }
    }

    /// <summary>
    /// Standards path: file-copy preserves the source XML byte-for-byte. Standards are not
    /// renamed during import, so no in-memory mutation is required.
    /// </summary>
    /// <summary>
    /// Fetches a source element/behavior file and writes it to <paramref name="destPath"/> in the
    /// destination project's own format. The source and destination formats are independent - a
    /// .gumx source imported into a .gumj project has to be re-serialized, not byte-copied, or the
    /// destination gets XML inside a .gucj the project can never load (issue #4595).
    /// </summary>
    private async Task<bool> CopyElementFileAsync<T>(string relativeSourcePath, string sourceBase, string destPath)
        where T : ElementSave, new()
    {
        string? content = await _sourceService.FetchElementTextAsync(relativeSourcePath, sourceBase);
        if (content == null) return false;

        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

        T? element = DeserializeElementText<T>(relativeSourcePath, content);
        if (element == null) return false;

        element.Save(destPath, UseCompact);
        return true;
    }

    /// <summary>Behavior counterpart of <see cref="CopyElementFileAsync{T}"/>.</summary>
    private async Task<bool> CopyBehaviorFileAsync(string relativeSourcePath, string sourceBase, string destPath)
    {
        string? content = await _sourceService.FetchElementTextAsync(relativeSourcePath, sourceBase);
        if (content == null) return false;

        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

        BehaviorSave? behavior =
            string.Equals(FileManager.GetExtension(relativeSourcePath), BehaviorReference.JsonExtension,
                StringComparison.OrdinalIgnoreCase)
                ? GumJsonFileSerializer.DeserializeBehavior(content)
                : GumFileSerializer.DeserializeBehaviorSave(content, GumProjectSave.NativeVersion);
        if (behavior == null) return false;

        behavior.Save(destPath, UseCompact);
        return true;
    }

    /// <summary>
    /// Deserializes element text whose format is decided by <paramref name="relativeSourcePath"/>'s
    /// own extension - the source project's format, which need not match the destination's.
    /// </summary>
    private static T? DeserializeElementText<T>(string relativeSourcePath, string content)
        where T : ElementSave, new()
    {
        string extension = FileManager.GetExtension(relativeSourcePath);
        string jsonExtension = new T().GetFileExtension(isJsonFormat: true);

        return string.Equals(extension, jsonExtension, StringComparison.OrdinalIgnoreCase)
            ? GumJsonFileSerializer.DeserializeElement<T>(content)
            : GumFileSerializer.DeserializeElementSave<T>(content, GumProjectSave.NativeVersion);
    }

    private static string ComponentExtension(bool isJsonFormat) =>
        isJsonFormat ? GumProjectSave.ComponentJsonExtension : GumProjectSave.ComponentExtension;

    private static string ScreenExtension(bool isJsonFormat) =>
        isJsonFormat ? GumProjectSave.ScreenJsonExtension : GumProjectSave.ScreenExtension;

    private static string StandardExtension(bool isJsonFormat) =>
        isJsonFormat ? GumProjectSave.StandardJsonExtension : GumProjectSave.StandardExtension;

    private static string BehaviorExtension(bool isJsonFormat) =>
        isJsonFormat ? BehaviorReference.JsonExtension : BehaviorReference.Extension;

    /// <summary>True when the destination project is JSON-formatted (.gumj).</summary>
    private bool IsDestinationJsonFormat =>
        GumProjectSave.IsJsonFormat(_projectState.GumProjectSave?.FullFileName ?? "");

    private bool UseCompact =>
        _projectState.GumProjectSave?.Version >= (int)GumProjectSave.GumxVersions.AttributeVersion;
}
