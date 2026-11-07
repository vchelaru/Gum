using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.Plugins.InternalPlugins.VariableGrid;

namespace Gum.Managers;

/// <inheritdoc cref="IProjectRestrictions"/>
public class ProjectRestrictions : IProjectRestrictions
{
    // Variables the tool itself needs; the project's variable rules never hide them.
    private static readonly HashSet<string> RuleExemptVariableNames = new HashSet<string>
    {
        "Name", "BaseType", "Locked", "DefaultChildContainer", "State",
    };

    private readonly ProjectVariableRuleMatcher _matcher = new ProjectVariableRuleMatcher();

    private static GumProjectSave? Project => ObjectFinder.Self.GumProjectSave;

    /// <inheritdoc/>
    public bool AreComponentsDisabled => Project?.DisableComponents == true;

    /// <inheritdoc/>
    public bool AreBehaviorsDisabled => Project?.DisableBehaviors == true;

    /// <inheritdoc/>
    public bool IsVariableHidden(string? standardTypeName, string variableRootName)
    {
        GumProjectSave? project = Project;
        if (project == null || (!project.HideVariablesByDefault && project.VariableRules.Count == 0))
        {
            return false;
        }
        if (RuleExemptVariableNames.Contains(variableRootName))
        {
            return false;
        }
        return _matcher.IsHidden(project, standardTypeName, variableRootName);
    }

    /// <inheritdoc/>
    public bool IsVariableHidden(InstanceSave instance, string variableRootName) =>
        IsVariableHidden(ObjectFinder.Self.GetRootStandardElementSave(instance)?.Name, variableRootName);

    /// <inheritdoc/>
    public bool IsVariableHidden(ElementSave element, string variableRootName) =>
        IsVariableHidden(ObjectFinder.Self.GetRootStandardElementSave(element)?.Name, variableRootName);

    /// <inheritdoc/>
    public bool IsEveryVariableHidden(ElementSave element, params string[] variableRootNames) =>
        variableRootNames.All(name => IsVariableHidden(element, name));

    /// <inheritdoc/>
    public bool IsInstanceTypeBlocked(ElementSave elementToAdd)
    {
        if (elementToAdd is StandardElementSave)
        {
            return IsStandardTypeBlocked(elementToAdd.Name);
        }
        if (elementToAdd is ComponentSave)
        {
            return AreComponentsDisabled;
        }
        return false;
    }

    /// <inheritdoc/>
    public bool IsStandardTypeBlocked(string standardTypeName)
    {
        List<string>? supported = Project?.SupportedStandardTypes;
        return supported != null && supported.Count > 0 && !supported.Contains(standardTypeName);
    }

    /// <inheritdoc/>
    public bool IsHierarchyBlockedFor(ElementSave element) => IsVariableHidden(element, "Parent");

    /// <inheritdoc/>
    public bool IsHierarchyBlocked
    {
        get
        {
            GumProjectSave? project = Project;
            if (project == null)
            {
                return false;
            }
            List<string> typeNames = project.SupportedStandardTypes.Count > 0
                ? project.SupportedStandardTypes
                : project.StandardElements.Select(item => item.Name).ToList();
            return typeNames.Count > 0 && typeNames.All(name => IsVariableHidden(name, "Parent"));
        }
    }
}
