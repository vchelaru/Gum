using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.ToolStates;
using Gum.Wireframe;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Logic;

/// <inheritdoc/>
public class VariableSaveLogic : IVariableSaveLogic, IVariableDefinedByTypeLogic
{
    private readonly ISelectedState _selectedState;
    private readonly IPluginManager _pluginManager;
    private readonly IVariableTypeConverterProvider _variableTypeConverterProvider;

    public VariableSaveLogic(ISelectedState selectedState, IPluginManager pluginManager,
        IVariableTypeConverterProvider variableTypeConverterProvider)
    {
        _selectedState = selectedState;
        _pluginManager = pluginManager;
        _variableTypeConverterProvider = variableTypeConverterProvider;
    }

    /// <summary>
    /// Returns the VariableSave from the argument element's default state, walking the base-element
    /// chain. Headless replacement for the tool-only <c>ElementSaveExtensionMethodsGumTool
    /// .GetVariableFromThisOrBase</c>'s <c>forceDefault: true</c> overload (the only overload this
    /// class uses) - that overload never touches <see cref="ISelectedState"/>/<c>Locator</c> either,
    /// it only does so on the <c>forceDefault: false</c> path this class doesn't call.
    /// </summary>
    private static VariableSave? GetVariableFromThisOrBaseDefaultState(ElementSave element, string variable) =>
        element.GetDefaultStateOrThrow().GetVariableRecursive(variable);

    /// <inheritdoc/>
    public bool GetIfVariableIsActive(VariableSave defaultVariable, ElementSave? container, InstanceSave? currentInstance)
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
            StandardElementSave? rootElementSave = null;

            if (currentInstance != null)
            {
                rootElementSave = ObjectFinder.Self.GetRootStandardElementSave(currentInstance);
            }
            else if (container != null && (container is ScreenSave) == false)
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

