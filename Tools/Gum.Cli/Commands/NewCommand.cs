using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using Gum.DataTypes;
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

        var platformOption = new Option<string?>(
            aliases: new[] { "--platform", "-p" },
            description: "Also create a runnable host game project for a platform, referencing Gum through NuGet, " +
                         "with the Gum project inside it at Content/GumProject and code generation already configured. " +
                         "<path> then names the project folder. Accepted values: 'monogame', 'kni', 'raylib'.");

        var noRestoreOption = new Option<bool>(
            "--no-restore",
            "With --platform, skip the 'dotnet restore' that otherwise runs after the project is created. " +
            "Code generation detects which Gum version the project uses from the restored package, so run " +
            "'dotnet restore' yourself before 'gumcli codegen' or the Gum tool's first code generation.");

        var command = new Command("new", "Create a new Gum project.")
        {
            pathArgument,
            templateOption,
            platformOption,
            noRestoreOption
        };

        command.SetHandler((InvocationContext context) =>
        {
            string? path = context.ParseResult.GetValueForArgument(pathArgument);
            string template = context.ParseResult.GetValueForOption(templateOption) ?? "forms";
            string? platform = context.ParseResult.GetValueForOption(platformOption);
            bool noRestore = context.ParseResult.GetValueForOption(noRestoreOption);
            context.ExitCode = Execute(path, template, platform, noRestore);
        });

        return command;
    }

    private const string DefaultProjectName = "GumProject";
    private const string DefaultPlatformProjectName = "MyGumGame";

    private static int Execute(string? path, string template, string? platform, bool noRestore)
    {
        if (!string.Equals(template, "forms", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(template, "empty", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"Unknown template '{template}'. Valid values are: forms, empty.");
            return 2;
        }

        if (platform != null)
        {
            return ExecuteWithPlatform(path, template, platform, noRestore);
        }

        string fullPath;

        if (string.IsNullOrEmpty(path))
        {
            fullPath = Path.GetFullPath(Path.Combine(DefaultProjectName, DefaultProjectName + "." + GumProjectSave.ProjectJsonExtension));
        }
        else if (GumProjectSave.IsProjectFile(path))
        {
            fullPath = Path.GetFullPath(path);
        }
        else
        {
            // Treat as a project name: create <name>/<name>.gumj
            var directoryName = Path.GetFileName(path);
            fullPath = Path.GetFullPath(Path.Combine(path, directoryName + "." + GumProjectSave.ProjectJsonExtension));
        }

        if (File.Exists(fullPath))
        {
            Console.Error.WriteLine($"Project already exists: {fullPath}");
            return 2;
        }

        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (string.Equals(template, "forms", StringComparison.OrdinalIgnoreCase))
        {
            IFormsTemplateCreator formsCreator = new FormsTemplateCreator();
            formsCreator.Create(fullPath);
        }
        else
        {
            IProjectCreator creator = new ProjectCreator();
            creator.Create(fullPath);
        }

        Console.WriteLine($"Created project: {fullPath}");
        return 0;
    }

    private static int ExecuteWithPlatform(string? path, string template, string platform, bool noRestore)
    {
        HostPlatform hostPlatform;
        switch (platform.ToLowerInvariant())
        {
            case "monogame":
                hostPlatform = HostPlatform.MonoGame;
                break;
            case "kni":
                hostPlatform = HostPlatform.Kni;
                break;
            case "raylib":
                hostPlatform = HostPlatform.Raylib;
                break;
            case "fna":
                Console.Error.WriteLine("FNA projects can't be scaffolded: FNA is not published to NuGet, so it has to be linked from source.");
                return 2;
            default:
                Console.Error.WriteLine($"Unknown platform '{platform}'. Valid values are: monogame, kni, raylib.");
                return 2;
        }

        if (!string.IsNullOrEmpty(path) && GumProjectSave.IsProjectFile(path))
        {
            Console.Error.WriteLine("With --platform, <path> names the project folder, not a Gum project file.");
            return 2;
        }

        string projectDirectory = Path.GetFullPath(string.IsNullOrEmpty(path) ? DefaultPlatformProjectName : path);

        IPlatformProjectScaffolder scaffolder = new PlatformProjectScaffolder();
        PlatformProjectResult result = scaffolder.Create(
            projectDirectory,
            hostPlatform,
            includeFormsTemplate: string.Equals(template, "forms", StringComparison.OrdinalIgnoreCase));

        if (!result.Success)
        {
            Console.Error.WriteLine(result.ErrorMessage);
            return 2;
        }

        Console.WriteLine($"Created project: {result.CsprojPath}");
        Console.WriteLine($"Created Gum project: {result.GumProjectPath}");

        if (!noRestore)
        {
            Restore(result.CsprojPath);
        }

        return 0;
    }

    // Code generation reads the Gum version from the restored package, so restoring now makes the
    // first generation target the right syntax. A failure (offline, no SDK on PATH) is not fatal.
    private static void Restore(string csprojPath)
    {
        Console.WriteLine("Restoring NuGet packages...");

        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo("dotnet")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("restore");
            startInfo.ArgumentList.Add(csprojPath);

            using Process? process = Process.Start(startInfo);
            process?.WaitForExit();

            if (process == null || process.ExitCode != 0)
            {
                Console.Error.WriteLine($"warning: 'dotnet restore' failed. Run it on {csprojPath} before generating code.");
            }
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            Console.Error.WriteLine($"warning: could not run 'dotnet restore' ({exception.Message}). Run it on {csprojPath} before generating code.");
        }
    }
}
