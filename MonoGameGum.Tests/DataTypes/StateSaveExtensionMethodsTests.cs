using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using System.Collections.Generic;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.DataTypes;

public class StateSaveExtensionMethodsTests : BaseTestClass
{
    [Fact]
    public void GetValueRecursive_StateWithoutParentContainer_ReturnsNullForUnsetVariable()
    {
        StateSave state = new StateSave();
        state.Variables.Add(new VariableSave { Name = "Text", Type = "string", Value = "Hello", SetsValue = true });

        object? value = state.GetValueRecursive("Width");

        value.ShouldBeNull();
    }

    [Fact]
    public void GetStateSaveRecursively_BaseTypeMissingFromProject_ReturnsNull()
    {
        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
        ComponentSave component = new ComponentSave { Name = "Derived", BaseType = "MissingBase" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        ObjectFinder.Self.GumProjectSave.Components.Add(component);

        StateSave? found = component.GetStateSaveRecursively("Highlighted");

        found.ShouldBeNull();
    }

    [Fact]
    public void SetValue_InstanceWhoseBaseTypeIsMissing_AddsTheVariable()
    {
        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
        ComponentSave component = new ComponentSave { Name = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(state);
        InstanceSave instance = new InstanceSave { Name = "Orphan", BaseType = "MissingComponent", ParentContainer = component };
        component.Instances.Add(instance);
        ObjectFinder.Self.GumProjectSave.Components.Add(component);

        StateSaveExtensionMethods.SetValue(state, "Orphan.X", 5f, instance);
        StateSaveExtensionMethods.SetValue(state, "Orphan.Items", new List<string> { "a" }, instance, "string");

        state.GetValue("Orphan.X").ShouldBe(5f);
        state.GetVariableListSave("Orphan.Items").ShouldNotBeNull();
    }

    [Fact]
    public void SetValue_NameOnStateWithoutParentContainer_StoresItAsAVariable()
    {
        StateSave state = new StateSave();

        StateSaveExtensionMethods.SetValue(state, "Name", "Renamed", instanceSave: null);

        state.GetValue("Name").ShouldBe("Renamed");
    }

    [Fact]
    public void SetValue_ReservedInstanceVariableWithoutInstanceArgument_SetsItOnTheNamedInstance()
    {
        ComponentSave component = new ComponentSave { Name = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(state);
        InstanceSave instance = new InstanceSave { Name = "ButtonInstance", BaseType = "Button", ParentContainer = component };
        component.Instances.Add(instance);

        StateSaveExtensionMethods.SetValue(state, "ButtonInstance.Locked", true, instanceSave: null);

        instance.Locked.ShouldBeTrue();
    }
}
