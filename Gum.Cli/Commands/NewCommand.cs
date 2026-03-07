using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Gum.ProjectServices;

namespace Gum.Cli.Commands;

/// <summary>
/// Defines the <c>gum new</c> command which creates a blank Gum project.
/// </summary>
public static class NewCommand
{
    /// <summary>
    /// Creates the <c>new</c> command definition.
    /// </summary>
    public static Command Create()
    {
        var pathArgument = new Argument<string>(
            "path",
            "Path for the new .gumx project file. If no .gumx extension is provided, " +
            "a project folder and file are created using the given name.");

        var command = new Command("new", "Create a new blank Gum project.")
        {
            pathArgument
        };

        command.SetHandler((InvocationContext context) =>
        {
            string path = context.ParseResult.GetValueForArgument(pathArgument);
            context.ExitCode = Execute(path);
        });

        return command;
    }

    private static int Execute(string path)
    {
        string fullPath;

        if (path.EndsWith(".gumx", StringComparison.OrdinalIgnoreCase))
        {
            fullPath = Path.GetFullPath(path);
        }
        else
        {
            // Treat as a project name: create <name>/<name>.gumx
            var directoryName = Path.GetFileName(path);
            fullPath = Path.GetFullPath(Path.Combine(path, directoryName + ".gumx"));
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

        IProjectCreator creator = new ProjectCreator();
        creator.Create(fullPath);

        Console.WriteLine($"Created project: {fullPath}");
        return 0;
    }
}
