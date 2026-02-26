using Gum.DataTypes;
using Gum.DataTypes.Variables;
using ImportFromGumxPlugin.Services;
using Shouldly;

namespace GumToolUnitTests.Plugins.ImportFromGumxPlugin;

public class GumxDependencyResolverTests
{
    private readonly GumxDependencyResolver _resolver = new();

    [Fact]
    public void ComputeTransitive_ChainedInheritance_ReturnsLeavesFirstOrder()
    {
        // A inherits B inherits C — topological order should be C, B
        var compC = Component("C");
        var compB = Component("B");
        compB.BaseType = "C";
        var compA = Component("A");
        compA.BaseType = "B";

        var source = CreateProject(compA, compB, compC);
        var destination = new GumProjectSave();

        var result = _resolver.ComputeTransitive(
            new[] { compA }, source, destination);

        result.TransitiveComponents.Count.ShouldBe(2);
        result.TransitiveComponents[0].Name.ShouldBe("C");
        result.TransitiveComponents[1].Name.ShouldBe("B");
    }

    [Fact]
    public void ComputeTransitive_ComponentInheritingFromAnother_IncludesBaseAsTransitive()
    {
        var baseComp = Component("BaseButton");
        var derived = Component("FancyButton");
        derived.BaseType = "BaseButton";

        var source = CreateProject(derived, baseComp);
        var destination = new GumProjectSave();

        var result = _resolver.ComputeTransitive(
            new[] { derived }, source, destination);

        result.TransitiveComponents.Count.ShouldBe(1);
        result.TransitiveComponents[0].Name.ShouldBe("BaseButton");
    }

    [Fact]
    public void ComputeTransitive_ComponentWithDependency_IncludesDependencyAsTransitive()
    {
        var source = CreateProject(
            Component("Dialog", instance: ("Background", "NineSlice")),
            Component("NineSlice"));
        var destination = new GumProjectSave();

        var result = _resolver.ComputeTransitive(
            new[] { source.Components[0] }, source, destination);

        result.TransitiveComponents.Count.ShouldBe(1);
        result.TransitiveComponents[0].Name.ShouldBe("NineSlice");
    }

    [Fact]
    public void ComputeTransitive_DependencyAlreadyInDestination_ExcludesFromTransitive()
    {
        var source = CreateProject(
            Component("Dialog", instance: ("Background", "NineSlice")),
            Component("NineSlice"));
        var destination = CreateProject(
            Component("NineSlice"));

        var result = _resolver.ComputeTransitive(
            new[] { source.Components[0] }, source, destination);

        result.TransitiveComponents.ShouldBeEmpty();
    }

    [Fact]
    public void ComputeTransitive_MixedInheritanceAndComposition_IncludesBoth()
    {
        // A inherits Base and has an instance of Widget — both should be transitive
        var baseComp = Component("Base");
        var widget = Component("Widget");
        var compA = Component("A", instance: ("w", "Widget"));
        compA.BaseType = "Base";

        var source = CreateProject(compA, baseComp, widget);
        var destination = new GumProjectSave();

        var result = _resolver.ComputeTransitive(
            new[] { compA }, source, destination);

        result.TransitiveComponents.Count.ShouldBe(2);
        var names = result.TransitiveComponents.Select(c => c.Name).ToList();
        names.ShouldContain("Base");
        names.ShouldContain("Widget");
    }

    [Fact]
    public void ComputeTransitive_MultiLevelDeps_ReturnsLeavesFirstOrder()
    {
        // A -> B -> C  — topological order should be C, B
        var source = CreateProject(
            Component("A", instance: ("b", "B")),
            Component("B", instance: ("c", "C")),
            Component("C"));
        var destination = new GumProjectSave();

        var result = _resolver.ComputeTransitive(
            new[] { source.Components[0] }, source, destination);

        result.TransitiveComponents.Count.ShouldBe(2);
        result.TransitiveComponents[0].Name.ShouldBe("C");
        result.TransitiveComponents[1].Name.ShouldBe("B");
    }

    [Fact]
    public void ComputeTransitive_StandardWithExtraCategories_IncludedAsDiffering()
    {
        // Source Text standard has a "TextColor" category; destination Text does not.
        // A component using Text should pull in the differing standard.
        var sourceText = new StandardElementSave { Name = "Text" };
        sourceText.Categories.Add(new StateSaveCategory { Name = "TextColor" });
        sourceText.States.Add(new StateSave { Name = "Default" });

        var destText = new StandardElementSave { Name = "Text" };
        destText.States.Add(new StateSave { Name = "Default" });

        var source = CreateProject(
            Component("Button", instance: ("TextInstance", "Text")));
        source.StandardElements.Add(sourceText);

        var destination = new GumProjectSave();
        destination.StandardElements.Add(destText);

        var result = _resolver.ComputeTransitive(
            new[] { source.Components[0] }, source, destination);

        result.DifferingStandards.Count.ShouldBe(1);
        result.DifferingStandards[0].Name.ShouldBe("Text");
    }

    private static GumProjectSave CreateProject(params ComponentSave[] components)
    {
        var project = new GumProjectSave();
        project.Components.AddRange(components);
        return project;
    }

    private static ComponentSave Component(string name, (string name, string baseType)? instance = null)
    {
        var component = new ComponentSave { Name = name };
        if (instance is var (instName, baseType))
        {
            component.Instances.Add(new InstanceSave { Name = instName, BaseType = baseType });
        }
        return component;
    }
}
