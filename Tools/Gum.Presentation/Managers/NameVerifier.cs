﻿using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.ProjectServices;
using Gum.ToolStates;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
namespace Gum.Managers;

public class NameVerifier : INameVerifier
{
    #region Fields/Properties

    private readonly StandardElementsManager _standardElementsManager;
    private readonly IVariableSaveLogic _variableSaveLogic;
    private readonly Plugins.IPluginManager _pluginManager;
    private readonly ISelectedState _selectedState;

    // The rules that need no tool service; the CLI runs the same ones.
    private readonly NameRules _rules = new NameRules();

    #endregion

    #region Folder

    public NameVerifier(IVariableSaveLogic variableSaveLogic, Plugins.IPluginManager pluginManager, ISelectedState selectedState)
    {
        _standardElementsManager = StandardElementsManager.Self;
        _variableSaveLogic = variableSaveLogic;
        _pluginManager = pluginManager;
        _selectedState = selectedState;
    }
    public bool IsFolderNameValid(string? folderName, out string whyNotValid)
    {
        IsNameValidCommon(folderName, out whyNotValid, out _);
        return string.IsNullOrEmpty(whyNotValid);
    }
    
    #endregion
    
    public bool IsElementNameValid(string? componentNameWithoutFolder, string? folderName, ElementSave? elementSave, [NotNullWhen(false)] out string? whyNotValid)
    {
        IsNameValidCommon(componentNameWithoutFolder, out whyNotValid, out _);
        if (string.IsNullOrEmpty(whyNotValid))
        {
            _rules.IsFileNameWindowsReserved(componentNameWithoutFolder, out whyNotValid);
        }
        if (string.IsNullOrEmpty(whyNotValid) && componentNameWithoutFolder != null)
        {
            IsNameAnExistingElement(componentNameWithoutFolder, folderName, elementSave, out whyNotValid);
        }
        if (string.IsNullOrEmpty(whyNotValid) && componentNameWithoutFolder != null && elementSave != null)
        {
            // The generated class takes the element's name, and none of its members can share it.
            InstanceSave? sameNamed = elementSave.Instances.FirstOrDefault(item =>
                _rules.Standardize(item.Name) == _rules.Standardize(componentNameWithoutFolder));
            if (sameNamed != null)
            {
                whyNotValid = $"The element has an instance named '{sameNamed.Name}'";
            }
        }
        return string.IsNullOrEmpty(whyNotValid);
    }
    public bool IsCategoryNameValid(string? name, IStateContainer categoryContainer, [NotNullWhen(false)] out string? whyNotValid, StateSaveCategory? categoryToIgnore = null)
    {
        if (!IsNameValidCommon(name, out string commonWhyNotValid, out _))
        {
            whyNotValid = commonWhyNotValid;
            return false;
        }
        whyNotValid = null;

        if (name == categoryContainer.Name)
        {
            whyNotValid = "Category name cannot be the same as its container's";
            return false;
        }
            
        if(name.Contains(" "))
        {
            whyNotValid = "Category names cannot contain spaces";
            return false;
        }
            
        {
            string standardizedName = _rules.Standardize(name);
            string? existingName = null;
            categoryContainer.GetStateSaveCategoryRecursively(item =>
            {
                if (item == categoryToIgnore) return false;
                if (_rules.Standardize(item.Name) == standardizedName)
                {
                    existingName = item.Name;
                    return true;
                }
                return false;
            });
            if (existingName != null)
            {
                whyNotValid = $"A category with the name {existingName} is already defined in {categoryContainer.Name}";
                return false;
            }
        }

        if(categoryContainer is ElementSave element)
        {
            return IsNameValidTopLevel(name, element, categoryToIgnore, out whyNotValid);
        }

        return true;
    }
    public bool IsStateNameValid(string? name, StateSaveCategory? category, StateSave? stateSave, [NotNullWhen(false)] out string? whyNotValid)
    {
        if (!IsNameValidCommon(name, out string commonWhyNotValid, out _))
        {
            whyNotValid = commonWhyNotValid;
            return false;
        }
        whyNotValid = null;

        if (category != null && name == category.Name)
        {
            whyNotValid = "State name cannot be the same as its category's";
            return false;
        }

        var existing = category?.States.Find(item => _rules.Standardize(item.Name) == _rules.Standardize(name) && item != stateSave);
        if (category != null && existing != null)
        {
            whyNotValid = $"The category {category.Name} already has a state named {name}";
            return false;
        }

        return true;
    }
    public bool IsInstanceNameValid(string? instanceName, InstanceSave? instanceSave, IInstanceContainer? instanceContainer, [NotNullWhen(false)] out string? whyNotValid)
    {
        if (!IsNameValidCommon(instanceName, out string commonWhyNotValid, out _))
        {
            whyNotValid = commonWhyNotValid;
            return false;
        }
        whyNotValid = null;
        // See if this is a variable used by any state in the StandardElementsManager:
        if(string.IsNullOrEmpty(whyNotValid))
        {
            IsNameUsedByStandardVariables(instanceName, out whyNotValid);
        }
        if (string.IsNullOrEmpty(whyNotValid) && instanceContainer != null)
        {
            IsNameAlreadyUsed(instanceName, instanceSave, instanceContainer, out whyNotValid);
        }

        if (string.IsNullOrEmpty(whyNotValid) && instanceContainer is ElementSave element)
        {
            IsNameValidTopLevel(instanceName, element, instanceSave, out whyNotValid);
        }

        return string.IsNullOrEmpty(whyNotValid);
    }
    private void IsNameUsedByStandardVariables(string nameToCheck, out string? whyNotValid)
    {
        var defaultStates = _standardElementsManager.DefaultStates ??
            throw new System.InvalidOperationException("StandardElementsManager hasn't been initialized.");
        var variables = defaultStates.SelectMany(item => item.Value.Variables);
        var names = variables.Select(item => item.Name).ToHashSet();
        whyNotValid = null;
        if(names.Contains(nameToCheck))
        {
            whyNotValid = $"The name {nameToCheck} cannot be used because it is a reserved variable name";
        }
        else if(nameToCheck == "Name")
        {
            whyNotValid = $"\"Name\" is a reserved keyword so it cannot be used as a name";
        }
    }
    public bool IsNameValidTopLevel(string name, ElementSave element, object? objectToIgnore, [NotNullWhen(false)] out string? whyNotValid)
    {
        whyNotValid = null;
        var names = new List<TopLevelName>();

        foreach (var instance in element.Instances)
        {
            names.Add(new TopLevelName(instance.Name, "Instance", instance));
        }

        foreach (var category in element.Categories)
        {
            names.Add(new TopLevelName(category.Name, "Category", category));
        }

        if (element.DefaultState != null)
        {
            foreach (var variable in element.DefaultState.Variables.Where(item => !string.IsNullOrEmpty(item.ExposedAsName)))
            {
                // The Where above keeps only variables with an ExposedAsName.
                names.Add(new TopLevelName(variable.ExposedAsName!, "Variable", variable));
            }
        }

        _pluginManager.FillTopLevelNames(element, names);

        string standardizedName = _rules.Standardize(name);

        var match = names.FirstOrDefault(item => item.SourceObject != objectToIgnore && _rules.Standardize(item.Name) == standardizedName);

        if (match != null)
        {
            whyNotValid = $"The name {name} is already used by a(n) {match.Type}";
            return false;
        }

        return true;
    }
    public bool IsVariableNameValid(string variableName, ElementSave? elementSave, VariableSave? variableSave, [NotNullWhen(false)] out string? whyNotValid)
    {
        IsNameValidCommon(variableName, out string commonWhyNotValid, out _);
        whyNotValid = commonWhyNotValid;

        // variables should not allow spaces because previous versions of Gum used to have variables with spaces
        // and that caused confusion when creating variable referencs. Therefore, Gum strips spaces from names. We 
        // should prevent spaces from being added here:
        if(string.IsNullOrEmpty(whyNotValid) && variableName.Contains(" "))
        {
            whyNotValid = "Variable names cannot contain spaces";
        }

        if (string.IsNullOrEmpty(whyNotValid) && elementSave != null)
        {
            IsNameAlreadyUsed(variableName, variableSave, elementSave, out whyNotValid);
        }

        if (string.IsNullOrEmpty(whyNotValid) && elementSave != null)
        {
            IsNameValidTopLevel(variableName, elementSave, variableSave, out whyNotValid);
        }

        if (string.IsNullOrEmpty(whyNotValid))
        {
            var existingVariable = elementSave != null ? GetVariableFromThisOrBase(elementSave, variableName) : null;
            // there's a variable but we shouldn't consider it
            // unless it's "Active" - inactive variables may be
            // leftovers from a type change
            // A state can hold its own copy of the edited variable, so match by name as well as by reference.
            var isVariableBeingEdited = variableSave != null && existingVariable != null &&
                (existingVariable == variableSave || existingVariable.Name == variableSave.Name);
            if(existingVariable != null && elementSave != null && !isVariableBeingEdited)
            {
                var isActive = _variableSaveLogic.GetIfVariableIsActive(existingVariable,
                    elementSave, null);
                if(isActive)
                {
                    whyNotValid = $"The variable name {variableName} is already used";
                }
            }
        }
        return string.IsNullOrEmpty(whyNotValid);
    }
    public bool IsBehaviorNameValid(string? behaviorName, BehaviorSave? behaviorSave, [NotNullWhen(false)] out string? whyNotValid)
    {
        // Behaviors are files named after them, so a duplicate (compared as elements are) would overwrite the other's file.
        IEnumerable<BehaviorSave> allBehaviors = ObjectFinder.Self.GumProjectSave?.Behaviors ?? Enumerable.Empty<BehaviorSave>();
        return _rules.IsBehaviorNameValid(behaviorName, behaviorSave, allBehaviors, out whyNotValid);
    }
    public bool IsNameValidCommon([NotNullWhen(true)] string? name, out string whyNotValid, out CommonValidationError commonValidationError)
    {
        return _rules.IsNameValidCommon(name, out whyNotValid, out commonValidationError);
    }
    /// <summary>
    /// Returns the variable from the element's currently-selected state if <paramref name="element"/>
    /// is the currently selected element and a state is selected, otherwise from its default state.
    /// Headless replacement for the tool-only <c>ElementSaveExtensionMethodsGumTool.GetVariableFromThisOrBase</c>,
    /// which resolves <see cref="ISelectedState"/> via <c>Locator</c> instead of taking it as a dependency.
    /// </summary>
    private VariableSave? GetVariableFromThisOrBase(ElementSave element, string variable)
    {
        var stateToPullFrom = (element == _selectedState.SelectedElement && _selectedState.SelectedStateSave != null)
            ? _selectedState.SelectedStateSave
            : element.GetDefaultStateOrThrow();

        return stateToPullFrom.GetVariableRecursive(variable);
    }
    public bool IsValidCSharpName(string name, [NotNullWhen(false)] out string? whyNotValid, out CommonValidationError commonValidationError)
    {
        return _rules.IsValidCSharpName(name, out whyNotValid, out commonValidationError);
    }
    public bool IsComponentNameAlreadyUsed(string name)
    {
        return ObjectFinder.Self.GetComponent(name) != null;
    }
    private void IsNameAlreadyUsed(string name, object? objectToIgnore, IInstanceContainer instanceContainer, out string? whyNotValid)
    {
        whyNotValid = null;
        // The generated class is named after the element without its folder, and a member cannot be named like its class.
        string containerNameWithoutFolder = instanceContainer.Name.Substring(instanceContainer.Name.LastIndexOfAny(new[] { '/', '\\' }) + 1);
        if (objectToIgnore != instanceContainer && _rules.Standardize(name) == _rules.Standardize(containerNameWithoutFolder))
        {
            whyNotValid = $"The element is named '{containerNameWithoutFolder}'";
        }
        var instance = instanceContainer.Instances.FirstOrDefault(item => item != objectToIgnore && _rules.Standardize(item.Name) == _rules.Standardize(name));
        if (instance != null)
        {
            whyNotValid = $"There is already an instance named '{instance.Name}'";
        }
        var stateContainer = instanceContainer as IStateContainer;
        var state = stateContainer?.AllStates.FirstOrDefault(item => item != objectToIgnore && _rules.Standardize(item.Name) == _rules.Standardize(name));
        if (state != null)
        {
            whyNotValid = $"There is already a state named '{state.Name}'";
        }
        
        var variable = stateContainer?.AllStates
                                      .SelectMany(item => item.Variables)
                                      .FirstOrDefault(item => item != objectToIgnore && _rules.Standardize(item.ExposedAsName) == _rules.Standardize(name));
        if (variable != null)
        {
            whyNotValid = $"There is already a variable named '{variable.Name}'";
        }
        //element = ObjectFinder.Self.GumProjectSave.StandardElements.FirstOrDefault(item=>item != objectToIgnore && item.Name == name)
        //{
        //    whyNotValid = "There is a standard element named " + element.Name + " so this name can't be used.";
        //}
    }
    private void IsNameAnExistingElement(string name, string? folderName, object? objectToIgnore, out string? whyNotValid)
    {
        whyNotValid = null;

        var folderPrefix = folderName;
        if(!string.IsNullOrEmpty(folderPrefix) && !folderPrefix!.EndsWith("/") && !folderPrefix.EndsWith("/"))
        {
            folderPrefix += "/";
        }

        string newStandardizedNameWithFolder = _rules.Standardize(folderPrefix + name);
        var standardElement = ObjectFinder.Self.GumProjectSave?.StandardElements.FirstOrDefault(item =>
            item != objectToIgnore && _rules.Standardize(item.Name) == newStandardizedNameWithFolder);
        if (standardElement != null)
        {
            whyNotValid = "There is a standard element named " + standardElement.Name + " so this name can't be used.";
        }
        var component = ObjectFinder.Self.GumProjectSave?.Components.FirstOrDefault(item =>
            item != objectToIgnore && _rules.Standardize(item.Name) == newStandardizedNameWithFolder);
        if (component != null)
        {
            whyNotValid = "There is a component named " + component.Name + " so this name can't be used.";
        }
        var screen = ObjectFinder.Self.GumProjectSave?.Screens.FirstOrDefault(item =>
            item != objectToIgnore && _rules.Standardize(item.Name) == newStandardizedNameWithFolder);
        if (screen != null)
        {
            whyNotValid = "There is a screen named " + screen.Name + " so this name can't be used.";
        }
    }
    public bool IsNameValidAndroidFile(string name, [NotNullWhen(false)] out string? whyNotValid)
    {
        return _rules.IsNameValidAndroidFile(name, out whyNotValid);
    }
}