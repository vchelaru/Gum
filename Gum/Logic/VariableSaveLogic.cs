using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Services;
using Gum.ToolStates;
using Gum.Wireframe;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Logic;

public class VariableSaveLogic
{
    private static readonly ISelectedState _selectedState = Locator.GetRequiredService<ISelectedState>();

    
    public bool GetIfVariableIsActive(VariableSave defaultVariable, ElementSave container, InstanceSave? currentInstance)
    {
        bool shouldInclude = GetIfShouldIncludeAccordingToDefaultState(defaultVariable, container, currentInstance);

        if (shouldInclude)
        {
            shouldInclude = GetShouldIncludeBasedOnAttachments(defaultVariable, container, currentInstance);
        }

        if(shouldInclude && defaultVariable.Name == "State" && currentInstance != null)
        {
            // include this if either:
            // 1. It has a non-null value
            // 2. It has a null value but the instance's element has uncategorized states:

            shouldInclude = defaultVariable.Value != null;

            if(!shouldInclude)
            {
                var instanceElement = ObjectFinder.Self.GetElementSave(currentInstance);
                // check states (uncateogrized) to see if anything besides default exists:
                if (instanceElement != null && instanceElement.States.Count > 1)
                {
                    shouldInclude = true;
                }
            }
        }

        if (shouldInclude)
        {
            StandardElementSave rootElementSave = null;

            if (currentInstance != null)
            {
                rootElementSave = ObjectFinder.Self.GetRootStandardElementSave(currentInstance);
            }
            else if ((container is ScreenSave) == false)
            {
                rootElementSave = ObjectFinder.Self.GetRootStandardElementSave(container);
            }

            shouldInclude = defaultVariable.IsCustomVariable || GetShouldIncludeBasedOnBaseType(defaultVariable, container, currentInstance, rootElementSave);
        }

        if (shouldInclude)
        {
            if (currentInstance != null && defaultVariable.ExcludeFromInstances)
            {
                shouldInclude = false;
            }
        }

        if (shouldInclude)
        {
            RecursiveVariableFinder rvf;
            if (currentInstance != null)
            {
                //rvf = new RecursiveVariableFinder(currentInstance, container);
                // this should respect the current state:

                var elementWithState = new ElementWithState(container);
                elementWithState.InstanceName = currentInstance?.Name;
                elementWithState.StateName = _selectedState.SelectedStateSave?.Name;
                var stack = new List<ElementWithState>() { elementWithState };

                // Pass in the current instance so that everything is relative to that and the checks don't have to prefix anything
                rvf = new RecursiveVariableFinder(currentInstance, stack);
            }
            else
            {
                if(_selectedState.SelectedStateSave != null)
                {
                    rvf = new RecursiveVariableFinder(_selectedState.SelectedStateSave);
                }
                else
                {
                    rvf = new RecursiveVariableFinder(container.DefaultState);
                }
            }

            shouldInclude = !PluginManager.Self.ShouldExclude(defaultVariable, rvf);
        }

        return shouldInclude;
    }

    private bool GetIfShouldIncludeAccordingToDefaultState(VariableSave defaultVariable, ElementSave container, InstanceSave currentInstance)
    {
        bool canOnlyBeSetInDefaultState = defaultVariable.CanOnlyBeSetInDefaultState;
        if (currentInstance != null)
        {
            var root = ObjectFinder.Self.GetRootStandardElementSave(currentInstance);
            if (root != null && root.GetVariableFromThisOrBase(defaultVariable.Name, true) != null)
            {
                var foundVariable = root.GetVariableFromThisOrBase(defaultVariable.Name, true);
                canOnlyBeSetInDefaultState = foundVariable.CanOnlyBeSetInDefaultState;
            }
        }
        else if (container != null)
        {
            var root = ObjectFinder.Self.GetRootStandardElementSave(container);
            if (root != null && root.GetVariableFromThisOrBase(defaultVariable.Name, true) != null)
            {
                canOnlyBeSetInDefaultState = root.GetVariableFromThisOrBase(defaultVariable.Name, true).CanOnlyBeSetInDefaultState;
            }
        }
        bool shouldInclude = true;

        bool isDefault = _selectedState.SelectedStateSave == _selectedState.SelectedElement.DefaultState;

        if (currentInstance != null)
        {
            isDefault = currentInstance.DefinedByBase == false;
        }

        if (!isDefault && canOnlyBeSetInDefaultState)
        {
            shouldInclude = false;
        }
        return shouldInclude;
    }

