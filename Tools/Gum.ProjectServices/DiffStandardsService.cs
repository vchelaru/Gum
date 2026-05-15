using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;

namespace Gum.ProjectServices;

/// <inheritdoc/>
public class DiffStandardsService : IDiffStandardsService
{
    /// <inheritdoc/>
    public DiffStandardsResult Diff(string projectFilePath)
    {
        // ProjectLoader.Load already calls StandardElementsManager.Self.Initialize() and
        // RegisterExtendedDefaultStates() before deserializing the project, so by the time
        // we build the reference below those defaults are available.
        IProjectLoader loader = new ProjectLoader();
        ProjectLoadResult loadResult = loader.Load(projectFilePath);
        if (!loadResult.Success)
        {
            throw new InvalidOperationException(
                loadResult.ErrorMessage ?? $"Failed to load project: {projectFilePath}");
        }

        GumProjectSave reference = BuildReferenceProject();
        return DiffProjects(loadResult.Project!, reference);
    }

    /// <summary>
    /// Builds a reference project the same way the Gum tool's File → New does — by
    /// populating from <see cref="StandardElementsManager.Self"/>'s programmatic defaults.
    /// This is the "universal base" that every runtime uses, and is what the tool's
    /// import dialog implicitly compares against. The on-disk
    /// <c>Templates/Default/Standards/*.gutx</c> files are a separate snapshot that may
    /// drift from these programmatic defaults; that drift is its own concern, not what
    /// this command enforces.
    /// </summary>
    private static GumProjectSave BuildReferenceProject()
    {
        GumProjectSave reference = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(reference);
        return reference;
    }

    /// <summary>
    /// Diffs two loaded projects' StandardElements. Internal so tests can drive the
    /// comparison without writing a project to disk.
    /// </summary>
    internal static DiffStandardsResult DiffProjects(
        GumProjectSave project, GumProjectSave reference)
    {
        var result = new DiffStandardsResult();

        Dictionary<string, StandardElementSave> referenceStandards = reference.StandardElements
            .ToDictionary(s => s.Name, s => s);
        Dictionary<string, StandardElementSave> projectStandards = project.StandardElements
            .ToDictionary(s => s.Name, s => s);

        foreach (string name in referenceStandards.Keys.OrderBy(n => n))
        {
            if (!projectStandards.TryGetValue(name, out StandardElementSave? projectStandard))
            {
                result.MissingFromProject.Add(name);
                continue;
            }

            DiffStandard(name, referenceStandards[name], projectStandard, result.Differences);
        }

        foreach (string name in projectStandards.Keys)
        {
            if (!referenceStandards.ContainsKey(name))
            {
                result.ProjectOnlyStandards.Add(name);
            }
        }

        result.ProjectOnlyStandards.Sort();
        return result;
    }

    private static void DiffStandard(
        string standardName,
        StandardElementSave referenceStandard,
        StandardElementSave projectStandard,
        List<StandardVariableDiff> diffs)
    {
        Dictionary<string, StateSave> referenceStates = referenceStandard.AllStates
            .Where(s => !string.IsNullOrEmpty(s.Name))
            .GroupBy(s => s.Name)
            .ToDictionary(g => g.Key, g => g.First());
        Dictionary<string, StateSave> projectStates = projectStandard.AllStates
            .Where(s => !string.IsNullOrEmpty(s.Name))
            .GroupBy(s => s.Name)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (string stateName in referenceStates.Keys.Union(projectStates.Keys).OrderBy(n => n))
        {
            referenceStates.TryGetValue(stateName, out StateSave? referenceState);
            projectStates.TryGetValue(stateName, out StateSave? projectState);

            DiffState(standardName, stateName, referenceState, projectState, diffs);
        }
    }

    private static void DiffState(
        string standardName,
        string stateName,
        StateSave? referenceState,
        StateSave? projectState,
        List<StandardVariableDiff> diffs)
    {
        Dictionary<string, VariableSave> referenceVars = (referenceState?.Variables ?? new List<VariableSave>())
            .GroupBy(v => v.Name)
            .ToDictionary(g => g.Key, g => g.First());
        Dictionary<string, VariableSave> projectVars = (projectState?.Variables ?? new List<VariableSave>())
            .GroupBy(v => v.Name)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (string variableName in referenceVars.Keys.Union(projectVars.Keys).OrderBy(n => n))
        {
            bool inReference = referenceVars.TryGetValue(variableName, out VariableSave? referenceVar);
            bool inProject = projectVars.TryGetValue(variableName, out VariableSave? projectVar);

            if (inReference && inProject)
            {
                if (!AreValuesEqual(referenceVar!.Value, projectVar!.Value))
                {
                    diffs.Add(new StandardVariableDiff
                    {
                        StandardName = standardName,
                        StateName = stateName,
                        VariableName = variableName,
                        Kind = StandardVariableDiffKind.Changed,
                        DefaultValue = FormatValue(referenceVar.Value),
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
                    DefaultValue = FormatValue(referenceVar!.Value),
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
}
