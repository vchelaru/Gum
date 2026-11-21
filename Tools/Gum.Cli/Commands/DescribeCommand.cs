using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.ProjectServices.Editing;

namespace Gum.Cli.Commands;

/// <summary>
/// Defines the <c>gumcli describe</c> command, which lists the variables a Screen, Component or standard element (or one of its
/// instances) can set with <c>gumcli edit</c>'s <c>setVariable</c>, and the element's states and categories.
/// </summary>
public static class DescribeCommand
{
    /// <summary>
    /// Creates the <c>describe</c> command definition.
    /// </summary>
    public static Command Create()
    {
        Argument<string> projectArgument = new Argument<string>(
            "project",
            "Path to the .gumx or .gumj project file.");

        Argument<string?> elementArgument = new Argument<string?>(
            "element",
            "The Screen, Component or standard element to describe, such as Hud, Menus/MainMenu or Text. Omit it to list the project's elements.")
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        Option<string?> instanceOption = new Option<string?>(
            "--instance",
            "List the variables of this instance in the element instead of the element's own.");

        Option<bool> jsonOption = new Option<bool>(
            "--json",
            "Output the description as a JSON document.");

        Command command = new Command(
            "describe",
            "List the variables an element or instance can set, with their types, and the element's states, categories, base type, behaviors, instances, exposed variables and variable references. Without an element, list the project's elements.")
        {
            projectArgument,
            elementArgument,
            instanceOption,
            jsonOption
        };

        command.SetHandler((InvocationContext context) =>
        {
            context.ExitCode = Execute(
                context.ParseResult.GetValueForArgument(projectArgument),
                context.ParseResult.GetValueForArgument(elementArgument),
                context.ParseResult.GetValueForOption(instanceOption),
                context.ParseResult.GetValueForOption(jsonOption));
        });

        return command;
    }

    private static int Execute(string projectPath, string? elementName, string? instanceName, bool json)
    {
        if (elementName == null && instanceName != null)
        {
            Console.Error.WriteLine("error: --instance needs an element. Run gumcli describe <project> to list the elements.");
            return 2;
        }

        ProjectLoadResult loadResult = new ProjectLoader().Load(Path.GetFullPath(projectPath));
        if (!loadResult.Success)
        {
            Console.Error.WriteLine(loadResult.ErrorMessage);
            return 2;
        }
        GumProjectSave project = loadResult.Project!;
        ObjectFinder.Self.GumProjectSave = project;

        if (elementName == null)
        {
            IReadOnlyList<ListedElement> listed = ProjectElementFinder.List(project);
            Console.WriteLine(json ? ListToJson(listed) : ListToText(listed));
            return 0;
        }

        ElementSave? element = ProjectElementFinder.Find(project, elementName);
        if (element == null)
        {
            Console.Error.WriteLine($"error: The project has no Screen, Component or standard element named {elementName}. Run gumcli describe <project> to list them.");
            return 2;
        }

        InstanceSave? instance = null;
        if (instanceName != null)
        {
            instance = element.GetInstance(instanceName);
            if (instance == null)
            {
                Console.Error.WriteLine($"error: {elementName} has no instance named {instanceName}.");
                return 2;
            }
            if (ObjectFinder.Self.GetElementSave(instance.BaseType) == null)
            {
                Console.Error.WriteLine($"error: {instanceName}'s type {instance.BaseType} is not in the project.");
                return 2;
            }
        }

        IElementDescriptionService service = new ElementDescriptionService(new VariableDefinedByTypeLogic(), new DefaultTypeResolver());
        ElementDescription description = service.Describe(element, instance);
        Console.WriteLine(json ? ToJson(description) : ToText(description));
        return 0;
    }

