using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.DataTypes;

/// <summary>
/// #7468: a component that contains itself, directly or through another component, must not overflow the
/// stack when a game builds it. The cyclic instance is skipped and reported.
/// </summary>
public class CircularInstanceRuntimeTests : BaseTestClass
{
    private readonly GumProjectSave _project = new GumProjectSave();
    private readonly System.Action<string>? _previousReporter = ElementSaveExtensions.ReportPropertyAssignmentError;
    private readonly List<string> _reports = new List<string>();

    public CircularInstanceRuntimeTests()
    {
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(_project);
        ObjectFinder.Self.GumProjectSave = _project;
        ElementSaveExtensions.ReportPropertyAssignmentError = _reports.Add;
    }

    public override void Dispose()
    {
        ElementSaveExtensions.ReportPropertyAssignmentError = _previousReporter;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private ComponentSave AddComponent(string name)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        _project.Components.Add(component);
        return component;
    }

    private static void AddInstance(ComponentSave parent, string name, string baseType)
    {
        parent.Instances.Add(new InstanceSave { Name = name, BaseType = baseType, ParentContainer = parent });
    }

    [Fact]
    public void ToGraphicalUiElement_ComponentContainingItself_SkipsTheInstanceAndReports()
    {
        ComponentSave loop = AddComponent("Loop");
        AddInstance(loop, "Inner", "Loop");

        GraphicalUiElement element = loop.ToGraphicalUiElement();

        element.Children.Count.ShouldBe(0);
        _reports.ShouldHaveSingleItem().ShouldContain("Circular instance");
    }

    [Fact]
    public void ToGraphicalUiElement_TwoComponentsContainingEachOther_BuildsUntilTheCycle()
    {
        ComponentSave first = AddComponent("First");
        ComponentSave second = AddComponent("Second");
        AddInstance(first, "InnerSecond", "Second");
        AddInstance(second, "InnerFirst", "First");

        GraphicalUiElement element = first.ToGraphicalUiElement();

        GraphicalUiElement innerSecond = element.Children.Cast<GraphicalUiElement>().ShouldHaveSingleItem();
        innerSecond.Children.Count.ShouldBe(0);
        _reports.ShouldHaveSingleItem().ShouldContain("Circular instance");
    }

    [Fact]
    public void ToGraphicalUiElement_SameComponentTwiceSideBySide_IsNotCircular()
    {
        ComponentSave leaf = AddComponent("Leaf");
        ComponentSave holder = AddComponent("Holder");
        AddInstance(holder, "A", "Leaf");
        AddInstance(holder, "B", "Leaf");

        GraphicalUiElement element = holder.ToGraphicalUiElement();

        element.Children.Count.ShouldBe(2);
        _reports.ShouldBeEmpty();
    }

    [Fact]
    public void ToGraphicalUiElement_HundredLevelsOfNesting_BuildsEveryLevel()
    {
        ComponentSave previous = AddComponent("Level0");
        for (int i = 1; i <= 100; i++)
        {
            ComponentSave level = AddComponent("Level" + i);
            AddInstance(level, "Inner", previous.Name);
            previous = level;
        }

        GraphicalUiElement element = previous.ToGraphicalUiElement();

        int depth = 0;
        GraphicalUiElement current = element;
        while (current.Children.Count > 0)
        {
            current = (GraphicalUiElement)current.Children[0];
            depth++;
        }
        depth.ShouldBe(100);
        _reports.ShouldBeEmpty();
    }
}
