using Gum.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ToolsUtilities;

namespace Gum.ProjectServices.CodeGeneration;

/// <inheritdoc cref="IOrphanCodeFileScanService"/>
/// <remarks>
/// Detection is best effort in one direction only: it never flags a file Gum did not write.
/// A <c>.Generated.cs</c> file counts as Gum's only when it carries the <c>//Code for</c> header
/// code generation emits, and a custom <c>.cs</c> file counts only when its <c>.Generated.cs</c>
/// sibling is itself an orphan. Extra hand-written partials (<c>Foo.Input.cs</c>) and generated
/// files predating the header are therefore invisible to the scan. Elements set to
/// <see cref="GenerationBehavior.NeverGenerate"/>, and elements whose source file is missing, keep
/// their files off the orphan list — the user is hand-managing the former, and the latter is
/// already surfaced as a missing-source-file element.
/// </remarks>
public class OrphanCodeFileScanService : IOrphanCodeFileScanService
{
    /// <summary>
    /// First-line marker code generation writes into every generated file. Used as the "Gum wrote
    /// this" proof so third-party <c>.Generated.cs</c> files sharing the output folder are left alone.
    /// </summary>
    private const string GeneratedFileHeaderPrefix = "//Code for ";

    private const string GeneratedFileSuffix = ".Generated.cs";

    private const string ElementSettingsExtension = ".codsj";

    private readonly CodeGenerator _codeGenerator;
    private readonly CodeGenerationFileLocationsService _fileLocationsService;
    private readonly CodeOutputElementSettingsManager _elementSettingsManager;
    private readonly IProjectDirectoryProvider _projectDirectoryProvider;

    public OrphanCodeFileScanService(
        CodeGenerator codeGenerator,
        CodeGenerationFileLocationsService fileLocationsService,
        CodeOutputElementSettingsManager elementSettingsManager,
        IProjectDirectoryProvider projectDirectoryProvider)
    {
        _codeGenerator = codeGenerator;
        _fileLocationsService = fileLocationsService;
        _elementSettingsManager = elementSettingsManager;
        _projectDirectoryProvider = projectDirectoryProvider;
    }

    /// <inheritdoc/>
    public IReadOnlyList<OrphanCodeFile> Scan(GumProjectSave project, CodeOutputProjectSettings projectSettings)
    {
        List<OrphanCodeFile> orphans = new List<OrphanCodeFile>();

        AddCodeFileOrphans(project, projectSettings, orphans);
        AddElementSettingsOrphans(project, orphans);

        return orphans;
    }

    private void AddCodeFileOrphans(GumProjectSave project, CodeOutputProjectSettings projectSettings,
        List<OrphanCodeFile> orphans)
    {
        ///////////////////Early Out///////////////////
        if (string.IsNullOrEmpty(projectSettings.CodeProjectRoot))
        {
            // Nothing to compare the project against, so nothing can be proven orphaned.
            return;
        }
        /////////////////End Early Out/////////////////

        string codeRoot = projectSettings.CodeProjectRoot;
        if (FileManager.IsRelative(codeRoot))
        {
            if (string.IsNullOrEmpty(_projectDirectoryProvider.ProjectDirectory))
            {
                return;
            }
            codeRoot = _projectDirectoryProvider.ProjectDirectory + codeRoot;
        }

        FilePath codeRootPath = Normalize(codeRoot);
        if (!Directory.Exists(codeRootPath.FullPath))
        {
            return;
        }

        HashSet<FilePath> expectedGenerated = new HashSet<FilePath>();
        HashSet<FilePath> expectedCustom = new HashSet<FilePath>();

        foreach (ElementSave element in GetCodeGeneratedElements(project))
        {
            CodeOutputElementSettings elementSettings = _elementSettingsManager.LoadOrCreateSettingsFor(element);
            VisualApi visualApi = _codeGenerator.GetVisualApiForElement(element);

            FilePath? generatedFileName = _fileLocationsService.GetGeneratedFileName(
                element, elementSettings, projectSettings, visualApi);

            if (generatedFileName != null)
            {
                expectedGenerated.Add(Normalize(generatedFileName));
                expectedCustom.Add(Normalize(GetCustomCodePathFor(generatedFileName)));
            }
        }

        FilePath? fallbackFileName = _fileLocationsService.GetStandardElementsFallbackFileName(projectSettings);
        if (fallbackFileName != null)
        {
            // Owned by the project rather than any single element, so it is never an orphan.
            expectedGenerated.Add(Normalize(fallbackFileName));
        }

        foreach (string file in EnumerateGeneratedFilesPruningBuildOutput(codeRootPath.FullPath))
        {
            FilePath generatedPath = Normalize(file);
            if (expectedGenerated.Contains(generatedPath) || IsInBuildOutputFolder(generatedPath))
            {
                continue;
            }

            string? elementName = TryReadGeneratedHeaderElementName(generatedPath);
            if (elementName == null)
            {
                // Not written by Gum's code generation - leave it alone.
                continue;
            }

            orphans.Add(new OrphanCodeFile(generatedPath, OrphanCodeFileKind.Generated, elementName));

            FilePath customCodePath = GetCustomCodePathFor(generatedPath);
            if (!expectedCustom.Contains(customCodePath) && File.Exists(customCodePath.FullPath))
            {
                orphans.Add(new OrphanCodeFile(customCodePath, OrphanCodeFileKind.CustomCode, elementName));
            }
        }
    }

