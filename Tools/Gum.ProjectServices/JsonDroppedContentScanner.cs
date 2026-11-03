using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;

namespace Gum.ProjectServices;

/// <summary>
/// Finds content in a hand-written JSON element or behavior file that the loader silently drops
/// (a misspelled collection such as <c>"Instance"</c> for <c>"Instances"</c>, or a wrong-case property name).
/// System.Text.Json skips unknown and wrong-case properties, so the element loads with that content missing.
/// The JSON counterpart of the XML wrong-pattern check in <see cref="ProjectLoader"/> (#7552).
/// </summary>
internal static class JsonDroppedContentScanner
{
    private static readonly string[] ElementKeys =
        { "Name", "BaseType", "States", "Categories", "Instances", "Events", "Behaviors", "VariablesHiddenFromInstances" };

    private static readonly string[] BehaviorKeys =
        { "Name", "RequiredVariables", "FormsProperties", "ToolOnlyVariableReferences", "Categories", "RequiredInstances", "RequiredAnimations", "DefaultImplementation" };

    private static readonly string[] StateKeys = { "Name", "Variables", "VariableLists" };

    private static readonly string[] CategoryKeys = { "Name", "States" };

    // Singular forms the XML format uses (<Instance>, <State>, <Variable>) mapped to the JSON collection name.
    private static readonly Dictionary<string, string> Misspellings = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Instance"] = "Instances",
        ["State"] = "States",
        ["Category"] = "Categories",
        ["Variable"] = "Variables",
        ["VariableList"] = "VariableLists",
        ["RequiredInstance"] = "RequiredInstances",
    };

    public static List<ErrorResult> Scan(GumProjectSave project, string projectDirectory)
    {
        List<ErrorResult> errors = new List<ErrorResult>();

        foreach (ElementReference reference in project.ScreenReferences)
        {
            ScanFile(errors, projectDirectory, ElementReference.ScreenSubfolder, reference.Name, GumProjectSave.ScreenJsonExtension, isBehavior: false);
        }
        foreach (ElementReference reference in project.ComponentReferences)
        {
            ScanFile(errors, projectDirectory, ElementReference.ComponentSubfolder, reference.Name, GumProjectSave.ComponentJsonExtension, isBehavior: false);
        }
        foreach (ElementReference reference in project.StandardElementReferences)
        {
            ScanFile(errors, projectDirectory, ElementReference.StandardSubfolder, reference.Name, GumProjectSave.StandardJsonExtension, isBehavior: false);
        }
        if (project.BehaviorReferences != null)
        {
            foreach (BehaviorReference reference in project.BehaviorReferences)
            {
                ScanFile(errors, projectDirectory, BehaviorReference.Subfolder, reference.Name, BehaviorReference.JsonExtension, isBehavior: true);
            }
        }

        return errors;
    }

    private static void ScanFile(List<ErrorResult> errors, string projectDirectory, string subfolder, string name, string extension, bool isBehavior)
    {
        string path = Path.Combine(projectDirectory, subfolder, name + "." + extension);
        if (!File.Exists(path))
        {
            return;
        }

        JsonObject? root;
        try
        {
            root = JsonNode.Parse(File.ReadAllText(path)) as JsonObject;
        }
        catch (System.Text.Json.JsonException)
        {
            // Malformed JSON is already reported by the load itself.
            return;
        }
        if (root == null)
        {
            return;
        }

        List<string> messages = new List<string>();
        ScanObject(root, isBehavior ? BehaviorKeys : ElementKeys, messages);

        if (isBehavior)
        {
            ScanState(root["RequiredVariables"], messages);
        }
        else
        {
            ScanStates(root["States"], messages);
        }
        if (root["Categories"] is JsonArray categories)
        {
            foreach (JsonObject category in categories.OfType<JsonObject>())
            {
                ScanObject(category, CategoryKeys, messages);
                ScanStates(category["States"], messages);
            }
        }

        foreach (string message in messages.Distinct())
        {
            errors.Add(new ErrorResult { ElementName = name, Message = message, Severity = ErrorSeverity.Error });
        }
    }

    private static void ScanStates(JsonNode? states, List<string> messages)
    {
        if (states is JsonArray array)
        {
            foreach (JsonNode? state in array)
            {
                ScanState(state, messages);
            }
        }
    }

    private static void ScanState(JsonNode? state, List<string> messages)
    {
        if (state is JsonObject stateObject)
        {
            ScanObject(stateObject, StateKeys, messages);
        }
    }

    private static void ScanObject(JsonObject obj, string[] knownKeys, List<string> messages)
    {
        foreach (KeyValuePair<string, JsonNode?> pair in obj)
        {
            if (knownKeys.Contains(pair.Key, StringComparer.Ordinal))
            {
                continue;
            }

            string? caseMatch = knownKeys.FirstOrDefault(known => string.Equals(known, pair.Key, StringComparison.OrdinalIgnoreCase));
            if (caseMatch != null)
            {
                messages.Add($"JSON property \"{pair.Key}\" is ignored because property names are case-sensitive. Use \"{caseMatch}\".");
            }
            else if (Misspellings.TryGetValue(pair.Key, out string? correct) && knownKeys.Contains(correct))
            {
                messages.Add($"JSON property \"{pair.Key}\" is ignored. The collection is named \"{correct}\" (plural), so its content is dropped at load.");
            }
        }
    }
}
