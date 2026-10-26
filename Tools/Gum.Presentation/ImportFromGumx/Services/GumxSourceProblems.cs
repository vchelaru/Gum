using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using System.Collections.Generic;
using System.Linq;

namespace ImportFromGumxPlugin.Services;

/// <summary>
/// Finds what a loaded source project lists but could not give the importer. A file the project
/// lists that is missing comes back as an empty stand-in the importer must not write, and one that
/// is damaged (or could not be fetched) is left out of the loaded lists entirely, so neither is
/// visible as an element to import.
/// </summary>
public static class GumxSourceProblems
{
    /// <summary>
    /// The listed items of <paramref name="source"/> that did not load, as the name the import reports
    /// them under (a component or screen by its name, a standard or behavior prefixed with its folder)
    /// and why.
    /// </summary>
    public static List<(string Name, string Reason)> Find(GumProjectSave source)
    {
        bool isJson = GumProjectSave.IsJsonFormat(source.FullFileName ?? "");
        List<(string Name, string Reason)> problems = new();

        Check(problems, source.ComponentReferences, source.Components, "Components",
            isJson ? GumProjectSave.ComponentJsonExtension : GumProjectSave.ComponentExtension, prefixName: false);
        Check(problems, source.ScreenReferences, source.Screens, "Screens",
            isJson ? GumProjectSave.ScreenJsonExtension : GumProjectSave.ScreenExtension, prefixName: false);
        Check(problems, source.StandardElementReferences, source.StandardElements, "Standards",
            isJson ? GumProjectSave.StandardJsonExtension : GumProjectSave.StandardExtension, prefixName: true);

        foreach (BehaviorReference reference in source.BehaviorReferences ?? new List<BehaviorReference>())
        {
            BehaviorSave? loaded = source.Behaviors.FirstOrDefault(behavior => behavior.Name == reference.Name);
            string path = reference.GetRelativeFilePath(isJson);
            string? reason = Describe(loaded?.IsSourceFileMissing, loaded == null, path);
            if (reason != null)
            {
                problems.Add(("Behaviors/" + reference.Name, reason));
            }
        }
        return problems;
    }

    private static void Check(
        List<(string Name, string Reason)> problems,
        IEnumerable<ElementReference> references,
        IEnumerable<ElementSave> loadedElements,
        string subfolder,
        string extension,
        bool prefixName)
    {
        foreach (ElementReference reference in references)
        {
            ElementSave? loaded = loadedElements.FirstOrDefault(element => element.Name == reference.Name);
            string? reason = Describe(loaded?.IsSourceFileMissing, loaded == null, $"{subfolder}/{reference.Name}.{extension}");
            if (reason != null)
            {
                problems.Add((prefixName ? $"{subfolder}/{reference.Name}" : reference.Name, reason));
            }
        }
    }

    private static string? Describe(bool? isStandIn, bool isDropped, string path)
    {
        if (isStandIn == true)
        {
            return $"file not found: {path}";
        }
        return isDropped ? $"{path} could not be read" : null;
    }
}
