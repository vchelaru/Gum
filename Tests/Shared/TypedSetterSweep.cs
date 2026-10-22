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
/// Setting a property can also change other variables (Red changes the Red1 alias, a Color changes
/// Red/Green/Blue/Alpha); every typed variable that changes as a side effect gets its own reference row
/// and must follow too (#6410). A color-typed property that is not itself a variable (Color) is set the
/// same way and only its side effects are checked.
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

    // Turning IsFilled off clears the fill slot's color, which the Red/Green/Blue/Alpha getters (and their numbered
    // gradient forms) read back, while the authored color variables are unchanged: no alias to report.
    private static bool IsGatedRead(string variable, string reads) =>
        variable == "IsFilled" && System.Text.RegularExpressions.Regex.IsMatch(reads, "^(Red|Green|Blue|Alpha)[0-9]?$");

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
                if (property == null || IsColorType(property.PropertyType) || isExcluded?.Invoke(pair.Key, variable.Name) == true)
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
                    if (AreEqual(values[i], initial))
                    {
                        continue;
                    }
                    yield return new object[] { pair.Key, variable.Name, i, false };
                    if (includeExpressions && !IsKnownExpressionGap(variable.Name, values[i]))
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
            // Color is not a variable, but setting it changes Red, Green, Blue and Alpha (and on shapes their
            // numbered aliases), which are.
            PropertyInfo? colorProperty = GetTypedProperty(fresh.GetType(), "Color");
            if (colorProperty != null && IsColorType(colorProperty.PropertyType) && isExcluded?.Invoke(pair.Key, "Color") != true)
            {
                IReadOnlyList<object> colors = ValuesFor(colorProperty.PropertyType);
                object? initialColor = colorProperty.GetValue(fresh);
                for (int i = 0; i < colors.Count; i++)
                {
                    if (AreEqual(colors[i], initialColor))
                    {
                        continue;
                    }
                    yield return new object[] { pair.Key, "Color", i, false };
                    if (includeExpressions)
                    {
                        yield return new object[] { pair.Key, "Color", i, true };
                    }
                }
            }
        }
    }

    // FontSize is saved as an int but TextRuntime.FontSize is a float. Gum.Expressions casts every result to the saved
    // variable type, so a fractional FontSize set in code reaches a reference as an int. Carrying it would need the saved
    // type to become float (a saved-file format change), so it is a known gap of the expression path (#6414).
    private static bool IsKnownExpressionGap(string variable, object value) =>
        variable == "FontSize" && value is float fontSize && fontSize != Math.Floor(fontSize);

    private static bool IsColorType(Type type) => type.Name is "Color" or "SKColor";

    private static object ColorValue(Type type, byte r, byte g, byte b, byte a)
    {
        ConstructorInfo constructor = type.GetConstructors()
            .First(c => c.GetParameters().Length == 4 && c.GetParameters().All(p => p.ParameterType == typeof(byte)));
        return constructor.Invoke(new object[] { r, g, b, a });
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
        // Edge values: zero, negative, a fraction that is not exact in binary, a large fraction, and an int beyond what a float holds.
        if (underlying == typeof(float)) return new object[] { 3f, 7f, 100f, 0f, -2.5f, 0.1f, 123456.789f };
        if (underlying == typeof(int)) return new object[] { 5, 200, 0, 255, -1, 16777217 };
        if (underlying == typeof(bool)) return new object[] { true, false };
        if (underlying == typeof(string)) return new object[] { "changed", "", "two\nlines \"q\" {0} \\ é" };
        if (underlying.IsEnum) return Enum.GetValues(underlying).Cast<object>().ToList();
        if (IsColorType(underlying) && underlying.GetConstructors().Any(c => c.GetParameters().Length == 4 && c.GetParameters().All(p => p.ParameterType == typeof(byte))))
        {
            return new object[]
            {
                ColorValue(underlying, 11, 22, 33, 44), ColorValue(underlying, 200, 150, 100, 250),
                ColorValue(underlying, 0, 0, 0, 0), ColorValue(underlying, 255, 255, 255, 255),
            };
        }
        return Array.Empty<object>();
    }

    private static bool AreEqual(object? a, object? b)
    {
        if (a == null && b == null)
        {
            return true;
        }
        if (a is string || b is string || a is bool || b is bool || a is Enum || b is Enum || (a != null && IsColorType(a.GetType())))
        {
            return Equals(a, b);
        }
        // Exact: a reference must carry the value unchanged, not within a tolerance.
        return a != null && b != null && Convert.ToDouble(a) == Convert.ToDouble(b);
    }

    /// <summary>
    /// The typed variables other than <paramref name="variable"/> that change when <paramref name="variable"/> is
    /// set on a fresh element: its aliases (Red and Red1, Color and Red).
    /// </summary>
    private static List<string> FindAliases(Func<GraphicalUiElement> factory, string standard, string variable, object newValue)
    {
        GraphicalUiElement probe = factory();
        Dictionary<string, object?> before = new();
        foreach (VariableSave candidate in StandardElementsManager.Self.GetDefaultStateFor(standard)!.Variables)
        {
            PropertyInfo? other = GetTypedProperty(probe.GetType(), candidate.Name);
            if (other != null && candidate.Name != variable && !IsColorType(other.PropertyType))
            {
                before[candidate.Name] = other.GetValue(probe);
            }
        }
        GetTypedProperty(probe.GetType(), variable)!.SetValue(probe, newValue);
        List<string> aliases = new();
        foreach (KeyValuePair<string, object?> pair in before)
        {
            PropertyInfo other = GetTypedProperty(probe.GetType(), pair.Key)!;
            if (!AreEqual(other.GetValue(probe), pair.Value) && !IsGatedRead(variable, pair.Key))
            {
                aliases.Add(pair.Key);
            }
        }
        return aliases;
    }

    /// <summary>
    /// Sets the label's property in code and asserts a same-component reference follows: one pass for the
    /// variable itself, then one pass per alias with only that alias's row present (a getter that reads
    /// through to the variable would otherwise hide a missing report).
    /// </summary>
    public static void Run(Dictionary<string, Func<GraphicalUiElement>> factories, Func<ScreenSave, GraphicalUiElement> createRuntime,
        string standard, string variable, int valueIndex)
    {
        PropertyInfo property = GetTypedProperty(factories[standard]().GetType(), variable)!;
        object? valueForAliases = valueIndex == SetBackToNull ? ValuesFor(property.PropertyType)[0] : ValuesFor(property.PropertyType)[valueIndex];
        List<string> followed = new();
        if (!IsColorType(property.PropertyType))
        {
            followed.Add(variable);
        }
        followed.AddRange(FindAliases(factories[standard], standard, variable, valueForAliases!));
        foreach (string referenced in followed)
        {
            RunPass(factories, createRuntime, standard, variable, valueIndex, referenced);
        }
    }

    /// <summary>Builds the component with the single row <c>referenced = referenced</c>, sets the label and asserts the shadow follows.</summary>
    private static void RunPass(Dictionary<string, Func<GraphicalUiElement>> factories, Func<ScreenSave, GraphicalUiElement> createRuntime,
        string standard, string variable, int valueIndex, string referenced)
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
        rows.Value.Add($"{referenced} = {referenced}");
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

        PropertyInfo followed = GetTypedProperty(label.GetType(), referenced)!;
        // The label may normalize the value (an obsolete enum value, a clamp), so the shadow is compared with
        // what the label reads back.
        object? expected = followed.GetValue(label);
        if (referenced == variable && expected == null && newValue != null)
        {
            // The label cannot hold the value (a Blend with no named state reads back null), so nothing to follow.
            return;
        }
        object? actual = followed.GetValue(shadow);
        if (!AreEqual(actual, expected))
        {
            string how = referenced == variable ? "" : $" (a reference reading {referenced})";
            throw new Xunit.Sdk.XunitException(
                $"{standard}.{variable}{how} set to {newValue ?? "null"} (label reads {expected}), the shadow has {actual}");
        }
    }
}
