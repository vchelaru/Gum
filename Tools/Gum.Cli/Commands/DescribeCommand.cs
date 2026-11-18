using System;
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
/// Defines the <c>gumcli describe</c> command, which lists the variables a Screen or Component (or one of its
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

        Argument<string> elementArgument = new Argument<string>(
            "element",
            "The Screen or Component to describe, such as Hud or Menus/MainMenu.");

        Option<string?> instanceOption = new Option<string?>(
            "--instance",
            "List the variables of this instance in the element instead of the element's own.");

        Option<bool> jsonOption = new Option<bool>(
            "--json",
            "Output the description as a JSON document.");

        Command command = new Command(
            "describe",
            "List the variables an element or instance can set, with their types, and the element's states and categories.")
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

    private static int Execute(string projectPath, string elementName, string? instanceName, bool json)
    {
        ProjectLoadResult loadResult = new ProjectLoader().Load(Path.GetFullPath(projectPath));
        if (!loadResult.Success)
        {
            Console.Error.WriteLine(loadResult.ErrorMessage);
            return 2;
        }
        GumProjectSave project = loadResult.Project!;
        ObjectFinder.Self.GumProjectSave = project;

        ElementSave? element = (ElementSave?)project.Screens.FirstOrDefault(item => item.Name == elementName)
            ?? project.Components.FirstOrDefault(item => item.Name == elementName);
        if (element == null)
        {
            Console.Error.WriteLine($"error: The project has no Screen or Component named {elementName}.");
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
        return text.ToString();
    }
}
