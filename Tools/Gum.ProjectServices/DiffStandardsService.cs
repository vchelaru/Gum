using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.Managers;

namespace Gum.ProjectServices;

/// <inheritdoc/>
public class DiffStandardsService : IDiffStandardsService
{
    private readonly IStandardComparer _comparer;

    public DiffStandardsService() : this(new StandardComparer()) { }

    public DiffStandardsService(IStandardComparer comparer)
    {
        _comparer = comparer;
    }

    /// <inheritdoc/>
    public DiffStandardsResult Diff(string projectFilePath)
    {
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
    /// </summary>
    private static GumProjectSave BuildReferenceProject()
    {
        GumProjectSave reference = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(reference);
        return reference;
    }

    /// <summary>
    /// Diffs two loaded projects' StandardElements via <see cref="IStandardComparer"/>.
    /// Internal so tests can drive the comparison without writing a project to disk.
    /// </summary>
    internal DiffStandardsResult DiffProjects(GumProjectSave project, GumProjectSave reference)
    {
        DiffStandardsResult result = new DiffStandardsResult();

        Dictionary<string, StandardElementSave> referenceStandards = reference.StandardElements
            .ToDictionary(s => s.Name, s => s);
        Dictionary<string, StandardElementSave> projectStandards = project.StandardElements
            .ToDictionary(s => s.Name, s => s);

        foreach (string name in referenceStandards.Keys.OrderBy(n => n, StringComparer.Ordinal))
        {
            if (!projectStandards.TryGetValue(name, out StandardElementSave? projectStandard))
            {
                result.MissingFromProject.Add(name);
                continue;
            }

            // source=project, destination=reference — matches the tool's call convention.
            StandardComparisonResult comparison =
                _comparer.Compare(projectStandard, referenceStandards[name]);

            if (comparison.CategoryNamesDiffer)
            {
                foreach (string cat in comparison.CategoryNamesOnlyInSource)
                {
                    result.Differences.Add(new StandardVariableDiff
                    {
                        StandardName = name,
                        StateName = "(category)",
                        VariableName = cat,
                        Kind = StandardVariableDiffKind.AddedInProject,
                        ProjectValue = "(present)",
                        DefaultValue = "(absent)"
                    });
                }
                foreach (string cat in comparison.CategoryNamesOnlyInDestination)
                {
                    result.Differences.Add(new StandardVariableDiff
                    {
                        StandardName = name,
                        StateName = "(category)",
                        VariableName = cat,
                        Kind = StandardVariableDiffKind.RemovedFromProject,
                        ProjectValue = "(absent)",
                        DefaultValue = "(present)"
                    });
                }
            }

            foreach (StandardVariableDiff varDiff in comparison.VariableDifferences)
            {
                varDiff.StandardName = name;
                varDiff.StateName = "Default";
                result.Differences.Add(varDiff);
            }
        }

        foreach (string name in projectStandards.Keys)
        {
            if (!referenceStandards.ContainsKey(name))
            {
                result.ProjectOnlyStandards.Add(name);
            }
        }

        result.ProjectOnlyStandards.Sort(StringComparer.Ordinal);
        return result;
    }
}
