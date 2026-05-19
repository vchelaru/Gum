using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text.Json;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.ProjectServices;

namespace Gum.Cli.Commands;

/// <summary>
/// Defines the <c>gumcli check-references</c> command which detects
/// <c>VariableReferences</c> rows whose left-hand-side scalars are not
/// materialized in the owning state's <c>Variables</c> — the inconsistent
/// shape AI agents and hand edits commonly leave behind. With <c>--fix</c>
/// it runs the same propagation the tool does on interactive edits and
/// saves the affected element files.
/// </summary>
public static class CheckReferencesCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static Command Create()
    {
        var projectArgument = new Argument<string>(
            "project",
            "Path to the .gumx project file.");

        var jsonOption = new Option<bool>(
            "--json",
            "Output results as a JSON array.");

        var fixOption = new Option<bool>(
            "--fix",
            "Propagate references on affected states and save the modified element files.");

        var command = new Command(
            "check-references",
            "Detect (and optionally fix) VariableReferences rows whose left-hand-side scalars are not materialized in state.Variables.")
        {
            projectArgument,
            jsonOption,
            fixOption
        };

        command.SetHandler((InvocationContext context) =>
        {
            string projectPath = context.ParseResult.GetValueForArgument(projectArgument);
            bool json = context.ParseResult.GetValueForOption(jsonOption);
            bool fix = context.ParseResult.GetValueForOption(fixOption);
            context.ExitCode = Execute(projectPath, json, fix);
        });

        return command;
    }

    private static int Execute(string projectPath, bool json, bool fix)
    {
        var fullPath = Path.GetFullPath(projectPath);

        IProjectLoader loader = new ProjectLoader();
        ProjectLoadResult loadResult = loader.Load(fullPath);

        if (!loadResult.Success)
        {
            Console.Error.WriteLine(loadResult.ErrorMessage);
            return 2;
        }

        // Wire the Roslyn-based right-side evaluator so literals and expressions
        // resolve during --fix (mirrors what the Gum tool does at startup via
        // MainVariableGridPlugin.Initialize).
        GumExpressionService.Initialize();

        // ObjectFinder lookups inside ApplyVariableReferences (cross-element refs,
        // GetRootVariable for type inference) require the singleton to point at
        // the loaded project.
        ObjectFinder.Self.GumProjectSave = loadResult.Project;

        IReferencePropagationService service = new ReferencePropagationService();

        if (fix)
        {
            return ExecuteFix(service, loadResult.Project!, json, fullPath);
        }

        return ExecuteDetect(service, loadResult.Project!, json);
    }

    private static string? GetElementSavePath(string projectDirectory, ElementSave element)
    {
        string subfolder;
        string extension;
        switch (element)
        {
            case ScreenSave:
                subfolder = ElementReference.ScreenSubfolder;
                extension = GumProjectSave.ScreenExtension;
                break;
            case ComponentSave:
                subfolder = ElementReference.ComponentSubfolder;
                extension = GumProjectSave.ComponentExtension;
                break;
            case StandardElementSave:
                subfolder = ElementReference.StandardSubfolder;
                extension = GumProjectSave.StandardExtension;
                break;
            default:
                return null;
        }
        return Path.Combine(projectDirectory, subfolder, element.Name + "." + extension);
    }

    private static int ExecuteDetect(IReferencePropagationService service, GumProjectSave project, bool json)
    {
        DetectUnpropagatedReferencesResult result = service.Detect(project);

        if (json)
        {
            WriteJson(result);
        }
        else
        {
            WriteHumanReadable(result);
        }

        return result.HasUnpropagatedReferences ? 1 : 0;
    }

    private static int ExecuteFix(IReferencePropagationService service, GumProjectSave project, bool json, string projectFilePath)
    {
        IReadOnlyList<ElementSave> modified = service.PropagateReferences(project);

        // ElementSave.FileName isn't populated by the loader, so reconstruct the
        // canonical save path the same way GumProjectSave.Save does: project dir +
        // element-type subfolder + name + extension.
        string projectDirectory = Path.GetDirectoryName(projectFilePath) ?? "";
        bool useCompact = project.Version >= (int)GumProjectSave.GumxVersions.AttributeVersion;
        foreach (ElementSave element in modified)
        {
            string elementPath = GetElementSavePath(projectDirectory, element);
            if (!string.IsNullOrEmpty(elementPath))
            {
                element.Save(elementPath, useCompact);
            }
        }

        // Re-detect to confirm everything that needed fixing was fixed.
        DetectUnpropagatedReferencesResult after = service.Detect(project);

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                fixedElements = modified.Select(e => e.Name).ToList(),
                stillBroken = after.Elements.Select(e => new
                {
                    element = e.Element.Name,
                    states = e.States.Select(s => s.Name).ToList()
                }).ToList()
            }, JsonOptions));
        }
        else
        {
            if (modified.Count == 0 && !after.HasUnpropagatedReferences)
            {
                Console.WriteLine("No unpropagated references found.");
            }
            else
            {
                foreach (ElementSave element in modified)
                {
                    Console.WriteLine($"fixed: {element.Name}");
                }
                if (after.HasUnpropagatedReferences)
                {
                    Console.WriteLine();
                    Console.WriteLine("The following references could not be evaluated:");
                    foreach (ElementWithUnpropagatedReferences entry in after.Elements)
                    {
                        foreach (StateSave state in entry.States)
                        {
                            Console.WriteLine($"  {entry.Element.Name} [{state.Name}]");
                        }
                    }
                }
                Console.WriteLine();
                Console.WriteLine($"{modified.Count} element(s) fixed.");
                if (after.HasUnpropagatedReferences)
                {
                    Console.WriteLine($"{after.Elements.Count} element(s) still have unpropagated references.");
                }
            }
        }

        return after.HasUnpropagatedReferences ? 1 : 0;
    }

    private static void WriteJson(DetectUnpropagatedReferencesResult result)
    {
        var output = result.Elements.Select(e => new
        {
            element = e.Element.Name,
            states = e.States.Select(s => s.Name).ToList()
        });

        Console.WriteLine(JsonSerializer.Serialize(output, JsonOptions));
    }

    private static void WriteHumanReadable(DetectUnpropagatedReferencesResult result)
    {
        if (!result.HasUnpropagatedReferences)
        {
            Console.WriteLine("No unpropagated references found.");
            return;
        }

        foreach (ElementWithUnpropagatedReferences entry in result.Elements)
        {
            foreach (StateSave state in entry.States)
            {
                Console.WriteLine($"{entry.Element.Name} [{state.Name}]: has VariableReferences but missing materialized scalars");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"{result.Elements.Count} element(s) with unpropagated references.");
        Console.WriteLine("Run with --fix to propagate.");
    }
}
