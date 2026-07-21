using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Logic;

/// <inheritdoc cref="IGumProjectRepairLogic"/>
public class GumProjectRepairLogic : IGumProjectRepairLogic
{
    /// <inheritdoc/>
    public bool RemoveSpacesInVariables(GumProjectSave gumProjectSave)
    {
        bool didChange = false;
        foreach (ElementSave element in gumProjectSave.AllElements)
        {
            foreach (StateSave state in element.AllStates)
            {
                foreach (VariableSave variable in state.Variables)
                {
                    Replace(variable, "Base Type");
                    Replace(variable, "Children Layout");
                    Replace(variable, "Clips Children");
                    Replace(variable, "Contained Type");
                    Replace(variable, "Font Scale");
                    Replace(variable, "Height Units");
                    Replace(variable, "Texture Address");
                    Replace(variable, "Texture Height");
                    Replace(variable, "Texture Height Scale");
                    Replace(variable, "Texture Left");
                    Replace(variable, "Texture Top");
                    Replace(variable, "Texture Width");
                    Replace(variable, "Texture Width Scale");
                    Replace(variable, "Width Units");
                    Replace(variable, "Wraps Children");
                    Replace(variable, "X Origin");
                    Replace(variable, "X Units");
                    Replace(variable, "Y Origin");
                    Replace(variable, "Y Units");

                    void Replace(VariableSave variableSave, string oldName)
                    {
                        if (variable.Name.EndsWith(oldName))
                        {
                            string newName = variable.Name.Substring(0, variable.Name.Length - oldName.Length) +
                                oldName.Replace(" ", "");
                            variable.Name = newName;
                            didChange = true;
                        }
                    }
                }
            }
        }
        return didChange;
    }

    /// <inheritdoc/>
    public bool FixRecursiveAssignments(GumProjectSave gumProjectSave)
    {
        bool toReturn = false;
        // Instances can't be of type screen, so don't check this (unless someone messes with the XML but that's on them)
        foreach (ComponentSave component in gumProjectSave.Components)
        {
            if (FixRecursiveAssignments(component))
            {
                toReturn = true;
            }
        }

        return toReturn;
    }

    private bool FixRecursiveAssignments(ElementSave element)
    {
        bool didModify = false;
        // see if the child is either of this type, or a base type
        foreach (InstanceSave instance in element.Instances)
        {
            bool isRecursive = ObjectFinder.Self.IsInstanceRecursivelyReferencingElement(instance, element);

            if (isRecursive)
            {
                instance.BaseType = "Container";
                didModify = true;
            }
        }

        return didModify;
    }

    /// <inheritdoc/>
    public bool FixSlashesInNames(GumProjectSave gumProjectSave)
    {
        bool didAnythingChange = false;

        foreach (ElementReference reference in gumProjectSave.ScreenReferences)
        {
            if (reference.Name?.Contains("\\") == true)
            {
                reference.Name = reference.Name.Replace("\\", "/");
                didAnythingChange = true;
            }
        }

        foreach (ElementReference reference in gumProjectSave.ComponentReferences)
        {
            if (reference?.Name.Contains("\\") == true)
            {
                reference.Name = reference.Name.Replace("\\", "/");
                didAnythingChange = true;
            }
        }

        foreach (BehaviorReference reference in gumProjectSave.BehaviorReferences)
        {
            if (reference.Name?.Contains("\\") == true)
            {
                reference.Name = reference.Name.Replace("\\", "/");
                didAnythingChange = true;
            }
        }


        foreach (ScreenSave screen in gumProjectSave.Screens)
        {
            if (screen.Name?.Contains("\\") == true)
            {
                screen.Name = screen.Name.Replace("\\", "/");
                didAnythingChange = true;
            }
            foreach (InstanceSave instance in screen.Instances)
            {
                if (instance.BaseType?.Contains("\\") == true)
                {
                    instance.BaseType = instance.BaseType.Replace("\\", "/");
                    didAnythingChange = true;
                }
            }

            foreach (ElementBehaviorReference behavior in screen.Behaviors)
            {
                if (behavior.BehaviorName?.Contains("\\") == true)
                {
                    behavior.BehaviorName = behavior.BehaviorName.Replace("\\", "/");
                    didAnythingChange = true;
                }
            }
        }

        foreach (ComponentSave component in gumProjectSave.Components)
        {
            if (component.Name?.Contains("\\") == true)
            {
                component.Name = component.Name.Replace("\\", "/");
                didAnythingChange = true;
            }

            foreach (InstanceSave instance in component.Instances)
            {
                if (instance.BaseType?.Contains("\\") == true)
                {
                    instance.BaseType = instance.BaseType.Replace("\\", "/");
                    didAnythingChange = true;
                }
            }

            foreach (ElementBehaviorReference behavior in component.Behaviors)
            {
                if (behavior.BehaviorName?.Contains("\\") == true)
                {
                    behavior.BehaviorName = behavior.BehaviorName.Replace("\\", "/");
                    didAnythingChange = true;
                }
            }
        }

        foreach (BehaviorSave behavior in gumProjectSave.Behaviors)
        {
            if (behavior.Name?.Contains("\\") == true)
            {
                behavior.Name = behavior.Name.Replace("\\", "/");
                didAnythingChange = true;
            }

            foreach (InstanceSave instance in behavior.RequiredInstances)
            {
                if (instance.BaseType?.Contains("\\") == true)
                {
                    instance.BaseType = instance.BaseType.Replace("\\", "/");
                    didAnythingChange = true;
                }
            }
        }

        return didAnythingChange;
    }

    /// <inheritdoc/>
    public bool RemoveDuplicateVariables(GumProjectSave gumProjectSave)
    {
        bool didChange = false;
        foreach (ScreenSave screen in gumProjectSave.Screens)
        {
            didChange = RemoveDuplicateVariables(screen) || didChange;
        }
        foreach (ComponentSave component in gumProjectSave.Components)
        {
            didChange = RemoveDuplicateVariables(component) || didChange;
        }
        foreach (StandardElementSave standard in gumProjectSave.StandardElements)
        {
            didChange = RemoveDuplicateVariables(standard) || didChange;
        }
        return didChange;
    }

    private bool RemoveDuplicateVariables(ElementSave element)
    {
        bool didChange = false;
        foreach (StateSave state in element.AllStates)
        {
            didChange = RemoveDuplicateVariables(state) || didChange;
        }
        return didChange;
    }

    private bool RemoveDuplicateVariables(StateSave state)
    {
        HashSet<string> variableNames = state.Variables.Select(item => item.Name).ToHashSet();

        bool didChange = false;
        if (variableNames.Count != state.Variables.Count)
        {
            List<VariableSave> newVariables = new List<VariableSave>();
            foreach (string variableName in variableNames)
            {
                VariableSave matchingVariable = state.Variables.FirstOrDefault(item => item.Name == variableName);
                newVariables.Add(matchingVariable);
            }

            state.Variables.Clear();
            state.Variables.AddRange(newVariables);
            didChange = true;
        }
        return didChange;
    }
}
