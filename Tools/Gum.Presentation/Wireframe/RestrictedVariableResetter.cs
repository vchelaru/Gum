using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using RenderingLibrary.Graphics;
using System.Collections.Generic;

namespace Gum.Wireframe;

/// <inheritdoc cref="IRestrictedVariableResetter"/>
public class RestrictedVariableResetter : IRestrictedVariableResetter
{
    // Resetting Parent would detach the object from its container rather than draw a default.
    private const string ParentVariableName = "Parent";

    private readonly IProjectRestrictions _projectRestrictions;

    public RestrictedVariableResetter(IProjectRestrictions projectRestrictions)
    {
        _projectRestrictions = projectRestrictions;
    }

    /// <inheritdoc/>
    public void Reset(GraphicalUiElement? root)
    {
        if (root == null || ObjectFinder.Self.GumProjectSave == null || !_projectRestrictions.HasVariableRules)
        {
            return;
        }

        Dictionary<string, StateSave?> defaultsByStandardType = new Dictionary<string, StateSave?>();
        HashSet<GraphicalUiElement> visited = new HashSet<GraphicalUiElement>();
        Visit(root, defaultsByStandardType, visited);
    }

    private void Visit(GraphicalUiElement gue, Dictionary<string, StateSave?> defaultsByStandardType, HashSet<GraphicalUiElement> visited)
    {
        if (!visited.Add(gue))
        {
            return;
        }

        StateSave? defaults = GetRestrictedDefaults(gue, defaultsByStandardType);
        if (defaults != null)
        {
            gue.ApplyState(defaults);
        }

        if (gue.Children != null)
        {
            foreach (IRenderableIpso child in gue.Children)
            {
                if (child is GraphicalUiElement childGue)
                {
                    Visit(childGue, defaultsByStandardType, visited);
                }
            }
        }
        else
        {
            foreach (GraphicalUiElement contained in gue.ContainedElements)
            {
                if (contained.Parent == null)
                {
                    Visit(contained, defaultsByStandardType, visited);
                }
            }
        }
    }

    private StateSave? GetRestrictedDefaults(GraphicalUiElement gue, Dictionary<string, StateSave?> defaultsByStandardType)
    {
        StandardElementSave? standard = gue.Tag switch
        {
            InstanceSave instance => ObjectFinder.Self.GetRootStandardElementSave(instance),
            ElementSave element => ObjectFinder.Self.GetRootStandardElementSave(element),
            _ => null
        };
        if (standard == null)
        {
            return null;
        }

        if (!defaultsByStandardType.TryGetValue(standard.Name, out StateSave? defaults))
        {
            defaults = BuildRestrictedDefaults(standard);
            defaultsByStandardType[standard.Name] = defaults;
        }
        return defaults;
    }

    private StateSave? BuildRestrictedDefaults(StandardElementSave standard)
    {
        StateSave? standardDefaults = standard.DefaultState;
        if (standardDefaults == null)
        {
            return null;
        }

        StateSave restricted = new StateSave { Name = "RestrictedDefaults" };
        foreach (VariableSave variable in standardDefaults.Variables)
        {
            string rootName = variable.GetRootName();
            if (variable.Value != null && string.IsNullOrEmpty(variable.SourceObject) && rootName != ParentVariableName &&
                _projectRestrictions.IsVariableHidden(standard.Name, rootName))
            {
                restricted.Variables.Add(new VariableSave
                {
                    Name = rootName,
                    Type = variable.Type,
                    Value = variable.Value,
                    SetsValue = true
                });
            }
        }
        return restricted.Variables.Count > 0 ? restricted : null;
    }
}
