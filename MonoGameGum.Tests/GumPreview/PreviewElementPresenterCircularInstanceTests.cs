using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using GumPreview;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.GumPreview;

/// <summary>
/// A component that contains itself, directly or through another component, can arrive from a text
/// editor edit of the project. Showing it must not overflow the stack: that cannot be caught and ends
/// the whole Preview process. Kept apart from the other presenter tests because a stack overflow
/// takes the test host down with it.
/// </summary>
public class PreviewElementPresenterCircularInstanceTests : BaseTestClass
{
    private readonly GumProjectSave _project = new GumProjectSave();

    public PreviewElementPresenterCircularInstanceTests()
    {
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(_project);
        ObjectFinder.Self.GumProjectSave = _project;
    }

    public override void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
        GumService.Default.Root.Children.Clear();
        base.Dispose();
    }

    private ComponentSave AddComponent(string name)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        _project.Components.Add(component);
        return component;
    }

    [Fact]
    public void Show_ComponentContainingAnInstanceOfItself_DoesNotOverflowTheStack()
    {
        ComponentSave loop = AddComponent("Loop");
        loop.Instances.Add(new InstanceSave { Name = "Inner", BaseType = "Loop", ParentContainer = loop });

        Should.NotThrow(() => PreviewElementPresenter.Show(new PreviewSelectionMessage("Loop")));
    }

    [Fact]
    public void Show_TwoComponentsThatContainEachOther_DoesNotOverflowTheStack()
    {
        ComponentSave first = AddComponent("First");
        ComponentSave second = AddComponent("Second");
        first.Instances.Add(new InstanceSave { Name = "InnerSecond", BaseType = "Second", ParentContainer = first });
        second.Instances.Add(new InstanceSave { Name = "InnerFirst", BaseType = "First", ParentContainer = second });

        Should.NotThrow(() => PreviewElementPresenter.Show(new PreviewSelectionMessage("First")));
    }
}
