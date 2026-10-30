using System;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Managers;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;

namespace Gum.Bundle.Tests;

/// <summary>
/// A component that instantiates itself, directly or through other components, used to overflow the
/// stack in <see cref="FontReferenceCollector"/> (#7125). Each test returning at all is the proof.
/// </summary>
public class FontReferenceCollectorInstanceCycleTests : IDisposable
{
    public FontReferenceCollectorInstanceCycleTests()
    {
        StandardElementsManager.Self.Initialize();
    }

    [Fact]
    public void Collect_ShouldReturn_WhenComponentInstantiatesItself()
    {
        ComponentSave matryoshka = TestProjectBuilder.BuildComponent("Matryoshka");
        AddInstance(matryoshka, "Inner", "Matryoshka");

        Dictionary<string, BmfcSave> fonts = Collect(matryoshka, matryoshka);

        fonts.ShouldBeEmpty();
    }

    [Fact]
    public void Collect_ShouldReturn_WhenTwoComponentsInstantiateEachOther()
    {
        ComponentSave a = TestProjectBuilder.BuildComponent("A");
        ComponentSave b = TestProjectBuilder.BuildComponent("B");
        AddInstance(a, "ToB", "B");
        AddInstance(b, "ToA", "A");

        Dictionary<string, BmfcSave> fonts = Collect(a, a, b);

        fonts.ShouldBeEmpty();
    }

    [Fact]
    public void Collect_ShouldReturn_WhenCycleIsThreeComponentsDeep()
    {
        ComponentSave a = TestProjectBuilder.BuildComponent("A");
        ComponentSave b = TestProjectBuilder.BuildComponent("B");
        ComponentSave c = TestProjectBuilder.BuildComponent("C");
        AddInstance(a, "ToB", "B");
        AddInstance(b, "ToC", "C");
        AddInstance(c, "ToA", "A");

        Dictionary<string, BmfcSave> fonts = Collect(a, a, b, c);

        fonts.ShouldBeEmpty();
    }

    private static void AddInstance(ElementSave element, string name, string baseType)
    {
        element.Instances.Add(new InstanceSave { Name = name, BaseType = baseType, ParentContainer = element });
    }

    private static Dictionary<string, BmfcSave> Collect(ElementSave collected, params ComponentSave[] components)
    {
        List<ComponentSave> all = new List<ComponentSave>(components);
        if (collected is ComponentSave asComponent && !all.Contains(asComponent))
        {
            all.Add(asComponent);
        }

        GumProjectSave project = TestProjectBuilder.BuildProject(
            screens: new List<ScreenSave>(), components: all, standards: new StandardElementSave[0]);
        ObjectFinder.Self.GumProjectSave = project;

        FontReferenceCollector collector = new FontReferenceCollector(
            instance => ObjectFinder.Self.GetElementSave(instance));
        return collector.Collect(project, new[] { collected });
    }

    public void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
    }
}
