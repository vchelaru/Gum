using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// #7143: the tool keeps an instance whose component was deleted, with a base type that no longer
/// exists, and a restart drops it. A reload leaves the running tree as a restart would show it.
/// </summary>
public class HotReloadDeletedComponentTests : BaseTestClass
{
    private readonly List<string> _errors = new List<string>();

    public HotReloadDeletedComponentTests()
    {
        CustomSetPropertyOnRenderable.PropertyAssignmentError += Collect;
    }

    private void Collect(string message)
    {
        if (message.Contains("Hot reload"))
        {
            _errors.Add(message);
        }
    }

    public override void Dispose()
    {
        CustomSetPropertyOnRenderable.PropertyAssignmentError -= Collect;
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static StateSave NewDefaultState(ElementSave element)
    {
        StateSave state = new StateSave { Name = "Default", ParentContainer = element };
        element.States.Add(state);
        return state;
    }

    private static ComponentSave AddComponent(GumProjectSave project, string name)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        NewDefaultState(component);
        project.Components.Add(component);
        return component;
    }

    private static void AddInstance(ElementSave owner, string name, string baseType)
    {
        owner.Instances.Add(new InstanceSave { Name = name, BaseType = baseType, ParentContainer = owner });
    }

    private static GumProjectSave NewProject()
    {
        GumProjectSave project = new GumProjectSave();
        StandardElementSave container = new StandardElementSave { Name = "Container" };
        NewDefaultState(container);
        project.StandardElements.Add(container);
        ObjectFinder.Self.GumProjectSave = project;
        return project;
    }

    private static string Describe(GraphicalUiElement element, int depth = 0)
    {
        StringBuilder text = new StringBuilder();
        text.Append(' ', depth * 2).Append(element.Name).Append(" <").Append(element.ElementSave?.Name).AppendLine(">");
        foreach (GraphicalUiElement child in element.ContainedElements.Concat(element.Children).Distinct()
                     .Where(child => child.Tag is InstanceSave))
        {
            text.Append(Describe(child, depth + 1));
        }
        return text.ToString();
    }

    private static string DescribeAFreshLoad(ScreenSave screen) =>
        Describe(screen.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false));

    [Fact]
    public void ComponentDeletedFromScreen_InstanceAndItsChildrenAreRemovedAndReportedOnce()
    {
        GumProjectSave project = NewProject();
        ComponentSave card = AddComponent(project, "Card");
        AddInstance(card, "Title", "Container");
        ScreenSave screen = new ScreenSave { Name = "Main" };
        NewDefaultState(screen);
        project.Screens.Add(screen);
        AddInstance(screen, "Card1", "Card");
        AddInstance(screen, "Heading", "Container");
        GraphicalUiElement screenGue = screen.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);

        project.Components.Remove(card);
        GumHotReloadManager.ApplyDiff(new[] { screenGue }, project, SystemManagers.Default);
        GumHotReloadManager.ApplyDiff(new[] { screenGue }, project, SystemManagers.Default);

        Describe(screenGue).ShouldBe(DescribeAFreshLoad(screen));
        screenGue.GetGraphicalUiElementByName("Card1").ShouldBeNull();
        screenGue.GetGraphicalUiElementByName("Heading").ShouldNotBeNull();
        _errors.Count.ShouldBe(1, "the second reload has nothing left to remove");
        _errors[0].ShouldContain("'Card1'");
        _errors[0].ShouldContain("'Card'");
    }

    [Fact]
    public void InstanceOfDeletedComponent_ProducesExactlyOneMessage_NotTheShownElementOneToo()
    {
        GumProjectSave project = NewProject();
        ComponentSave card = AddComponent(project, "Card");
        ScreenSave screen = new ScreenSave { Name = "Main" };
        NewDefaultState(screen);
        project.Screens.Add(screen);
        AddInstance(screen, "Card1", "Card");
        GraphicalUiElement screenGue = screen.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);

        project.Components.Remove(card);
        GumHotReloadManager.ApplyDiff(new[] { screenGue }, project, SystemManagers.Default);

        _errors.Count.ShouldBe(1);
        _errors[0].ShouldContain("It was removed");
        _errors[0].ShouldNotContain("the game is showing");
    }

    [Fact]
    public void ComponentDeletedFromAnotherComponent_InstanceInsideItIsRemoved()
    {
        GumProjectSave project = NewProject();
        ComponentSave icon = AddComponent(project, "Icon");
        ComponentSave card = AddComponent(project, "Card");
        AddInstance(card, "Icon1", "Icon");
        ScreenSave screen = new ScreenSave { Name = "Main" };
        NewDefaultState(screen);
        project.Screens.Add(screen);
        AddInstance(screen, "Card1", "Card");
        GraphicalUiElement screenGue = screen.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        screenGue.GetGraphicalUiElementByName("Card1")!.GetGraphicalUiElementByName("Icon1").ShouldNotBeNull("sanity");

        project.Components.Remove(icon);
        GumHotReloadManager.ApplyDiff(new[] { screenGue }, project, SystemManagers.Default);

        Describe(screenGue).ShouldBe(DescribeAFreshLoad(screen));
        screenGue.GetGraphicalUiElementByName("Card1")!.GetGraphicalUiElementByName("Icon1").ShouldBeNull();
    }

    [Fact]
    public void ComponentStillInTheProject_NothingIsRemovedOrReported()
    {
        GumProjectSave project = NewProject();
        AddComponent(project, "Card");
        ScreenSave screen = new ScreenSave { Name = "Main" };
        NewDefaultState(screen);
        project.Screens.Add(screen);
        AddInstance(screen, "Card1", "Card");
        GraphicalUiElement screenGue = screen.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);

        GumHotReloadManager.ApplyDiff(new[] { screenGue }, project, SystemManagers.Default);

        screenGue.GetGraphicalUiElementByName("Card1").ShouldNotBeNull();
        _errors.ShouldBeEmpty();
    }

    [Fact]
    public void ComponentMovedToAnotherFolder_InstanceIsRetypedToIt()
    {
        GumProjectSave project = NewProject();
        ComponentSave card = AddComponent(project, "Card");
        ScreenSave screen = new ScreenSave { Name = "Main" };
        NewDefaultState(screen);
        project.Screens.Add(screen);
        InstanceSave instance = new InstanceSave { Name = "Card1", BaseType = "Card", ParentContainer = screen };
        screen.Instances.Add(instance);
        GraphicalUiElement screenGue = screen.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);

        card.Name = "Cards/Card";
        instance.BaseType = "Cards/Card";
        GumHotReloadManager.ApplyDiff(new[] { screenGue }, project, SystemManagers.Default);

        Describe(screenGue).ShouldBe(DescribeAFreshLoad(screen));
        screenGue.GetGraphicalUiElementByName("Card1")!.ElementSave!.Name.ShouldBe("Cards/Card");
        _errors.ShouldBeEmpty();
    }
}
