using Avalonia.Headless.XUnit;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// #7005: a derived component or screen whose file lacks instances its base has gets them back on
/// load. These scenarios save a project with the derived file stripped of its base-defined
/// instances, reopen it through the tool's real load path, and check the tree, the selection, undo
/// and a second save. The plugin-failure guard fails any of them that disables the tree plugin.
/// </summary>
[Trait("Category", "EndToEnd")]
public class MissingBaseInstancesLoadScenarioTests
{
    private static ComponentSave Component(ProjectTreeHarness tree, string name) =>
        tree.Project.Project.Components.Single(component => component.Name == name);

    private static ScreenSave Screen(ProjectTreeHarness tree, string name) =>
        tree.Project.Project.Screens.Single(screen => screen.Name == name);

    private static ComponentSave AddWith(ProjectTreeHarness tree, string name, string baseType, params string[] instanceNames)
    {
        ComponentSave component = tree.Project.AddComponent(name, baseType);
        foreach (string instanceName in instanceNames)
        {
            tree.Project.AddInstance(component, instanceName, "Container");
        }
        return component;
    }

    /// <summary>Saves, then reopens the project with the base-defined instances removed from the derived elements, as a file written outside the tool leaves them.</summary>
    private static void ReloadWithoutBaseDefinedInstances(ProjectTreeHarness tree, params string[] derivedNames)
    {
        tree.SaveAll();
        foreach (string name in derivedNames)
        {
            ElementSave element = tree.Project.Project.AllElements.Single(item => item.Name == name);
            element.Instances.RemoveAll(instance => instance.DefinedByBase);
        }
        tree.SaveAll();
        tree.Project.SaveAndReload();
        tree.TreeManager.RefreshUi();
    }

    private static void AssertInstances(ProjectTreeHarness tree, ElementSave element, params string[] expectedNames)
    {
        element.Instances.Select(instance => instance.Name).ShouldBe(expectedNames);
        element.Instances.ShouldAllBe(instance => ReferenceEquals(instance.ParentContainer, element));
        foreach (InstanceSave instance in element.Instances)
        {
            // Every one has a place in its own element's sibling list.
            instance.GetSiblingsIncludingThis().IndexOf(instance).ShouldBeGreaterThanOrEqualTo(0, instance.Name);
        }
    }

    #region One level

    [AvaloniaFact]
    public void DerivedFileLackingEveryBaseInstance_ShowsThemInTheTree()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        AddWith(tree, "Base", "Container", "A", "B");
        AddWith(tree, "Derived", "Base");

        ReloadWithoutBaseDefinedInstances(tree, "Derived");

        ComponentSave derived = Component(tree, "Derived");
        AssertInstances(tree, derived, "A", "B");
        derived.Instances.ShouldAllBe(instance => instance.DefinedByBase);
        tree.ChildTexts(tree.NodeFor(derived)).ShouldBe(new[] { "A", "B" });
        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void DerivedFileLackingSomeBaseInstances_KeepsItsOwnAfterTheBaseOnes()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        AddWith(tree, "Base", "Container", "A", "B");
        ComponentSave derived = AddWith(tree, "Derived", "Base", "Own");

        ReloadWithoutBaseDefinedInstances(tree, "Derived");

        derived = Component(tree, "Derived");
        AssertInstances(tree, derived, "A", "B", "Own");
        derived.GetInstance("Own")!.DefinedByBase.ShouldBeFalse();
        tree.ChildTexts(tree.NodeFor(derived)).ShouldBe(new[] { "A", "B", "Own" });
        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void DerivedFileLackingOnlyTheMiddleBaseInstance_RestoresItInBaseOrder()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        AddWith(tree, "Base", "Container", "A", "B", "C");
        AddWith(tree, "Derived", "Base");
        tree.SaveAll();
        Component(tree, "Derived").Instances.RemoveAll(instance => instance.Name == "B");
        tree.SaveAll();

        tree.Project.SaveAndReload();
        tree.TreeManager.RefreshUi();

        ComponentSave derived = Component(tree, "Derived");
        AssertInstances(tree, derived, "A", "B", "C");
        tree.ChildTexts(tree.NodeFor(derived)).ShouldBe(new[] { "A", "B", "C" });
        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void BaseInstanceAddedAfterTheDerivedFileWasSaved_AppearsInTheDerivedOnLoad()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        AddWith(tree, "Base", "Container", "A");
        AddWith(tree, "Derived", "Base");
        tree.SaveAll();
        // The base grows a child while the derived file stays as it was.
        ComponentSave baseComponent = Component(tree, "Base");
        InstanceSave late = new InstanceSave { Name = "Late", BaseType = "Container", ParentContainer = baseComponent };
        baseComponent.Instances.Add(late);
        Component(tree, "Derived").Instances.RemoveAll(instance => instance.Name == "Late");
        tree.SaveAll();

