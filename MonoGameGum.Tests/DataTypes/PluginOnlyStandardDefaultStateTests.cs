using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.DataTypes;

/// <summary>
/// A standard that only a plugin registers (RoundedRectangle, Arc, Svg...) has no built-in default.
/// A host that loads no plugin (FRB1, a bare runtime) falls back to the project's own .gutx default (#8447).
/// </summary>
public class PluginOnlyStandardDefaultStateTests : BaseTestClass
{
    private readonly Func<string, StateSave?>? _originalCustomGetDefaultState = StandardElementsManager.Self.CustomGetDefaultState;

    public PluginOnlyStandardDefaultStateTests()
    {
        StandardElementsManager.Self.CustomGetDefaultState = null;
    }

    public override void Dispose()
    {
        StandardElementsManager.Self.CustomGetDefaultState = _originalCustomGetDefaultState;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    [Fact]
    public void ElementInitialize_ShouldUseProjectStandardDefault_WhenTypeHasNoBuiltInOrCustomDefault()
    {
        GumProjectSave project = CreateProjectWithInstanceOf("RoundedRectangle", out ComponentSave component, includeStandard: true);
        VariableSave instanceVariable = component.DefaultState!.Variables[0];
        instanceVariable.Value = "5";

        bool wasModified = component.Initialize(StandardElementsManager.Self.GetDefaultStateFor("Component"));

        // The .gutx default types CornerRadius as float, so the string value is coerced through it.
        instanceVariable.Value.ShouldBe(5f);
        wasModified.ShouldBeTrue();
    }

    [Fact]
    public void ElementInitialize_ShouldThrow_WhenTypeHasNoDefaultAnywhere()
    {
        GumProjectSave project = CreateProjectWithInstanceOf("RoundedRectangle", out ComponentSave component, includeStandard: false);
        // An instance must still resolve to a standard to reach the lookup, so its base is a
        // standard element in the project that has no default state of its own.
        StandardElementSave stateless = new StandardElementSave { Name = "RoundedRectangle" };
        project.StandardElements.Add(stateless);

        Should.Throw<InvalidOperationException>(() =>
            component.Initialize(StandardElementsManager.Self.GetDefaultStateFor("Component")));
    }

    [Fact]
    public void ProjectInitialize_ShouldLeavePluginOnlyStandardUnchanged_WhenFilledFromItsOwnDefault()
    {
        GumProjectSave project = CreateProjectWithInstanceOf("RoundedRectangle", out ComponentSave component, includeStandard: true);
        StandardElementSave standard = project.StandardElements.Find(item => item.Name == "RoundedRectangle")!;
        int variableCountBefore = standard.DefaultState!.Variables.Count;
        List<string> modifications = new List<string>();

        project.Initialize(modifications: modifications);

        standard.DefaultState!.Variables.Count.ShouldBe(variableCountBefore);
        modifications.ShouldNotContain(item => item.StartsWith("Standard:RoundedRectangle"));
    }

    private static GumProjectSave CreateProjectWithInstanceOf(string standardType, out ComponentSave component, bool includeStandard)
    {
        GumProjectSave project = new GumProjectSave();
        project.Version = (int)GumProjectSave.GumxVersions.BbCodeTextEffects;

        if (includeStandard)
        {
            StandardElementSave standard = new StandardElementSave { Name = standardType };
            StateSave standardDefault = new StateSave { Name = "Default", ParentContainer = standard };
            standardDefault.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 0f, Name = "CornerRadius" });
            standardDefault.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = true, Name = "Visible" });
            standard.States.Add(standardDefault);
            project.StandardElements.Add(standard);
        }

        component = new ComponentSave { Name = "LevelListItem", BaseType = "Container" };
        StateSave componentDefault = new StateSave { Name = "Default", ParentContainer = component };
        componentDefault.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 0f, Name = "Instance.CornerRadius" });
        component.States.Add(componentDefault);
        component.Instances.Add(new InstanceSave { Name = "Instance", BaseType = standardType, ParentContainer = component });
        project.Components.Add(component);

        ObjectFinder.Self.GumProjectSave = project;
        return project;
    }
}
