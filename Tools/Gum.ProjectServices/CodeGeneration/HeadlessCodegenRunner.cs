using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Generates code for a Gum project file, with the output <c>gumcli codegen</c> prints. The one pipeline behind
/// <c>gumcli codegen</c> and the New Project dialog's code generation step.
/// </summary>
public class HeadlessCodegenRunner : IHeadlessCodegenRunner
{
    private readonly Action? _initializeExpressions;
    private readonly string? _toolVersion;

    /// <param name="initializeExpressions">
    /// Registers the Gum.Expressions evaluator (<c>GumExpressionService.Initialize</c>), which the GUM0002 check needs; this
    /// project does not reference it. May be null.
    /// </param>
    /// <param name="toolVersion">The running tool's version, for the stale-tool warning; null skips the warning.</param>
    public HeadlessCodegenRunner(Action? initializeExpressions = null, string? toolVersion = null)
    {
        _initializeExpressions = initializeExpressions;
        _toolVersion = toolVersion;
    }

    /// <inheritdoc/>
    public int Run(string projectPath, IReadOnlyList<string>? elementNames, bool prune, TextWriter output, TextWriter error)
    {
        // The finder is a process-wide singleton the loaded project shares with the caller (the tool keeps its own
        // project in it), so this run puts back what it found.
        GumProjectSave? previousProject = ObjectFinder.Self.GumProjectSave;
        try
        {
            return RunCore(projectPath, elementNames, prune, output, error);
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = previousProject;
        }
    }

