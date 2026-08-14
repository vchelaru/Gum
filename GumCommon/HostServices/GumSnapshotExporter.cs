using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using ToolsUtilities;

// Companion to Gum.GumService / GumServiceSkiaBase (issue #4452 / #4460): the runtime-tree-to-project
// snapshot export logic both host families need. Lives in GumCommon so GumService (MonoGame/Raylib/
// Silk-partial family) and GumServiceSkiaBase (WPF/MAUI/standalone/Silk-on-Skia) share one
// implementation instead of duplicating it.
namespace Gum;

/// <summary>
/// Exports a live <see cref="GraphicalUiElement"/> tree to a Gum project on disk, so it can be opened
/// and inspected in the Gum tool. Composed by the owning service (<see cref="GumService.ExportSnapshot"/>
/// / <see cref="GumServiceSkiaBase.ExportSnapshot"/>) rather than static, matching
/// <see cref="GumHotReloadManager"/>'s shape: the one platform-specific piece (saving an embedded/
/// generated texture to a PNG) is supplied by the owning service via the constructor instead of being
/// hardcoded to a specific concrete engine.
/// </summary>
public class GumSnapshotExporter
{
    private readonly Action<IRuntimeSnapshotSerializer, string> _extractUnresolvedTextures;

    /// <param name="extractUnresolvedTextures">
    /// Saves any embedded/generated textures the serializer could not resolve to a file path (e.g. the
    /// Forms default visuals' shared sheet) next to the project, filling their placeholder SourceFile
    /// variables. Needs <c>Texture2D.SaveAsPng</c> (XNALIKE) or an engine-equivalent, so it's supplied by
    /// the host rather than implemented here. Omit (or pass <c>null</c>) on a host with no such
    /// capability — those textures stay unresolved, same as before this seam existed.
    /// </param>
    public GumSnapshotExporter(Action<IRuntimeSnapshotSerializer, string>? extractUnresolvedTextures = null)
    {
        _extractUnresolvedTextures = extractUnresolvedTextures ?? ((_, _) => { });
    }

    /// <summary>
    /// Exports the live UI tree under <paramref name="root"/> to a Gum project at <paramref name="filePath"/>,
    /// so it can be opened and inspected in the Gum tool. This is the headline path for code-only games,
    /// which have no design-time .gumx to open. Each runtime element is written as a standard-element
    /// instance and the screen is named after the file.
    /// </summary>
    /// <param name="root">The live tree to snapshot (typically the owning service's Root).</param>
    /// <param name="filePath">
    /// Destination project (.gumx) path. Its directory receives the Screens/ and Standards/ subfolders.
    /// </param>
    /// <param name="shake">
    /// When true (default), values equal to the standard-element default are pruned so the artifact is
    /// light and reads as "unedited" in the tool. When false, every value is written — heavier, but the
    /// always-correct baseline-free form.
    /// </param>
    public void ExportSnapshot(GraphicalUiElement root, string filePath, bool shake = true)
    {
        // Resolve to an absolute path up front. A bare/relative file name (e.g. "MyTestSnapshot.gumx", as
        // the samples pass) would otherwise make Path.GetDirectoryName below return "", skipping the whole
        // directory block that extracts embedded textures and copies referenced files -- leaving those
        // textures unresolved (blank in the tool). project.Save resolves relative paths against the current
        // directory anyway, so this changes only the directory computation, not where the project is written.
        filePath = Path.GetFullPath(filePath);

        // A code-only game may never have triggered standards population; ensure the catalog exists
        // before reading it (as the serializer's baseline) and writing it (as the project's standards).
        if (StandardElementsManager.Self.DefaultStates == null)
        {
            StandardElementsManager.Self.Initialize();
        }

        string screenName = Path.GetFileNameWithoutExtension(filePath);

        // Non-null here: the guard above initializes the catalog when it was missing. The baseline provider
        // lets the serializer collapse Forms-control subtrees (Button, CheckBox, ...) into synthesized
        // components by diffing each against the control type's pristine default-template visual.
        RuntimeSnapshotSerializer serializer = new(StandardElementsManager.Self.DefaultStates!,
            type => FrameworkElement.GetGraphicalUiElementForFrameworkElement(type));
        ScreenSave screen = serializer.CreateScreenSave(root, screenName, shake);

        GumProjectSave project = new();
        // A snapshot seeds the full default standards (the current native variable surface), so it
        // genuinely uses native-version features. Stamp NativeVersion explicitly -- the ctor default is
        // the older fallback for legacy files lacking a <Version>, which would make the tool's
        // variable-grid version gate hide the newer-only (v3 shape) variables. Matches the new-project
        // factories (ProjectManager.CreateNewProject, ProjectCreator.Create).
        project.Version = GumProjectSave.NativeVersion;
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(project);

        // Match the project's canvas resolution to the live canvas (the game's resolution) so the
        // snapshot lays out in the tool exactly as it did at runtime, rather than the 800x600 default.
        if (GraphicalUiElement.CanvasWidth > 0)
        {
            project.DefaultCanvasWidth = (int)GraphicalUiElement.CanvasWidth;
        }
        if (GraphicalUiElement.CanvasHeight > 0)
        {
            project.DefaultCanvasHeight = (int)GraphicalUiElement.CanvasHeight;
        }

        project.Screens.Add(screen);
        project.ScreenReferences.Add(new ElementReference { Name = screenName, ElementType = ElementType.Screen });

        // Forms-control subtrees collapse into reusable components (one per control type) plus thin instances.
        foreach (ComponentSave component in serializer.SynthesizedComponents)
        {
            project.Components.Add(component);
            project.ComponentReferences.Add(
                new ElementReference { Name = component.Name, ElementType = ElementType.Component });
        }

        EnsureReferencedStandardsExist(project, screen, serializer.SynthesizedComponents);

        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(Path.Combine(directory, ElementReference.ScreenSubfolder));
            Directory.CreateDirectory(Path.Combine(directory, ElementReference.StandardSubfolder));
            if (serializer.SynthesizedComponents.Count > 0)
            {
                Directory.CreateDirectory(Path.Combine(directory, ElementReference.ComponentSubfolder));
            }

            // Save embedded/generated textures (e.g. the Forms default visuals' shared sheet) to files and
            // fill their SourceFile paths BEFORE Save, so the written XML carries the resolved paths.
            _extractUnresolvedTextures(serializer, directory);
        }

