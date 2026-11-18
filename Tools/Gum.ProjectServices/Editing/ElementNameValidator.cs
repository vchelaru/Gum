using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Adds names to an element's top-level names beyond its instances, categories and exposed variables. The tool's
/// plugins add theirs (animation names) through it.
/// </summary>
public interface ITopLevelNameSource
{
    /// <summary>
    /// Adds the names <paramref name="element"/> declares to <paramref name="names"/>.
    /// </summary>
    void FillTopLevelNames(ElementSave element, List<TopLevelName> names);
}

/// <summary>
/// The name checks for elements, instances, categories, states and variables that need only the project. The
/// tool's <c>NameVerifier</c> delegates to it and adds the checks that need tool state.
/// </summary>
public interface IElementNameValidator
{
    /// <summary>
    /// Whether a Screen or Component may be named <paramref name="nameWithoutFolder"/> in
    /// <paramref name="folderName"/>, ignoring <paramref name="element"/> (the element being renamed).
    /// </summary>
    bool IsElementNameValid(string? nameWithoutFolder, string? folderName, ElementSave? element, [NotNullWhen(false)] out string? whyNotValid);

    /// <summary>
    /// Whether a category in <paramref name="container"/> may be named <paramref name="name"/>.
    /// </summary>
    bool IsCategoryNameValid(string? name, IStateContainer container, [NotNullWhen(false)] out string? whyNotValid, StateSaveCategory? categoryToIgnore = null);

    /// <summary>
    /// Whether a state in <paramref name="category"/> may be named <paramref name="name"/>.
    /// </summary>
    bool IsStateNameValid(string? name, StateSaveCategory? category, StateSave? state, [NotNullWhen(false)] out string? whyNotValid);

    /// <summary>
    /// Whether an instance in <paramref name="container"/> may be named <paramref name="name"/>.
    /// </summary>
    bool IsInstanceNameValid(string? name, InstanceSave? instance, IInstanceContainer? container, [NotNullWhen(false)] out string? whyNotValid);

    /// <summary>
    /// Whether <paramref name="name"/> is free among <paramref name="element"/>'s instances, categories, exposed
    /// variables and the names the <see cref="ITopLevelNameSource"/> adds.
    /// </summary>
    bool IsNameValidTopLevel(string name, ElementSave element, object? objectToIgnore, [NotNullWhen(false)] out string? whyNotValid);

    /// <summary>
    /// Whether a custom or exposed variable may be named <paramref name="name"/> on <paramref name="element"/>. It
    /// does not check whether the element's type already has a variable of that name; callers add that.
    /// </summary>
    bool IsVariableNameValid(string name, ElementSave? element, VariableSave? variable, [NotNullWhen(false)] out string? whyNotValid);
}

/// <inheritdoc/>
public class ElementNameValidator : IElementNameValidator
{
    private readonly NameRules _rules;
    private readonly ITopLevelNameSource? _topLevelNameSource;

    /// <param name="topLevelNameSource">Adds names beyond the element's own, or null for none.</param>
    public ElementNameValidator(ITopLevelNameSource? topLevelNameSource)
    {
        _rules = new NameRules();
        _topLevelNameSource = topLevelNameSource;
    }

    /// <inheritdoc/>
    public bool IsElementNameValid(string? nameWithoutFolder, string? folderName, ElementSave? element, [NotNullWhen(false)] out string? whyNotValid)
    {
        _rules.IsNameValidCommon(nameWithoutFolder, out whyNotValid, out _);
        if (string.IsNullOrEmpty(whyNotValid))
        {
            _rules.IsFileNameWindowsReserved(nameWithoutFolder, out whyNotValid);
        }
        if (string.IsNullOrEmpty(whyNotValid) && nameWithoutFolder != null)
        {
            IsNameAnExistingElement(nameWithoutFolder, folderName, element, out whyNotValid);
        }
        if (string.IsNullOrEmpty(whyNotValid) && nameWithoutFolder != null && element != null)
        {
            // The generated class takes the element's name, and none of its members can share it.
            InstanceSave? sameNamed = element.Instances.FirstOrDefault(item =>
                _rules.Standardize(item.Name) == _rules.Standardize(nameWithoutFolder));
            if (sameNamed != null)
            {
                whyNotValid = $"The element has an instance named '{sameNamed.Name}', so rename or delete the instance first.";
            }
        }
        return string.IsNullOrEmpty(whyNotValid);
    }

    /// <inheritdoc/>
    public bool IsCategoryNameValid(string? name, IStateContainer container, [NotNullWhen(false)] out string? whyNotValid, StateSaveCategory? categoryToIgnore = null)
    {
        if (!_rules.IsNameValidCommon(name, out string commonWhyNotValid, out _))
        {
            whyNotValid = commonWhyNotValid;
            return false;
        }
        whyNotValid = null;

        if (name == container.Name)
        {
            whyNotValid = "Category name cannot be the same as its container's";
            return false;
        }

        if (name.Contains(" "))
        {
            whyNotValid = "Category names cannot contain spaces";
            return false;
        }

        string standardizedName = _rules.Standardize(name);
        string? existingName = null;
        container.GetStateSaveCategoryRecursively(item =>
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
            whyNotValid = $"A category with the name {existingName} is already defined in {container.Name}";
            return false;
        }

        if (container is ElementSave element)
        {
            return IsNameValidTopLevel(name, element, categoryToIgnore, out whyNotValid);
        }

        return true;
    }