    private int RunCore(string projectPath, IReadOnlyList<string>? elementNames, bool prune, TextWriter output, TextWriter error)
    {
        var fullPath = Path.GetFullPath(projectPath);

        IProjectLoader loader = new ProjectLoader();
        ProjectLoadResult loadResult = loader.Load(fullPath);

        if (!loadResult.Success)
        {
            error.WriteLine(loadResult.ErrorMessage);
            return 2;
        }

        foreach (ErrorResult loadError in loadResult.LoadErrors)
        {
            string severity = loadError.Severity == ErrorSeverity.Warning ? "warning" : "error";
            error.WriteLine($"{severity}: {loadError.ElementName}: {loadError.Message}");
        }

        var project = loadResult.Project!;
        var projectDirectory = Path.GetDirectoryName(fullPath);
        if (projectDirectory != null && !projectDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
        {
            projectDirectory += Path.DirectorySeparatorChar;
        }

        // Set up ObjectFinder for element lookups
        ObjectFinder.Self.GumProjectSave = project;

        // Load project code settings
        ICodeGenLogger logger = new TextWriterCodeGenLogger(output, error);
        IProjectDirectoryProvider projectDirectoryProvider = new FixedProjectDirectoryProvider(projectDirectory);
        var projectSettingsManager = new CodeOutputProjectSettingsManager(logger, projectDirectoryProvider);
        string settingsFilePath = Path.Combine(projectDirectory!, "ProjectCodeSettings.codsj");
        CodeOutputProjectSettings projectSettings;
        if (File.Exists(settingsFilePath))
        {
            // An unreadable file is the user's settings, so it is reported and never replaced by
            // auto-configured defaults. The loader has already printed the parser's message.
            CodeOutputProjectSettings? loaded = projectSettingsManager.TryLoadSettingsForProject();
            if (loaded == null)
            {
                error.WriteLine($"error: could not read code generation settings from {settingsFilePath}. " +
                    "Fix or delete the file and run codegen again; it was left unchanged.");
                return 2;
            }
            projectSettings = loaded;
        }
        else
        {
            projectSettings = projectSettingsManager.CreateOrLoadSettingsForProject();
        }

        if (string.IsNullOrEmpty(projectSettings.CodeProjectRoot))
        {
            var autoSetup = new CodeGenerationAutoSetupService();
            AutoSetupResult autoResult = autoSetup.Run(fullPath);

            if (!autoResult.Success)
            {
                error.WriteLine($"error: CodeProjectRoot is not configured and auto-detection failed: {autoResult.ErrorMessage}");
                error.WriteLine($"Run 'gumcli codegen-init {projectPath}' after placing your .gumx inside a project with a .csproj.");
                return 2;
            }

            if (File.Exists(settingsFilePath))
            {
                // The file holds the user's other settings: fill in only CodeProjectRoot and write
                // that one field back, keeping every other key (including ones Gum doesn't know).
                projectSettings.CodeProjectRoot = autoResult.Settings!.CodeProjectRoot;
                JObject settingsJson = JObject.Parse(File.ReadAllText(settingsFilePath));
                settingsJson[nameof(CodeOutputProjectSettings.CodeProjectRoot)] = projectSettings.CodeProjectRoot;
                File.WriteAllText(settingsFilePath, settingsJson.ToString(Formatting.Indented));
                output.WriteLine($"Auto-configured CodeProjectRoot: {projectSettings.CodeProjectRoot}.");
            }
            else
            {
                projectSettings = autoResult.Settings!;
                projectSettingsManager.WriteSettingsForProject(projectSettings);
                output.WriteLine($"Auto-configured code generation settings (CodeProjectRoot: {projectSettings.CodeProjectRoot}, Namespace: {projectSettings.RootNamespace}).");
            }
        }

        try
        {
            CodeGenerator.AssertSupportedCombination(projectSettings);
        }
        catch (NotSupportedException ex)
        {
            error.WriteLine($"error: {ex.Message}");
            return 1;
        }

        // Print the resolved (absolute) output root so misconfigured CodeProjectRoot
        // values (e.g. machine-specific absolute paths) are obvious in the output.
        // Settings saved on Windows use backslashes, which are file-name characters on macOS/Linux.
        string resolvedCodeProjectRoot = projectSettings.CodeProjectRoot!.Replace('\\', Path.DirectorySeparatorChar);
        if (!Path.IsPathRooted(resolvedCodeProjectRoot))
        {
            resolvedCodeProjectRoot = Path.GetFullPath(Path.Combine(projectDirectory!, resolvedCodeProjectRoot));
        }
        string resolvedOutputFolder = resolvedCodeProjectRoot;
        if (!string.IsNullOrEmpty(projectSettings.GeneratedCodeFolder))
        {
            string generatedCodeFolder = projectSettings.GeneratedCodeFolder.Replace('\\', Path.DirectorySeparatorChar);
            resolvedOutputFolder = Path.GetFullPath(Path.Combine(resolvedCodeProjectRoot, generatedCodeFolder));
            if (!Path.EndsInDirectorySeparator(resolvedOutputFolder))
            {
                resolvedOutputFolder += Path.DirectorySeparatorChar;
            }
        }
        output.WriteLine($"Generating code into {resolvedOutputFolder}");
        if (!string.IsNullOrEmpty(projectSettings.CustomCodeFolder))
        {
            string customCodeFolder = projectSettings.CustomCodeFolder.Replace('\\', Path.DirectorySeparatorChar);
            output.WriteLine($"Custom code files into {Path.GetFullPath(Path.Combine(resolvedCodeProjectRoot, customCodeFolder))}");
        }

        // Build the codegen pipeline
        var codeGenNameVerifier = new CodeGenerationNameVerifier(new NameRules());
        var elementSettingsManager = new CodeOutputElementSettingsManager(projectDirectoryProvider);
        var localizationService = new Gum.Localization.LocalizationService();
        IHeadlessLocalizationLoader localizationLoader = new HeadlessLocalizationLoader(logger);
        localizationLoader.LoadLocalizationFiles(project, projectDirectory!, localizationService);
        var syntaxVersionDetectionService = new SyntaxVersionDetectionService(logger);

        // Warning only: a stale tool still generates, just for an older runtime than the project uses.
        if (_toolVersion != null)
        {
            string? staleCliWarning = StaleCliWarning.GetMessage(
                _toolVersion,
                syntaxVersionDetectionService.FindRuntimePackageVersion(projectSettings, projectDirectory));
            if (staleCliWarning != null)
            {
                error.WriteLine($"warning: {staleCliWarning}");
            }
        }

        var codeGenerator = new CodeGenerator(
            codeGenNameVerifier, localizationService, elementSettingsManager, projectDirectoryProvider,
            typeStringResolver: null,
            syntaxVersionDetectionService: syntaxVersionDetectionService,
            cSharpVersionDetectionService: new CSharpVersionDetectionService());

        var customCodeGenerator = new CustomCodeGenerator(codeGenerator, codeGenNameVerifier);
        var fileLocationsService = new CodeGenerationFileLocationsService(
            codeGenerator, codeGenNameVerifier, projectDirectoryProvider);

        // A logger of its own, so the exit code can reflect files the service failed to write
        // without counting errors other services report through the shared logger.
        TextWriterCodeGenLogger generationLogger = new TextWriterCodeGenLogger(output, error);
        var codeGenService = new HeadlessCodeGenerationService(
            codeGenerator, customCodeGenerator, fileLocationsService, elementSettingsManager, generationLogger);

        // GUM0002 evaluates reference right-hand sides (including cross-element ones) with the Roslyn evaluator.
        _initializeExpressions?.Invoke();
        ITypeResolver typeResolver = new DefaultTypeResolver();
        IHeadlessErrorChecker errorChecker = new HeadlessErrorChecker(
            typeResolver, new IAdditionalErrorSource[]
            {
                new FormsCodegenBaseTypeErrorSource(projectSettings),
                new CategoryNameCollisionErrorSource(codeGenerator, projectSettings),
            });

        // GUM0015 and GUM0023 are project-level rows, so the per-element gate adds the ones that name its element.
        IReadOnlyList<ErrorResult> projectErrors = errorChecker.GetProjectErrors(project);

        // --element narrows the run to those elements (which also leaves the project-wide
        // Standard Elements fallback file alone, issue #6042); otherwise the whole project.
        bool hasElementSubset = elementNames != null && elementNames.Count > 0;
        HashSet<string> printedProjectErrors = new HashSet<string>();
        CodeGenerationRunResult runResult = codeGenService.GenerateCodeForElements(
            project, projectSettings, new CodeGenerationRunOptions
            {
                Elements = hasElementSubset ? ResolveElements(elementNames!, error) : null,
                Gate = element => PassesErrorCheck(errorChecker, projectErrors, printedProjectErrors, element, project, error)
            });

        output.WriteLine($"Generated code for {runResult.GeneratedCount} element(s).");

        if (prune)
        {
            // Scan after generating so files written by this run are never mistaken for orphans.
            var scanService = new OrphanCodeFileScanService(
                codeGenerator, fileLocationsService, elementSettingsManager, projectDirectoryProvider);
            OrphanCodeFileScanResult scanResult = scanService.Scan(project, projectSettings);
            if (scanResult.IsTruncated)
            {
                error.WriteLine("warning: " + OrphanCodeFileScanService.GetTruncatedMessage(scanResult.CodeRoot));
            }
            Prune(scanResult.Orphans, output, error);
        }

        if (runResult.BlockedCount > 0)
        {
            error.WriteLine($"{runResult.BlockedCount} element(s) skipped due to errors.");
        }
        if (generationLogger.ErrorCount > 0)
        {
            error.WriteLine($"{generationLogger.ErrorCount} code generation error(s).");
        }

        return runResult.BlockedCount > 0 || generationLogger.ErrorCount > 0 ? 1 : 0;
    }

    // Prints the element's warnings and errors; only errors stop it from being generated.
    private static bool PassesErrorCheck(
        IHeadlessErrorChecker errorChecker, IReadOnlyList<ErrorResult> projectErrors, HashSet<string> printedProjectErrors,
        ElementSave element, GumProjectSave project, TextWriter error)
    {
        List<ErrorResult> errors = errorChecker.GetErrorsFor(element, project).ToList();
        errors.AddRange(projectErrors.Where(e => e.Code != null && HeadlessErrorChecker.CodegenGatingProjectCodes.Contains(e.Code) && e.ElementName == element.Name));

        foreach (var warning in errors.Where(e => e.Severity == ErrorSeverity.Warning))
        {
            error.WriteLine($"warning: {element.Name}: {warning.Message}");
        }

        List<ErrorResult> blockingErrors = errors
            .Where(e => e.Severity == ErrorSeverity.Error)
            .ToList();

        foreach (var blockingError in blockingErrors)
        {
            // Two elements loaded from a doubly-referenced name carry the same project-level row; print it once.
            if (!projectErrors.Contains(blockingError) || printedProjectErrors.Add(blockingError.Message))
            {
                error.WriteLine($"error: {element.Name}: {blockingError.Message}");
            }
        }

        return blockingErrors.Count == 0;
    }

    /// <summary>
    /// Deletes orphaned generated files — derived data, so removing them is lossless — and reports
    /// everything else the scan found without touching it. Custom code is user-authored and
    /// unrecoverable through Gum, and a .codsj carries hand-set per-element settings, so both are
    /// left for the user to remove deliberately.
    /// </summary>
    private static void Prune(IReadOnlyList<OrphanCodeFile> orphans, TextWriter output, TextWriter error)
    {
        int prunedCount = 0;

        foreach (OrphanCodeFile orphan in orphans.Where(item => item.Kind == OrphanCodeFileKind.Generated))
        {
            try
            {
                File.Delete(orphan.FilePath.FullPath);
                output.WriteLine($"Pruned {orphan.FilePath.FullPath}");
                prunedCount++;
            }
            catch (Exception e)
            {
                error.WriteLine($"error: could not prune {orphan.FilePath.FullPath}: {e.Message}");
            }
        }

        output.WriteLine($"Pruned {prunedCount} orphaned generated file(s).");

        List<OrphanCodeFile> reportOnly = orphans
            .Where(item => item.Kind != OrphanCodeFileKind.Generated)
            .ToList();

        foreach (OrphanCodeFile orphan in reportOnly)
        {
            string description = orphan.Kind == OrphanCodeFileKind.CustomCode
                ? "custom code file"
                : "element code settings file";
            output.WriteLine($"Orphaned {description} (not removed): {orphan.FilePath.FullPath}");
        }
    }

    private static List<ElementSave> ResolveElements(IReadOnlyList<string> elementNames, TextWriter error)
    {
        var elements = new List<ElementSave>();

        foreach (string name in elementNames)
        {
            ElementSave? element = ObjectFinder.Self.GetElementSave(name);

            if (element == null)
            {
                error.WriteLine($"error: Element '{name}' not found in the project.");
            }
            else
            {
                elements.Add(element);
            }
        }

        return elements;
    }
}

/// <summary>
/// Runs code generation for a Gum project file the way <c>gumcli codegen</c> does.
/// </summary>
public interface IHeadlessCodegenRunner
{
    /// <summary>
    /// Generates code for the named elements (everything when empty or null), printing what <c>gumcli codegen</c> prints.
    /// </summary>
    /// <param name="projectPath">The <c>.gumj</c>/<c>.gumx</c> file.</param>
    /// <param name="elementNames">Element names to generate, with the referenced elements that have no code yet; null or empty means the whole project.</param>
    /// <param name="prune">Also deletes generated files no element accounts for.</param>
    /// <param name="output">Receives the normal output lines.</param>
    /// <param name="error">Receives warnings and errors.</param>
    /// <returns>The <c>gumcli codegen</c> exit code: 0 on success.</returns>
    int Run(string projectPath, IReadOnlyList<string>? elementNames, bool prune, TextWriter output, TextWriter error);
}

/// <summary>An <see cref="ICodeGenLogger"/> that writes to two writers and counts the errors it printed.</summary>
public class TextWriterCodeGenLogger : ICodeGenLogger
{
    private readonly TextWriter _output;
    private readonly TextWriter _error;

    public TextWriterCodeGenLogger(TextWriter output, TextWriter error)
    {
        _output = output;
        _error = error;
    }

    /// <summary>How many errors this logger has printed.</summary>
    public int ErrorCount { get; private set; }

    /// <inheritdoc/>
    public void PrintOutput(string message) => _output.WriteLine(message);

    /// <inheritdoc/>
    public void PrintError(string message)
    {
        ErrorCount++;
        _error.WriteLine(message);
    }
}
