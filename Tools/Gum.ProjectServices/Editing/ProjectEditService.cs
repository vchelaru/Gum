using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using GumRuntime;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Why a <see cref="IProjectEditService.Apply"/> call failed.
/// </summary>
public enum ProjectEditFailure
{
    /// <summary>Every operation applied.</summary>
    None,
    /// <summary>The operations file is not valid: bad JSON, an unknown version, op or field, or a missing field.</summary>
    InvalidFile,
    /// <summary>An operation could not apply to the project, such as a missing element or a name the type does not define.</summary>
    OperationFailed
}

/// <summary>
/// The outcome of applying an operations file.
/// </summary>
public class ProjectEditResult
{
    /// <summary>Why the edit failed, or <see cref="ProjectEditFailure.None"/>.</summary>
    public ProjectEditFailure Failure { get; init; }

    /// <summary>What went wrong, naming the operation; empty on success.</summary>
    public string Message { get; init; } = "";

    /// <summary>Whether every operation applied.</summary>
    public bool Succeeded => Failure == ProjectEditFailure.None;
}

/// <summary>
/// An operation the operations file accepts and its fields.
/// </summary>
/// <param name="Op">The operation's <c>op</c> value.</param>
/// <param name="Required">The fields the operation must have.</param>
/// <param name="Optional">The fields the operation may have.</param>
public record EditOperationSchema(string Op, IReadOnlyList<string> Required, IReadOnlyList<string> Optional);

/// <summary>
/// Applies an operations file (<c>{ "version": 1, "operations": [ { "op": ..., ... } ] }</c>) to a loaded project,
/// through the same logic the tool uses for each change. The project must be the one
/// <see cref="ObjectFinder"/> resolves. On failure the project may be partly changed, so the caller discards it.
/// </summary>
public interface IProjectEditService
{
    /// <summary>
    /// Validates the whole file, then applies its operations in order, stopping at the first that fails.
    /// </summary>
    ProjectEditResult Apply(GumProjectSave project, string operationsJson);

    /// <summary>
    /// Every operation <see cref="Apply"/> accepts, with its required and optional fields.
    /// </summary>
    IReadOnlyList<EditOperationSchema> GetOperationSchemas();
}

/// <inheritdoc/>
public class ProjectEditService : IProjectEditService
{
    /// <summary>The operations file format version this service reads.</summary>
    public const int SupportedVersion = 1;

    private sealed record OperationSpec(string[] Required, string[] Optional, Action<GumProjectSave, JsonElement> Apply);

    private readonly Dictionary<string, OperationSpec> _operations;
    private readonly IElementCreationLogic _elementCreation;
    private readonly IInstanceCreationLogic _instanceCreation;
    private readonly IInstanceAddRules _instanceAddRules;
    private readonly IStateCreationLogic _stateCreation;
    private readonly ICategoryVariablePropagator _propagator;
    private readonly ICustomVariableLogic _customVariables;
    private readonly IExposeVariableLogic _exposeVariables;
    private readonly IStateVariableWriter _variableWriter;
    private readonly IVariableDefinedByTypeLogic _variableDefinedByType;
    private readonly IElementNameValidator _nameValidator;
    private readonly ITypeResolver _typeResolver;
    private readonly IInstanceRemovalLogic _instanceRemoval;
    private readonly IInstanceChildDetacher _childDetacher;
    private readonly IStateRemovalLogic _stateRemoval;
    private readonly IVariableRemovalLogic _variableRemoval;
    private readonly IVariableOverrideRemover _overrideRemover;
    private readonly IVariableClearLogic _variableClear;

