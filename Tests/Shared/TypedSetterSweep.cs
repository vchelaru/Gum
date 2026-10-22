using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Gum.Tests.Shared;

/// <summary>
/// Shared by the MonoGameGum and SkiaGum test projects. For every standard-element variable that has a
/// typed property, a component with an instance <c>Shadow</c> holding the row <c>P = P</c> must follow
/// <c>label.P = value</c> in code, for every enum value and a few float, int and bool values (#6393).
/// </summary>
public static class TypedSetterSweep
{
    // Variables with no simple typed property to assign (files, textures, state names, the parent), and
    // CurrentChainName, which keeps only the name of a chain the element actually has.
    private static readonly HashSet<string> NotTypedVariables = new()
    {
        "Parent", "State", "ContainedType", "SourceShaderFile", "SourceFile", "RenderTargetTextureSource",
        "CurrentChainName",
    };

    /// <summary>The value index of a case that sets a nullable variable to a value, then back to null.</summary>
    public const int SetBackToNull = -1;

    /// <summary>Rows of (standard, variable, value index, useExpressions).</summary>
    /// <param name="isExcluded">Optional (standard, variable) filter for a variable a backend cannot take part in.</param>
    public static IEnumerable<object[]> Cases(Dictionary<string, Func<GraphicalUiElement>> factories, bool includeExpressions,
        Func<string, string, bool>? isExcluded = null)
    {
        StandardElementsManager.Self.Initialize();
        foreach (KeyValuePair<string, Func<GraphicalUiElement>> pair in factories)
        {
            GraphicalUiElement fresh = pair.Value();
            StateSave defaults = StandardElementsManager.Self.GetDefaultStateFor(pair.Key)!;
            foreach (VariableSave variable in defaults.Variables)
            {
                PropertyInfo? property = GetTypedProperty(fresh.GetType(), variable.Name);
                if (property == null || isExcluded?.Invoke(pair.Key, variable.Name) == true)
                {
                    continue;
                }
                IReadOnlyList<object> values = ValuesFor(property.PropertyType);
                object? initial = property.GetValue(fresh);
                // Only a variable whose type is nullable (float?, int?) can hold null. Blend is a nullable property
                // of a non-nullable variable, where null means no named blend and a reference keeps its value.
                bool isNullable = variable.Type?.EndsWith("?") == true && Nullable.GetUnderlyingType(property.PropertyType) != null;
                for (int i = 0; i < values.Count; i++)
                {
                    if (AreEqual(values[i], initial) || !IsRepresentable(variable.Name, values[i]))
                    {
                        continue;
                    }
                    yield return new object[] { pair.Key, variable.Name, i, false };
                    if (includeExpressions)
                    {
                        yield return new object[] { pair.Key, variable.Name, i, true };
                    }
                }
                // A nullable variable is also set to a value and then back to null (#6400).
                if (isNullable)
                {
                    yield return new object[] { pair.Key, variable.Name, SetBackToNull, false };
                    if (includeExpressions)
                    {
                        yield return new object[] { pair.Key, variable.Name, SetBackToNull, true };
                    }
                }
            }
        }
    }

    private static PropertyInfo? GetTypedProperty(Type runtimeType, string variableName)
    {
        if (variableName.Contains('.') || NotTypedVariables.Contains(variableName))
        {
            return null;
        }
        PropertyInfo? property = runtimeType.GetProperty(variableName.Replace(" ", ""), BindingFlags.Public | BindingFlags.Instance);
        if (property == null || !property.CanRead || !property.CanWrite)
        {
            return null;
        }
        return ValuesFor(property.PropertyType).Count == 0 ? null : property;
    }

