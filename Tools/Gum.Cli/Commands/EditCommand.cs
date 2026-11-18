using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.Expressions;
using Gum.Logic;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.ProjectServices.Editing;
using Gum.Services;

namespace Gum.Cli.Commands;

/// <summary>
/// Defines the <c>gumcli edit</c> command, which applies an operations file to a project through the tool's own
/// editing logic and saves it in the tool's format. All or nothing: it runs the <c>check</c> error check before and
/// after, and an error the edit introduces, like any operation that fails, leaves every file untouched.
/// </summary>
public static class EditCommand
{
    /// <summary>
    /// Creates the <c>edit</c> command definition.
    /// </summary>
    public static Command Create()
    {
        Argument<string> projectArgument = new Argument<string>(
            "project",
            "Path to the .gumx or .gumj project file.");

        Argument<string> operationsArgument = new Argument<string>(
            "operations",
            "Path to the JSON operations file.");

        Option<bool> jsonOption = new Option<bool>(
            "--json",
            "Report new errors as a JSON array, as check --json does.");

        Command command = new Command(
            "edit",
            "Apply an operations file to a Gum project. Nothing is saved if an operation fails or the edit adds an error.")
        {
            projectArgument,
            operationsArgument,
            jsonOption
        };

        command.SetHandler((InvocationContext context) =>
        {
            string projectPath = context.ParseResult.GetValueForArgument(projectArgument);
            string operationsPath = context.ParseResult.GetValueForArgument(operationsArgument);
            bool json = context.ParseResult.GetValueForOption(jsonOption);
            context.ExitCode = Execute(projectPath, operationsPath, json);
        });

        return command;
    }

    private static int Execute(string projectPath, string operationsPath, bool json)
    {
        string fullPath = Path.GetFullPath(projectPath);
        if (!File.Exists(operationsPath))
        {
            Console.Error.WriteLine($"Operations file not found: {Path.GetFullPath(operationsPath)}");
            return 2;
        }
        string operationsJson = File.ReadAllText(operationsPath);

        IProjectLoader loader = new ProjectLoader();
        ProjectLoadResult loadResult = loader.Load(fullPath);
        if (!loadResult.Success)
        {
            Console.Error.WriteLine(loadResult.ErrorMessage);
            return 2;
        }
        GumProjectSave project = loadResult.Project!;

        // GUM0002 evaluates reference right-hand sides (including cross-element ones) with the Roslyn evaluator.
        GumExpressionService.Initialize();
        HeadlessErrorChecker checker = CheckCommand.CreateChecker();
        IReadOnlyList<ErrorResult> errorsBefore = checker.GetAllErrors(project);

        // The editing logic resolves types and base elements through ObjectFinder.
        ObjectFinder.Self.GumProjectSave = project;
        ProjectEditResult editResult = CreateEditService().Apply(project, operationsJson);
        if (!editResult.Succeeded)
        {
            Console.Error.WriteLine($"error: {editResult.Message}");
            return editResult.Failure == ProjectEditFailure.InvalidFile ? 2 : 1;
        }

        List<ErrorResult> newErrors = GetNewErrors(errorsBefore, checker.GetAllErrors(project));
        CheckCommand.WriteReport(newErrors, json);
        if (newErrors.Any(error => error.Severity == ErrorSeverity.Error))
        {
            return 1;
        }

        try
        {
            IStateVariableSorter sorter = new StateVariableSorter();
            foreach (ElementSave element in project.Screens.Concat<ElementSave>(project.Components))
            {
                sorter.SortStateVariables(element);
            }
            project.Save(fullPath, saveElements: true);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }

        if (!json)
        {
            Console.WriteLine($"Saved {fullPath}");
        }
        return 0;
    }

    private static IProjectEditService CreateEditService()
    {
        ReferenceFinder referenceFinder = new ReferenceFinder(new ObjectFinderProjectProvider());
        VariableOverrideRemover overrideRemover = new VariableOverrideRemover();
        return new ProjectEditService(
            new ElementCreationLogic(),
            new InstanceCreationLogic(),
            new InstanceAddRules(new CircularReferenceManager(ObjectFinder.Self)),
            new StateCreationLogic(),
            new CategoryVariablePropagator(),
            new CustomVariableLogic(new StateVariableSorter()),
            new ExposeVariableLogic(),
            new StateVariableWriter(),
            new VariableDefinedByTypeLogic(),
            new ElementNameValidator(topLevelNameSource: null),
            new DefaultTypeResolver(),
            new InstanceRemovalLogic(referenceFinder, overrideRemover),
            new InstanceChildDetacher(),
            new StateRemovalLogic(referenceFinder, new ObjectFinderProjectProvider()),
            new VariableRemovalLogic(referenceFinder),
            overrideRemover,
            new VariableClearLogic());
    }

    /// <summary>
    /// The errors in <paramref name="after"/> that <paramref name="before"/> did not have, compared by element,
    /// code, severity and message, and counted so a second copy of an existing error is new.
    /// </summary>
    private static List<ErrorResult> GetNewErrors(IReadOnlyList<ErrorResult> before, IReadOnlyList<ErrorResult> after)
    {
        Dictionary<(string, string?, ErrorSeverity, string), int> remaining = before
            .GroupBy(Key)
            .ToDictionary(group => group.Key, group => group.Count());

        List<ErrorResult> newErrors = new List<ErrorResult>();
        foreach (ErrorResult error in after)
        {
            (string, string?, ErrorSeverity, string) key = Key(error);
            if (remaining.TryGetValue(key, out int count) && count > 0)
            {
                remaining[key] = count - 1;
            }
            else
            {
                newErrors.Add(error);
            }
        }
        return newErrors;
    }

    private static (string, string?, ErrorSeverity, string) Key(ErrorResult error) =>
        (error.ElementName, error.Code, error.Severity, error.Message);
}
