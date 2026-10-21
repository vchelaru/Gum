using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Every typed property that is also a Gum variable updates a same-component reference that reads it:
/// a component with an instance <c>Shadow</c> holding the row <c>P = P</c>, and <c>label.P = value</c>
/// in code, must reach <c>Shadow.P</c>. The variables come from the standard elements' defaults, so a
/// new variable with a typed setter that does not report is caught here (#6393).
/// </summary>
public class VariableReferenceTypedSetterTests : BaseTestClass
{
    private static readonly Dictionary<string, Func<GraphicalUiElement>> Factories = new()
    {
        ["Text"] = () => new TextRuntime(),
        ["Container"] = () => new ContainerRuntime(),
        ["Sprite"] = () => new SpriteRuntime(),
        ["NineSlice"] = () => new NineSliceRuntime(),
#pragma warning disable CS0618 // still a standard element until it is removed
        ["ColoredRectangle"] = () => new ColoredRectangleRuntime(),
#pragma warning restore CS0618
        ["Rectangle"] = () => new RectangleRuntime(),
        ["Circle"] = () => new CircleRuntime(),
        ["Polygon"] = () => new PolygonRuntime(),
    };

    // Variables with no simple typed property to assign (files, textures, state names, the parent), and
    // CurrentChainName, which keeps only the name of a chain the element actually has.
    private static readonly HashSet<string> NotTypedVariables = new()
    {
        "Parent", "State", "ContainedType", "SourceShaderFile", "SourceFile", "RenderTargetTextureSource",
        "CurrentChainName",
    };

    public VariableReferenceTypedSetterTests()
    {
        StandardElementsManager.Self.Initialize();
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
    }

    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    public static IEnumerable<object[]> Variables()
    {
        StandardElementsManager.Self.Initialize();
        foreach (KeyValuePair<string, Func<GraphicalUiElement>> pair in Factories)
        {
            string standard = pair.Key;
            Type runtimeType = pair.Value().GetType();
            StateSave defaults = StandardElementsManager.Self.GetDefaultStateFor(standard)!;
            foreach (VariableSave variable in defaults.Variables)
            {
                PropertyInfo? property = GetTypedProperty(runtimeType, variable.Name);
                if (property == null || NextValue(property.PropertyType, null, probeOnly: true) == null)
                {
                    continue;
                }
                yield return new object[] { standard, variable.Name, false };

                // Under Gum.Expressions a nullable variable is evaluated to null (#6400).
                bool isNullable = Nullable.GetUnderlyingType(property.PropertyType) != null;
                if (!isNullable)
                {
                    yield return new object[] { standard, variable.Name, true };
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
        return property != null && property.CanRead && property.CanWrite ? property : null;
    }

    // A value different from the current one. probeOnly only asks whether the type is supported.
    private static object? NextValue(Type type, object? current, bool probeOnly)
    {
        Type underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying == typeof(float))
        {
            return probeOnly ? 0f : (float)(current ?? 0f) + 3f;
        }
        if (underlying == typeof(int))
        {
            return probeOnly ? 0 : ((int)(current ?? 0) == 5 ? 6 : 5);
        }
        if (underlying == typeof(bool))
        {
            return probeOnly ? false : !(bool)(current ?? false);
        }
        if (underlying == typeof(string))
        {
            return "changed";
        }
        if (underlying.IsEnum)
        {
            return Enum.GetValues(underlying).Cast<object>().First(value => probeOnly || !Equals(value, current));
        }
        return null;
    }

    private static bool AreEqual(object? a, object? b)
    {
        if (a is string || b is string || a is bool || b is bool || a is Enum || b is Enum)
        {
            return Equals(a, b);
        }
        return a != null && b != null && Math.Abs(Convert.ToDouble(a) - Convert.ToDouble(b)) < 0.0001;
    }

    [Theory]
    [MemberData(nameof(Variables))]
    public void TypedSetAfterCreation_SameComponentReferenceFollows(string standard, string variable, bool useExpressions)
    {
        if (useExpressions)
        {
            GumExpressionService.Initialize();
        }
        GumProjectSave project = new GumProjectSave();
        foreach (KeyValuePair<string, Func<GraphicalUiElement>> pair in Factories)
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

        GraphicalUiElement root = screen.ToGraphicalUiElement();
        GraphicalUiElement label = root.GetGraphicalUiElementByName("Label1")!;
        GraphicalUiElement shadow = label.GetGraphicalUiElementByName("Shadow")!;
        PropertyInfo property = GetTypedProperty(label.GetType(), variable)!;
        object? newValue = NextValue(property.PropertyType, property.GetValue(label), probeOnly: false);

        property.SetValue(label, newValue);

        AreEqual(property.GetValue(shadow), newValue).ShouldBeTrue(
            $"{standard}.{variable} set to {newValue} on the label, the shadow has {property.GetValue(shadow)}");
    }
}
