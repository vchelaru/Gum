using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using RenderingLibrary.Graphics.Fonts;

namespace Gum.Bundle;

/// <summary>
/// Walks a <see cref="GumProjectSave"/> and produces the set of files that need to be packaged
/// to make the project loadable from a `.gumpkg` bundle. The single source of truth for
/// "what does this project depend on" — `gumcli pack` consumes the walker's output.
/// </summary>
/// <remarks>
/// The walker is intentionally standalone — it does not depend on the <see cref="Gum.Managers.ObjectFinder"/>
/// singleton, so callers (CLI, build tasks) can pack a project without first wiring it into the tool.
/// </remarks>
public class GumProjectDependencyWalker
{
    /// <summary>
    /// Enumerates the files the given project depends on, partitioned by category.
    /// Missing files are reported via <see cref="WalkResult.MissingFiles"/> rather than thrown.
    /// </summary>
    /// <param name="project">The loaded project to walk. Element/screen/component lists must be populated.</param>
    /// <param name="projectRootDirectory">Directory that contains the `.gumx` and the `Screens/`, `Components/`, etc. subfolders.</param>
    /// <param name="inclusion">Which categories of files to enumerate.</param>
    public WalkResult Walk(GumProjectSave project, string projectRootDirectory, GumBundleInclusion inclusion)
    {
        return Walk(project, projectRootDirectory, inclusion, scopeElement: null);
    }

    /// <summary>
    /// Same as <see cref="Walk(GumProjectSave, string, GumBundleInclusion)"/> but, when
    /// <paramref name="scopeElement"/> is non-null, restricts the walk to that single element.
    /// In scoped mode the project-wide core files (every screen/component/standard/behavior)
    /// are NOT added — only the .gucx/.gutx files corresponding to the scope element's child
    /// instances and its own file/font references are emitted.
    /// </summary>
    public WalkResult Walk(GumProjectSave project, string projectRootDirectory, GumBundleInclusion inclusion, ElementSave? scopeElement)
    {
        if (project == null)
        {
            throw new ArgumentNullException(nameof(project));
        }
        if (projectRootDirectory == null)
        {
            throw new ArgumentNullException(nameof(projectRootDirectory));
        }

        HashSet<string> core = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> fontCache = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> external = new HashSet<string>(StringComparer.Ordinal);
        List<DependencyWarning> missing = new List<DependencyWarning>();

        if (scopeElement == null)
        {
            if (inclusion.HasFlag(GumBundleInclusion.Core))
            {
                CollectCoreFiles(project, projectRootDirectory, core, missing);
                CollectInstanceTypeCoreFiles(project, core);
            }

            if (inclusion.HasFlag(GumBundleInclusion.FontCache) || inclusion.HasFlag(GumBundleInclusion.ExternalFiles))
            {
                CollectFileAndFontReferences(project, projectRootDirectory, inclusion, fontCache, external, missing);
            }
        }
        else
        {
            CollectForSingleElement(project, scopeElement, projectRootDirectory, inclusion, core, fontCache, external, missing);
        }

        // Enforce precedence: Core wins over FontCache wins over External.
        fontCache.ExceptWith(core);
        external.ExceptWith(core);
        external.ExceptWith(fontCache);

        return new WalkResult(
            coreFiles: core.OrderBy(p => p, StringComparer.Ordinal).ToList(),
            fontCacheFiles: fontCache.OrderBy(p => p, StringComparer.Ordinal).ToList(),
            externalFiles: external.OrderBy(p => p, StringComparer.Ordinal).ToList(),
            missingFiles: missing);
    }

