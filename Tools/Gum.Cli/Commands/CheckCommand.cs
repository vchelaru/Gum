using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text.Json;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Expressions;
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

        string extension = Path.GetExtension(fullPath).TrimStart('.').ToLowerInvariant();
        if (ElementFileExtensions.Contains(extension))
        {
            Console.Error.WriteLine(FormatElementFileMessage(fullPath));
            return 2;
        }

        IProjectLoader loader = new ProjectLoader();
        ProjectLoadResult loadResult = loader.Load(fullPath);

        if (!loadResult.Success)
        {
            Console.Error.WriteLine(loadResult.ErrorMessage);
            return 2;
        }

        // GUM0002 evaluates reference right-hand sides (including cross-element ones) with the Roslyn evaluator.
        GumExpressionService.Initialize();

        ITypeResolver typeResolver = new DefaultTypeResolver();
        HeadlessErrorChecker checker = new HeadlessErrorChecker(
            typeResolver,
            new IAdditionalErrorSource[] { new AnimationKeyframeErrorSource(new FileElementAnimationsProvider()) })
        {
            ReportsLeftoverInstanceVariables = true
        };
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

    private static readonly HashSet<string> ElementFileExtensions = new HashSet<string>
    {
        GumProjectSave.ScreenExtension, GumProjectSave.ScreenJsonExtension,
        GumProjectSave.ComponentExtension, GumProjectSave.ComponentJsonExtension,
        GumProjectSave.StandardExtension, GumProjectSave.StandardJsonExtension,
        BehaviorReference.Extension, BehaviorReference.JsonExtension
    };

    private static string FormatElementFileMessage(string elementPath)
    {
        string message = $"{elementPath} is an element file, not a project file. Pass the project file (.gumx or .gumj).";

        for (string? directory = Path.GetDirectoryName(elementPath); directory != null; directory = Path.GetDirectoryName(directory))
        {
            string? projectFile = Directory.EnumerateFiles(directory)
                .FirstOrDefault(file => file.EndsWith("." + GumProjectSave.ProjectExtension, StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith("." + GumProjectSave.ProjectJsonExtension, StringComparison.OrdinalIgnoreCase));
            if (projectFile != null)
            {
                return message + $" The project file is probably {projectFile}.";
            }
        }

        return message;
    }

    private static void WriteJson(IReadOnlyList<ErrorResult> errors)
    {
        var output = errors.Select(e => new
        {
            element = e.ElementName,
            message = e.Message,
            severity = e.Severity.ToString(),
            code = e.Code
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

        int errorCount = errors.Count(e => e.Severity == ErrorSeverity.Error);
        int warningCount = errors.Count(e => e.Severity == ErrorSeverity.Warning);

        Console.WriteLine();
        Console.WriteLine(FormatSummary(errorCount, warningCount));
    }

    private static string FormatSummary(int errorCount, int warningCount)
    {
        if (errorCount == 0)
        {
            return "No errors found.";
        }

        if (warningCount == 0)
        {
            return $"{errorCount} error(s) found.";
        }

        return $"{errorCount} error(s), {warningCount} warning(s) found.";
    }
}
