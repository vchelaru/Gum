using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading;
using Gum.ProjectServices;

namespace Gum.Cli.Commands;

/// <summary>
/// Defines the <c>gumcli new</c> command which creates a new Gum project.
/// </summary>
public static class NewCommand
{
    /// <summary>
    /// Creates the <c>new</c> command definition.
    /// </summary>
    public static Command Create()
    {
        var pathArgument = new Argument<string?>(
            "path",
            "Path for the new Gum project file. Accepts a .gumj (JSON, AOT-safe) or .gumx (XML) " +
            "extension explicitly; otherwise a project folder and file are created using the given " +
            "name, defaulting to .gumj. If omitted, a 'GumProject' subdirectory is created in the " +
            "current directory.")
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        var templateOption = new Option<string>(
            aliases: new[] { "--template", "-t" },
            getDefaultValue: () => "forms",
            description: "Template to use when creating the project. " +
                         "Accepted values: 'forms' (default) includes all Forms controls, behaviors, and assets; " +
                         "'empty' creates a minimal project with only the standard elements.");

        var themeOption = new Option<string?>(
            "--theme",
            "With the 'forms' template, start from a bundled Forms theme (for example Meadow or Bubblegum) instead of the " +
            "plain Forms controls. 'Standard' is the plain Forms controls. Unknown names list the themes found.");

        var platformOption = new Option<string?>(
            aliases: new[] { "--platform", "-p" },
            description: "Create a full, runnable game project for a platform (not just a Gum project), referencing Gum " +
                         "through NuGet, with the Gum project inside it at Content/GumProject and code generation already " +
                         "configured. <path> then names the project folder. Omit this option to create only a Gum project. " +
                         $"Accepted values: {NewProjectPlatforms.ValidNames}. 'stride' runs on Windows only; 'kniskia' is KNI drawing Gum through Skia.");

        var noRestoreOption = new Option<bool>(
            "--no-restore",
            "With --platform, skip the 'dotnet restore' that otherwise runs after the project is created. " +
            "Code generation detects which Gum version the project uses from the restored package, so run " +
            "'dotnet restore' yourself before 'gumcli codegen' or the Gum tool's first code generation.");

        var sourceLinkedOption = new Option<bool>(
            "--source-linked",
            "With --platform, reference the runtime project of the Gum checkout this gumcli was built from " +
            "(or that the current folder is in) instead of the NuGet package, for testing local Gum changes.");

        var command = new Command("new", "Create a new Gum project.")
        {
            pathArgument,
            templateOption,
            themeOption,
            platformOption,
            noRestoreOption,
            sourceLinkedOption
        };

        command.SetHandler((InvocationContext context) =>
        {
            string? path = context.ParseResult.GetValueForArgument(pathArgument);
            string template = context.ParseResult.GetValueForOption(templateOption) ?? "forms";
            string? theme = context.ParseResult.GetValueForOption(themeOption);
            string? platform = context.ParseResult.GetValueForOption(platformOption);
            bool noRestore = context.ParseResult.GetValueForOption(noRestoreOption);
            bool sourceLinked = context.ParseResult.GetValueForOption(sourceLinkedOption);
            context.ExitCode = Execute(path, template, theme, platform, noRestore, sourceLinked);
        });

        return command;
    }

    private static int Execute(
        string? path, string template, string? theme, string? platform, bool noRestore, bool sourceLinked)
    {
        INewProjectService service = new NewProjectService(
            FormsThemeLocator.CreateDefault(AppContext.BaseDirectory, Directory.GetCurrentDirectory()),
            new DotnetRestoreRunner(),
            new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() });

        NewProjectValidation validation = service.Validate(new NewProjectRequest
        {
            Path = path,
            Template = template,
            Theme = theme,
            Platform = platform,
            SourceLinked = sourceLinked,
            Restore = !noRestore
        });

        if (validation.Resolved == null)
        {
            Console.Error.WriteLine(validation.Error!.Message);
            return 2;
        }

        NewProjectFilesResult files = service.CreateFilesAsync(validation.Resolved, CancellationToken.None).GetAwaiter().GetResult();

        if (!files.Success)
        {
            Console.Error.WriteLine(files.ErrorMessage);
            return 2;
        }

        if (files.CsprojPath == null)
        {
            Console.WriteLine($"Created project: {files.GumProjectPath}");
            return 0;
        }

        Console.WriteLine($"Created project: {files.CsprojPath}");
        Console.WriteLine($"Created Gum project: {files.GumProjectPath}");

        if (!noRestore)
        {
            Restore(service, files.CsprojPath);
        }

        return 0;
    }

    // Code generation reads the Gum version from the restored package, so restoring now makes the
    // first generation target the right syntax. A failure (offline, no SDK on PATH) is not fatal.
    private static void Restore(INewProjectService service, string csprojPath)
    {
        Console.WriteLine("Restoring NuGet packages...");

        RestoreResult result = service
            .RestoreAsync(csprojPath, Console.WriteLine, CancellationToken.None)
            .GetAwaiter().GetResult();

        if (result.Status == RestoreStatus.ProcessNotFound)
        {
            Console.Error.WriteLine($"warning: could not run 'dotnet restore' ({result.ErrorMessage}). Run it on {csprojPath} before generating code.");
        }
        else if (result.Status != RestoreStatus.Succeeded)
        {
            Console.Error.WriteLine($"warning: 'dotnet restore' failed. Run it on {csprojPath} before generating code.");
        }
    }
}
