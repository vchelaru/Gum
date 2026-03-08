using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.ProjectServices.CodeGeneration;

namespace Gum.Cli.Commands;

/// <summary>
/// Defines the <c>gumcli codegen</c> command which generates C# code for all elements in a Gum project.
/// </summary>
public static class CodegenCommand
{
    /// <summary>
    /// Creates the <c>codegen</c> command definition.
    /// </summary>
    public static Command Create()
    {
        var projectArgument = new Argument<string>(
            "project",
            "Path to the .gumx project file.");

        var elementOption = new Option<string[]>(
            "--element",
            "Name of a specific element to generate code for. Can be specified multiple times.")
        {
            AllowMultipleArgumentsPerToken = true
        };

        var command = new Command("codegen", "Generate C# code for a Gum project.")
        {
            projectArgument,
            elementOption
        };

        command.SetHandler((InvocationContext context) =>
        {
            string projectPath = context.ParseResult.GetValueForArgument(projectArgument);
            string[]? elements = context.ParseResult.GetValueForOption(elementOption);
            context.ExitCode = Execute(projectPath, elements);
        });

        return command;
    }

    private static int Execute(string projectPath, string[]? elementNames)
    {
        var fullPath = Path.GetFullPath(projectPath);

        IProjectLoader loader = new ProjectLoader();
        ProjectLoadResult loadResult = loader.Load(fullPath);

        if (!loadResult.Success)
        {
            Console.Error.WriteLine(loadResult.ErrorMessage);
            return 2;
        }

        foreach (ErrorResult loadError in loadResult.LoadErrors)
        {
            string severity = loadError.Severity == ErrorSeverity.Warning ? "warning" : "error";
            Console.Error.WriteLine($"{severity}: {loadError.ElementName}: {loadError.Message}");
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
        ICodeGenLogger logger = new ConsoleCodeGenLogger();
        var projectSettingsManager = new CodeOutputProjectSettingsManager(logger, projectDirectory);
        CodeOutputProjectSettings projectSettings = projectSettingsManager.CreateOrLoadSettingsForProject();

        if (string.IsNullOrEmpty(projectSettings.CodeProjectRoot))
        {
            Console.Error.WriteLine("No CodeProjectRoot configured in ProjectCodeSettings.codsj. " +
                "Code generation requires a code project root to be set.");
            return 2;
        }

        // Build the codegen pipeline
        INameVerifier nameVerifier = new HeadlessNameVerifier();
        var codeGenNameVerifier = new CodeGenerationNameVerifier(nameVerifier);
        var elementSettingsManager = new CodeOutputElementSettingsManager(projectDirectory);
        var localizationService = new Gum.Localization.LocalizationService();
        var codeGenerator = new CodeGenerator(
            codeGenNameVerifier, localizationService, elementSettingsManager);
        codeGenerator.ProjectDirectory = projectDirectory;

        var customCodeGenerator = new CustomCodeGenerator(codeGenerator, codeGenNameVerifier);
        var fileLocationsService = new CodeGenerationFileLocationsService(
            codeGenerator, codeGenNameVerifier, projectDirectory);

        var codeGenService = new HeadlessCodeGenerationService(
            codeGenerator, customCodeGenerator, fileLocationsService, elementSettingsManager, logger);

        // Resolve which elements to generate
        List<ElementSave> elements = ResolveElements(project, elementNames);

        // Check errors and generate
        ITypeResolver typeResolver = new DefaultTypeResolver();
        IHeadlessErrorChecker errorChecker = new HeadlessErrorChecker(typeResolver);

        int generatedCount = 0;
        int blockedCount = 0;

        ObjectFinder.Self.EnableCache();
        try
        {
            foreach (var element in elements)
            {
                var settings = elementSettingsManager.LoadOrCreateSettingsFor(element);

                if (settings.GenerationBehavior == GenerationBehavior.NeverGenerate)
                {
                    continue;
                }

                IReadOnlyList<ErrorResult> errors = errorChecker.GetErrorsFor(element, project);

                foreach (var warning in errors.Where(e => e.Severity == ErrorSeverity.Warning))
                {
                    Console.Error.WriteLine($"warning: {element.Name}: {warning.Message}");
                }

                List<ErrorResult> blockingErrors = errors
                    .Where(e => e.Severity == ErrorSeverity.Error)
                    .ToList();

                if (blockingErrors.Count > 0)
                {
                    foreach (var error in blockingErrors)
                    {
                        Console.Error.WriteLine($"error: {element.Name}: {error.Message}");
                    }
                    blockedCount++;
                    continue;
                }

                bool checkForMissing = elementNames != null && elementNames.Length > 0;
                if (codeGenService.GenerateCodeForElement(element, settings, projectSettings,
                    checkForMissing: checkForMissing))
                {
                    generatedCount++;
                }
            }
        }
        finally
        {
            ObjectFinder.Self.DisableCache();
        }

        Console.WriteLine($"Generated code for {generatedCount} element(s).");

        if (blockedCount > 0)
        {
            Console.Error.WriteLine($"{blockedCount} element(s) skipped due to errors.");
            return 1;
        }

        return 0;
    }

    private static List<ElementSave> ResolveElements(GumProjectSave project, string[]? elementNames)
    {
        if (elementNames == null || elementNames.Length == 0)
        {
            return project.Screens.Cast<ElementSave>()
                .Concat(project.Components)
                .ToList();
        }

        var elements = new List<ElementSave>();

        foreach (string name in elementNames)
        {
            ElementSave? element = ObjectFinder.Self.GetElementSave(name);

            if (element == null)
            {
                Console.Error.WriteLine($"error: Element '{name}' not found in the project.");
            }
            else
            {
                elements.Add(element);
            }
        }

        return elements;
    }
}