    private bool GetShouldIncludeBasedOnAttachments(VariableSave variableSave, ElementSave container, InstanceSave currentInstance)
    {
        bool toReturn = true;
        if (variableSave.Name == "Guide")
        {
            if (currentInstance != null && _selectedState.SelectedScreen == null)
            {
                toReturn = false;
            }
        }

        return toReturn;
    }

    public bool GetShouldIncludeBasedOnBaseType(VariableListSave variableList, ElementSave container, StandardElementSave rootElementSave)
    {
        bool shouldInclude = false;

        if (string.IsNullOrEmpty(variableList.SourceObject))
        {
            if (container is ScreenSave)
            {
                // If it's a Screen, then the answer is "yes" because
                // Screens don't have a base type that they can switch,
                // so any variable that's part of the Screen is always part
                // of the Screen.
                shouldInclude = true;
            }
            else
            {
                if (container is ComponentSave)
                {
                    // See if it's defined in the standards list
                    var foundInstance = StandardElementsManager.Self.GetDefaultStateFor("Component").VariableLists.FirstOrDefault(
                        item => item.Name == variableList.Name);

                    shouldInclude = foundInstance != null;
                }
                // If the defaultVariable's
                // source object is null then
                // that means that the variable
                // is being set on "this".  However,
                // variables that are set on "this" may
                // not actually be valid for the type, but
                // they may still exist because the object type
                // was switched.  Therefore, we want to make sure
                // that the variable is valid given the type of object
                // that "this" currently is by checking the default state
                // on the rootElementSave
                if (!shouldInclude && rootElementSave != null)
                {
                    shouldInclude = rootElementSave.DefaultState.GetVariableListRecursive(variableList.Name) != null;
                }
            }
        }

        else
        {

            shouldInclude = _selectedState.SelectedInstance != null
                // VariableLists cannot be exposed (currently)
                //|| !string.IsNullOrEmpty(variableList.ExposedAsName);
                ;
        }
        return shouldInclude;
    }

    private bool GetShouldIncludeBasedOnBaseType(VariableSave defaultVariable, ElementSave container, InstanceSave instanceSave, StandardElementSave rootElementSave)
    {
        bool shouldInclude = false;

        if (string.IsNullOrEmpty(defaultVariable.SourceObject))
        {
            if (container is ScreenSave)
            {
                // If it's a Screen, then the answer is "yes" because
                // Screens don't have a base type that they can switch,
                // so any variable that's part of the Screen is always part
                // of the Screen.
                // do nothing to shouldInclude

                shouldInclude = true;
            }
            else
            {
                if (container is ComponentSave)
                {
                    // See if it's defined in the standards list
                    var foundInstance = StandardElementsManager.Self.GetDefaultStateFor("Component").Variables.FirstOrDefault(
                        item => item.Name == defaultVariable.Name);

                    shouldInclude = foundInstance != null;
                }
                // If the defaultVariable's
                // source object is null then
                // that means that the variable
                // is being set on "this".  However,
                // variables that are set on "this" may
                // not actually be valid for the type, but
                // they may still exist because the object type
                // was switched.  Therefore, we want to make sure
                // that the variable is valid given the type of object
                // that "this" currently is by checking the default state
                // on the rootElementSave
                if (!shouldInclude && rootElementSave != null)
                {
                    shouldInclude = rootElementSave.DefaultState.GetVariableSave(defaultVariable.Name) != null;
                }

                string nameWithoutState = null;
                if (!shouldInclude && defaultVariable.Name.EndsWith("State") && instanceSave != null)
                {
                    nameWithoutState = defaultVariable.Name.Substring(0, defaultVariable.Name.Length - "State".Length);

                    var instanceElement = ObjectFinder.Self.GetElementSave(instanceSave.BaseType);


                    // See if this is a category:
                    if (!string.IsNullOrEmpty(nameWithoutState) &&
                        instanceElement != null && instanceElement.Categories.Any(item => item.Name == nameWithoutState))
                    {
                        shouldInclude = true;
                    }
                }

                if (!shouldInclude && instanceSave == null && defaultVariable.IsState(container))
                {
                    return true;
                }
            }
        }

        else
        {

            shouldInclude = _selectedState.SelectedInstance != null || !string.IsNullOrEmpty(defaultVariable.ExposedAsName);
        }
        return shouldInclude;
    }
}
