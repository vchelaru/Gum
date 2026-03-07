using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.ProjectServices.CodeGeneration;

namespace Gum.Cli.Commands;

/// <summary>
/// Defines the <c>gum codegen</c> command which generates C# code for all elements in a Gum project.
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

        var command = new Command("codegen", "Generate C# code for a Gum project.")
        {
            projectArgument
        };

        command.SetHandler((InvocationContext context) =>
        {
            string projectPath = context.ParseResult.GetValueForArgument(projectArgument);
            context.ExitCode = Execute(projectPath);
        });

        return command;
    }

    private static int Execute(string projectPath)
    {
        var fullPath = Path.GetFullPath(projectPath);

        IProjectLoader loader = new ProjectLoader();
        ProjectLoadResult loadResult = loader.Load(fullPath);

        if (!loadResult.Success)
        {
            Console.Error.WriteLine(loadResult.ErrorMessage);
            return 2;
        }

        if (loadResult.MissingFiles.Count > 0)
        {
            foreach (string missingFile in loadResult.MissingFiles)
            {
                Console.Error.WriteLine($"warning: missing file: {missingFile}");
            }
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

        int generatedCount = codeGenService.GenerateCodeForAllElements(project, projectSettings);

        Console.WriteLine($"Generated code for {generatedCount} element(s).");
        return 0;
    }
}
