using Gum.DataTypes;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class InheritanceSyncTests : BaseTestClass
{
    private readonly InheritanceSync _sut = new InheritanceSync();

    [Fact]
    public void AddInstance_PlacesTheCopyWhereTheBaseHasIt()
    {
        ComponentSave baseComponent = EditingTestProject.AddComponent(Project, "Base");
        ComponentSave derived = EditingTestProject.AddComponent(Project, "Derived", "Base");
        InstanceSave a = EditingTestProject.AddInstance(baseComponent, "A", "Text");
        InstanceSave c = EditingTestProject.AddInstance(baseComponent, "C", "Text");
        _sut.AddInstance(baseComponent, a);
        _sut.AddInstance(baseComponent, c);
        InstanceSave b = new InstanceSave { Name = "B", BaseType = "Text", ParentContainer = baseComponent };
        baseComponent.Instances.Insert(1, b);

        IReadOnlyList<ElementSave> changed = _sut.AddInstance(baseComponent, b);

        changed.ShouldBe(new[] { derived });
        derived.Instances.Select(item => item.Name).ShouldBe(new[] { "A", "B", "C" });
        derived.Instances.ShouldAllBe(item => item.DefinedByBase);
    }

    [Fact]
    public void ReorderInstance_MovesTheCopyAndReportsOnlyElementsThatMoved()
    {
        ComponentSave baseComponent = EditingTestProject.AddComponent(Project, "Base");
        ComponentSave derived = EditingTestProject.AddComponent(Project, "Derived", "Base");
        InstanceSave a = EditingTestProject.AddInstance(baseComponent, "A", "Text");
        InstanceSave b = EditingTestProject.AddInstance(baseComponent, "B", "Text");
        _sut.AddInstance(baseComponent, a);
        _sut.AddInstance(baseComponent, b);
        _sut.ReorderInstance(a).ShouldBeEmpty();

        baseComponent.Instances.Remove(a);
        baseComponent.Instances.Add(a);
        IReadOnlyList<ElementSave> changed = _sut.ReorderInstance(a);

        changed.ShouldBe(new[] { derived });
        derived.Instances.Select(item => item.Name).ShouldBe(new[] { "B", "A" });
    }

    [Fact]
    public void RenameAndRemoveInstance_FollowTheNameInDerivedElements()
    {
        ComponentSave baseComponent = EditingTestProject.AddComponent(Project, "Base");
        ComponentSave derived = EditingTestProject.AddComponent(Project, "Derived", "Base");
        InstanceSave a = EditingTestProject.AddInstance(baseComponent, "A", "Text");
        _sut.AddInstance(baseComponent, a);

        a.Name = "Renamed";
        _sut.RenameInstance(baseComponent, a, "A").ShouldBe(new[] { derived });
        derived.Instances.Select(item => item.Name).ShouldBe(new[] { "Renamed" });

        _sut.RemoveInstance(baseComponent, a);
        derived.Instances.ShouldBeEmpty();
    }

    [Fact]
    public void ReplaceInheritedInstances_DropsOldInheritedAndKeepsOwnInstances()
    {
        ComponentSave oldBase = EditingTestProject.AddComponent(Project, "OldBase");
        EditingTestProject.AddInstance(oldBase, "FromOld", "Text");
        ComponentSave newBase = EditingTestProject.AddComponent(Project, "NewBase");
        EditingTestProject.AddInstance(newBase, "FromNew", "Sprite");
        ComponentSave derived = EditingTestProject.AddComponent(Project, "Derived", "OldBase");
        _sut.ReplaceInheritedInstances(derived);
        EditingTestProject.AddInstance(derived, "Own", "Text");

        derived.BaseType = "NewBase";
        _sut.ReplaceInheritedInstances(derived);

        derived.Instances.Select(item => (item.Name, item.DefinedByBase))
            .ShouldBe(new[] { ("Own", false), ("FromNew", true) });
    }

    [Fact]
    public void ReplaceInheritedInstances_WithStandardBase_LeavesOnlyOwnInstances()
    {
        ComponentSave oldBase = EditingTestProject.AddComponent(Project, "OldBase");
        EditingTestProject.AddInstance(oldBase, "FromOld", "Text");
        ComponentSave derived = EditingTestProject.AddComponent(Project, "Derived", "OldBase");
        _sut.ReplaceInheritedInstances(derived);

        derived.BaseType = "Container";
        _sut.ReplaceInheritedInstances(derived);

        derived.Instances.ShouldBeEmpty();
    }
}
