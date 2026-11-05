using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using Shouldly;

namespace Gum.Presentation.Tests.ExposedForms;

/// <summary>
/// A value a container sets on an exposed Forms property reaches the inner instance's variable, for every
/// Forms property the shipped behaviors declare, at each depth of nesting and through inheritance.
/// </summary>
public class ExposedFormsPropertyResolutionTests : BaseTestClass
{

    private static object? ResolveProperty(ExposedFormsProject world, InstanceSave innermost, List<ElementWithState> stack) =>
        new RecursiveVariableFinder(innermost, stack).GetValue(world.Property.Name);

    [Theory]
    [MemberData(nameof(ExposedFormsProject.AllPairs), MemberType = typeof(ExposedFormsProject))]
    public void ValueSetOnContainerInstance_ReachesTheInnerInstance(string behavior, string property)
    {
        ExposedFormsProject world = new ExposedFormsProject(behavior, property);
        world.ExposeProperty();
        object value = world.SampleValue();
        world.Screen.DefaultState!.SetValue("RowInstance." + ExposedFormsProject.ExposedName, value, world.RowInstance, world.Property.Type);

        List<ElementWithState> stack = new() { ExposedFormsProject.Level(world.Screen), ExposedFormsProject.Level(world.Row, "RowInstance") };

        ResolveProperty(world, world.Inner, stack).ShouldBe(value);
    }

    [Theory]
    [MemberData(nameof(ExposedFormsProject.AllPairs), MemberType = typeof(ExposedFormsProject))]
    public void UnsetExposedValue_LeavesTheInnerInstanceWithoutAnOverride(string behavior, string property)
    {
        ExposedFormsProject world = new ExposedFormsProject(behavior, property);
        world.ExposeProperty();

        List<ElementWithState> stack = new() { ExposedFormsProject.Level(world.Screen), ExposedFormsProject.Level(world.Row, "RowInstance") };

        ResolveProperty(world, world.Inner, stack).ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(ExposedFormsProject.AllPairs), MemberType = typeof(ExposedFormsProject))]
    public void ValueSetOnTheInnerInstanceInsideTheRow_IsUsedWhenTheContainerSetsNothing(string behavior, string property)
    {
        ExposedFormsProject world = new ExposedFormsProject(behavior, property);
        world.ExposeProperty();
        object value = world.SampleValue();
        world.Row.DefaultState!.SetValue("Inner." + property, value, world.Inner, world.Property.Type);
        world.Row.DefaultState.GetVariableSave("Inner." + property)!.ExposedAsName.ShouldBe(ExposedFormsProject.ExposedName);

        List<ElementWithState> stack = new() { ExposedFormsProject.Level(world.Screen), ExposedFormsProject.Level(world.Row, "RowInstance") };

        ResolveProperty(world, world.Inner, stack).ShouldBe(value);
    }

    [Theory]
    [MemberData(nameof(ExposedFormsProject.AllPairs), MemberType = typeof(ExposedFormsProject))]
    public void ContainerValue_BeatsTheValueInsideTheRow(string behavior, string property)
    {
        ExposedFormsProject world = new ExposedFormsProject(behavior, property);
        world.ExposeProperty();
        object inside = world.SampleValue();
        world.Row.DefaultState!.SetValue("Inner." + property, inside, world.Inner, world.Property.Type);
        object outside = inside is string ? "Outside" : inside is bool flag ? !flag : (object)99;
        world.Screen.DefaultState!.SetValue("RowInstance." + ExposedFormsProject.ExposedName, outside, world.RowInstance, world.Property.Type);

        List<ElementWithState> stack = new() { ExposedFormsProject.Level(world.Screen), ExposedFormsProject.Level(world.Row, "RowInstance") };

        ResolveProperty(world, world.Inner, stack).ShouldBe(outside);
    }

    [Theory]
    [MemberData(nameof(ExposedFormsProject.AllPairs), MemberType = typeof(ExposedFormsProject))]
    public void NestedExposure_ValueSetAtTheOutermostLevel_ReachesTheInnermostInstance(string behavior, string property)
    {
        ExposedFormsProject world = new ExposedFormsProject(behavior, property);
        world.ExposeProperty();
        ComponentSave panel = world.AddComponent("Panel", "Container");
        InstanceSave rowInPanel = ExposedFormsProject.AddInstance(panel, "RowInPanel", world.Row.Name);
        world.Expose(panel, rowInPanel, ExposedFormsProject.ExposedName, "PanelValue");
        InstanceSave panelInstance = ExposedFormsProject.AddInstance(world.Screen, "PanelInstance", panel.Name);
        object value = world.SampleValue();
        world.Screen.DefaultState!.SetValue("PanelInstance.PanelValue", value, panelInstance, world.Property.Type);

        List<ElementWithState> stack = new()
        {
            ExposedFormsProject.Level(world.Screen),
            ExposedFormsProject.Level(panel, "PanelInstance"),
            ExposedFormsProject.Level(world.Row, "RowInPanel"),
        };

        ResolveProperty(world, world.Inner, stack).ShouldBe(value);
    }
}
