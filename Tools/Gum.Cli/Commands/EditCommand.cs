using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text.Json;
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
        // Both are optional only so that --schema can run without them; Execute requires them otherwise.
        Argument<string?> projectArgument = new Argument<string?>(
            "project",
            "Path to the .gumx or .gumj project file.")
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        Argument<string?> operationsArgument = new Argument<string?>(
            "operations",
            "Path to the JSON operations file.")
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        Option<bool> jsonOption = new Option<bool>(
            "--json",
            "Report new errors (or the --schema output) as JSON.");

        Option<bool> schemaOption = new Option<bool>(
            "--schema",
            "Print every operation and its required and optional fields instead of editing. Takes no project.");

        Command command = new Command(
            "edit",
            "Apply an operations file to a Gum project. Nothing is saved if an operation fails or the edit adds an error.")
        {
            projectArgument,
            operationsArgument,
            jsonOption,
            schemaOption
        };

        command.SetHandler((InvocationContext context) =>
        {
            string? projectPath = context.ParseResult.GetValueForArgument(projectArgument);
            string? operationsPath = context.ParseResult.GetValueForArgument(operationsArgument);
            bool json = context.ParseResult.GetValueForOption(jsonOption);
            if (context.ParseResult.GetValueForOption(schemaOption))
            {
                context.ExitCode = WriteSchema(json);
            }
            else if (projectPath == null || operationsPath == null)
            {
                Console.Error.WriteLine("edit needs a project and an operations file, or --schema.");
                context.ExitCode = 2;
            }
            else
            {
                context.ExitCode = Execute(projectPath, operationsPath, json);
            }
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

    private static int WriteSchema(bool json)
    {
        IReadOnlyList<EditOperationSchema> schemas = CreateEditService().GetOperationSchemas();
        if (json)
        {
            var document = new
            {
                version = ProjectEditService.SupportedVersion,
                operations = schemas.Select(schema => new { op = schema.Op, required = schema.Required, optional = schema.Optional })
            };
            Console.WriteLine(JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        Console.WriteLine($"Operations file version {ProjectEditService.SupportedVersion}. Fields in [brackets] are optional.");
        int width = schemas.Max(schema => schema.Op.Length);
        foreach (EditOperationSchema schema in schemas)
        {
            IEnumerable<string> fields = schema.Required.Concat(schema.Optional.Select(field => $"[{field}]"));
            Console.WriteLine($"  {schema.Op.PadRight(width)}  {string.Join(", ", fields)}");
        }
        return 0;
    }

    /// <summary>
    /// Builds the edit service with the headless implementations of the tool's editing logic.
    /// </summary>
    public static IProjectEditService CreateEditService()
    {
        ReferenceFinder referenceFinder = new ReferenceFinder(new ObjectFinderProjectProvider());
        VariableOverrideRemover overrideRemover = new VariableOverrideRemover();
        ElementCreationLogic elementCreation = new ElementCreationLogic();
        return new ProjectEditService(
            elementCreation,
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
            new VariableClearLogic(),
            new ComponentExtractionLogic(elementCreation),
            new MovedInstanceReferenceDropLogic(referenceFinder));
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
