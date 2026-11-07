using Gum.DataTypes;

namespace Gum.Managers;

/// <summary>
/// Answers what the loaded project's restrictions (<see cref="GumProjectSave.SupportedStandardTypes"/>,
/// <see cref="GumProjectSave.VariableRules"/>, <see cref="GumProjectSave.DisableComponents"/>,
/// <see cref="GumProjectSave.DisableBehaviors"/>) take away from the tool. Every query is false
/// when the project has no restrictions. Hierarchy is the Parent variable: it is unsupported when the rules hide it.
/// </summary>
public interface IProjectRestrictions
{
    /// <summary>
    /// True when the project's variable rules hide the variable for the standard type, using the same
    /// exemptions as the Variables tab. A null type matches only unqualified rules.
    /// </summary>
    bool IsVariableHidden(string? standardTypeName, string variableRootName);

    /// <summary>
    /// True when the rules hide the variable for the instance's root standard type.
    /// </summary>
    bool IsVariableHidden(InstanceSave instance, string variableRootName);

    /// <summary>
    /// True when the rules hide the variable for the element's root standard type.
    /// </summary>
    bool IsVariableHidden(ElementSave element, string variableRootName);

    /// <summary>
    /// True when the project does not allow adding an instance of the element: an unlisted standard
    /// type, or any component while components are disabled.
    /// </summary>
    bool IsInstanceTypeBlocked(ElementSave elementToAdd);

    /// <summary>
    /// True when the project does not allow a standard type name (for example a base type the user typed).
    /// </summary>
    bool IsStandardTypeBlocked(string standardTypeName);

    /// <summary>
    /// True when the project disables components.
    /// </summary>
    bool AreComponentsDisabled { get; }

    /// <summary>
    /// True when the project disables behaviors.
    /// </summary>
    bool AreBehaviorsDisabled { get; }

    /// <summary>
    /// True when instances of the element cannot have a parent because the rules hide Parent for its type.
    /// </summary>
    bool IsHierarchyBlockedFor(ElementSave element);

    /// <summary>
    /// True when Parent is hidden for every standard type the project allows, so nothing can have a child.
    /// </summary>
    bool IsHierarchyBlocked { get; }
}