    private static void CollectCoreFiles(
        GumProjectSave project,
        string projectRootDirectory,
        HashSet<string> core,
        List<DependencyWarning> missing)
    {
        // .gumx itself — derive from FullFileName when present, else from any single .gumx in the root.
        string? gumxRelative = TryGetGumxRelativePath(project, projectRootDirectory);
        if (gumxRelative != null)
        {
            core.Add(gumxRelative);
        }

        foreach (ElementReference reference in project.ScreenReferences ?? new List<ElementReference>())
        {
            reference.ElementType = ElementType.Screen;
            AddElementReference(reference, projectRootDirectory, core, missing);
        }
        foreach (ElementReference reference in project.ComponentReferences ?? new List<ElementReference>())
        {
            reference.ElementType = ElementType.Component;
            AddElementReference(reference, projectRootDirectory, core, missing);
        }
        foreach (ElementReference reference in project.StandardElementReferences ?? new List<ElementReference>())
        {
            reference.ElementType = ElementType.Standard;
            AddElementReference(reference, projectRootDirectory, core, missing);
        }
        foreach (BehaviorReference reference in project.BehaviorReferences ?? new List<BehaviorReference>())
        {
            string relative = NormalizeRelative(BehaviorReference.Subfolder + "/" + reference.Name + "." + BehaviorReference.Extension);
            core.Add(relative);
            string fullPath = Path.Combine(projectRootDirectory, relative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath))
            {
                missing.Add(new DependencyWarning(relative, reference.Name ?? string.Empty,
                    $"Behavior file '{relative}' was not found on disk."));
            }
        }
    }

    private static void AddElementReference(
        ElementReference reference,
        string projectRootDirectory,
        HashSet<string> core,
        List<DependencyWarning> missing)
    {
        string relative = NormalizeRelative(reference.Subfolder + "/" + reference.Name + "." + reference.Extension);
        core.Add(relative);
        string fullPath = Path.Combine(projectRootDirectory, relative.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
        {
            missing.Add(new DependencyWarning(relative, reference.Name ?? string.Empty,
                $"Element file '{relative}' was not found on disk."));
        }
    }

    private static string? TryGetGumxRelativePath(GumProjectSave project, string projectRootDirectory)
    {
        if (!string.IsNullOrEmpty(project.FullFileName))
        {
            string fileName = Path.GetFileName(project.FullFileName);
            if (!string.IsNullOrEmpty(fileName))
            {
                return NormalizeRelative(fileName);
            }
        }
        if (Directory.Exists(projectRootDirectory))
        {
            string[] gumxFiles = Directory.GetFiles(projectRootDirectory, "*." + GumProjectSave.ProjectExtension, SearchOption.TopDirectoryOnly);
            if (gumxFiles.Length == 1)
            {
                return NormalizeRelative(Path.GetFileName(gumxFiles[0]));
            }
        }
        return null;
    }

    private static void CollectFileAndFontReferences(
        GumProjectSave project,
        string projectRootDirectory,
        GumBundleInclusion inclusion,
        HashSet<string> fontCache,
        HashSet<string> external,
        List<DependencyWarning> missing)
    {
        bool includeFontCache = inclusion.HasFlag(GumBundleInclusion.FontCache);
        bool includeExternal = inclusion.HasFlag(GumBundleInclusion.ExternalFiles);

        foreach (ElementSave element in project.AllElements)
        {
            // Element-level (no instance): default state can carry SourceFile / CustomFontFile.
            CollectFromState(element, instance: null, element.DefaultState, projectRootDirectory,
                includeFontCache, includeExternal, fontCache, external, missing);

            foreach (StateSave state in element.AllStates)
            {
                if (element.Instances == null)
                {
                    continue;
                }
                foreach (InstanceSave instance in element.Instances)
                {
                    CollectFromState(element, instance, state, projectRootDirectory,
                        includeFontCache, includeExternal, fontCache, external, missing);
                }
            }
        }

        // SinglePixelTextureFile is a project-level external reference.
        if (includeExternal && !string.IsNullOrEmpty(project.SinglePixelTextureFile))
        {
            AddExternalOrFontCache(project.SinglePixelTextureFile, ownerName: "(project)",
                projectRootDirectory, includeFontCache, includeExternal, fontCache, external, missing);
        }
    }

    private static void CollectInstanceTypeCoreFiles(GumProjectSave project, HashSet<string> core)
    {
        // Mirror the historical ObjectFinder behavior: for every instance, add the .gucx/.gutx
        // file for its BaseType. Projects loaded normally have ComponentReferences/StandardElementReferences
        // populated (and CollectCoreFiles already adds those), but projects built programmatically
        // — common in tests and headless tools — may only have Components/StandardElements lists.
        Dictionary<string, ElementSave> elementsByName = new Dictionary<string, ElementSave>(StringComparer.OrdinalIgnoreCase);
        foreach (ElementSave e in project.AllElements)
        {
            if (!string.IsNullOrEmpty(e.Name) && !elementsByName.ContainsKey(e.Name))
            {
                elementsByName[e.Name] = e;
            }
        }

        foreach (ElementSave element in project.AllElements)
        {
            if (element.Instances == null)
            {
                continue;
            }
            foreach (InstanceSave instance in element.Instances)
            {
                if (string.IsNullOrEmpty(instance.BaseType)
                    || !elementsByName.TryGetValue(instance.BaseType, out ElementSave? instanceElement))
                {
                    continue;
                }
                string? subfolder = instanceElement is ComponentSave ? "Components"
                    : instanceElement is StandardElementSave ? "Standards"
                    : null;
                if (subfolder != null)
                {
                    core.Add(NormalizeRelative(subfolder + "/" + instanceElement.Name + "." + instanceElement.FileExtension));
                }
            }
        }
    }

    private static void CollectForSingleElement(
        GumProjectSave project,
        ElementSave scopeElement,
        string projectRootDirectory,
        GumBundleInclusion inclusion,
        HashSet<string> core,
        HashSet<string> fontCache,
        HashSet<string> external,
        List<DependencyWarning> missing)
    {
        bool includeCore = inclusion.HasFlag(GumBundleInclusion.Core);
        bool includeFontCache = inclusion.HasFlag(GumBundleInclusion.FontCache);
        bool includeExternal = inclusion.HasFlag(GumBundleInclusion.ExternalFiles);
        bool includeFileOrFont = includeFontCache || includeExternal;

        // Build a name -> element lookup once for resolving instance BaseTypes; matches the
        // private dictionary the project-wide walk uses, scoped here to the single-element path.
        Dictionary<string, ElementSave> elementsByName = new Dictionary<string, ElementSave>(StringComparer.OrdinalIgnoreCase);
        foreach (ElementSave e in project.AllElements)
        {
            if (!string.IsNullOrEmpty(e.Name) && !elementsByName.ContainsKey(e.Name))
            {
                elementsByName[e.Name] = e;
            }
        }

        if (includeFileOrFont)
        {
            CollectFromState(scopeElement, instance: null, scopeElement.DefaultState, projectRootDirectory,
                includeFontCache, includeExternal, fontCache, external, missing);
        }

        if (scopeElement.Instances != null)
        {
            foreach (StateSave state in scopeElement.AllStates)
            {
                foreach (InstanceSave instance in scopeElement.Instances)
                {
                    if (includeCore && !string.IsNullOrEmpty(instance.BaseType)
                        && elementsByName.TryGetValue(instance.BaseType, out ElementSave? instanceElement))
                    {
                        string? subfolder = instanceElement is ComponentSave ? "Components"
                            : instanceElement is StandardElementSave ? "Standards"
                            : null;
                        if (subfolder != null)
                        {
                            string relative = NormalizeRelative(subfolder + "/" + instanceElement.Name + "." + instanceElement.FileExtension);
                            core.Add(relative);
                        }
                    }

                    if (includeFileOrFont)
                    {
                        CollectFromState(scopeElement, instance, state, projectRootDirectory,
                            includeFontCache, includeExternal, fontCache, external, missing);
                    }
                }
            }
        }
    }

    private static void CollectFromState(
        ElementSave element,
        InstanceSave? instance,
        StateSave? state,
        string projectRootDirectory,
        bool includeFontCache,
        bool includeExternal,
        HashSet<string> fontCache,
        HashSet<string> external,
        List<DependencyWarning> missing)
    {
        if (state == null)
        {
            return;
        }

        string ownerName = instance == null ? element.Name : element.Name + "." + instance.Name;
        string instancePrefix = instance == null ? string.Empty : instance.Name + ".";

        VariableSave? sourceFileVariable = state.GetVariableSave(instancePrefix + "SourceFile");
        if (sourceFileVariable?.Value is string sourceFile && !string.IsNullOrEmpty(sourceFile))
        {
            AddExternalOrFontCache(sourceFile, ownerName, projectRootDirectory,
                includeFontCache, includeExternal, fontCache, external, missing);
        }

        VariableSave? useCustomFontVariable = state.GetVariableSave(instancePrefix + "UseCustomFont");
        bool useCustomFont = useCustomFontVariable?.Value is bool b && b;

        if (useCustomFont)
        {
            VariableSave? customFontVariable = state.GetVariableSave(instancePrefix + "CustomFontFile");
            if (customFontVariable?.Value is string customFont && !string.IsNullOrEmpty(customFont))
            {
                AddExternalOrFontCache(customFont, ownerName, projectRootDirectory,
                    includeFontCache, includeExternal, fontCache, external, missing);
            }
        }
        else
        {
            // Derive font cache .fnt name from the standard font variables; this mirrors the runtime's
            // own derivation in BmfcSave.GetFontCacheFileNameFor.
            string? fontName = state.GetVariableSave(instancePrefix + "Font")?.Value as string;
            int? fontSize = state.GetVariableSave(instancePrefix + "FontSize")?.Value as int?;
            int? outlineThickness = state.GetVariableSave(instancePrefix + "OutlineThickness")?.Value as int?;
            bool? useFontSmoothing = state.GetVariableSave(instancePrefix + "UseFontSmoothing")?.Value as bool?;
            bool? isItalic = state.GetVariableSave(instancePrefix + "IsItalic")?.Value as bool?;
            bool? isBold = state.GetVariableSave(instancePrefix + "IsBold")?.Value as bool?;

            if (fontName != null && fontSize.HasValue && outlineThickness.HasValue
                && useFontSmoothing.HasValue && isItalic.HasValue && isBold.HasValue)
            {
                string fntRelative = BmfcSave.GetFontCacheFileNameFor(
                    fontSize.Value, fontName, outlineThickness.Value, useFontSmoothing.Value,
                    isItalic.Value, isBold.Value);
                AddFontCacheFntAndPages(fntRelative, ownerName, projectRootDirectory,
                    includeFontCache, includeExternal, fontCache, external, missing);
            }
        }
    }

    private static void AddExternalOrFontCache(
        string referencedPath,
        string ownerName,
        string projectRootDirectory,
        bool includeFontCache,
        bool includeExternal,
        HashSet<string> fontCache,
        HashSet<string> external,
        List<DependencyWarning> missing)
    {
        string relative = NormalizeRelative(referencedPath);
        bool isFontCache = relative.StartsWith("FontCache/", StringComparison.OrdinalIgnoreCase);

        if (isFontCache && includeFontCache)
        {
            fontCache.Add(relative);
        }
        else if (!isFontCache && includeExternal)
        {
            external.Add(relative);
        }
        else
        {
            return;
        }

        string fullPath = Path.Combine(projectRootDirectory, relative.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
        {
            missing.Add(new DependencyWarning(relative, ownerName,
                $"Referenced file '{relative}' was not found on disk."));
        }
    }

    private static void AddFontCacheFntAndPages(
        string fntRelative,
        string ownerName,
        string projectRootDirectory,
        bool includeFontCache,
        bool includeExternal,
        HashSet<string> fontCache,
        HashSet<string> external,
        List<DependencyWarning> missing)
    {
        string normalizedFnt = NormalizeRelative(fntRelative);
        AddExternalOrFontCache(normalizedFnt, ownerName, projectRootDirectory,
            includeFontCache, includeExternal, fontCache, external, missing);

        if (!includeFontCache)
        {
            return;
        }

        // Multi-page bitmap fonts emit MyFont.png OR MyFont_0.png, MyFont_1.png, etc.
        // Enumerate the FontCache directory for any .png whose name matches the .fnt base.
        string fntDirectoryRelative = Path.GetDirectoryName(normalizedFnt)?.Replace('\\', '/') ?? "FontCache";
        string fntBaseName = Path.GetFileNameWithoutExtension(normalizedFnt);
        string fntFullDir = Path.Combine(projectRootDirectory, fntDirectoryRelative.Replace('/', Path.DirectorySeparatorChar));

        if (!Directory.Exists(fntFullDir))
        {
            return;
        }

        // Match "BaseName.png" and "BaseName_<digits>.png".
        foreach (string pngPath in Directory.EnumerateFiles(fntFullDir, fntBaseName + "*.png"))
        {
            string pngFileName = Path.GetFileName(pngPath);
            string withoutExt = Path.GetFileNameWithoutExtension(pngFileName);
            if (withoutExt == fntBaseName || IsPagedSuffix(withoutExt, fntBaseName))
            {
                string pngRelative = NormalizeRelative(fntDirectoryRelative + "/" + pngFileName);
                fontCache.Add(pngRelative);
            }
        }
    }

    private static bool IsPagedSuffix(string candidate, string baseName)
    {
        if (!candidate.StartsWith(baseName + "_", StringComparison.Ordinal))
        {
            return false;
        }
        string suffix = candidate.Substring(baseName.Length + 1);
        return suffix.Length > 0 && suffix.All(char.IsDigit);
    }

    private static string NormalizeRelative(string path)
    {
        string normalized = path.Replace('\\', '/').TrimStart('/');
        return normalized;
    }
}
