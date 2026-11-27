using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices.Editing;
using Gum.Wireframe;

namespace Gum.ProjectServices.Screenshot;

/// <summary>
/// A variable value set on the rendered element for one screenshot only, as typed after <c>--set</c>
/// (<c>Width=300</c>, <c>Hud.BossState=Visible</c>, or through nested instances, <c>Hud.Bar.Visible=false</c>).
/// Nothing is written to the project.
/// </summary>
/// <param name="Name">The variable name, prefixed with the instance path (dot-separated) for an instance variable.</param>
/// <param name="Value">The value text, converted to the variable's type as <c>gumcli edit setVariable</c> does.</param>
public record ScreenshotVariableOverride(string Name, string Value)
{
    /// <summary>
    /// Splits <paramref name="text"/> at its first <c>=</c>. False when there is none or the name is empty.
    /// </summary>
    public static bool TryParse(string text, out ScreenshotVariableOverride? result)
    {
        int equals = text.IndexOf('=');
        result = equals > 0 ? new ScreenshotVariableOverride(text.Substring(0, equals).Trim(), text.Substring(equals + 1)) : null;
        return result != null;
    }

    /// <summary>
    /// Sets each override on <paramref name="element"/>, built from <paramref name="elementSave"/>, in order.
    /// Returns why the first one that cannot apply failed (an unknown instance or variable, a value that does not
    /// convert, a state its category lacks), or null when all applied.
    /// </summary>
    public static string? ApplyAll(IEnumerable<ScreenshotVariableOverride> overrides, ElementSave elementSave, GraphicalUiElement element)
    {
        VariableDefinedByTypeLogic variableDefinedByType = new VariableDefinedByTypeLogic();
        VariableValueConverter converter = new VariableValueConverter(variableDefinedByType, new DefaultTypeResolver());
        StateVariableWriter writer = new StateVariableWriter();

        foreach (ScreenshotVariableOverride variableOverride in overrides)
        {
            try
            {
                object? value = variableOverride.Convert(elementSave, variableDefinedByType, converter, writer);
                // SetProperty reports the change, so variable references that read it update as they do at runtime.
                element.SetProperty(variableOverride.Name, value);
            }
            catch (EditException ex)
            {
                return $"--set {variableOverride.Name}={variableOverride.Value}: {ex.Message}";
            }
        }
        return null;
    }

    private object? Convert(ElementSave elementSave, IVariableDefinedByTypeLogic variableDefinedByType,
        VariableValueConverter converter, IStateVariableWriter writer)
    {
        (ElementSave owner, string variableName, InstanceSave? instance) = converter.ResolveNestedVariable(elementSave, Name, "set");
        string type = converter.ResolveType(writer, owner, owner.GetDefaultStateOrThrow(), variableName)
            ?? throw new EditException($"The type of {Name} could not be found.");

        object? value = ConvertText(converter, owner, variableName, type);
        ElementSave typeElement = instance == null ? owner : ObjectFinder.Self.GetElementSave(instance.BaseType)!;
        ThrowIfUnknownState(typeElement, variableDefinedByType, VariableSave.GetRootName(variableName), value);
        return value;
    }

    // Text that reads as JSON (300, true, null) converts as that value; anything else, or a JSON value the type
    // rejects (123 for a string), converts as a string.
    private object? ConvertText(VariableValueConverter converter, ElementSave owner, string variableName, string type)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(Value);
            return converter.ConvertVariableValue(document.RootElement, owner, variableName, type, Name);
        }
        catch (JsonException)
        {
        }
        catch (EditException)
        {
        }
        return converter.ConvertVariableValue(JsonSerializer.SerializeToElement(Value), owner, variableName, type, Name);
    }

    // A state name that does not exist would render nothing different, which is easy to miss in a screenshot.
    private void ThrowIfUnknownState(ElementSave typeElement, IVariableDefinedByTypeLogic variableDefinedByType,
        string rootName, object? value)
    {
        if (value is not string stateName)
        {
            return;
        }
        DefinedVariable? defined = variableDefinedByType.GetVariablesDefinedByType(typeElement)
            .FirstOrDefault(variable => variable.Name == rootName);
        if (defined?.Category is { } category && category.States.All(state => state.Name != stateName))
        {
            throw new EditException($"{stateName} is not a state of category {category.Name}, so {Name} cannot be set.");
        }
        if (rootName == "State" && new[] { typeElement }.Concat(ObjectFinder.Self.GetBaseElements(typeElement))
                .SelectMany(inChain => inChain.States).All(state => state.Name != stateName))
        {
            throw new EditException($"{typeElement.Name} has no uncategorized state named {stateName}, so {Name} cannot be set.");
        }
    }
}