    public ProjectEditService(
        IElementCreationLogic elementCreation,
        IInstanceCreationLogic instanceCreation,
        IInstanceAddRules instanceAddRules,
        IStateCreationLogic stateCreation,
        ICategoryVariablePropagator propagator,
        ICustomVariableLogic customVariables,
        IExposeVariableLogic exposeVariables,
        IStateVariableWriter variableWriter,
        IVariableDefinedByTypeLogic variableDefinedByType,
        IElementNameValidator nameValidator,
        ITypeResolver typeResolver,
        IInstanceRemovalLogic instanceRemoval,
        IInstanceChildDetacher childDetacher,
        IStateRemovalLogic stateRemoval,
        IVariableRemovalLogic variableRemoval,
        IVariableOverrideRemover overrideRemover,
        IVariableClearLogic variableClear)
    {
        _elementCreation = elementCreation;
        _instanceCreation = instanceCreation;
        _instanceAddRules = instanceAddRules;
        _stateCreation = stateCreation;
        _propagator = propagator;
        _customVariables = customVariables;
        _exposeVariables = exposeVariables;
        _variableWriter = variableWriter;
        _variableDefinedByType = variableDefinedByType;
        _nameValidator = nameValidator;
        _typeResolver = typeResolver;
        _instanceRemoval = instanceRemoval;
        _childDetacher = childDetacher;
        _stateRemoval = stateRemoval;
        _variableRemoval = variableRemoval;
        _overrideRemover = overrideRemover;
        _variableClear = variableClear;

        _operations = new Dictionary<string, OperationSpec>
        {
            ["addScreen"] = new(new[] { "name" }, Array.Empty<string>(), AddScreen),
            ["addComponent"] = new(new[] { "name" }, new[] { "baseType" }, AddComponent),
            ["addInstance"] = new(new[] { "element", "name", "type" }, new[] { "parent" }, AddInstance),
            ["addVariable"] = new(new[] { "element", "name", "type" }, new[] { "value" }, AddVariable),
            ["setVariable"] = new(new[] { "element", "name", "value" }, new[] { "state", "category" }, SetVariable),
            ["addCategory"] = new(new[] { "element", "name" }, Array.Empty<string>(), AddCategory),
            ["addState"] = new(new[] { "element", "name" }, new[] { "category" }, AddState),
            ["exposeVariable"] = new(new[] { "element", "instance", "variable", "name" }, Array.Empty<string>(), ExposeVariable),
            ["removeInstance"] = new(new[] { "element", "name" }, Array.Empty<string>(), RemoveInstance),
            ["removeState"] = new(new[] { "element", "name" }, new[] { "category" }, RemoveState),
            ["removeCategory"] = new(new[] { "element", "name" }, Array.Empty<string>(), RemoveCategory),
            ["removeVariable"] = new(new[] { "element", "name" }, Array.Empty<string>(), RemoveVariable),
            ["clearVariable"] = new(new[] { "element", "name" }, new[] { "state", "category" }, ClearVariable),
        };
    }

    /// <inheritdoc/>
    public IReadOnlyList<EditOperationSchema> GetOperationSchemas() =>
        _operations.Select(pair => new EditOperationSchema(pair.Key, pair.Value.Required, pair.Value.Optional)).ToList();

    /// <inheritdoc/>
    public ProjectEditResult Apply(GumProjectSave project, string operationsJson)
    {
        List<(string Op, JsonElement Operation)> operations;
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(operationsJson);
        }
        catch (JsonException ex)
        {
            return Fail(ProjectEditFailure.InvalidFile, $"The operations file is not valid JSON: {ex.Message}");
        }

        using (document)
        {
            if (ReadOperations(document.RootElement, out operations) is { } fileError)
            {
                return Fail(ProjectEditFailure.InvalidFile, fileError);
            }

            for (int i = 0; i < operations.Count; i++)
            {
                try
                {
                    _operations[operations[i].Op].Apply(project, operations[i].Operation);
                }
                catch (EditException ex)
                {
                    return Fail(ProjectEditFailure.OperationFailed, $"operations[{i}] ({operations[i].Op}): {ex.Message}");
                }
            }
        }

