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

    /// <summary>
    /// The animation, code settings and behavior files the operations changed beyond what the project's save writes.
    /// The caller commits them after the save. Null when the edit failed.
    /// </summary>
    public PendingFileChanges? PendingFiles { get; init; }
}

/// <summary>
/// The JSON value an operation field accepts.
/// </summary>
public enum EditFieldType
{
    /// <summary>A JSON string.</summary>
    String,
    /// <summary>JSON <c>true</c> or <c>false</c>.</summary>
    Bool,
    /// <summary>Any JSON value.</summary>
    Any,
    /// <summary>A JSON array of strings.</summary>
    StringArray,
}

/// <summary>
/// A field of an operation and the JSON value it accepts.
/// </summary>
/// <param name="Name">The field's name in the operation object.</param>
/// <param name="Type">The JSON value the field accepts.</param>
public record EditField(string Name, EditFieldType Type)
{
    /// <summary>The type as the schema prints it: <c>string</c>, <c>bool</c> or <c>any</c>.</summary>
    public string TypeName => Type switch
    {
        EditFieldType.String => "string",
        EditFieldType.Bool => "bool",
        EditFieldType.StringArray => "string[]",
        _ => "any",
    };
}

/// <summary>
/// An operation the operations file accepts and its fields.
/// </summary>
/// <param name="Op">The operation's <c>op</c> value.</param>
/// <param name="Required">The fields the operation must have.</param>
/// <param name="Optional">The fields the operation may have.</param>
/// <param name="AppliesToStandardElements">Whether the operation's <c>element</c> may be a standard element such as Text.</param>
public record EditOperationSchema(string Op, IReadOnlyList<EditField> Required, IReadOnlyList<EditField> Optional, bool AppliesToStandardElements);

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

    /// <summary>
    /// Every project property the <c>setProjectProperty</c> operation sets, with its value type and allowed values.
    /// </summary>
    IReadOnlyList<ProjectPropertySchema> GetProjectPropertySchemas();
}

/// <inheritdoc/>
public partial class ProjectEditService : IProjectEditService
{
    /// <summary>The operations file format version this service reads.</summary>
    public const int SupportedVersion = 1;

    private sealed record OperationSpec(EditField[] Required, EditField[] Optional, Action<GumProjectSave, JsonElement> Apply, bool StandardElements = false)
    {
        public EditField? Find(string name) =>
            Required.FirstOrDefault(field => field.Name == name) ?? Optional.FirstOrDefault(field => field.Name == name);
    }

    private static EditField[] Strings(params string[] names) =>
        names.Select(name => new EditField(name, EditFieldType.String)).ToArray();

    private static EditField[] Fields(params EditField[] fields) => fields;

    private static EditField Str(string name) => new EditField(name, EditFieldType.String);