    private static IReadOnlyList<object> ValuesFor(Type type)
    {
        Type underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying == typeof(float)) return new object[] { 3f, 7f, 100f };
        if (underlying == typeof(int)) return new object[] { 5, 200 };
        if (underlying == typeof(bool)) return new object[] { true, false };
        if (underlying == typeof(string)) return new object[] { "changed" };
        if (underlying.IsEnum) return Enum.GetValues(underlying).Cast<object>().ToList();
        return Array.Empty<object>();
    }

    // XUnits and YUnits are GeneralUnitType properties but PositionUnitType variables. The dimension-only units
    // (PercentageOfFile, PercentageOfOtherDimension, MaintainFileAspectRatio) and, on X, the obsolete
    // PixelsFromMiddleInverted have no PositionUnitType value, so a reference cannot carry them.
    private static bool IsRepresentable(string variableName, object value)
    {
        if (value is not Gum.Converters.GeneralUnitType generalUnit || (variableName != "XUnits" && variableName != "YUnits"))
        {
            return true;
        }
        bool isXAxis = variableName == "XUnits";
        return Gum.Converters.UnitConverter.ConvertToGeneralUnit(Gum.Converters.UnitConverter.ConvertToPositionUnit(generalUnit, isXAxis)) == generalUnit;
    }

    private static bool AreEqual(object? a, object? b)
    {
        if (a == null && b == null)
        {
            return true;
        }
        if (a is string || b is string || a is bool || b is bool || a is Enum || b is Enum)
        {
            return Equals(a, b);
        }
        return a != null && b != null && Math.Abs(Convert.ToDouble(a) - Convert.ToDouble(b)) < 0.0001;
    }

    /// <summary>Builds the component, sets the label's property in code and asserts the shadow follows.</summary>
    public static void Run(Dictionary<string, Func<GraphicalUiElement>> factories, Func<ScreenSave, GraphicalUiElement> createRuntime,
        string standard, string variable, int valueIndex)
    {
        GumProjectSave project = new GumProjectSave();
        foreach (KeyValuePair<string, Func<GraphicalUiElement>> pair in factories)
        {
            StandardElementSave element = new StandardElementSave { Name = pair.Key };
            element.States.Add(StandardElementsManager.Self.GetDefaultStateFor(pair.Key)!);
            element.DefaultState!.ParentContainer = element;
            project.StandardElements.Add(element);
            ElementSaveExtensions.RegisterGueInstantiation(pair.Key, pair.Value);
        }
        ObjectFinder.Self.GumProjectSave = project;

        ComponentSave component = new ComponentSave { Name = "Label", BaseType = standard };
        StateSave state = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(state);
        component.Instances.Add(new InstanceSave { Name = "Shadow", BaseType = standard, ParentContainer = component });
        VariableListSave<string> rows = new VariableListSave<string> { Type = "string", Name = "Shadow.VariableReferences" };
        rows.Value.Add($"{variable} = {variable}");
        state.VariableLists.Add(rows);
        project.Components.Add(component);
        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Label1", BaseType = "Label", ParentContainer = screen });
        project.Screens.Add(screen);

        GraphicalUiElement root = createRuntime(screen);
        GraphicalUiElement label = root.GetGraphicalUiElementByName("Label1")!;
        GraphicalUiElement shadow = label.GetGraphicalUiElementByName("Shadow")!;
        PropertyInfo property = GetTypedProperty(label.GetType(), variable)!;
        object? newValue;
        if (valueIndex == SetBackToNull)
        {
            property.SetValue(label, ValuesFor(property.PropertyType)[0]);
            newValue = null;
        }
        else
        {
            newValue = ValuesFor(property.PropertyType)[valueIndex];
        }

        property.SetValue(label, newValue);

        // The label may normalize the value (an obsolete enum value, a clamp), so the shadow is compared with
        // what the label reads back.
        object? expected = property.GetValue(label);
        if (expected == null && newValue != null)
        {
            // The label cannot hold the value (a Blend with no named state reads back null), so nothing to follow.
            return;
        }
        object? actual = property.GetValue(shadow);
        if (!AreEqual(actual, expected))
        {
            throw new Xunit.Sdk.XunitException(
                $"{standard}.{variable} set to {newValue} (label reads {expected}), the shadow has {actual}");
        }
    }
}