        tree.Project.SaveAndReload();
        tree.TreeManager.RefreshUi();

        AssertInstances(tree, Component(tree, "Derived"), "A", "Late");
        tree.ChildTexts(tree.NodeFor(Component(tree, "Derived"))).ShouldBe(new[] { "A", "Late" });
        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void DerivedWithAnInstanceOfTheSameNameAsABaseInstance_IsNotDuplicated()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        AddWith(tree, "Base", "Container", "A", "B");
        ComponentSave derived = AddWith(tree, "Derived", "Base");
        tree.SaveAll();
        // The derived file keeps its own "A" that was not marked DefinedByBase.
        derived.Instances.RemoveAll(instance => instance.DefinedByBase);
        derived.Instances.Add(new InstanceSave { Name = "A", BaseType = "Container", ParentContainer = derived });
        tree.SaveAll();

        tree.Project.SaveAndReload();
        tree.TreeManager.RefreshUi();

        derived = Component(tree, "Derived");
        AssertInstances(tree, derived, "A", "B");
        derived.Instances.Count(instance => instance.Name == "A").ShouldBe(1);
        derived.GetInstance("A")!.DefinedByBase.ShouldBeFalse();
        tree.ChildTexts(tree.NodeFor(derived)).ShouldBe(new[] { "A", "B" });
        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void BaseInComponentsFolder_RepairsAFolderedDerived()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        AddWith(tree, "Controls/Base", "Container", "A");
        AddWith(tree, "Other/Derived", "Controls/Base");

        ReloadWithoutBaseDefinedInstances(tree, "Other/Derived");

        ComponentSave derived = Component(tree, "Other/Derived");
        AssertInstances(tree, derived, "A");
        tree.ChildTexts(tree.NodeFor(derived)).ShouldBe(new[] { "A" });
        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void BaseWithNoInstances_LeavesTheDerivedAlone()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        AddWith(tree, "Base", "Container");
        AddWith(tree, "Derived", "Base", "Own");

        ReloadWithoutBaseDefinedInstances(tree, "Derived");

        AssertInstances(tree, Component(tree, "Derived"), "Own");
        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void DerivedOfAStandardElement_IsUntouched()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        AddWith(tree, "Plain", "Container", "Own");

        ReloadWithoutBaseDefinedInstances(tree, "Plain");

