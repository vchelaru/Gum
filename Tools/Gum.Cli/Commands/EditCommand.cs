using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text.Json;
using Gum.DataTypes;
using Gum.Logic;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.ProjectServices.Editing;
using Gum.Services;
using ToolsUtilities;

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
            "Apply an operations file to a Gum project's Screens, Components and standard elements. Nothing is saved if an operation fails or the edit adds an error.")
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

        // Animation sidecars the edit changes are held here and written after the project saves.
        PendingAnimationEdits pendingAnimations = new PendingAnimationEdits(new FileElementAnimationsProvider());
        HeadlessErrorChecker checker = CheckCommand.CreateChecker(pendingAnimations);
        IReadOnlyList<ErrorResult> errorsBefore = checker.GetAllErrors(project);

        // The editing logic resolves types and base elements through ObjectFinder.
        ObjectFinder.Self.GumProjectSave = project;
        HashSet<FilePath> elementFilesBefore = GetElementFiles(project, fullPath);
        ProjectEditResult editResult = CreateEditService(pendingAnimations).Apply(project, operationsJson);
        if (!editResult.Succeeded)
        {
            Console.Error.WriteLine($"error: {editResult.Message}");
            return editResult.Failure == ProjectEditFailure.InvalidFile ? 2 : 1;
        }

        // The animation files a rename changes are checked as they will be saved, not as they sit on disk.
        PendingFileChanges? pendingFiles = editResult.PendingFiles;
        HeadlessErrorChecker checkerAfter = pendingFiles?.HasChanges == true ? CheckCommand.CreateChecker(pendingFiles) : checker;
        List<ErrorResult> newErrors = GetNewErrors(errorsBefore, checkerAfter.GetAllErrors(project));
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
            // Sidecars first: a failed write leaves the project unsaved, and a failed project save puts them back.
            pendingAnimations.Save(project);
            project.Save(fullPath, saveElements: true);

            // Removed elements' files go only once the saved project no longer references them, so a failed save
            // leaves every file in place. A file a later operation wrote again (remove, then add the same name) stays.
            elementFilesBefore.ExceptWith(GetElementFiles(project, fullPath));
            foreach (FilePath removedFile in elementFilesBefore)
            {
                File.Delete(removedFile.FullPath);
            }
            pendingFiles?.Commit(project);
        }
        catch (Exception ex)
        {
            pendingAnimations.Rollback();
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }

        if (!json)
        {
            Console.WriteLine($"Saved {fullPath}");
        }
        return 0;
    }

    // The files GumProjectSave.Save writes for the project's Screens and Components.
    private static HashSet<FilePath> GetElementFiles(GumProjectSave project, string projectPath)
    {
        string directory = FileManager.GetDirectory(projectPath);
        bool isJsonFormat = GumProjectSave.IsJsonFormat(projectPath);
        return project.Screens.Concat<ElementSave>(project.Components)
            .Select(element => new FilePath(directory + element.Subfolder + "/" + element.Name + "." + element.GetFileExtension(isJsonFormat)))
            .ToHashSet();
    }

    private static int WriteSchema(bool json)
    {
        IProjectEditService editService = CreateEditService();
        IReadOnlyList<EditOperationSchema> schemas = editService.GetOperationSchemas();
        IReadOnlyList<ProjectPropertySchema> projectProperties = editService.GetProjectPropertySchemas();
        if (json)
        {
            var document = new
            {
                version = ProjectEditService.SupportedVersion,
                operations = schemas.Select(schema => new
                {
                    op = schema.Op,
                    standardElements = schema.AppliesToStandardElements,
                    required = schema.Required.Select(field => new { name = field.Name, type = field.TypeName }),
                    optional = schema.Optional.Select(field => new { name = field.Name, type = field.TypeName })
                }),
                projectProperties = projectProperties.Select(property => new
                {
                    name = property.Name,
                    type = property.TypeName,
                    allowed = property.AllowedValues,
                    description = property.Description
                })
            };
            Console.WriteLine(JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        Console.WriteLine($"Operations file version {ProjectEditService.SupportedVersion}. Fields in [brackets] are optional.");
        Console.WriteLine("Operations marked * also accept a standard element such as Text as their element.");
        int width = schemas.Max(schema => schema.Op.Length) + 1;
        foreach (EditOperationSchema schema in schemas)
        {
            IEnumerable<string> fields = schema.Required.Select(field => $"{field.Name}: {field.TypeName}")
                .Concat(schema.Optional.Select(field => $"[{field.Name}: {field.TypeName}]"));
            Console.WriteLine($"  {(schema.Op + (schema.AppliesToStandardElements ? "*" : "")).PadRight(width)}  {string.Join(", ", fields)}");
        }

        Console.WriteLine("setProjectProperty names:");
        int nameWidth = projectProperties.Max(property => property.Name.Length) + 1;
        foreach (ProjectPropertySchema property in projectProperties)
        {
            string allowed = property.AllowedValues.Count == 0 ? "" : $" ({string.Join(" | ", property.AllowedValues)})";
            Console.WriteLine($"  {property.Name.PadRight(nameWidth)}  {property.TypeName}{allowed}: {property.Description}");
        }
        return 0;
    }

    /// <summary>
    /// Builds the edit service with the headless implementations of the tool's editing logic.
    /// </summary>
    public static IProjectEditService CreateEditService(PendingAnimationEdits? pendingAnimations = null)
    {
        ReferenceFinder referenceFinder = new ReferenceFinder(new ObjectFinderProjectProvider());
        VariableOverrideRemover overrideRemover = new VariableOverrideRemover();
        ElementCreationLogic elementCreation = new ElementCreationLogic();
        pendingAnimations ??= new PendingAnimationEdits(new FileElementAnimationsProvider());
        ElementNameValidator nameValidator = new ElementNameValidator(pendingAnimations);
        InstanceAddRules instanceAddRules = new InstanceAddRules(new CircularReferenceManager(ObjectFinder.Self));
        VariableDefinedByTypeLogic variableDefinedByType = new VariableDefinedByTypeLogic();
        InheritanceSync inheritanceSync = new InheritanceSync();
        return new ProjectEditService(
            elementCreation,
            new InstanceCreationLogic(),
            instanceAddRules,
            new StateCreationLogic(),
            new CategoryVariablePropagator(),
            new CustomVariableLogic(new StateVariableSorter()),
            new ExposeVariableLogic(),
            new StateVariableWriter(),
            variableDefinedByType,
            nameValidator,
            new DefaultTypeResolver(),
            new InstanceRemovalLogic(referenceFinder, overrideRemover),
            new InstanceChildDetacher(),
            new StateRemovalLogic(referenceFinder, new ObjectFinderProjectProvider()),
            new VariableRemovalLogic(referenceFinder),
            overrideRemover,
            referenceFinder,
            new VariableClearLogic(),
            new ComponentExtractionLogic(elementCreation),
            new MovedInstanceReferenceDropLogic(referenceFinder),
            new ElementRemovalLogic(),
            new VariableReferenceValidator(),
            new VariableReferenceCascade(),
            inheritanceSync,
            new BaseTypeChangeLogic(instanceAddRules, new BaseTypeVariableRemover(variableDefinedByType, referenceFinder, overrideRemover), inheritanceSync),
            new AnimationEditOperations(new AnimationEditLogic(nameValidator), pendingAnimations));
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
