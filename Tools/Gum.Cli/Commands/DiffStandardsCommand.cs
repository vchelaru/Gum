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
/// Defines the <c>gumcli diff-standards</c> command which compares a project's Standards
/// against the bundled Default template and reports any drift. Used by theme authors and
/// CI to enforce the "Standards must match Default" invariant — see issue #2778.
/// </summary>
public static class DiffStandardsCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Creates the <c>diff-standards</c> command definition.
    /// </summary>
    public static Command Create()
    {
        var projectArgument = new Argument<string>(
            "project",
            "Path to the .gumx project file.");

        var jsonOption = new Option<bool>(
            "--json",
            "Output drift as a JSON document.");

        var command = new Command(
            "diff-standards",
            "Compare a project's Standards against the bundled Default template and report drift.")
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
        string fullPath = Path.GetFullPath(projectPath);

        if (!File.Exists(fullPath))
        {
            Console.Error.WriteLine($"Project file not found: {fullPath}");
            return 2;
        }

        IDiffStandardsService diffService = new DiffStandardsService();
        DiffStandardsResult result;
        try
        {
            result = diffService.Diff(fullPath);
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }

        if (json)
        {
            WriteJson(result);
        }
        else
        {
            WriteHumanReadable(result);
        }

        return result.HasDrift ? 1 : 0;
    }

    private static void WriteJson(DiffStandardsResult result)
    {
        var payload = new
        {
            hasDrift = result.HasDrift,
            differences = result.Differences.Select(d => new
            {
                standard = d.StandardName,
                state = d.StateName,
                variable = d.VariableName,
                kind = d.Kind.ToString(),
                defaultValue = d.DefaultValue,
                projectValue = d.ProjectValue
            }),
            missingFromProject = result.MissingFromProject,
            projectOnlyStandards = result.ProjectOnlyStandards
        };

        Console.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static void WriteHumanReadable(DiffStandardsResult result)
    {
        if (!result.HasDrift)
        {
            Console.WriteLine("No drift found.");
            if (result.ProjectOnlyStandards.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Project-only Standards (not compared):");
                foreach (string name in result.ProjectOnlyStandards)
                {
                    Console.WriteLine($"  {name}");
                }
            }
            return;
        }

        IEnumerable<IGrouping<string, StandardVariableDiff>> grouped = result.Differences
            .GroupBy(d => d.StandardName)
            .OrderBy(g => g.Key);

        foreach (IGrouping<string, StandardVariableDiff> group in grouped)
        {
            Console.WriteLine($"{group.Key}.gutx:");
            foreach (StandardVariableDiff diff in group)
            {
                string locator = diff.StateName == "Default"
                    ? diff.VariableName
                    : $"{diff.StateName} · {diff.VariableName}";

                string change = diff.Kind switch
                {
                    StandardVariableDiffKind.Changed => $"{diff.DefaultValue} → {diff.ProjectValue}",
                    StandardVariableDiffKind.AddedInProject => $"(absent in Default) → {diff.ProjectValue}",
                    StandardVariableDiffKind.RemovedFromProject => $"{diff.DefaultValue} → (absent in project)",
                    _ => $"{diff.DefaultValue} → {diff.ProjectValue}"
                };

                Console.WriteLine($"  {locator}: {change}");
            }
        }

        if (result.MissingFromProject.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Default Standards missing from project:");
            foreach (string name in result.MissingFromProject)
            {
                Console.WriteLine($"  {name}");
            }
        }

        if (result.ProjectOnlyStandards.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Project-only Standards (not compared):");
            foreach (string name in result.ProjectOnlyStandards)
            {
                Console.WriteLine($"  {name}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"{result.Differences.Count} variable difference(s) across {result.Differences.Select(d => d.StandardName).Distinct().Count()} Standard(s).");
    }
}