        project.Save(filePath, saveElements: true);

        if (!string.IsNullOrEmpty(directory))
        {
            CopyReferencedFiles(serializer, screen, directory);
        }
    }

    // Instances may reference standard types the default seed omits -- notably deprecated ones like
    // ColoredRectangle, which new (v3) projects no longer include but an old/live tree may still contain.
    // Add any such referenced standard so the snapshot's instances don't dangle on a missing base type.
    // Synthesized components carry instances too, so their base types are checked alongside the screen's.
    private static void EnsureReferencedStandardsExist(GumProjectSave project, ScreenSave screen,
        IReadOnlyList<ComponentSave> components)
    {
        HashSet<string> existing = new(project.StandardElements.Select(standard => standard.Name));

        EnsureStandardsForInstances(project, screen.Instances, existing);
        foreach (ComponentSave component in components)
        {
            EnsureStandardsForInstances(project, component.Instances, existing);
        }
    }

    private static void EnsureStandardsForInstances(GumProjectSave project, IEnumerable<InstanceSave> instances,
        HashSet<string> existing)
    {
        foreach (InstanceSave instance in instances)
        {
            string baseType = instance.BaseType;
            if (string.IsNullOrEmpty(baseType) || existing.Contains(baseType))
            {
                continue;
            }

            if (StandardElementsManager.Self.IsDefaultType(baseType))
            {
                StandardElementsManager.Self.AddStandardElementSaveInstance(project, baseType);
                existing.Add(baseType);
            }
        }
    }

    // Bundles the files referenced by the snapshot (Sprite/NineSlice textures, ...) next to the project
    // so it opens self-contained in the tool. Relative references are copied preserving their relative
    // path; absolute references already resolve on their own, and missing files are skipped (logged).
    private static void CopyReferencedFiles(IRuntimeSnapshotSerializer serializer, ScreenSave screen, string snapshotDirectory)
    {
        // Textures extracted from embedded/generated sources are already written next to the project by
        // the extractUnresolvedTextures seam, yet their now-filled SourceFile paths also surface in
        // GetReferencedFiles. Skip them here: they don't exist under the content directory (so the copy
        // would just log a miss), and a coincidentally same-named content file must not clobber the
        // extracted PNG.
        HashSet<string> extractedPaths = new();
        foreach (UnresolvedTextureReference reference in serializer.UnresolvedTextureReferences)
        {
            if (reference.SourceFileVariable.Value is string extractedPath && !string.IsNullOrEmpty(extractedPath))
            {
                extractedPaths.Add(extractedPath);
            }
        }

        foreach (string referencedPath in serializer.GetReferencedFiles(screen))
        {
            if (extractedPaths.Contains(referencedPath))
            {
                continue;
            }

            if (!FileManager.IsRelative(referencedPath))
            {
                continue;
            }

            string absoluteSource;
            try
            {
                absoluteSource = FileManager.MakeAbsolute(referencedPath);
            }
            catch (ArgumentException)
            {
                continue;
            }

            if (!File.Exists(absoluteSource))
            {
                System.Diagnostics.Debug.WriteLine($"Snapshot: referenced file not found, skipping: {referencedPath}");
                continue;
            }

            string destination = Path.Combine(snapshotDirectory,
                referencedPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar));
            string? destinationDirectory = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }
            File.Copy(absoluteSource, destination, overwrite: true);
        }
    }

    // Pure orchestration over the serializer's unresolved textures: dedupe by texture instance, give each a
    // relative file name, persist it via the supplied saver, and write the resulting path into the placeholder
    // SourceFile variable. The saver seam keeps the GPU-bound texture save out of this method so the
    // dedup/path-fill logic is testable headlessly. A texture the saver declines (returns false) is left
    // unresolved -- its placeholder keeps its null value, rendering blank rather than dangling on a bad path.
    public static void FillUnresolvedTextureSourceFiles(
        IReadOnlyList<UnresolvedTextureReference> references, Func<object, string, bool> trySaveTexture)
    {
        // Dedupe by texture instance (not value): the one shared sheet -> one file, many filled placeholders.
        Dictionary<object, string> savedRelativePaths = new(ReferenceEqualityComparer.Instance);
        int index = 0;
        foreach (UnresolvedTextureReference reference in references)
        {
            if (!savedRelativePaths.TryGetValue(reference.Texture, out string? relativePath))
            {
                string candidate = $"EmbeddedTexture{index}.png";
                if (!trySaveTexture(reference.Texture, candidate))
                {
                    continue;
                }
                relativePath = candidate;
                index++;
                savedRelativePaths[reference.Texture] = relativePath;
            }
            reference.SourceFileVariable.Value = relativePath;
        }
    }
}