    private static string ListToJson(IReadOnlyList<ListedElement> elements)
    {
        using MemoryStream stream = new MemoryStream();
        using (Utf8JsonWriter writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteStartArray("elements");
            foreach (ListedElement element in elements)
            {
                writer.WriteStartObject();
                writer.WriteString("name", element.Name);
                writer.WriteString("kind", element.Kind.ToString().ToLowerInvariant());
                writer.WriteString("baseType", element.BaseType);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string ListToText(IReadOnlyList<ListedElement> elements)
    {
        StringBuilder text = new StringBuilder();
        foreach (ListedElementKind kind in Enum.GetValues<ListedElementKind>())
        {
            string heading = kind switch
            {
                ListedElementKind.Screen => "Screens",
                ListedElementKind.Component => "Components",
                ListedElementKind.Standard => "Standards",
                _ => "Behaviors"
            };
            text.AppendLine($"{heading}:");
            foreach (ListedElement element in elements.Where(item => item.Kind == kind))
            {
                text.AppendLine(element.BaseType == null ? $"  {element.Name}" : $"  {element.Name} ({element.BaseType})");
            }
        }
        return text.ToString().TrimEnd();
    }

    private static string ToJson(ElementDescription description)
    {
        using MemoryStream stream = new MemoryStream();
        using (Utf8JsonWriter writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("element", description.Element);
            writer.WriteString("instance", description.Instance);
            writer.WriteString("type", description.Type);

            writer.WriteStartArray("variables");
            foreach (DescribedVariable variable in description.Variables)
            {
                writer.WriteStartObject();
                writer.WriteString("name", variable.Name);
                writer.WriteString("type", variable.Type);
                if (variable.Values != null)
                {
                    WriteStrings(writer, "values", variable.Values);
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            WriteStrings(writer, "states", description.States);

            writer.WriteStartArray("categories");
            foreach (DescribedCategory category in description.Categories)
            {
                writer.WriteStartObject();
                writer.WriteString("name", category.Name);
                WriteStrings(writer, "states", category.States);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            if (description.Instances != null)
            {
                writer.WriteString("baseType", description.BaseType);
                WriteStrings(writer, "behaviors", description.Behaviors!);

                writer.WriteStartArray("instances");
                foreach (DescribedInstance item in description.Instances)
                {
                    writer.WriteStartObject();
                    writer.WriteString("name", item.Name);
                    writer.WriteString("type", item.Type);
                    writer.WriteString("parent", item.Parent);
                    writer.WriteBoolean("definedByBase", item.DefinedByBase);
                    writer.WriteBoolean("locked", item.Locked);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();

                writer.WriteStartArray("exposedVariables");
                foreach (DescribedExposedVariable item in description.ExposedVariables!)
                {
                    writer.WriteStartObject();
                    writer.WriteString("name", item.Name);
                    writer.WriteString("variable", item.Variable);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();

                writer.WriteStartArray("variableReferences");
                foreach (DescribedVariableReferences item in description.VariableReferences!)
                {
                    writer.WriteStartObject();
                    writer.WriteString("instance", item.Instance);
                    writer.WriteString("state", item.State);
                    writer.WriteString("category", item.Category);
                    WriteStrings(writer, "lines", item.Lines);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteStrings(Utf8JsonWriter writer, string name, System.Collections.Generic.IEnumerable<string> values)
    {
        writer.WriteStartArray(name);
        foreach (string value in values)
        {
            writer.WriteStringValue(value);
        }
        writer.WriteEndArray();
    }

    private static void AppendElementDetails(StringBuilder text, ElementDescription description)
    {
        if (description.BaseType != null)
        {
            text.AppendLine().Append($"Base type: {description.BaseType}");
        }
        if (description.Behaviors!.Count > 0)
        {
            text.AppendLine().Append($"Behaviors: {string.Join(", ", description.Behaviors)}");
        }
        if (description.Instances!.Count > 0)
        {
            text.AppendLine().Append("Instances:");
            foreach (DescribedInstance item in description.Instances)
            {
                string parent = item.Parent == null ? "" : $" in {item.Parent}";
                string inherited = item.DefinedByBase ? " inherited" : "";
                string locked = item.Locked ? " locked" : "";
                text.AppendLine().Append($"  {item.Name} ({item.Type}){parent}{inherited}{locked}");
            }
        }
        if (description.ExposedVariables!.Count > 0)
        {
            text.AppendLine().Append("Exposed:");
            foreach (DescribedExposedVariable item in description.ExposedVariables)
            {
                text.AppendLine().Append($"  {item.Name} -> {item.Variable}");
            }
        }
        if (description.VariableReferences!.Count > 0)
        {
            text.AppendLine().Append("References:");
            foreach (DescribedVariableReferences item in description.VariableReferences)
            {
                string owner = item.Instance ?? "(element)";
                string state = item.Category == null ? item.State : $"{item.Category}/{item.State}";
                string where = item.State == "Default" && item.Category == null ? "" : $" [{state}]";
                foreach (string line in item.Lines)
                {
                    text.AppendLine().Append($"  {owner}{where}: {line}");
                }
            }
        }
    }

    private static string ToText(ElementDescription description)
    {
        StringBuilder text = new StringBuilder();
        text.AppendLine(description.Instance == null
            ? $"{description.Element}"
            : $"{description.Element}.{description.Instance} ({description.Type})");

        text.AppendLine("Variables:");
        int width = description.Variables.Count == 0 ? 0 : description.Variables.Max(variable => variable.Name.Length);
        foreach (DescribedVariable variable in description.Variables)
        {
            string values = variable.Values == null ? "" : $" ({string.Join(", ", variable.Values)})";
            text.AppendLine($"  {variable.Name.PadRight(width)}  {variable.Type}{values}");
        }

        text.AppendLine($"States: {string.Join(", ", description.States)}");
        text.Append("Categories:");
        foreach (DescribedCategory category in description.Categories)
        {
            text.AppendLine();
            text.Append($"  {category.Name}: {string.Join(", ", category.States)}");
        }
        if (description.Categories.Count == 0)
        {
            text.Append(" none");
        }

        if (description.Instances != null)
        {
            AppendElementDetails(text, description);
        }
        return text.ToString();
    }
}
