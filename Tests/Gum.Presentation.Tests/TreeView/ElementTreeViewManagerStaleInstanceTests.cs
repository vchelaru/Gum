using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

public class ElementTreeViewManagerStaleInstanceTests : BaseTestClass
{
    [Fact]
    public void GetContainerOfStaleInstance_BehaviorNoLongerHoldingTheInstance_ReturnsTheBehavior()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "Clickable" };
        BehaviorInstanceSave replaced = new BehaviorInstanceSave { Name = "Label" };
        behavior.RequiredInstances.Add(new BehaviorInstanceSave { Name = "Label" });

        ElementTreeViewManager.GetContainerOfStaleInstance(replaced, behavior).ShouldBeSameAs(behavior);
    }

    [Fact]
    public void GetContainerOfStaleInstance_BehaviorStillHoldingTheInstance_ReturnsNull()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "Clickable" };
        BehaviorInstanceSave held = new BehaviorInstanceSave { Name = "Label" };
        behavior.RequiredInstances.Add(held);

        ElementTreeViewManager.GetContainerOfStaleInstance(held, behavior).ShouldBeNull();
    }

    [Fact]
    public void GetContainerOfStaleInstance_ElementNoLongerHoldingTheInstance_ReturnsTheElement()
    {
        ComponentSave component = new ComponentSave { Name = "Button" };
        InstanceSave removed = new InstanceSave { Name = "Label", ParentContainer = component };

        ElementTreeViewManager.GetContainerOfStaleInstance(removed, component).ShouldBeSameAs(component);
        ElementTreeViewManager.GetContainerOfStaleInstance(removed, null).ShouldBeSameAs(component);
    }

    [Fact]
    public void GetContainerOfStaleInstance_ElementStillHoldingTheInstance_ReturnsNull()
    {
        ComponentSave component = new ComponentSave { Name = "Button" };
        InstanceSave held = new InstanceSave { Name = "Label", ParentContainer = component };
        component.Instances.Add(held);

        ElementTreeViewManager.GetContainerOfStaleInstance(held, component).ShouldBeNull();
    }

    [Fact]
    public void GetContainerOfStaleInstance_NoContainerKnown_ReturnsNull()
    {
        ElementTreeViewManager.GetContainerOfStaleInstance(new InstanceSave { Name = "Label" }, null).ShouldBeNull();
    }
}
