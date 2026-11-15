using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;

namespace Gum.Logic;

/// <inheritdoc/>
public class ElementDuplicateRestrictions : IElementDuplicateRestrictions
{
    private readonly IProjectRestrictions _projectRestrictions;

    public ElementDuplicateRestrictions(IProjectRestrictions projectRestrictions)
    {
        _projectRestrictions = projectRestrictions;
    }

    /// <inheritdoc/>
    public string? GetRefusalMessage(ElementSave element)
    {
        foreach (InstanceSave instance in element.Instances)
        {
            if (ObjectFinder.Self.GetElementSave(instance.BaseType) is { } instanceType &&
                _projectRestrictions.IsInstanceTypeBlocked(instanceType))
            {
                return $"The project does not support {instance.BaseType} objects, so {element.Name} cannot be duplicated";
            }
        }
        return null;
    }

    /// <inheritdoc/>
    public void RemoveUnsupportedParents(ElementSave copy)
    {
        foreach (InstanceSave instance in copy.Instances)
        {
            if (!_projectRestrictions.IsVariableHidden(instance, "Parent"))
            {
                continue;
            }
            foreach (StateSave state in copy.AllStates)
            {
                state.Variables.RemoveAll(variable =>
                    variable.SourceObject == instance.Name && variable.GetRootName() == "Parent");
            }
        }
    }
}
