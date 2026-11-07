using System;
using System.Collections.Generic;
using Gum.DataTypes;
using ToolsUtilities;

namespace Gum.Managers;

/// <summary>
/// Answers what the loaded project's engine profile (<see cref="GumProjectSave.EngineProfile"/>) takes away
/// from the tool. This is the only reader of the profile. Every query is false when the project
/// references none. Hierarchy is the Parent variable: it is unsupported when the rules hide it.
/// </summary>
public interface IProjectRestrictions : IVariableRestrictions
{
    /// <summary>
    /// True when the rules hide the variable for the instance's root standard type.
    /// </summary>
    bool IsVariableHidden(InstanceSave instance, string variableRootName);

    /// <summary>
    /// True when the rules hide the variable for the element's root standard type.
    /// </summary>
    bool IsVariableHidden(ElementSave element, string variableRootName);

    /// <summary>
    /// True when the rules hide every one of the variables for the element's root standard type.
    /// </summary>
    bool IsEveryVariableHidden(ElementSave element, params string[] variableRootNames);

    /// <summary>
    /// True when the project does not allow adding an instance of the element: an unlisted standard
    /// type, or any component while components are disabled.
    /// </summary>
    bool IsInstanceTypeBlocked(ElementSave elementToAdd);

    /// <summary>
    /// True when instances of the element cannot have a parent because the rules hide Parent for its type.
    /// </summary>
    bool IsHierarchyBlockedFor(ElementSave element);

    /// <summary>
    /// True when Parent is hidden for every standard type the project allows, so nothing can have a child.
    /// </summary>
    bool IsHierarchyBlocked { get; }

    /// <summary>
    /// The standard types the profile supports; empty means every type.
    /// </summary>
    IReadOnlyList<string> SupportedStandardTypes { get; }

    /// <summary>
    /// True when the profile hides variables by default or has any variable rule.
    /// </summary>
    bool HasVariableRules { get; }

    /// <summary>
    /// The resolved engine profile file of the loaded project, or null when it has none.
    /// </summary>
    FilePath? ProfileFilePath { get; }

    /// <summary>
    /// Re-reads the engine profile from disk and raises <see cref="Changed"/>.
    /// </summary>
    void Reload();

    /// <summary>
    /// Raised after <see cref="Reload"/>, so surfaces built from the restrictions can refresh.
    /// </summary>
    event EventHandler? Changed;
}