                // An instance shown in the grid always belongs to an element.
                var elementWithState = new ElementWithState(container!);
                elementWithState.InstanceName = currentInstance.Name;
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
                    // Without an instance, the container is the element being shown.
                    rvf = new RecursiveVariableFinder(container!.GetDefaultStateOrThrow());
                }
            }

            shouldInclude = !_pluginManager.ShouldExclude(defaultVariable, rvf);
        }

        return shouldInclude;
    }

    private bool GetIfShouldIncludeAccordingToDefaultState(VariableSave defaultVariable, ElementSave? container, InstanceSave? currentInstance)
    {
        bool canOnlyBeSetInDefaultState = defaultVariable.CanOnlyBeSetInDefaultState;
        if (currentInstance != null)
        {
            var root = ObjectFinder.Self.GetRootStandardElementSave(currentInstance);
            var foundVariable = root != null ? GetVariableFromThisOrBaseDefaultState(root, defaultVariable.Name) : null;
            if (foundVariable != null)
            {
                canOnlyBeSetInDefaultState = foundVariable.CanOnlyBeSetInDefaultState;
            }
        }
        else if (container != null)
        {
            var root = ObjectFinder.Self.GetRootStandardElementSave(container);
            var foundVariable = root != null ? GetVariableFromThisOrBaseDefaultState(root, defaultVariable.Name) : null;
            if (foundVariable != null)
            {
                canOnlyBeSetInDefaultState = foundVariable.CanOnlyBeSetInDefaultState;
            }
        }
        bool shouldInclude = true;

        bool isDefault = _selectedState.SelectedStateSave == _selectedState.SelectedElement?.DefaultState;

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

    private bool GetShouldIncludeBasedOnAttachments(VariableSave variableSave, ElementSave? container, InstanceSave? currentInstance)
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

    /// <inheritdoc/>
    public bool GetShouldIncludeBasedOnBaseType(VariableListSave variableList, ElementSave container, StandardElementSave? rootElementSave)
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
                    // Built-in types always have a default state; GetDefaultStateFor throws otherwise.
                    var foundInstance = StandardElementsManager.Self.GetDefaultStateFor("Component")!.VariableLists.FirstOrDefault(
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
                    shouldInclude = rootElementSave.GetDefaultStateOrThrow().GetVariableListRecursive(variableList.Name) != null;
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

    private bool GetShouldIncludeBasedOnBaseType(VariableSave defaultVariable, ElementSave? container, InstanceSave? instanceSave, StandardElementSave? rootElementSave)
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
                    // Built-in types always have a default state; GetDefaultStateFor throws otherwise.
                    var foundInstance = StandardElementsManager.Self.GetDefaultStateFor("Component")!.Variables.FirstOrDefault(
                        item => item.Name == defaultVariable.Name);

                    shouldInclude = foundInstance != null;
                }
                // If the defaultVariable's
                // source object is null then
                // that means that the variable
                // is being set on "this".  However,
                // variables that are set on "this" may
                // not actually be valid for the type, but
                // a project saved by an earlier Gum may still hold them
                // because the object type was switched.  Therefore, we want to make sure
                // that the variable is valid given the type of object
                // that "this" currently is by checking the default state
                // on the rootElementSave
                if (!shouldInclude && rootElementSave != null)
                {
                    shouldInclude = rootElementSave.GetDefaultStateOrThrow().GetVariableSave(defaultVariable.Name) != null;
                }

                string? nameWithoutState = null;
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

                if (!shouldInclude && instanceSave == null && container != null && defaultVariable.IsState(container))
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

    /// <inheritdoc/>
    public bool GetIfVariableIsDefinedByType(string variableName, ElementSave typeElement)
    {
        if (variableName == "State")
        {
            return true;
        }

        bool isCategorySelector = variableName.Length > "State".Length && variableName.EndsWith("State");
        foreach (ElementSave inChain in GetSelfAndBaseElements(typeElement))
        {
            if (isCategorySelector && inChain.Categories.Any(category => category.Name + "State" == variableName))
            {
                return true;
            }

            // A custom variable is declared by the element that owns it. An exposed variable is declared by name
            // on the element that exposes it, and a derived element or an instance of it uses that name.
            if (inChain.DefaultState?.Variables.Any(variable => string.IsNullOrEmpty(variable.SourceObject)
                    ? variable.IsCustomVariable && variable.Name == variableName
                    : variable.ExposedAsName == variableName) == true)
            {
                return true;
            }
        }

        return GetDefaultStatesDefiningType(typeElement).Any(state => state.Variables.Any(variable => variable.Name == variableName));
    }

    /// <inheritdoc/>
    public bool GetIfVariableListIsDefinedByType(string variableListName, ElementSave typeElement) =>
        GetDefaultStatesDefiningType(typeElement).Any(state => state.VariableLists.Any(list => list.Name == variableListName));

    private static IEnumerable<ElementSave> GetSelfAndBaseElements(ElementSave element)
    {
        yield return element;
        foreach (ElementSave baseElement in ObjectFinder.Self.GetBaseElements(element))
        {
            yield return baseElement;
        }
    }

    /// <summary>
    /// The default states that list what an element of this type has: its root standard element's (the project's
    /// file and the built-in defaults, since a project can predate a variable) and, for a Component or Screen,
    /// that kind's built-in defaults.
    /// </summary>
    private static IEnumerable<StateSave> GetDefaultStatesDefiningType(ElementSave typeElement)
    {
        StandardElementSave? rootStandard = ObjectFinder.Self.GetRootStandardElementSave(typeElement);
        if (rootStandard != null)
        {
            if (rootStandard.DefaultState != null)
            {
                yield return rootStandard.DefaultState;
            }
            if (StandardElementsManager.Self.TryGetDefaultStateFor(rootStandard.Name, throwExceptionOnMissing: false) is { } builtIn)
            {
                yield return builtIn;
            }
        }

        string? kindName = typeElement switch
        {
            ComponentSave => "Component",
            ScreenSave => "Screen",
            _ => null
        };
        if (kindName != null && StandardElementsManager.Self.TryGetDefaultStateFor(kindName, throwExceptionOnMissing: false) is { } kindDefaults)
        {
            yield return kindDefaults;
        }
    }

    /// <inheritdoc/>
    public bool IsVariableHiddenForInstance(string variableName, InstanceSave instance)
    {
        var instanceElement = ObjectFinder.Self.GetElementSave(instance);
        if (instanceElement == null)
        {
            return false;
        }

        return ObjectFinder.Self.IsVariableHiddenRecursively(instanceElement, variableName);
    }

    /// <inheritdoc/>
    public System.ComponentModel.TypeConverter GetTypeConverter(VariableSave defaultVariable, ElementSave container) =>
        _variableTypeConverterProvider.GetTypeConverter(defaultVariable, container);
}