        AssertInstances(tree, Component(tree, "Plain"), "Own");
        tree.AssertOracles();
    }

    #endregion

    #region Depth

    [AvaloniaFact]
    public void ThreeLevels_DerivedMissingEverything_GetsGrandparentThenParentInstances()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        AddWith(tree, "Grand", "Container", "G");
        AddWith(tree, "Parent", "Grand", "P");
        AddWith(tree, "Child", "Parent", "C");

        ReloadWithoutBaseDefinedInstances(tree, "Parent", "Child");

        AssertInstances(tree, Component(tree, "Parent"), "G", "P");
        AssertInstances(tree, Component(tree, "Child"), "G", "P", "C");
        tree.ChildTexts(tree.NodeFor(Component(tree, "Child"))).ShouldBe(new[] { "G", "P", "C" });
        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void ThreeLevels_OnlyTheGrandchildFileIsStripped_StillGetsBoth()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        AddWith(tree, "Grand", "Container", "G");
        AddWith(tree, "Parent", "Grand", "P");
        AddWith(tree, "Child", "Parent");

        ReloadWithoutBaseDefinedInstances(tree, "Child");

        AssertInstances(tree, Component(tree, "Parent"), "G", "P");
        AssertInstances(tree, Component(tree, "Child"), "G", "P");
        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void TwoSiblingsOfOneBase_AreBothRepaired()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        AddWith(tree, "Base", "Container", "A");
        AddWith(tree, "DerivedOne", "Base");
        AddWith(tree, "DerivedTwo", "Base", "Own");

        ReloadWithoutBaseDefinedInstances(tree, "DerivedOne", "DerivedTwo");

        AssertInstances(tree, Component(tree, "DerivedOne"), "A");
        AssertInstances(tree, Component(tree, "DerivedTwo"), "A", "Own");
        tree.AssertOracles();
    }

    #endregion

    #region Nesting and variables

    [AvaloniaFact]
    public void BaseChildNestedInAnotherBaseChild_IsNestedInTheDerivedTreeToo()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave baseComponent = AddWith(tree, "Base", "Container", "Outer", "Inner");
        baseComponent.DefaultState.SetValue("Inner.Parent", "Outer", "string");
        AddWith(tree, "Derived", "Base");

        ReloadWithoutBaseDefinedInstances(tree, "Derived");

        ComponentSave derived = Component(tree, "Derived");
        AssertInstances(tree, derived, "Outer", "Inner");
        tree.ChildTexts(tree.NodeFor(derived)).ShouldBe(new[] { "Outer" });
        tree.ChildTexts(tree.NodeFor(derived.GetInstance("Outer")!)).ShouldBe(new[] { "Inner" });
        tree.ThrowIfCrashed();
    }

    [AvaloniaFact]
    public void BaseChildNestedUnderADerivedOnlyChild_RemainsReachableAfterRepair()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        AddWith(tree, "Base", "Container", "A");
        ComponentSave derived = AddWith(tree, "Derived", "Base", "Own");
        derived.DefaultState.SetValue("A.Parent", "Own", "string");

        ReloadWithoutBaseDefinedInstances(tree, "Derived");

        derived = Component(tree, "Derived");
        AssertInstances(tree, derived, "A", "Own");
        tree.ChildTexts(tree.NodeFor(derived)).ShouldBe(new[] { "Own" });
        tree.ChildTexts(tree.NodeFor(derived.GetInstance("Own")!)).ShouldBe(new[] { "A" });
        tree.ThrowIfCrashed();
    }

    [AvaloniaFact]
    public void VariableSetOnTheInstanceInTheBase_IsStillInheritedByTheRepairedInstance()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave baseComponent = AddWith(tree, "Base", "Container", "A");
        baseComponent.DefaultState.SetValue("A.Width", 77f, "float");
        AddWith(tree, "Derived", "Base");

        ReloadWithoutBaseDefinedInstances(tree, "Derived");

        ComponentSave derived = Component(tree, "Derived");
        derived.DefaultState.GetValueRecursive("A.Width").ShouldBe(77f);
        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void StateSetOnTheInstanceInTheBase_IsStillInheritedByTheRepairedInstance()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave baseComponent = AddWith(tree, "Base", "Container", "A");
        StateSaveCategory category = tree.Project.AddCategory(baseComponent, "Size");
        StateSave big = tree.Project.AddState(baseComponent, category, "Big");
        big.SetValue("A.Width", 99f, "float");
        AddWith(tree, "Derived", "Base");

        ReloadWithoutBaseDefinedInstances(tree, "Derived");

        ComponentSave derived = Component(tree, "Derived");
        AssertInstances(tree, derived, "A");
        derived.Categories.ShouldBeEmpty();
        Component(tree, "Base").Categories.Single().States.Single().GetValue("A.Width").ShouldBe(99f);
        tree.AssertOracles();
    }

    #endregion

    #region Screens

    [AvaloniaFact]
    public void DerivedScreenLackingBaseScreenInstances_ShowsThemInTheTree()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave baseScreen = tree.Project.AddScreen("BaseScreen");
        tree.Project.AddInstance(baseScreen, "Hud", "Container");
        ScreenSave derived = tree.Project.AddScreen("DerivedScreen");
        derived.BaseType = "BaseScreen";
        tree.Project.AddInstance(derived, "Own", "Container");

        ReloadWithoutBaseDefinedInstances(tree, "DerivedScreen");

        derived = Screen(tree, "DerivedScreen");
        AssertInstances(tree, derived, "Hud", "Own");
        tree.ChildTexts(tree.NodeFor(derived)).ShouldBe(new[] { "Hud", "Own" });
        tree.AssertOracles();
    }

    #endregion

    #region After the load

    [AvaloniaFact]
    public void RepairedInstance_CanBeSelected_AndSelectsItsOwnElement()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        AddWith(tree, "Base", "Container", "A");
        AddWith(tree, "Derived", "Base");
        ReloadWithoutBaseDefinedInstances(tree, "Derived");

        InstanceSave repaired = Component(tree, "Derived").GetInstance("A")!;
        tree.Click(tree.NodeFor(repaired));

        tree.SelectedState.SelectedInstance.ShouldBeSameAs(repaired);
        tree.SelectedState.SelectedElement.ShouldBeSameAs(Component(tree, "Derived"));
        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void SavingAfterTheRepairLoad_WritesTheRepairedInstancesToTheDerivedFile()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        AddWith(tree, "Base", "Container", "A");
        AddWith(tree, "Derived", "Base");
        ReloadWithoutBaseDefinedInstances(tree, "Derived");

        tree.SaveAll();

        string file = Path.Combine(tree.Project.ProjectFolder, "Components", "Derived.gucx");
        string text = File.ReadAllText(file);
        text.ShouldContain("<Instance Name=\"A\" BaseType=\"Container\" DefinedByBase=\"true\" />");
        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void ReloadingTwice_ChangesNothingTheSecondTime()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        AddWith(tree, "Base", "Container", "A", "B");
        AddWith(tree, "Derived", "Base");
        ReloadWithoutBaseDefinedInstances(tree, "Derived");
        tree.SaveAll();
        ProjectFileSnapshot afterFirst = tree.SnapshotFiles();

        tree.Project.SaveAndReload();
        tree.TreeManager.RefreshUi();

        AssertInstances(tree, Component(tree, "Derived"), "A", "B");
        tree.SnapshotFiles().ShouldMatch(afterFirst, "a repaired project should reopen unchanged");
        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void AddingAnInstanceToTheRepairedDerived_ThenUndo_LeavesTheRepairedSet()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        AddWith(tree, "Base", "Container", "A");
        AddWith(tree, "Derived", "Base");
        ReloadWithoutBaseDefinedInstances(tree, "Derived");
        ComponentSave derived = Component(tree, "Derived");
        tree.Click(tree.NodeFor(derived));

        InstanceSave added = tree.Project.AddInstance(derived, "Extra", "Container");
        added.ParentContainer.ShouldBeSameAs(derived);
        tree.ChildTexts(tree.NodeFor(derived)).ShouldBe(new[] { "A", "Extra" });
        tree.Undo();

        AssertInstances(tree, Component(tree, "Derived"), "A");
        tree.ChildTexts(tree.NodeFor(Component(tree, "Derived"))).ShouldBe(new[] { "A" });
        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void AddingAnInstanceToTheBaseAfterTheRepair_ReachesTheRepairedDerived()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        AddWith(tree, "Base", "Container", "A");
        AddWith(tree, "Derived", "Base");
        ReloadWithoutBaseDefinedInstances(tree, "Derived");

        tree.Project.AddInstance(Component(tree, "Base"), "B", "Container");

        AssertInstances(tree, Component(tree, "Derived"), "A", "B");
        tree.ChildTexts(tree.NodeFor(Component(tree, "Derived"))).ShouldBe(new[] { "A", "B" });
        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void ChangingTheBaseTypeOfAnElementToOneWithInstances_GivesItAnOwnedCopyOfEach()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        AddWith(tree, "Base", "Container", "A", "B");
        ComponentSave other = AddWith(tree, "Other", "Container", "Own");
        tree.Click(tree.NodeFor(other));

        tree.Grid.PickComboItem("BaseType", "Base");

        other = Component(tree, "Other");
        other.Instances.Select(instance => instance.Name).ShouldBe(new[] { "A", "B", "Own" }, ignoreOrder: true);
        other.Instances.ShouldAllBe(instance => ReferenceEquals(instance.ParentContainer, other));
        other.Instances.Where(instance => instance.Name != "Own").ShouldAllBe(instance => instance.DefinedByBase);
        tree.ChildTexts(tree.NodeFor(other)).ShouldBe(new[] { "A", "B", "Own" }, ignoreOrder: true);
        tree.AssertOracles();
    }

    #endregion

    #region Odd projects

    [AvaloniaFact]
    public void DerivedWhoseBaseIsMissingFromTheProject_LoadsWithItsOwnInstances()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave orphan = AddWith(tree, "Orphan", "Container", "Own");
        tree.SaveAll();
        orphan.BaseType = "Gone";
        tree.SaveAll();

        tree.Project.SaveAndReload();
        tree.TreeManager.RefreshUi();

        AssertInstances(tree, Component(tree, "Orphan"), "Own");
        tree.ChildTexts(tree.NodeFor(Component(tree, "Orphan"))).ShouldBe(new[] { "Own" });
    }

    #endregion
}
