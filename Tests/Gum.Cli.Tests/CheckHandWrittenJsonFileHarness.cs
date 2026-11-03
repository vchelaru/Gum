using System.Text.Json;
using System.Text.Json.Nodes;

namespace Gum.Cli.Tests;

/// <summary>
/// Builds a project folder the way a coding agent hand-writes a JSON one (an empty <c>.gumj</c> project plus
/// <c>.gucj</c>/<c>.gusj</c> element files written as JSON) and runs <c>gumcli check</c> over it (#7059).
/// </summary>
public class CheckHandWrittenJsonFileHarness : CheckHandWrittenFileHarness
{
    protected string CreateJsonProject(string name)
    {
        string filePath = Path.Combine(TempDirectory, name, name + ".gumj");
        CliTestHelper.Run("new", filePath, "--template", "empty");
        return filePath;
    }

    protected static string JInstance(string name, string? baseType, bool definedByBase = false)
    {
        string baseTypeJson = baseType == null ? "" : $",\"BaseType\":\"{baseType}\"";
        string defined = definedByBase ? ",\"DefinedByBase\":true" : "";
        return $"{{\"Name\":\"{name}\"{baseTypeJson}{defined}}}";
    }

    /// <summary>One variable; <paramref name="valueProperty"/> is e.g. <c>"ValueAsFloat": 4</c> (or empty for no value).</summary>
    protected static string JVar(string type, string name, string valueProperty = "", bool setsValue = true)
    {
        string value = valueProperty == "" ? "" : "," + valueProperty;
        return $"{{\"Name\":\"{name}\",\"Type\":\"{type}\",\"SetsValue\":{(setsValue ? "true" : "false")}{value}}}";
    }

    protected static string JState(string name, params string[] variables) =>
        $"{{\"Name\":\"{name}\",\"Variables\":[{string.Join(",", variables)}]}}";

    protected static string JStates(params string[] states) => $"\"States\":[{string.Join(",", states)}]";

    protected static string JDefault(params string[] variables) => JStates(JState("Default", variables));

    protected static string JInstances(params string[] instances) => $"\"Instances\":[{string.Join(",", instances)}]";

    protected static string JElement(string name, string? baseType, params string[] properties)
    {
        List<string> parts = new List<string> { $"\"Name\":\"{name}\"" };
        if (baseType != null)
        {
            parts.Add($"\"BaseType\":\"{baseType}\"");
        }
        parts.AddRange(properties);
        return "{" + string.Join(",", parts) + "}";
    }

    protected static void AddJsonComponent(string gumjPath, string name, string? baseType, params string[] properties)
    {
        AddJsonElementFile(gumjPath, "Components", "gucj", "ComponentReferences", name, JElement(name, baseType, properties));
    }

    protected static void AddJsonScreen(string gumjPath, string name, string? baseType, params string[] properties)
    {
        AddJsonElementFile(gumjPath, "Screens", "gusj", "ScreenReferences", name, JElement(name, baseType, properties));
    }

    protected static void AddJsonElementFile(string gumjPath, string folder, string extension, string referenceList,
        string name, string fileContent)
    {
        WriteFile(gumjPath, folder, name + "." + extension, fileContent);
        AddJsonReference(gumjPath, referenceList, name);
    }

    protected static void AddJsonReference(string gumjPath, string referenceList, string name, int elementType = 0)
    {
        JsonNode root = JsonNode.Parse(File.ReadAllText(gumjPath))!;
        JsonArray list = root[referenceList]?.AsArray() ?? new JsonArray();
        list.Add(new JsonObject { ["Name"] = name, ["ElementType"] = elementType, ["LinkType"] = 0 });
        root[referenceList] = list;
        File.WriteAllText(gumjPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }
}
