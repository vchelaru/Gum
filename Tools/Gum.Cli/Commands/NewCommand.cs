using System;
using System.CommandLine;
using System.CommandLine.Invocation;
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

        var command = new Command("new", "Create a new Gum project.")
        {
            pathArgument,
            templateOption
        };

        command.SetHandler((InvocationContext context) =>
        {
            string? path = context.ParseResult.GetValueForArgument(pathArgument);
            string template = context.ParseResult.GetValueForOption(templateOption) ?? "forms";
            context.ExitCode = Execute(path, template);
        });

        return command;
    }

    private const string DefaultProjectName = "GumProject";

    private static int Execute(string? path, string template)
    {
        if (!string.Equals(template, "forms", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(template, "empty", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"Unknown template '{template}'. Valid values are: forms, empty.");
            return 2;
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
}
