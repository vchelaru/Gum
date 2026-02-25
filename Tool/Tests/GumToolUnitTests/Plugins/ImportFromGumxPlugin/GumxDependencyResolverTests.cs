using Gum.DataTypes;
using ImportFromGumxPlugin.Services;
using Shouldly;

namespace GumToolUnitTests.Plugins.ImportFromGumxPlugin;

public class GumxDependencyResolverTests
{
    private readonly GumxDependencyResolver _resolver = new();

    [Fact]
    public void ComputeTransitive_ComponentWithNoDeps_ReturnsEmptyTransitive()
    {
        var source = CreateProject(
            Component("Button"));
        var destination = new GumProjectSave();

        var result = _resolver.ComputeTransitive(
            new[] { source.Components[0] }, source, destination);

        result.TransitiveComponents.ShouldBeEmpty();
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
