using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins <see cref="GumProjectRepairLogic"/> after its extraction from <c>ProjectManager</c> (#3863,
/// part of the ADR-0005 headless-relocation effort) — a behavior-preserving move of the four
/// load-time normalization passes that only ever operated on an explicit <see cref="GumProjectSave"/>
/// parameter, so they carried no WPF/WinForms dependency in the first place.
/// </summary>
public class GumProjectRepairLogicTests
{
    private readonly GumProjectRepairLogic _repairLogic = new();

    [Fact]
    public void FixRecursiveAssignments_ForcesRecursiveInstanceToContainer()
    {
        ComponentSave component = new ComponentSave { Name = "MyComponent" };
        InstanceSave instance = new InstanceSave { Name = "SelfReferencing", BaseType = "MyComponent" };
        component.Instances.Add(instance);
        GumProjectSave gumProjectSave = new GumProjectSave();
        gumProjectSave.Components.Add(component);

        bool didChange = _repairLogic.FixRecursiveAssignments(gumProjectSave);

        didChange.ShouldBeTrue();
        instance.BaseType.ShouldBe("Container");
    }

    [Fact]
    public void FixRecursiveAssignments_ReturnsFalse_WhenNoInstanceIsRecursive()
    {
        ComponentSave component = new ComponentSave { Name = "MyComponent" };
        InstanceSave instance = new InstanceSave { Name = "Child", BaseType = "SomeOtherComponent" };
        component.Instances.Add(instance);
        GumProjectSave gumProjectSave = new GumProjectSave();
        gumProjectSave.Components.Add(component);

        bool didChange = _repairLogic.FixRecursiveAssignments(gumProjectSave);

        didChange.ShouldBeFalse();
        instance.BaseType.ShouldBe("SomeOtherComponent");
    }

    [Fact]
    public void FixSlashesInNames_ReplacesBackslashesWithForwardSlashes_InComponentAndInstanceNames()
    {
        ComponentSave component = new ComponentSave { Name = "Folder\\MyComponent" };
        InstanceSave instance = new InstanceSave { Name = "Child", BaseType = "Folder\\Base" };
        component.Instances.Add(instance);
        GumProjectSave gumProjectSave = new GumProjectSave();
        gumProjectSave.Components.Add(component);

        bool didChange = _repairLogic.FixSlashesInNames(gumProjectSave);

        didChange.ShouldBeTrue();
        component.Name.ShouldBe("Folder/MyComponent");
        instance.BaseType.ShouldBe("Folder/Base");
    }

    [Fact]
    public void FixSlashesInNames_ReturnsFalse_WhenNoNameContainsABackslash()
    {
        ComponentSave component = new ComponentSave { Name = "Folder/MyComponent" };
        GumProjectSave gumProjectSave = new GumProjectSave();
        gumProjectSave.Components.Add(component);

        bool didChange = _repairLogic.FixSlashesInNames(gumProjectSave);

        didChange.ShouldBeFalse();
    }

    [Fact]
    public void RemoveDuplicateVariables_RemovesLaterDuplicate_KeepingFirstOccurrence()
    {
        ComponentSave component = new ComponentSave { Name = "MyComponent" };
        StateSave state = new StateSave();
        component.States.Add(state);
        state.Variables.Add(new VariableSave { Name = "X", Value = 1f });
        state.Variables.Add(new VariableSave { Name = "X", Value = 2f });
        GumProjectSave gumProjectSave = new GumProjectSave();
        gumProjectSave.Components.Add(component);

        bool didChange = _repairLogic.RemoveDuplicateVariables(gumProjectSave);

        didChange.ShouldBeTrue();
        state.Variables.Count(v => v.Name == "X").ShouldBe(1);
    }

    [Fact]
    public void RemoveDuplicateVariables_ReturnsFalse_WhenNoVariableNameIsDuplicated()
    {
        ComponentSave component = new ComponentSave { Name = "MyComponent" };
        StateSave state = new StateSave();
        component.States.Add(state);
        state.Variables.Add(new VariableSave { Name = "X", Value = 1f });
        state.Variables.Add(new VariableSave { Name = "Y", Value = 2f });
        GumProjectSave gumProjectSave = new GumProjectSave();
        gumProjectSave.Components.Add(component);

        bool didChange = _repairLogic.RemoveDuplicateVariables(gumProjectSave);

        didChange.ShouldBeFalse();
    }

    [Fact]
    public void RemoveSpacesInVariables_StripsSpacesFromKnownLegacyVariableNames()
    {
        ComponentSave component = new ComponentSave { Name = "MyComponent" };
        StateSave state = new StateSave();
        component.States.Add(state);
        state.Variables.Add(new VariableSave { Name = "MyInstance.Base Type", Value = "Container" });
        GumProjectSave gumProjectSave = new GumProjectSave();
        gumProjectSave.Components.Add(component);

        bool didChange = _repairLogic.RemoveSpacesInVariables(gumProjectSave);

        didChange.ShouldBeTrue();
        state.Variables[0].Name.ShouldBe("MyInstance.BaseType");
    }

    [Fact]
    public void RemoveSpacesInVariables_ReturnsFalse_WhenNoVariableEndsWithALegacySpacedName()
    {
        ComponentSave component = new ComponentSave { Name = "MyComponent" };
        StateSave state = new StateSave();
        component.States.Add(state);
        state.Variables.Add(new VariableSave { Name = "MyInstance.Visible", Value = true });
        GumProjectSave gumProjectSave = new GumProjectSave();
        gumProjectSave.Components.Add(component);

        bool didChange = _repairLogic.RemoveSpacesInVariables(gumProjectSave);

        didChange.ShouldBeFalse();
    }
}
