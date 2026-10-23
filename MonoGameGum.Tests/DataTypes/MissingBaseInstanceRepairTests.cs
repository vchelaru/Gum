using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.DataTypes;

/// <summary>
/// #6597: an element that derives from another is expected to hold every base instance as a
/// DefinedByBase instance. A file that lacks one is repaired in memory on load, so the tool, gumcli
/// codegen and the runtime all see the base's children.
/// </summary>
public class MissingBaseInstanceRepairTests : BaseTestClass
{
    private static ComponentSave AddComponent(GumProjectSave project, string name, string baseType, params string[] ownInstances)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        foreach (string instanceName in ownInstances)
        {
            component.Instances.Add(new InstanceSave { Name = instanceName, BaseType = "Container", ParentContainer = component });
        }
        project.Components.Add(component);
        return component;
    }

    private static ScreenSave AddScreen(GumProjectSave project, string name, string? baseType, params string[] ownInstances)
    {
        ScreenSave screen = new ScreenSave { Name = name, BaseType = baseType };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        foreach (string instanceName in ownInstances)
        {
            screen.Instances.Add(new InstanceSave { Name = instanceName, BaseType = "Container", ParentContainer = screen });
        }
        project.Screens.Add(screen);
        return screen;
    }

    private static string Names(ElementSave element) => string.Join(",", element.Instances.Select(item => item.Name));

    [Fact]
    public void RepairMissingDefinedByBaseInstances_BaseNotInProject_LeavesElementAlone()
    {
        GumProjectSave project = new GumProjectSave();
        ComponentSave onStandard = AddComponent(project, "OnStandard", "Container", "C");
        ComponentSave onUnknown = AddComponent(project, "OnUnknown", "Elements/DoesNotExist", "D");

        project.RepairMissingDefinedByBaseInstances().ShouldBeFalse();

        Names(onStandard).ShouldBe("C");
        Names(onUnknown).ShouldBe("D");
    }

    [Fact]
    public void RepairMissingDefinedByBaseInstances_CalledTwice_SecondCallChangesNothing()
    {
        GumProjectSave project = new GumProjectSave();
        AddComponent(project, "Base", "Container", "A", "B");
        ComponentSave derived = AddComponent(project, "Derived", "Base", "C");

        project.RepairMissingDefinedByBaseInstances().ShouldBeTrue();
        project.RepairMissingDefinedByBaseInstances().ShouldBeFalse();

        derived.Instances.Count(item => item.Name == "A").ShouldBe(1);
        derived.Instances.Count(item => item.Name == "B").ShouldBe(1);
    }

    [Fact]
    public void RepairMissingDefinedByBaseInstances_CircularBaseTypes_DoesNotHang()
    {
        GumProjectSave project = new GumProjectSave();
        ComponentSave first = AddComponent(project, "First", "Second", "A");
        ComponentSave second = AddComponent(project, "Second", "First", "B");

        project.RepairMissingDefinedByBaseInstances();

        first.GetInstance("B").ShouldNotBeNull();
        second.GetInstance("A").ShouldNotBeNull();
    }

    [Fact]
    public void RepairMissingDefinedByBaseInstances_ComponentMissingSomeBaseInstances_AddsThemAsDefinedByBase()
    {
        GumProjectSave project = new GumProjectSave();
        ComponentSave baseComponent = AddComponent(project, "Base", "Container", "A", "B");
        ComponentSave derived = AddComponent(project, "Derived", "Base", "C");
        derived.Instances.Insert(0, new InstanceSave { Name = "A", BaseType = "Container", DefinedByBase = true, ParentContainer = derived });

        bool changed = project.RepairMissingDefinedByBaseInstances();

        changed.ShouldBeTrue();
        Names(derived).ShouldBe("A,C,B");
        InstanceSave repaired = derived.GetInstance("B")!;
        repaired.DefinedByBase.ShouldBeTrue();
        repaired.BaseType.ShouldBe("Container");
        repaired.ParentContainer.ShouldBeSameAs(derived);
        Names(baseComponent).ShouldBe("A,B");
        baseComponent.GetInstance("B")!.DefinedByBase.ShouldBeFalse();
    }

    [Fact]
    public void RepairMissingDefinedByBaseInstances_ComponentThreeLevelsMissingEverything_AddsEveryAncestorInstanceOnce()
    {
        GumProjectSave project = new GumProjectSave();
        AddComponent(project, "Grand", "Container", "G");
        // The parent is itself missing the grandparent's instance.
        ComponentSave parent = AddComponent(project, "Parent", "Grand", "P");
        ComponentSave child = AddComponent(project, "Child", "Parent", "C");

        project.RepairMissingDefinedByBaseInstances().ShouldBeTrue();

        Names(parent).ShouldBe("P,G");
        child.Instances.Select(item => item.Name).OrderBy(item => item).ShouldBe(new[] { "C", "G", "P" });
        child.Instances.Where(item => item.Name != "C").ShouldAllBe(item => item.DefinedByBase);
    }

    [Fact]
    public void RepairMissingDefinedByBaseInstances_NothingMissing_ReturnsFalseAndAddsNothing()
    {
        GumProjectSave project = new GumProjectSave();
        AddComponent(project, "Base", "Container", "A");
        ComponentSave derived = AddComponent(project, "Derived", "Base", "C");
        derived.Instances.Insert(0, new InstanceSave { Name = "A", BaseType = "Container", DefinedByBase = true, ParentContainer = derived });
        ScreenSave baseScreen = AddScreen(project, "BaseScreen", null, "S");
        ScreenSave derivedScreen = AddScreen(project, "DerivedScreen", baseScreen.Name, "T");
        derivedScreen.Instances.Insert(0, new InstanceSave { Name = "S", BaseType = "Container", DefinedByBase = true, ParentContainer = derivedScreen });

        project.RepairMissingDefinedByBaseInstances().ShouldBeFalse();

        Names(derived).ShouldBe("A,C");
        Names(derivedScreen).ShouldBe("S,T");
    }

    [Fact]
    public void RepairMissingDefinedByBaseInstances_ScreenMissingBaseInstance_AddsItAsDefinedByBase()
    {
        GumProjectSave project = new GumProjectSave();
        AddScreen(project, "BaseScreen", null, "A", "B");
        ScreenSave derived = AddScreen(project, "DerivedScreen", "BaseScreen", "C");

        project.RepairMissingDefinedByBaseInstances().ShouldBeTrue();

        derived.Instances.Select(item => item.Name).OrderBy(item => item).ShouldBe(new[] { "A", "B", "C" });
        derived.GetInstance("B")!.DefinedByBase.ShouldBeTrue();
        derived.GetInstance("B")!.ParentContainer.ShouldBeSameAs(derived);
        derived.GetInstance("C")!.DefinedByBase.ShouldBeFalse();
    }

    [Fact]
    public void Initialize_ComponentMissingBaseInstance_RepairsAndReportsIt()
    {
        GumProjectSave project = new GumProjectSave();
        AddComponent(project, "Base", "Container", "A");
        ComponentSave derived = AddComponent(project, "Derived", "Base", "C");
        List<string> modifications = new List<string>();

        bool changed = project.Initialize(tolerateMissingDefaultStates: true, modifications);

        changed.ShouldBeTrue();
        derived.GetInstance("A")!.DefinedByBase.ShouldBeTrue();
        modifications.ShouldContain(nameof(GumProjectSaveExtensionMethods.RepairMissingDefinedByBaseInstances));
    }
}
