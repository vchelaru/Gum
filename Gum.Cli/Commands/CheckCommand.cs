using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text.Json;
using Gum.ProjectServices;

namespace Gum.Cli.Commands;

/// <summary>
/// Defines the <c>gumcli check</c> command which loads a project and reports errors.
/// </summary>
public static class CheckCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Creates the <c>check</c> command definition.
    /// </summary>
    public static Command Create()
    {
        var projectArgument = new Argument<string>(
            "project",
            "Path to the .gumx project file.");

        var jsonOption = new Option<bool>(
            "--json",
            "Output errors as a JSON array.");

        var command = new Command("check", "Check a Gum project for errors.")
        {
            projectArgument,
            jsonOption
        };

        command.SetHandler((InvocationContext context) =>
        {
            string projectPath = context.ParseResult.GetValueForArgument(projectArgument);
            bool json = context.ParseResult.GetValueForOption(jsonOption);
            context.ExitCode = Execute(projectPath, json);
        });

        return command;
    }

    private static int Execute(string projectPath, bool json)
    {
        var fullPath = Path.GetFullPath(projectPath);

        IProjectLoader loader = new ProjectLoader();
        ProjectLoadResult loadResult = loader.Load(fullPath);

        if (!loadResult.Success)
        {
            Console.Error.WriteLine(loadResult.ErrorMessage);
            return 2;
        }

        ITypeResolver typeResolver = new DefaultTypeResolver();
        IHeadlessErrorChecker checker = new HeadlessErrorChecker(typeResolver);
        IReadOnlyList<ErrorResult> checkerErrors = checker.GetAllErrors(loadResult.Project!);

        var allErrors = new List<ErrorResult>();
        allErrors.AddRange(loadResult.LoadErrors);
        allErrors.AddRange(checkerErrors);

        if (json)
        {
            WriteJson(allErrors);
        }
        else
        {
            WriteHumanReadable(allErrors);
        }

        return allErrors.Any(e => e.Severity == ErrorSeverity.Error) ? 1 : 0;
    }

    private static void WriteJson(IReadOnlyList<ErrorResult> errors)
    {
        var output = errors.Select(e => new
        {
            element = e.ElementName,
            message = e.Message,
            severity = e.Severity.ToString()
        });

        Console.WriteLine(JsonSerializer.Serialize(output, JsonOptions));
    }

    private static void WriteHumanReadable(IReadOnlyList<ErrorResult> errors)
    {
        if (errors.Count == 0)
        {
            Console.WriteLine("No errors found.");
            return;
        }

        foreach (ErrorResult error in errors)
        {
            string severity = error.Severity == ErrorSeverity.Warning ? "warning" : "error";
            Console.WriteLine($"{severity}: {error.ElementName}: {error.Message}");
        }

        Console.WriteLine();
        Console.WriteLine($"{errors.Count} error(s) found.");
    }
}