    private void AddElementSettingsOrphans(GumProjectSave project, List<OrphanCodeFile> orphans)
    {
        string? projectDirectory = _projectDirectoryProvider.ProjectDirectory;

        ///////////////////Early Out///////////////////
        if (string.IsNullOrEmpty(projectDirectory))
        {
            return;
        }
        /////////////////End Early Out/////////////////

        HashSet<FilePath> expected = new HashSet<FilePath>();

        IEnumerable<ElementSave> allElements = project.Screens.Cast<ElementSave>()
            .Concat(project.Components)
            .Concat(project.StandardElements);

        foreach (ElementSave element in allElements)
        {
            FilePath? settingsPath = _elementSettingsManager.GetCodeSettingsFilePath(element);
            if (settingsPath != null)
            {
                expected.Add(Normalize(settingsPath));
            }
        }

        string[] elementSubfolders =
        {
            ElementReference.ScreenSubfolder,
            ElementReference.ComponentSubfolder,
            ElementReference.StandardSubfolder
        };

        foreach (string subfolder in elementSubfolders)
        {
            FilePath directory = Normalize(projectDirectory + subfolder);
            if (!Directory.Exists(directory.FullPath))
            {
                continue;
            }

            foreach (string file in Directory.EnumerateFiles(directory.FullPath, "*" + ElementSettingsExtension,
                SearchOption.AllDirectories))
            {
                FilePath settingsPath = Normalize(file);
                if (expected.Contains(settingsPath))
                {
                    continue;
                }

                orphans.Add(new OrphanCodeFile(settingsPath, OrphanCodeFileKind.ElementSettings,
                    settingsPath.CaseSensitiveNoPathNoExtension));
            }
        }
    }

    /// <summary>
    /// Screens and Components that code generation is responsible for. Elements whose source file is
    /// missing are included so a temporarily absent .gucx/.gusx does not make its code look orphaned.
    /// </summary>
    private static IEnumerable<ElementSave> GetCodeGeneratedElements(GumProjectSave project) =>
        project.Screens.Cast<ElementSave>().Concat(project.Components);

    /// <summary>
    /// Walks <paramref name="root"/> for <c>*.Generated.cs</c> files, skipping <c>bin</c>/<c>obj</c>
    /// subtrees during traversal rather than enumerating into them and filtering afterward - build
    /// output can hold thousands of directory entries that are never going to match.
    /// </summary>
    private static IEnumerable<string> EnumerateGeneratedFilesPruningBuildOutput(string root)
    {
        Queue<string> directories = new Queue<string>();
        directories.Enqueue(root);

        while (directories.Count > 0)
        {
            string directory = directories.Dequeue();

            foreach (string file in Directory.EnumerateFiles(directory, "*" + GeneratedFileSuffix))
            {
                yield return file;
            }

            foreach (string subdirectory in Directory.EnumerateDirectories(directory))
            {
                string name = Path.GetFileName(subdirectory);
                if (!string.Equals(name, "bin", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(name, "obj", StringComparison.OrdinalIgnoreCase))
                {
                    directories.Enqueue(subdirectory);
                }
            }
        }
    }

    /// <summary>
    /// Whether a file sits under a <c>bin</c> or <c>obj</c> folder. Build output is a copy of, not
    /// the source of, whatever it contains, so it is skipped rather than reported. Traversal already
    /// prunes these subtrees; this is a cheap backstop for the case where <c>CodeProjectRoot</c>
    /// itself is named <c>bin</c>/<c>obj</c>, which pruning subdirectories alone would not catch.
    /// </summary>
    private static bool IsInBuildOutputFolder(FilePath filePath)
    {
        // FilePath.Standardized normalizes to Path.DirectorySeparatorChar and lowercases.
        string separator = Path.DirectorySeparatorChar.ToString();
        string standardized = filePath.Standardized;
        return standardized.Contains(separator + "bin" + separator)
            || standardized.Contains(separator + "obj" + separator);
    }

    /// <summary>
    /// Collapses <c>.</c> and <c>..</c> segments so paths built from a relative
    /// <see cref="CodeOutputProjectSettings.CodeProjectRoot"/> compare equal to the paths the file
    /// system enumeration returns. <see cref="FilePath"/> alone only collapses <c>..</c>.
    /// </summary>
    private static FilePath Normalize(FilePath filePath) => Path.GetFullPath(filePath.FullPath);

    private static FilePath GetCustomCodePathFor(FilePath generatedFilePath)
    {
        string fullPath = generatedFilePath.FullPath;
        return fullPath.Substring(0, fullPath.Length - GeneratedFileSuffix.Length) + ".cs";
    }

    /// <summary>
    /// Reads the element name out of a generated file's <c>//Code for</c> header, or returns null
    /// when the file does not carry that header and so was not written by Gum.
    /// </summary>
    private static string? TryReadGeneratedHeaderElementName(FilePath filePath)
    {
        string? firstLine;
        try
        {
            firstLine = File.ReadLines(filePath.FullPath).FirstOrDefault();
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }

        if (firstLine == null || !firstLine.StartsWith(GeneratedFileHeaderPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        string remainder = firstLine.Substring(GeneratedFileHeaderPrefix.Length).Trim();

        // ElementSave.ToString() appends " (BaseType)" when the element has a base type.
        int baseTypeIndex = remainder.IndexOf(" (", StringComparison.Ordinal);
        if (baseTypeIndex > 0)
        {
            remainder = remainder.Substring(0, baseTypeIndex);
        }

        return remainder;
    }
}
