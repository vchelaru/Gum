using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Gum.DataTypes;
using Gum.DataTypes.Variables;

namespace Gum.ProjectServices;

/// <inheritdoc/>
public class DiffStandardsService : IDiffStandardsService
{
    /// <summary>
    /// The Standards that ship in the Default template. Matches
    /// <c>ProjectCreator.StandardElementNames</c>; kept here so the diff service does
    /// not depend on project creation.
    /// </summary>
    internal static readonly IReadOnlyList<string> DefaultStandardNames = new[]
    {
        "Circle",
        "ColoredRectangle",
        "Component",
        "Container",
        "NineSlice",
        "Polygon",
        "Rectangle",
        "Sprite",
        "Text"
    };

    /// <inheritdoc/>
    public DiffStandardsResult Diff(string projectFilePath)
    {
        var result = new DiffStandardsResult();

        string projectDirectory = Path.GetDirectoryName(Path.GetFullPath(projectFilePath))
            ?? string.Empty;
        string standardsDirectory = Path.Combine(projectDirectory, ElementReference.StandardSubfolder);

        Dictionary<string, StandardElementSave> defaults = LoadDefaultStandards();
        Dictionary<string, StandardElementSave> projectStandards =
            LoadProjectStandardsFromDisk(standardsDirectory);

        foreach (string name in DefaultStandardNames)
        {
            if (!projectStandards.TryGetValue(name, out StandardElementSave? projectStandard))
            {
                result.MissingFromProject.Add(name);
                continue;
            }

            if (!defaults.TryGetValue(name, out StandardElementSave? defaultStandard))
            {
                // Resource missing — should not happen in practice but stays safe.
                continue;
            }

            DiffStandard(name, defaultStandard, projectStandard, result.Differences);
        }

        foreach (string name in projectStandards.Keys)
        {
            if (!DefaultStandardNames.Contains(name))
            {
                result.ProjectOnlyStandards.Add(name);
            }
        }

        result.ProjectOnlyStandards.Sort();
        return result;
    }

    private static Dictionary<string, StandardElementSave> LoadProjectStandardsFromDisk(
        string standardsDirectory)
    {
        var result = new Dictionary<string, StandardElementSave>();
        if (!Directory.Exists(standardsDirectory))
        {
            return result;
        }

        foreach (string filePath in Directory.EnumerateFiles(standardsDirectory, "*.gutx"))
        {
            string name = Path.GetFileNameWithoutExtension(filePath);
            StandardElementSave? standard = DeserializeStandard(File.ReadAllText(filePath));
            if (standard != null)
            {
                standard.Name = name;
                result[name] = standard;
            }
        }

        return result;
    }

    private static StandardElementSave? DeserializeStandard(string content)
    {
        try
        {
            // Pass AttributeVersion so the serializer routes by content shape, supporting
            // both compact (v2) and legacy element files.
            return GumFileSerializer.DeserializeElementSave<StandardElementSave>(
                content,
                (int)GumProjectSave.GumxVersions.AttributeVersion);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void DiffStandard(
        string standardName,
        StandardElementSave defaultStandard,
        StandardElementSave projectStandard,
        List<StandardVariableDiff> diffs)
    {
        Dictionary<string, StateSave> defaultStates = defaultStandard.AllStates
            .ToDictionary(s => s.Name, s => s);
        Dictionary<string, StateSave> projectStates = projectStandard.AllStates
            .ToDictionary(s => s.Name, s => s);

        foreach (string stateName in defaultStates.Keys.Union(projectStates.Keys).OrderBy(n => n))
        {
            defaultStates.TryGetValue(stateName, out StateSave? defaultState);
            projectStates.TryGetValue(stateName, out StateSave? projectState);

            DiffState(standardName, stateName, defaultState, projectState, diffs);
        }
    }

    private static void DiffState(
        string standardName,
        string stateName,
        StateSave? defaultState,
        StateSave? projectState,
        List<StandardVariableDiff> diffs)
    {
        Dictionary<string, VariableSave> defaultVars = (defaultState?.Variables ?? new List<VariableSave>())
            .GroupBy(v => v.Name)
            .ToDictionary(g => g.Key, g => g.First());
        Dictionary<string, VariableSave> projectVars = (projectState?.Variables ?? new List<VariableSave>())
            .GroupBy(v => v.Name)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (string variableName in defaultVars.Keys.Union(projectVars.Keys).OrderBy(n => n))
        {
            bool inDefault = defaultVars.TryGetValue(variableName, out VariableSave? defaultVar);
            bool inProject = projectVars.TryGetValue(variableName, out VariableSave? projectVar);

            if (inDefault && inProject)
            {
                if (!AreValuesEqual(defaultVar!.Value, projectVar!.Value))
                {
                    diffs.Add(new StandardVariableDiff
                    {
                        StandardName = standardName,
                        StateName = stateName,
                        VariableName = variableName,
                        Kind = StandardVariableDiffKind.Changed,
                        DefaultValue = FormatValue(defaultVar.Value),
                        ProjectValue = FormatValue(projectVar.Value)
                    });
                }
            }
            else if (inProject)
            {
                diffs.Add(new StandardVariableDiff
                {
                    StandardName = standardName,
                    StateName = stateName,
                    VariableName = variableName,
                    Kind = StandardVariableDiffKind.AddedInProject,
                    DefaultValue = "(absent)",
                    ProjectValue = FormatValue(projectVar!.Value)
                });
            }
            else
            {
                diffs.Add(new StandardVariableDiff
                {
                    StandardName = standardName,
                    StateName = stateName,
                    VariableName = variableName,
                    Kind = StandardVariableDiffKind.RemovedFromProject,
                    DefaultValue = FormatValue(defaultVar!.Value),
                    ProjectValue = "(absent)"
                });
            }
        }
    }

    private static bool AreValuesEqual(object? a, object? b)
    {
        if (a == null && b == null)
        {
            return true;
        }
        if (a == null || b == null)
        {
            return false;
        }

        // Use invariant-culture string form for numeric comparison so boxed values whose
        // runtime type differs across serializer paths (float vs double) still compare equal.
        if (a is float || a is double || b is float || b is double)
        {
            return FormatValue(a) == FormatValue(b);
        }

        return a.Equals(b);
    }

    private static string FormatValue(object? value)
    {
        if (value == null)
        {
            return "(unset)";
        }
        if (value is string s)
        {
            return s.Length == 0 ? "(empty)" : s;
        }
        if (value is IFormattable f)
        {
            return f.ToString(null, CultureInfo.InvariantCulture);
        }
        return value.ToString() ?? "(unset)";
    }

    internal static Dictionary<string, StandardElementSave> LoadDefaultStandards()
    {
        var result = new Dictionary<string, StandardElementSave>();
        Assembly assembly = typeof(DiffStandardsService).Assembly;

        foreach (string name in DefaultStandardNames)
        {
            string resourceName = $"Gum.ProjectServices.Templates.Default.Standards.{name}.gutx";
            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                continue;
            }

            using var reader = new StreamReader(stream);
            string content = reader.ReadToEnd();

            StandardElementSave? loaded = DeserializeStandard(content);
            if (loaded != null)
            {
                loaded.Name = name;
                result[name] = loaded;
            }
        }

        return result;
    }
}