    private static readonly EditField[] None = Array.Empty<EditField>();

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
    private readonly VariableValueConverter _values;
    private readonly IInstanceRemovalLogic _instanceRemoval;
    private readonly IInstanceChildDetacher _childDetacher;
    private readonly IStateRemovalLogic _stateRemoval;
    private readonly IVariableRemovalLogic _variableRemoval;
    private readonly IVariableOverrideRemover _overrideRemover;
    private readonly IReferenceFinder _referenceFinder;
    private readonly IVariableClearLogic _variableClear;
    private readonly IComponentExtractionLogic _componentExtraction;
    private readonly IMovedInstanceReferenceDropLogic _movedReferenceDrop;
    private readonly IElementRemovalLogic _elementRemoval;
    private readonly IVariableReferenceValidator _referenceValidator;
    private readonly IVariableReferenceCascade _referenceCascade;
    private readonly IComponentBehaviorLogic _componentBehaviors;
    private readonly IInheritanceSync _inheritanceSync;
    private readonly ReorderEditOperations _reorder;
    private readonly IBaseTypeChangeLogic _baseTypeChange;
    private readonly InstancePropertyLogic _instanceProperties = new InstancePropertyLogic();

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
        IReferenceFinder referenceFinder,
        IVariableClearLogic variableClear,
        IComponentExtractionLogic componentExtraction,
        IMovedInstanceReferenceDropLogic movedReferenceDrop,
        IElementRemovalLogic elementRemoval,
        IVariableReferenceValidator referenceValidator,
        IVariableReferenceCascade referenceCascade,
        IInheritanceSync inheritanceSync,
        IBaseTypeChangeLogic baseTypeChange,
        AnimationEditOperations animationOperations)
    {
        _reorder = new ReorderEditOperations(inheritanceSync, stateRemoval, propagator, nameValidator);
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
        _values = new VariableValueConverter(variableDefinedByType, typeResolver);
        _instanceRemoval = instanceRemoval;
        _childDetacher = childDetacher;
        _stateRemoval = stateRemoval;
        _variableRemoval = variableRemoval;
        _overrideRemover = overrideRemover;
        _referenceFinder = referenceFinder;
        _variableClear = variableClear;
        _componentExtraction = componentExtraction;
        _movedReferenceDrop = movedReferenceDrop;
        _elementRemoval = elementRemoval;
        _referenceValidator = referenceValidator;
        _referenceCascade = referenceCascade;
        _componentBehaviors = new ComponentBehaviorLogic(stateCreation);
        _inheritanceSync = inheritanceSync;
        _baseTypeChange = baseTypeChange;

        _operations = new Dictionary<string, OperationSpec>
        {
            ["addScreen"] = new(Strings("name"), None, AddScreen),
            ["addComponent"] = new(Strings("name"), Strings("baseType"), AddComponent),
            ["addInstance"] = new(Strings("element", "name", "type"), Strings("parent"), AddInstance),
            ["addVariable"] = new(Strings("element", "name", "type"), Fields(new EditField("value", EditFieldType.Any)), AddVariable),
            ["setVariable"] = new(Fields(Str("element"), Str("name"), new EditField("value", EditFieldType.Any)), Strings("state", "category"), SetVariable, StandardElements: true),
            ["addCategory"] = new(Strings("element", "name"), None, AddCategory, StandardElements: true),
            ["addState"] = new(Strings("element", "name"), Strings("category"), AddState, StandardElements: true),
            ["exposeVariable"] = new(Strings("element", "instance", "variable", "name"), None, ExposeVariable),
            ["unexposeVariable"] = new(Strings("element", "name"), None, UnexposeVariable),
            ["moveInstance"] = new(Strings("element", "name"), Fields(Str("before"), Str("after"),
                new EditField("toFront", EditFieldType.Bool), new EditField("toBack", EditFieldType.Bool)), _reorder.MoveInstance),
            ["moveState"] = new(Strings("element", "name"), Fields(Str("category"), Str("toCategory"), Str("before"), Str("after"),
                new EditField("toStart", EditFieldType.Bool), new EditField("toEnd", EditFieldType.Bool)), _reorder.MoveState),
            ["removeScreen"] = new(Strings("name"), None, RemoveScreen),
            ["removeComponent"] = new(Strings("name"), None, RemoveComponent),
            ["addBehavior"] = new(Strings("element", "behavior"), None, AddBehavior),
            ["removeBehavior"] = new(Strings("element", "behavior"), None, RemoveBehavior),
            ["removeInstance"] = new(Strings("element", "name"), None, RemoveInstance),
            ["removeState"] = new(Strings("element", "name"), Strings("category"), RemoveState, StandardElements: true),
            ["removeCategory"] = new(Strings("element", "name"), None, RemoveCategory, StandardElements: true),
            ["removeVariable"] = new(Strings("element", "name"), None, RemoveVariable),
            ["clearVariable"] = new(Strings("element", "name"), Strings("state", "category"), ClearVariable, StandardElements: true),
            ["extractToComponent"] = new(Fields(Str("element"), Str("instance"), Str("name"), new EditField("replace", EditFieldType.Bool)),
                None, ExtractToComponent),
            ["setVariableReferences"] = new(Fields(Str("element"), new EditField("lines", EditFieldType.StringArray)),
                Strings("instance", "state", "category"), SetVariableReferences),
            ["renameElement"] = new(Strings("name", "newName"), None, RenameElement),
            ["renameInstance"] = new(Strings("element", "name", "newName"), None, RenameInstance),
            ["renameState"] = new(Strings("element", "name", "newName"), Strings("category"), RenameState),
            ["renameCategory"] = new(Strings("element", "name", "newName"), None, RenameCategory),
            ["renameVariable"] = new(Strings("element", "name", "newName"), None, RenameVariable),
            ["renameBehavior"] = new(Strings("name", "newName"), None, RenameBehavior),
            ["setBaseType"] = new(Strings("element", "baseType"), Strings("instance"), SetBaseType),
            ["setInstanceProperty"] = new(Fields(Str("element"), Str("instance"), Str("property"), new EditField("value", EditFieldType.Bool)), None, SetInstanceProperty),
            ["setProjectProperty"] = new(Fields(Str("name"), new EditField("value", EditFieldType.Any)), None, SetProjectProperty),
            ["addAnimation"] = new(Strings("element", "name"), Fields(new EditField("loops", EditFieldType.Bool)), animationOperations.AddAnimation),
            ["removeAnimation"] = new(Strings("element", "name"), None, animationOperations.RemoveAnimation),
            ["setAnimationLoops"] = new(Fields(Str("element"), Str("animation"), new EditField("loops", EditFieldType.Bool)), None, animationOperations.SetAnimationLoops),
            ["addKeyframe"] = new(Fields(Str("element"), Str("animation"), Str("state"), new EditField("time", EditFieldType.Any)),
                Strings("interpolation", "easing"), animationOperations.AddKeyframe),
            ["removeKeyframe"] = new(Fields(Str("element"), Str("animation"), Str("state"), new EditField("time", EditFieldType.Any)), None, animationOperations.RemoveKeyframe),
        };
    }

    /// <inheritdoc/>
    public IReadOnlyList<ProjectPropertySchema> GetProjectPropertySchemas() => ProjectPropertySetter.GetSchemas();

    /// <inheritdoc/>
    public IReadOnlyList<EditOperationSchema> GetOperationSchemas() =>
        _operations.Select(pair => new EditOperationSchema(pair.Key, pair.Value.Required, pair.Value.Optional, pair.Value.StandardElements)).ToList();

    /// <inheritdoc/>
    public ProjectEditResult Apply(GumProjectSave project, string operationsJson)
    {
        StartRenames(project);
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

            if (GetWhyCannotMixRenamesAndAnimations(operations.Select(item => item.Op)) is { } whyNot)
            {
                return Fail(ProjectEditFailure.OperationFailed, whyNot);
            }

            for (int i = 0; i < operations.Count; i++)
            {
                try
                {
                    OperationSpec spec = _operations[operations[i].Op];
                    RefuseStandardElement(project, operations[i].Op, spec, operations[i].Operation);
                    spec.Apply(project, operations[i].Operation);
                }
                catch (EditException ex)
                {
                    return Fail(ProjectEditFailure.OperationFailed, $"operations[{i}] ({operations[i].Op}): {ex.Message}");
                }
            }
        }

        return new ProjectEditResult { PendingFiles = _pendingFiles };
    }

    // An op that works on instances, custom variables or variable references has nothing to do on a standard element.
    private static void RefuseStandardElement(GumProjectSave project, string op, OperationSpec spec, JsonElement operation)
    {
        if (!spec.StandardElements
            && operation.TryGetProperty("element", out JsonElement element)
            && ProjectElementFinder.Find(project, element.GetString()!) is StandardElementSave standard)
        {
            throw new EditException(
                $"{standard.Name} is a standard element, which has no instances, custom variables or variable references. Apply {op} to a Screen or Component.");
        }
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
            EditField? field = spec.Find(property.Name);
            if (field == null)
            {
                return $"{op} has an unknown field \"{property.Name}\".";
            }
            if (field.Type == EditFieldType.Bool && property.Value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                return $"{op}'s \"{property.Name}\" must be true or false.";
            }
            if (field.Type == EditFieldType.String && property.Value.ValueKind != JsonValueKind.String)
            {
                return $"{op}'s \"{property.Name}\" must be a string.";
            }
            if (field.Type == EditFieldType.StringArray && (property.Value.ValueKind != JsonValueKind.Array
                || property.Value.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String)))
            {
                return $"{op}'s \"{property.Name}\" must be an array of strings.";
            }
        }
        foreach (EditField required in spec.Required)
        {
            if (!operation.TryGetProperty(required.Name, out _))
            {
                return $"{op} needs \"{required.Name}\".";
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
        _inheritanceSync.ReplaceInheritedInstances(component);
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
        _inheritanceSync.AddInstance(element, instance);
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
        // Checked before the name rules, which refuse the same name without saying the variable can be set.
        if (_variableDefinedByType.GetIfVariableIsDefinedByType(name, element))
        {
            throw new EditException($"{element.Name} already has a variable named {name}; use setVariable to set it.");
        }
        if (!_nameValidator.IsVariableNameValid(name, element, null, out string? whyNotValid))
        {
            throw new EditException(whyNotValid);
        }

        object? value = operation.TryGetProperty("value", out JsonElement valueElement)
            ? _values.ConvertValue(valueElement, type, name)
            : _customVariables.GetDefaultValue(type);
        _customVariables.AddCustomVariable(element, name, type, value);
    }

    private void SetVariable(GumProjectSave project, JsonElement operation)
    {
        ElementSave element = GetElement(project, operation);
        string requestedName = GetString(operation, "name");
        (StateSave state, StateSaveCategory? category) = GetState(element, operation);

        (string variableName, InstanceSave? instance) = _values.ResolveVariable(element, requestedName, "set");

        string type = _values.ResolveType(_variableWriter, element, state, variableName)
            ?? throw new EditException($"The type of {requestedName} could not be found.");
        object? value = _values.ConvertValue(operation.GetProperty("value"), type, requestedName);

        Dictionary<string, object?> valuesBefore = VariableReferenceCascade.SnapshotValues(state);
        _variableWriter.SetVariable(element, state, variableName, value, instance, fallbackType: type);
        if (category != null)
        {
            _propagator.PropagateVariablesInCategory(variableName, element, category.States);
        }
        ReactToEdit(element, state, valuesBefore, instance, variableName);
    }

    // The reaction the tool runs after a typed edit, without the canvas, so no row that reads the edit is left
    // holding a stale value.
    private void ReactToEdit(ElementSave element, StateSave state, Dictionary<string, object?> valuesBefore,
        InstanceSave? instance, string qualifiedName, string? unqualifiedName = null)
    {
        // ResolveVariable returns an instance variable already qualified ("Instance.Width").
        EditedMember edit = new EditedMember(instance, unqualifiedName ?? VariableSave.GetRootName(qualifiedName), qualifiedName);
        _referenceCascade.ReactToEdit(element, state, valuesBefore, new[] { edit }, _ => null);
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

    // Like the tool's Un-expose with "clear values": the values other elements set through the exposed name go too.
    private void UnexposeVariable(GumProjectSave project, JsonElement operation)
    {
        ElementSave element = GetElement(project, operation);
        string exposedName = GetString(operation, "name");
        VariableSave variable = element.GetDefaultStateOrThrow().Variables
            .FirstOrDefault(item => item.ExposedAsName == exposedName)
            ?? throw new EditException($"{element.Name} has no variable exposed as {exposedName}.");

        VariableChangeResponse references = _referenceFinder.GetReferencesToVariable(element, variable.Name, exposedName);
        if (_exposeVariables.GetWhyCannotUnexpose(references, exposedName, out List<VariableChange> valuesToClear) is { } whyNot)
        {
            throw new EditException(whyNot);
        }

        _exposeVariables.Unexpose(element, variable);
        _overrideRemover.Remove(valuesToClear);
    }

    // An element still in use (an instance of it, or one deriving from it) is left to the error check, which names
    // what uses it, like check does.
    private void RemoveScreen(GumProjectSave project, JsonElement operation)
    {
        string name = GetString(operation, "name");
        ScreenSave screen = project.Screens.FirstOrDefault(item => item.Name == name)
            ?? throw MissingElement(project, name, "Screen");
        _elementRemoval.RemoveElement(project, screen);
    }

    private void RemoveComponent(GumProjectSave project, JsonElement operation)
    {
        string name = GetString(operation, "name");
        ComponentSave component = project.Components.FirstOrDefault(item => item.Name == name)
            ?? throw MissingElement(project, name, "Component");
        _elementRemoval.RemoveElement(project, component);
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
        _inheritanceSync.RemoveInstance(element, instance);
    }

    // Like picking another Base Type in the tool: the variables the new type lacks go, and inheriting elements follow.
    private void SetBaseType(GumProjectSave project, JsonElement operation)
    {
        ElementSave element = GetElement(project, operation);
        string baseType = GetString(operation, "baseType");
        if (GetOptionalString(operation, "instance") is { } instanceName)
        {
            InstanceSave instance = element.GetInstance(instanceName)
                ?? throw new EditException($"{element.Name} has no instance named {instanceName}.");
            if (_baseTypeChange.GetWhyCannotSetInstanceBaseType(element, instance, baseType) is { } whyNotInstance)
            {
                throw new EditException(whyNotInstance);
            }
            _baseTypeChange.SetInstanceBaseType(element, instance, baseType);
        }
        else
        {
            if (_baseTypeChange.GetWhyCannotSetElementBaseType(element, baseType) is { } whyNot)
            {
                throw new EditException(whyNot);
            }
            _baseTypeChange.SetElementBaseType(element, baseType);
        }
    }

    private void SetInstanceProperty(GumProjectSave project, JsonElement operation)
    {
        ElementSave element = GetElement(project, operation);
        string instanceName = GetString(operation, "instance");
        string property = GetString(operation, "property");
        InstanceSave instance = element.GetInstance(instanceName)
            ?? throw new EditException($"{element.Name} has no instance named {instanceName}.");
        if (_instanceProperties.GetWhyCannotSet(element, instance, property) is { } whyNot)
        {
            throw new EditException(whyNot);
        }
        _instanceProperties.Set(instance, property, operation.GetProperty("value").GetBoolean());
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
        (string variableName, InstanceSave? instance) = _values.ResolveVariable(element, requestedName, "cleared");

        VariableSave variable = state.GetVariableSave(variableName)
            ?? throw new EditException($"{element.Name}'s state {state.Name} does not set {requestedName}.");
        Dictionary<string, object?> valuesBefore = VariableReferenceCascade.SnapshotValues(state);
        _variableClear.ClearVariable(element, state, variable, isPartOfCategory: category != null);
        ReactToEdit(element, state, valuesBefore, instance, variableName);
    }

    private void AddBehavior(GumProjectSave project, JsonElement operation)
    {
        ComponentSave component = GetComponentForBehavior(project, operation);
        string behaviorName = GetString(operation, "behavior");
        if (_componentBehaviors.GetWhyCannotAdd(project, component, behaviorName) is { } whyNot)
        {
            throw new EditException(whyNot);
        }
        _componentBehaviors.AddBehavior(component, project.Behaviors.First(item => item.Name == behaviorName));
    }

    private void RemoveBehavior(GumProjectSave project, JsonElement operation)
    {
        ComponentSave component = GetComponentForBehavior(project, operation);
        string behaviorName = GetString(operation, "behavior");
        if (_componentBehaviors.GetWhyCannotRemove(component, behaviorName) is { } whyNot)
        {
            throw new EditException(whyNot);
        }
        _componentBehaviors.RemoveBehavior(component, behaviorName);
    }

    private static ComponentSave GetComponentForBehavior(GumProjectSave project, JsonElement operation) =>
        GetElement(project, operation) as ComponentSave
            ?? throw new EditException($"{GetString(operation, "element")} is a Screen; only Components have behaviors.");

    private void ExtractToComponent(GumProjectSave project, JsonElement operation)
    {
        ElementSave element = GetElement(project, operation);
        string instanceName = GetString(operation, "instance");
        string name = GetString(operation, "name");
        bool replace = operation.GetProperty("replace").GetBoolean();
        InstanceSave instance = element.GetInstance(instanceName)
            ?? throw new EditException($"{element.Name} has no instance named {instanceName}.");
        ValidateNewElementName(name);

        ComponentExtraction extraction = _componentExtraction.CreateComponent(instance, name);
        if (replace && extraction.MovedInstances.Prepend(instance)
            .Select(_instanceRemoval.GetWhyCannotRemove).FirstOrDefault(item => item != null) is { } whyNot)
        {
            throw new EditException(whyNot);
        }
        _elementCreation.AddComponent(project, extraction.Component);
        _inheritanceSync.ReplaceInheritedInstances(extraction.Component);
        if (replace)
        {
            // Runs while the moved instances are still in the element, so their references still resolve.
            _movedReferenceDrop.Drop(_movedReferenceDrop.Find(element, instance, extraction.MovedInstances, animations: null));
            InstanceSave replacement = _componentExtraction.ReplaceWithComponentInstance(extraction, moved =>
            {
                _instanceRemoval.RemoveInstance(moved, element);
                _inheritanceSync.RemoveInstance(element, moved);
            });
            _inheritanceSync.AddInstance(element, replacement);
        }
    }

    private void SetVariableReferences(GumProjectSave project, JsonElement operation)
    {
        ElementSave element = GetElement(project, operation);
        string? instanceName = GetOptionalString(operation, "instance");
        InstanceSave? instance = null;
        if (instanceName != null)
        {
            instance = element.GetInstance(instanceName)
                ?? throw new EditException($"{element.Name} has no instance named {instanceName}.");
        }
        (StateSave state, _) = GetState(element, operation);
        string listName = instance == null ? "VariableReferences" : instance.Name + ".VariableReferences";

        List<string> lines = operation.GetProperty("lines").EnumerateArray().Select(item => item.GetString()!).ToList();
        _referenceValidator.NormalizeLines(lines);
        if (_referenceValidator.GetFailures(element, instance, lines, liveRoot: null).FirstOrDefault() is { Line: { } badLine } failure)
        {
            throw new EditException($"line \"{badLine}\": {failure.Response.Message}");
        }

        state.VariableLists.RemoveAll(item => item.Name == listName);
        if (lines.Count == 0)
        {
            return;
        }
        state.VariableLists.Add(new VariableListSave<string>
        {
            Name = listName,
            Type = "string",
            Value = lines,
        });

        // The same reaction the tool runs after a typed edit, without the canvas: this state, its behaviors'
        // tool-only lines, the element's other states, the elements that read this one, and references inside
        // the instance's type.
        ReactToEdit(element, state, VariableReferenceCascade.SnapshotValues(state), instance, listName, "VariableReferences");
    }

    private void SetProjectProperty(GumProjectSave project, JsonElement operation) =>
        ProjectPropertySetter.Set(project, GetString(operation, "name"), operation.GetProperty("value"));


    #endregion

    #region Lookups and values

    /// <summary>
    /// The stored name of <paramref name="requestedName"/> on <paramref name="element"/>, and the instance it sets:
    /// <c>Instance.Var</c> for an instance's variable, or the element's own variable, where an exposed name maps to
    /// the instance variable it exposes. Fails for a name the type does not define.
    /// </summary>
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

    internal static ElementSave GetElement(GumProjectSave project, JsonElement operation)
    {
        string name = GetString(operation, "element");
        return ProjectElementFinder.Find(project, name)
            ?? throw new EditException($"The project has no Screen, Component or standard element named {name}.");
    }

    private static EditException MissingElement(GumProjectSave project, string name, string kind) =>
        new EditException(project.StandardElements.Any(item => item.Name == name)
            ? $"{name} is a standard element, which cannot be removed."
            : $"The project has no {kind} named {name}.");

    private static StateSaveCategory GetCategory(ElementSave element, string categoryName) =>
        element.Categories.FirstOrDefault(item => item.Name == categoryName)
            ?? throw new EditException(InheritedCategories.DescribeMissing(element, categoryName));

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

    internal static string GetString(JsonElement operation, string field) => operation.GetProperty(field).GetString()!;

    private static string? GetOptionalString(JsonElement operation, string field) =>
        operation.TryGetProperty(field, out JsonElement value) ? value.GetString() : null;

    #endregion
}
