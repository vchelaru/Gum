using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Gum.ProjectServices.CodeGeneration;

namespace Gum.Cli.Commands;

/// <summary>
/// Defines the <c>gumcli codegen-init</c> command which auto-configures code generation settings
/// for a Gum project by locating the nearest .csproj above the .gumx file.
/// </summary>
public static class CodegenInitCommand
{
    /// <summary>
    /// Creates the <c>codegen-init</c> command definition.
    /// </summary>
    public static Command Create()
    {
        var projectArgument = new Argument<string>(
            "project",
            "Path to the .gumx project file.");

        var forceOption = new Option<bool>(
            "--force",
            "Overwrite existing ProjectCodeSettings.codsj without prompting.");

        var csprojOption = new Option<string?>(
            "--csproj",
            "Explicit path to the .csproj file. When omitted, the nearest .csproj above the .gumx file is used.");

        var command = new Command("codegen-init",
            "Auto-configure code generation settings by locating the nearest .csproj above the .gumx file.")
        {
            projectArgument,
            forceOption,
            csprojOption
        };

        command.SetHandler((InvocationContext context) =>
        {
            string projectPath = context.ParseResult.GetValueForArgument(projectArgument);
            bool force = context.ParseResult.GetValueForOption(forceOption);
            string? csprojPath = context.ParseResult.GetValueForOption(csprojOption);
            context.ExitCode = Execute(projectPath, force, csprojPath);
        });

        return command;
    }

    private static int Execute(string projectPath, bool force, string? explicitCsprojPath)
    {
        string fullPath = Path.GetFullPath(projectPath);

        if (!File.Exists(fullPath))
        {
            Console.Error.WriteLine($"error: Project file not found: {fullPath}");
            return 2;
        }

        string? projectDirectory = Path.GetDirectoryName(fullPath);
        if (projectDirectory != null && !projectDirectory.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
        {
            projectDirectory += Path.DirectorySeparatorChar;
        }

        string settingsFilePath = Path.Combine(projectDirectory ?? string.Empty, "ProjectCodeSettings.codsj");

        if (File.Exists(settingsFilePath) && !force)
        {
            Console.Error.WriteLine($"warning: ProjectCodeSettings.codsj already exists at {settingsFilePath}.");
            Console.Error.WriteLine("Use --force to overwrite the existing configuration.");
            return 2;
        }

        ICodeGenerationAutoSetupService autoSetupService = new CodeGenerationAutoSetupService();
        AutoSetupResult result = explicitCsprojPath != null
            ? autoSetupService.Run(fullPath, explicitCsprojPath)
            : autoSetupService.Run(fullPath);

        if (!result.Success)
        {
            Console.Error.WriteLine($"error: {result.ErrorMessage}");
            return 2;
        }

        ICodeGenLogger logger = new ConsoleCodeGenLogger();
        var settingsManager = new CodeOutputProjectSettingsManager(logger, new FixedProjectDirectoryProvider(projectDirectory));
        settingsManager.WriteSettingsForProject(result.Settings!);

        Console.WriteLine("Code generation settings initialized successfully.");
        Console.WriteLine($"  CodeProjectRoot : {result.Settings!.CodeProjectRoot}");
        Console.WriteLine($"  RootNamespace   : {result.Settings.RootNamespace}");
        Console.WriteLine($"  OutputLibrary   : {result.Settings.OutputLibrary}");
        Console.WriteLine($"  Settings saved to: {settingsFilePath}");

        return 0;
    }
}