        return new ProjectEditResult();
    }

    private static ProjectEditResult Fail(ProjectEditFailure failure, string message) =>
        new ProjectEditResult { Failure = failure, Message = message };

    private string? ReadOperations(JsonElement root, out List<(string Op, JsonElement Operation)> operations)
    {
        operations = new List<(string, JsonElement)>();
        if (root.ValueKind != JsonValueKind.Object)
        {
            return "The operations file must be a JSON object with \"version\" and \"operations\".";
        }

        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (property.Name != "version" && property.Name != "operations")
            {
                return $"The operations file has an unknown field \"{property.Name}\".";
            }
        }

        if (!root.TryGetProperty("version", out JsonElement version) || version.ValueKind != JsonValueKind.Number
            || !version.TryGetInt32(out int versionNumber))
        {
            return "The operations file needs a numeric \"version\".";
        }
        if (versionNumber != SupportedVersion)
        {
            return $"The operations file has version {versionNumber}; this gumcli reads version {SupportedVersion}.";
        }

        if (!root.TryGetProperty("operations", out JsonElement list) || list.ValueKind != JsonValueKind.Array)
        {
            return "The operations file needs an \"operations\" array.";
        }

        int index = 0;
        foreach (JsonElement operation in list.EnumerateArray())
        {
            if (ValidateOperation(operation, out string op) is { } error)
            {
                return $"operations[{index}]: {error}";
            }
            operations.Add((op, operation.Clone()));
            index++;
        }
        return null;
    }

    private string? ValidateOperation(JsonElement operation, out string op)
    {
        op = "";
        if (operation.ValueKind != JsonValueKind.Object)
        {
            return "an operation must be a JSON object.";
        }
        if (!operation.TryGetProperty("op", out JsonElement opElement) || opElement.ValueKind != JsonValueKind.String)
        {
            return "an operation needs an \"op\" string.";
        }
        op = opElement.GetString()!;
        if (!_operations.TryGetValue(op, out OperationSpec? spec))
        {
            return $"unknown op \"{op}\".";
        }

        foreach (JsonProperty property in operation.EnumerateObject())
        {
            if (property.Name == "op")
            {
                continue;
            }
            if (!spec.Required.Contains(property.Name) && !spec.Optional.Contains(property.Name))
            {
                return $"{op} has an unknown field \"{property.Name}\".";
            }
            if (property.Name != "value" && property.Value.ValueKind != JsonValueKind.String)
            {
                return $"{op}'s \"{property.Name}\" must be a string.";
            }
        }
        foreach (string required in spec.Required)
        {
            if (!operation.TryGetProperty(required, out _))
            {
                return $"{op} needs \"{required}\".";
            }
        }
        return null;
    }

    #region Operations

    private void AddScreen(GumProjectSave project, JsonElement operation)
    {
        string name = GetString(operation, "name");
        ValidateNewElementName(name);
        _elementCreation.AddScreen(project, new ScreenSave { Name = name });
    }

    private void AddComponent(GumProjectSave project, JsonElement operation)
    {
        string name = GetString(operation, "name");
        ValidateNewElementName(name);
        ComponentSave component = new ComponentSave();
        _elementCreation.PrepareNewComponent(component, name, GetOptionalString(operation, "baseType") ?? "Container");
        _elementCreation.AddComponent(project, component);
    }

    private void AddInstance(GumProjectSave project, JsonElement operation)
    {
        ElementSave element = GetElement(project, operation);
        string name = GetString(operation, "name");
        string type = GetString(operation, "type");
        if (!_nameValidator.IsInstanceNameValid(name, null, element, out string? whyNotValid))
        {
            throw new EditException(whyNotValid);
        }
        // A type the project lacks is left to the error check, which reports it like check does.
        if (ObjectFinder.Self.GetElementSave(type) is { } typeElement
            && _instanceAddRules.GetWhyCannotAdd(typeElement, element) is { } whyCannotAdd)
        {
            throw new EditException(whyCannotAdd);
        }

        InstanceSave instance = new InstanceSave { Name = name, BaseType = type, ParentContainer = element };
        _instanceCreation.AddInstance(element, instance, GetOptionalString(operation, "parent"), desiredIndex: null);
    }

    private void AddVariable(GumProjectSave project, JsonElement operation)
    {
        ElementSave element = GetElement(project, operation);
        string name = GetString(operation, "name");
        string type = GetString(operation, "type");
        if (!_customVariables.AvailableTypes.Contains(type))
        {
            throw new EditException($"\"{type}\" is not a type a custom variable can have.");
        }
        if (!_nameValidator.IsVariableNameValid(name, element, null, out string? whyNotValid))
        {
            throw new EditException(whyNotValid);
        }
        if (_variableDefinedByType.GetIfVariableIsDefinedByType(name, element))
        {
            throw new EditException($"{element.Name} already has a variable named {name}; use setVariable to set it.");
        }

        object? value = operation.TryGetProperty("value", out JsonElement valueElement)
            ? ConvertValue(valueElement, type, name)
            : _customVariables.GetDefaultValue(type);
        _customVariables.AddCustomVariable(element, name, type, value);
    }

    private void SetVariable(GumProjectSave project, JsonElement operation)
    {
        ElementSave element = GetElement(project, operation);
        string requestedName = GetString(operation, "name");
        (StateSave state, StateSaveCategory? category) = GetState(element, operation);

        (string variableName, InstanceSave? instance) = ResolveVariable(element, requestedName, "set");

        string type = _variableWriter.ResolveVariableType(element, state, variableName)
            ?? throw new EditException($"The type of {requestedName} could not be found.");
        object? value = ConvertValue(operation.GetProperty("value"), type, requestedName);

        _variableWriter.SetVariable(element, state, variableName, value, instance, fallbackType: null);
        if (category != null)
        {
            _propagator.PropagateVariablesInCategory(variableName, element, category.States);
        }
    }

    private void AddCategory(GumProjectSave project, JsonElement operation)
    {
        ElementSave element = GetElement(project, operation);
        string name = GetString(operation, "name");
        if (!_nameValidator.IsCategoryNameValid(name, element, out string? whyNotValid))
        {
            throw new EditException(whyNotValid);
        }
        StateSaveCategory category = _stateCreation.AddCategory(element, name);
        _stateCreation.AddCategoryStateVariable(element, category.Name);
    }

    private void AddState(GumProjectSave project, JsonElement operation)
    {
        ElementSave element = GetElement(project, operation);
        string name = GetString(operation, "name");
        StateSaveCategory? category = null;
        if (GetOptionalString(operation, "category") is { } categoryName)
        {
            category = GetCategory(element, categoryName);
        }
        if (!_nameValidator.IsStateNameValid(name, category, null, out string? whyNotValid))
        {
            throw new EditException(whyNotValid);
        }

        _stateCreation.AddState(element, category, new StateSave { Name = name }, desiredIndex: null);
        if (category != null)
        {
            _propagator.PropagateAllVariablesInCategory(element, category);
        }
    }

    private void ExposeVariable(GumProjectSave project, JsonElement operation)
    {
        ElementSave element = GetElement(project, operation);
        string instanceName = GetString(operation, "instance");
        string rootName = GetString(operation, "variable");
        string exposedName = GetString(operation, "name");
        InstanceSave instance = element.GetInstance(instanceName)
            ?? throw new EditException($"{element.Name} has no instance named {instanceName}.");

        VariableSave? existing = element.DefaultState?.GetVariableSave(instanceName + "." + rootName);
        if (existing == null && _exposeVariables.GetUnauthoredVariableType(instance, rootName, element) == null)
        {
            throw new EditException($"{instanceName} has no variable named {rootName} to expose.");
        }
        if (!_nameValidator.IsVariableNameValid(exposedName, element, existing, out string? whyNotValid))
        {
            throw new EditException(whyNotValid);
        }

        _exposeVariables.Expose(element, instance, rootName, exposedName);
    }

    private void RemoveInstance(GumProjectSave project, JsonElement operation)
    {
        ElementSave element = GetElement(project, operation);
        string name = GetString(operation, "name");
        InstanceSave instance = element.GetInstance(name)
            ?? throw new EditException($"{element.Name} has no instance named {name}.");
        if (_instanceRemoval.GetWhyCannotRemove(instance) is { } whyNot)
        {
            throw new EditException(whyNot);
        }

        // The tool's default "Delete only parent(s)": children move up to the removed instance's parent.
        _childDetacher.DetachChildren(instance, element);
        _instanceRemoval.RemoveInstance(instance, element);
    }

    private void RemoveState(GumProjectSave project, JsonElement operation)
    {
        ElementSave element = GetElement(project, operation);
        string name = GetString(operation, "name");
        StateSave state;
        if (GetOptionalString(operation, "category") is { } categoryName)
        {
            state = GetCategory(element, categoryName).States.FirstOrDefault(item => item.Name == name)
                ?? throw new EditException($"{element.Name}'s category {categoryName} has no state named {name}.");
        }
        else
        {
            state = element.States.FirstOrDefault(item => item.Name == name)
                ?? throw new EditException($"{element.Name} has no uncategorized state named {name}.");
        }
        if (_stateRemoval.GetWhyCannotRemoveState(state, element) is { } whyNot)
        {
            throw new EditException(whyNot);
        }

        _stateRemoval.RemoveState(state, element);
    }

    private void RemoveCategory(GumProjectSave project, JsonElement operation)
    {
        ElementSave element = GetElement(project, operation);
        StateSaveCategory category = GetCategory(element, GetString(operation, "name"));
        if (_stateRemoval.GetWhyCannotRemoveCategory(category, element) is { } whyNot)
        {
            throw new EditException(whyNot);
        }

        _stateRemoval.RemoveCategory(category, element);
    }

    private void RemoveVariable(GumProjectSave project, JsonElement operation)
    {
        ElementSave element = GetElement(project, operation);
        string name = GetString(operation, "name");
        VariableSave variable = element.GetDefaultStateOrThrow().Variables
            .FirstOrDefault(item => item.Name == name && item.IsCustomVariable)
            ?? throw new EditException($"{element.Name} has no custom variable named {name}.");
        if (_variableRemoval.GetWhyCannotRemoveCustomVariable(variable, element, out List<VariableChange> cascading) is { } whyNot)
        {
            throw new EditException(whyNot);
        }

        _variableRemoval.RemoveCustomVariable(variable, element);
        _overrideRemover.Remove(cascading);
    }

    private void ClearVariable(GumProjectSave project, JsonElement operation)
    {
        ElementSave element = GetElement(project, operation);
        string requestedName = GetString(operation, "name");
        (StateSave state, StateSaveCategory? category) = GetState(element, operation);
        (string variableName, _) = ResolveVariable(element, requestedName, "cleared");

        VariableSave variable = state.GetVariableSave(variableName)
            ?? throw new EditException($"{element.Name}'s state {state.Name} does not set {requestedName}.");
        _variableClear.ClearVariable(element, state, variable, isPartOfCategory: category != null);
        ElementSaveExtensions.ApplyVariableReferences(element, state);
    }

    #endregion

    #region Lookups and values

    /// <summary>
    /// The stored name of <paramref name="requestedName"/> on <paramref name="element"/>, and the instance it sets:
    /// <c>Instance.Var</c> for an instance's variable, or the element's own variable, where an exposed name maps to
    /// the instance variable it exposes. Fails for a name the type does not define.
    /// </summary>
    private (string VariableName, InstanceSave? Instance) ResolveVariable(ElementSave element, string requestedName, string verb)
    {
        string? instanceName = VariableSave.GetSourceObject(requestedName);
        if (!string.IsNullOrEmpty(instanceName))
        {
            InstanceSave instance = element.GetInstance(instanceName)
                ?? throw new EditException($"{element.Name} has no instance named {instanceName}.");
            ElementSave typeElement = ObjectFinder.Self.GetElementSave(instance.BaseType)
                ?? throw new EditException($"{instanceName}'s type {instance.BaseType} is not in the project.");
            if (!_variableDefinedByType.GetIfVariableIsDefinedByType(VariableSave.GetRootName(requestedName), typeElement))
            {
                throw new EditException($"{typeElement.Name} has no variable named {VariableSave.GetRootName(requestedName)}, so {requestedName} cannot be {verb}.");
            }
            return (requestedName, instance);
        }

        if (!_variableDefinedByType.GetIfVariableIsDefinedByType(requestedName, element))
        {
            throw new EditException($"{element.Name} has no variable named {requestedName}.");
        }
        // An element's own exposed variable is stored under its instance's name.
        if (element.DefaultState?.Variables.FirstOrDefault(item => item.ExposedAsName == requestedName
                && !string.IsNullOrEmpty(item.SourceObject)) is { } exposed)
        {
            return (exposed.Name, element.GetInstance(exposed.SourceObject!));
        }
        return (requestedName, null);
    }

    private void ValidateNewElementName(string name)
    {
        int slash = name.LastIndexOf('/');
        string? folder = slash < 0 ? null : name.Substring(0, slash);
        string nameWithoutFolder = name.Substring(slash + 1);
        if (!_nameValidator.IsElementNameValid(nameWithoutFolder, folder, null, out string? whyNotValid))
        {
            throw new EditException(whyNotValid);
        }
    }

    private static ElementSave GetElement(GumProjectSave project, JsonElement operation)
    {
        string name = GetString(operation, "element");
        return (ElementSave?)project.Screens.FirstOrDefault(item => item.Name == name)
            ?? project.Components.FirstOrDefault(item => item.Name == name)
            ?? throw new EditException($"The project has no Screen or Component named {name}.");
    }

    private static StateSaveCategory GetCategory(ElementSave element, string categoryName) =>
        element.Categories.FirstOrDefault(item => item.Name == categoryName)
            ?? throw new EditException($"{element.Name} has no category named {categoryName}.");

    private static (StateSave State, StateSaveCategory? Category) GetState(ElementSave element, JsonElement operation)
    {
        string? stateName = GetOptionalString(operation, "state");
        string? categoryName = GetOptionalString(operation, "category");
        if (categoryName != null)
        {
            StateSaveCategory category = GetCategory(element, categoryName);
            if (stateName == null)
            {
                throw new EditException("category needs state.");
            }
            StateSave categoryState = category.States.FirstOrDefault(item => item.Name == stateName)
                ?? throw new EditException($"{element.Name}'s category {categoryName} has no state named {stateName}.");
            return (categoryState, category);
        }

        if (stateName == null)
        {
            return (element.GetDefaultStateOrThrow(), null);
        }

        StateSave state = element.States.FirstOrDefault(item => item.Name == stateName)
            ?? throw new EditException($"{element.Name} has no uncategorized state named {stateName}.");
        return (state, null);
    }

    private object? ConvertValue(JsonElement value, string type, string variableName)
    {
        if (value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        object? converted = type switch
        {
            "float" => value.ValueKind == JsonValueKind.Number && value.TryGetSingle(out float single) ? single : null,
            "double" => value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out double number) ? number : null,
            "int" => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int integer) ? integer : null,
            "bool" => value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : null,
            "string" => value.ValueKind == JsonValueKind.String ? value.GetString() : null,
            _ => ConvertOtherType(value, type)
        };

        return converted ?? throw new EditException($"{value.GetRawText()} is not a valid {type} value for {variableName}.");
    }

    private object? ConvertOtherType(JsonElement value, string type)
    {
        Type? resolved = _typeResolver.GetTypeFromString(type);
        if (resolved != null && resolved.IsEnum)
        {
            if (value.ValueKind == JsonValueKind.String && Enum.GetNames(resolved).Contains(value.GetString()))
            {
                return Enum.Parse(resolved, value.GetString()!);
            }
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number) && Enum.IsDefined(resolved, number))
            {
                return Enum.ToObject(resolved, number);
            }
            return null;
        }

        // States, file names and other text-valued variables.
        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static string GetString(JsonElement operation, string field) => operation.GetProperty(field).GetString()!;

    private static string? GetOptionalString(JsonElement operation, string field) =>
        operation.TryGetProperty(field, out JsonElement value) ? value.GetString() : null;

    #endregion

    private sealed class EditException : Exception
    {
        public EditException(string message) : base(message)
        {
        }
    }
}
