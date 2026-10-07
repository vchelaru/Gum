using Gum.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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

    /// <summary>
    /// Folder limit for the walk of the code output folder. A real code project is far below it; a
    /// code root that resolves to a drive root or a profile folder is far above it.
    /// </summary>
    public const int MaxCodeRootDirectories = 10000;

    /// <summary>
    /// The warning shown when a scan stopped at <see cref="MaxCodeRootDirectories"/>. Shared by the
    /// tool and <c>gumcli</c> so both say the same thing.
    /// </summary>
    public static string GetTruncatedMessage(string? codeRoot) =>
        $"The orphaned code file scan stopped after {MaxCodeRootDirectories} folders under " +
        $"{codeRoot}, so it may have missed some files. The project's Code Project Root probably points " +
        "at the wrong folder.";

    /// <summary>
    /// Runs <see cref="CreatePlan"/> and <see cref="Execute"/> back to back on the calling thread,
    /// for headless callers such as <c>gumcli codegen --prune</c>.
    /// </summary>
    public OrphanCodeFileScanResult Scan(GumProjectSave project, CodeOutputProjectSettings projectSettings,
        CodeOutputProjectSettings? previousSettings = null) =>
        Execute(CreatePlan(project, projectSettings, previousSettings), CancellationToken.None);

    /// <inheritdoc/>
    public OrphanCodeFileScanPlan CreatePlan(GumProjectSave project, CodeOutputProjectSettings projectSettings) =>
        CreatePlan(project, projectSettings, previousSettings: null);

    /// <inheritdoc/>
    public OrphanCodeFileScanPlan CreatePlan(GumProjectSave project, CodeOutputProjectSettings projectSettings,
        CodeOutputProjectSettings? previousSettings)
    {
        HashSet<FilePath> expectedGenerated = new HashSet<FilePath>();
        HashSet<FilePath> expectedCustom = new HashSet<FilePath>();
        string? codeRoot = ResolveCodeRoot(projectSettings);
        if (codeRoot != null)
        {
            AddExpectedCodeFiles(project, projectSettings, expectedGenerated, expectedCustom);
        }

        List<string> elementSettingsDirectories = new List<string>();
        HashSet<FilePath> expectedElementSettings = new HashSet<FilePath>();
        AddExpectedElementSettings(project, elementSettingsDirectories, expectedElementSettings);

        List<string> additionalCodeRoots = new List<string>();
        string? previousCodeRoot = previousSettings == null ? null : ResolveCodeRoot(previousSettings);
        if (previousCodeRoot != null && (codeRoot == null || !IsSameOrUnder(previousCodeRoot, codeRoot)))
        {
            additionalCodeRoots.Add(previousCodeRoot);
        }

        return new OrphanCodeFileScanPlan(
            codeRoot, expectedGenerated, expectedCustom, elementSettingsDirectories, expectedElementSettings)
        {
            AdditionalCodeRoots = additionalCodeRoots,
        };
    }

    /// <inheritdoc/>
    public OrphanCodeFileScanResult Execute(OrphanCodeFileScanPlan plan, CancellationToken cancellationToken)
    {
        List<OrphanCodeFile> orphans = new List<OrphanCodeFile>();
        bool isTruncated = false;

        if (plan.CodeRoot != null)
        {
            isTruncated = AddCodeFileOrphans(plan, plan.CodeRoot, orphans, cancellationToken);
        }
        foreach (string additionalRoot in plan.AdditionalCodeRoots)
        {
            isTruncated |= AddCodeFileOrphans(plan, additionalRoot, orphans, cancellationToken);
        }
        AddElementSettingsOrphans(plan, orphans, cancellationToken);

        return new OrphanCodeFileScanResult(orphans, isTruncated, plan.CodeRoot);
    }

    /// <summary>
    /// The absolute, normalized folder to walk, or null when none is configured or it does not
    /// exist - nothing can be proven orphaned then. This is the code project root, so files
    /// generated there before a <see cref="CodeOutputProjectSettings.GeneratedCodeFolder"/> was set
    /// are still found; it is the generated code folder only when that folder sits outside the root.
    /// </summary>
    private string? ResolveCodeRoot(CodeOutputProjectSettings projectSettings)
    {
        string codeProjectRoot = projectSettings.CodeProjectRoot;
        if (string.IsNullOrEmpty(codeProjectRoot))
        {
            return null;
        }

        if (FileManager.IsRelative(codeProjectRoot))
        {
            if (string.IsNullOrEmpty(_projectDirectoryProvider.ProjectDirectory))
            {
                return null;
            }
            codeProjectRoot = _projectDirectoryProvider.ProjectDirectory + codeProjectRoot;
        }

        string rootPath = Normalize(codeProjectRoot).FullPath;
        string outputPath = Normalize(_fileLocationsService.GetCodeOutputFolder(projectSettings)!).FullPath;
        string fullPath = IsSameOrUnder(outputPath, rootPath) ? rootPath : outputPath;
        return Directory.Exists(fullPath) ? fullPath : null;
    }

    private static bool IsSameOrUnder(string path, string folder)
    {
        string folderWithSeparator = Path.EndsInDirectorySeparator(folder) ? folder : folder + Path.DirectorySeparatorChar;
        return string.Equals(path, folder, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(folderWithSeparator, StringComparison.OrdinalIgnoreCase);
    }

    private void AddExpectedCodeFiles(GumProjectSave project, CodeOutputProjectSettings projectSettings,
        HashSet<FilePath> expectedGenerated, HashSet<FilePath> expectedCustom)
    {
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
    }

    /// <summary>
    /// Adds generated files under the code root that the plan does not account for, plus their
    /// custom siblings. Returns whether the walk stopped at <see cref="MaxCodeRootDirectories"/>.
    /// </summary>
    private static bool AddCodeFileOrphans(OrphanCodeFileScanPlan plan, string root, List<OrphanCodeFile> orphans,
        CancellationToken cancellationToken)
    {
        GeneratedFileWalk walk = WalkGeneratedFiles(
            root,
            directory => Directory.EnumerateFiles(directory, "*" + GeneratedFileSuffix),
            Directory.EnumerateDirectories,
            MaxCodeRootDirectories,
            cancellationToken);

        foreach (string file in walk.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            FilePath generatedPath = Normalize(file);
            if (plan.ExpectedGenerated.Contains(generatedPath) || IsInBuildOutputFolder(generatedPath))
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
            if (!plan.ExpectedCustom.Contains(customCodePath) && File.Exists(customCodePath.FullPath))
            {
                orphans.Add(new OrphanCodeFile(customCodePath, OrphanCodeFileKind.CustomCode, elementName));
            }
        }

        return walk.IsTruncated;
    }

    private void AddExpectedElementSettings(GumProjectSave project, List<string> elementSettingsDirectories,
        HashSet<FilePath> expectedElementSettings)
    {
        string? projectDirectory = _projectDirectoryProvider.ProjectDirectory;

        ///////////////////Early Out///////////////////
        if (string.IsNullOrEmpty(projectDirectory))
        {
            return;
        }
        /////////////////End Early Out/////////////////

        IEnumerable<ElementSave> allElements = project.Screens.Cast<ElementSave>()
            .Concat(project.Components)
            .Concat(project.StandardElements);

        foreach (ElementSave element in allElements)
        {
            FilePath? settingsPath = _elementSettingsManager.GetCodeSettingsFilePath(element);
            if (settingsPath != null)
            {
                expectedElementSettings.Add(Normalize(settingsPath));
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
            elementSettingsDirectories.Add(Normalize(projectDirectory + subfolder).FullPath);
        }
    }

    private static void AddElementSettingsOrphans(OrphanCodeFileScanPlan plan, List<OrphanCodeFile> orphans,
        CancellationToken cancellationToken)
    {
        foreach (string directory in plan.ElementSettingsDirectories)
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (string file in Directory.EnumerateFiles(directory, "*" + ElementSettingsExtension,
                SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();

                FilePath settingsPath = Normalize(file);
                if (plan.ExpectedElementSettings.Contains(settingsPath))
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
    /// The <c>*.Generated.cs</c> files a walk found, and whether it stopped at its folder limit.
    /// </summary>
    internal sealed class GeneratedFileWalk
    {
        public List<string> Files { get; }
        public bool IsTruncated { get; }

        public GeneratedFileWalk(List<string> files, bool isTruncated)
        {
            Files = files;
            IsTruncated = isTruncated;
        }
    }

    /// <summary>
    /// Breadth-first walk of <paramref name="root"/> for <c>*.Generated.cs</c> files over injectable
    /// enumerators. Folders that are never code output (<c>bin</c>, <c>obj</c>, <c>node_modules</c>,
    /// and dot-folders such as <c>.git</c>) are pruned during traversal. A folder the process may not
    /// read (a root-owned folder under a Linux or macOS code root, say) is skipped rather than ending
    /// the walk. Stops after visiting <paramref name="maxDirectories"/> folders, so a code root that
    /// resolves to a huge tree cannot keep the scan running for minutes.
    /// </summary>
    internal static GeneratedFileWalk WalkGeneratedFiles(
        string root,
        Func<string, IEnumerable<string>> enumerateGeneratedFiles,
        Func<string, IEnumerable<string>> enumerateDirectories,
        int maxDirectories,
        CancellationToken cancellationToken)
    {
        List<string> found = new List<string>();
        Queue<string> directories = new Queue<string>();
        directories.Enqueue(root);
        int visited = 0;

        while (directories.Count > 0)
        {
            if (visited >= maxDirectories)
            {
                return new GeneratedFileWalk(found, isTruncated: true);
            }
            cancellationToken.ThrowIfCancellationRequested();

            string directory = directories.Dequeue();
            visited++;

            List<string> files;
            List<string> subdirectories;
            try
            {
                files = enumerateGeneratedFiles(directory).ToList();
                subdirectories = enumerateDirectories(directory).ToList();
            }
            catch (Exception exception) when (exception is UnauthorizedAccessException || exception is IOException)
            {
                continue;
            }

            found.AddRange(files);

            foreach (string subdirectory in subdirectories)
            {
                if (!IsNeverCodeOutputFolder(Path.GetFileName(subdirectory)))
                {
                    directories.Enqueue(subdirectory);
                }
            }
        }

        return new GeneratedFileWalk(found, isTruncated: false);
    }

    private static bool IsNeverCodeOutputFolder(string name) =>
        name.StartsWith(".", StringComparison.Ordinal)
        || string.Equals(name, "bin", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, "obj", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, "node_modules", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Whether a file sits under a <c>bin</c> or <c>obj</c> folder. Build output is a copy of, not
    /// the source of, whatever it contains, so it is skipped rather than reported. Traversal already
    /// prunes these subtrees; this is a cheap backstop for the case where <c>CodeProjectRoot</c>
    /// itself is named <c>bin</c>/<c>obj</c>, which pruning subdirectories alone would not catch.
    /// </summary>
    private static bool IsInBuildOutputFolder(FilePath filePath)
    {
        // FilePath.FullPath normalizes to Path.DirectorySeparatorChar.
        string separator = Path.DirectorySeparatorChar.ToString();
        string fullPath = filePath.FullPath;
        return fullPath.Contains(separator + "bin" + separator, StringComparison.OrdinalIgnoreCase)
            || fullPath.Contains(separator + "obj" + separator, StringComparison.OrdinalIgnoreCase);
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
