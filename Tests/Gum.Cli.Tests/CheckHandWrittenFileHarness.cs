using System.Text.Json;

namespace Gum.Cli.Tests;

/// <summary>
/// Builds a project folder the way a coding agent hand-writes one (an empty project plus element files
/// written as XML) and runs <c>gumcli check</c> over it in-process (#7059).
/// </summary>
public class CheckHandWrittenFileHarness : BaseTestClass
{
    public record CheckEntry(string Element, string Message, string Severity, string? Code);

    public record CheckRun(int ExitCode, string StandardOutput, string StandardError, IReadOnlyList<CheckEntry> Entries);

    private const string Header =
        """<?xml version="1.0" encoding="utf-8"?>""";

    private const string Namespaces =
        """xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" """;

    protected string CreateProject(string name)
    {
        string filePath = Path.Combine(TempDirectory, name, name + ".gumx");
        CliTestHelper.Run("new", filePath, "--template", "empty");
        return filePath;
    }

    protected static string Instance(string name, string baseType, bool definedByBase = false)
    {
        string defined = definedByBase ? "<DefinedByBase>true</DefinedByBase>" : "";
        return $"<Instance><Name>{name}</Name><BaseType>{baseType}</BaseType>{defined}</Instance>";
    }

    protected static string Variable(string type, string name, string xsdType, string value, bool isCustom = false, string? exposedAs = null)
    {
        string custom = isCustom ? "<IsCustomVariable>true</IsCustomVariable>" : "";
        string exposed = exposedAs != null ? $"<ExposedAsName>{exposedAs}</ExposedAsName>" : "";
        return $"<Variable><Type>{type}</Type><Name>{name}</Name><Value xsi:type=\"xsd:{xsdType}\">{value}</Value><SetsValue>true</SetsValue>{custom}{exposed}</Variable>";
    }

    protected static string DefaultState(params string[] variables) =>
        "<State><Name>Default</Name>" + string.Concat(variables) + "</State>";

    protected static string ElementXml(string rootTag, string name, string? baseType, string body)
    {
        string baseTypeXml = baseType == null ? "" : $"<BaseType>{baseType}</BaseType>";
        return $"{Header}\n<{rootTag} {Namespaces}><Name>{name}</Name>{baseTypeXml}{body}</{rootTag}>";
    }

    protected static void AddComponent(string gumxPath, string name, string? baseType, string body = "")
    {
        AddElement(gumxPath, "Components", "gucx", "ComponentSave", "ComponentReference", name, baseType, body);
    }

    protected static void AddScreen(string gumxPath, string name, string? baseType, string body = "")
    {
        AddElement(gumxPath, "Screens", "gusx", "ScreenSave", "ScreenReference", name, baseType, body);
    }

    private static void AddElement(string gumxPath, string folder, string extension, string rootTag, string referenceTag,
        string name, string? baseType, string body)
    {
        WriteFile(gumxPath, folder, name + "." + extension, ElementXml(rootTag, name, baseType, body));
        AddReference(gumxPath, $"""  <{referenceTag} Name="{name}" />""");
    }

    protected static void WriteFile(string gumxPath, string folder, string fileName, string content)
    {
        string directory = Path.Combine(Path.GetDirectoryName(gumxPath)!, folder);
        Directory.CreateDirectory(Path.Combine(directory, Path.GetDirectoryName(fileName) ?? ""));
        File.WriteAllText(Path.Combine(directory, fileName), content);
    }

    protected static void AddReference(string gumxPath, string referenceXml)
    {
        string content = File.ReadAllText(gumxPath);
        content = content.Replace("</GumProjectSave>", referenceXml + "\n</GumProjectSave>");
        File.WriteAllText(gumxPath, content);
    }

    protected CheckRun Check(string gumxPath)
    {
        CliTestHelper result = CliTestHelper.Run("check", gumxPath, "--json");

        List<CheckEntry> entries = new List<CheckEntry>();
        if (result.StandardOutput.StartsWith("["))
        {
            using JsonDocument document = JsonDocument.Parse(result.StandardOutput);
            foreach (JsonElement item in document.RootElement.EnumerateArray())
            {
                entries.Add(new CheckEntry(
                    item.GetProperty("element").GetString() ?? "",
                    item.GetProperty("message").GetString() ?? "",
                    item.GetProperty("severity").GetString() ?? "",
                    item.GetProperty("code").GetString()));
            }
        }

        return new CheckRun(result.ExitCode, result.StandardOutput, result.StandardError, entries);
    }
}