    /// <inheritdoc/>
    public bool IsStateNameValid(string? name, StateSaveCategory? category, StateSave? state, [NotNullWhen(false)] out string? whyNotValid)
    {
        if (!_rules.IsNameValidCommon(name, out string commonWhyNotValid, out _))
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

        var existing = category?.States.Find(item => _rules.Standardize(item.Name) == _rules.Standardize(name) && item != state);
        if (category != null && existing != null)
        {
            whyNotValid = $"The category {category.Name} already has a state named {name}";
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public bool IsInstanceNameValid(string? name, InstanceSave? instance, IInstanceContainer? container, [NotNullWhen(false)] out string? whyNotValid)
    {
        if (!_rules.IsNameValidCommon(name, out string commonWhyNotValid, out _))
        {
            whyNotValid = commonWhyNotValid;
            return false;
        }

        IsNameUsedByStandardVariables(name, out whyNotValid);
        if (string.IsNullOrEmpty(whyNotValid) && container != null)
        {
            IsNameAlreadyUsed(name, instance, container, out whyNotValid);
        }

        if (string.IsNullOrEmpty(whyNotValid) && container is ElementSave element)
        {
            IsNameValidTopLevel(name, element, instance, out whyNotValid);
        }

        return string.IsNullOrEmpty(whyNotValid);
    }

    /// <inheritdoc/>
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

        _topLevelNameSource?.FillTopLevelNames(element, names);

        string standardizedName = _rules.Standardize(name);

        var match = names.FirstOrDefault(item => item.SourceObject != objectToIgnore && _rules.Standardize(item.Name) == standardizedName);

        if (match != null)
        {
            whyNotValid = $"The name {name} is already used by a(n) {match.Type}";
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public bool IsVariableNameValid(string name, ElementSave? element, VariableSave? variable, [NotNullWhen(false)] out string? whyNotValid)
    {
        _rules.IsNameValidCommon(name, out string commonWhyNotValid, out _);
        whyNotValid = commonWhyNotValid;

        // Earlier versions of Gum allowed spaces in variable names, which confused variable references, so Gum
        // strips spaces from names and new ones cannot have them.
        if (string.IsNullOrEmpty(whyNotValid) && name.Contains(" "))
        {
            whyNotValid = "Variable names cannot contain spaces";
        }

        if (string.IsNullOrEmpty(whyNotValid) && InstancePropertyNames.Contains(name))
        {
            whyNotValid = $"{name} cannot be used because every instance already has a {name} property";
        }

        if (string.IsNullOrEmpty(whyNotValid) && element != null)
        {
            IsNameAlreadyUsed(name, variable, element, out whyNotValid);
        }

        if (string.IsNullOrEmpty(whyNotValid) && element != null)
        {
            IsNameValidTopLevel(name, element, variable, out whyNotValid);
        }

        return string.IsNullOrEmpty(whyNotValid);
    }

    // The InstanceSave's own properties, which every instance shows in the Variables tab. A variable of the same
    // name on an instance would be read or set as the property instead (setting Name renames the instance).
    private static readonly HashSet<string> InstancePropertyNames = new HashSet<string>
    {
        "Name", "BaseType", "Locked", "IsSlot"
    };

    private void IsNameUsedByStandardVariables(string nameToCheck, out string? whyNotValid)
    {
        var defaultStates = StandardElementsManager.Self.DefaultStates ??
            throw new InvalidOperationException("StandardElementsManager hasn't been initialized.");
        var names = defaultStates.SelectMany(item => item.Value.Variables).Select(item => item.Name).ToHashSet();
        whyNotValid = null;
        if (names.Contains(nameToCheck))
        {
            whyNotValid = $"The name {nameToCheck} cannot be used because it is a reserved variable name";
        }
        else if (nameToCheck == "Name")
        {
            whyNotValid = $"\"Name\" is a reserved keyword so it cannot be used as a name";
        }
    }

    private void IsNameAlreadyUsed(string name, object? objectToIgnore, IInstanceContainer container, out string? whyNotValid)
    {
        whyNotValid = null;
        // The generated class is named after the element without its folder, and a member cannot be named like its class.
        string containerNameWithoutFolder = container.Name.Substring(container.Name.LastIndexOfAny(new[] { '/', '\\' }) + 1);
        if (objectToIgnore != container && _rules.Standardize(name) == _rules.Standardize(containerNameWithoutFolder))
        {
            whyNotValid = $"The element is named '{containerNameWithoutFolder}', so choose a different name.";
        }
        var instance = container.Instances.FirstOrDefault(item => item != objectToIgnore && _rules.Standardize(item.Name) == _rules.Standardize(name));
        if (instance != null)
        {
            whyNotValid = $"There is already an instance named '{instance.Name}'";
        }
        var stateContainer = container as IStateContainer;
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
    }

    private void IsNameAnExistingElement(string name, string? folderName, object? objectToIgnore, out string? whyNotValid)
    {
        whyNotValid = null;

        var folderPrefix = folderName;
        if (!string.IsNullOrEmpty(folderPrefix) && !folderPrefix!.EndsWith("/"))
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
}
