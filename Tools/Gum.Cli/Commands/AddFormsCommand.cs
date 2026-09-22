using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Gum.DataTypes;
using Gum.ProjectServices;

namespace Gum.Cli.Commands;

/// <summary>
/// Defines the <c>gumcli add-forms</c> command which merges the Forms control template into an
/// already-existing Gum project.
/// </summary>
public static class AddFormsCommand
{
    /// <summary>
    /// Creates the <c>add-forms</c> command definition.
    /// </summary>
    public static Command Create()
    {
        var pathArgument = new Argument<string>(
            "path",
            "Path to an existing Gum project file (.gumx or .gumj) to add Forms controls to.");

        var command = new Command("add-forms", "Add Forms controls to an already-existing Gum project.")
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
        if (!GumProjectSave.IsProjectFile(path))
        {
            Console.Error.WriteLine($"Not a Gum project file (expected .gumx or .gumj): {path}");
            return 2;
        }

        string fullPath = Path.GetFullPath(path);

        if (!File.Exists(fullPath))
        {
            Console.Error.WriteLine($"Project not found: {fullPath}");
            return 2;
        }

        IAddFormsToProjectService addFormsService = new AddFormsToProjectService();
        AddFormsResult result = addFormsService.AddFormsTo(fullPath);

        if (!result.Success)
        {
            Console.Error.WriteLine(result.ErrorMessage);
            return 2;
        }

        Console.WriteLine(
            $"Added {result.AddedComponents.Count} component(s), {result.AddedStandards.Count} standard(s), " +
            $"and {result.AddedBehaviors.Count} behavior(s) to: {fullPath}");

        return 0;
    }
}
