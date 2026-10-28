using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

public class ElementTreeViewManagerStaleInstanceTests : BaseTestClass
{
    [Fact]
    public void GetBehaviorOfStaleInstance_BehaviorNoLongerHoldingTheInstance_ReturnsTheBehavior()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "Clickable" };
        BehaviorInstanceSave replaced = new BehaviorInstanceSave { Name = "Label" };
        behavior.RequiredInstances.Add(new BehaviorInstanceSave { Name = "Label" });

        ElementTreeViewManager.GetBehaviorOfStaleInstance(replaced, behavior).ShouldBeSameAs(behavior);
    }

    [Fact]
    public void GetBehaviorOfStaleInstance_BehaviorStillHoldingTheInstance_ReturnsNull()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "Clickable" };
        BehaviorInstanceSave held = new BehaviorInstanceSave { Name = "Label" };
        behavior.RequiredInstances.Add(held);

        ElementTreeViewManager.GetBehaviorOfStaleInstance(held, behavior).ShouldBeNull();
    }

    [Fact]
    public void GetBehaviorOfStaleInstance_InstanceSelectedInAnElement_ReturnsNull()
    {
        ComponentSave component = new ComponentSave { Name = "Button" };
        InstanceSave removed = new InstanceSave { Name = "Label", ParentContainer = component };

        ElementTreeViewManager.GetBehaviorOfStaleInstance(removed, component).ShouldBeNull();
    }

    [Fact]
    public void GetBehaviorOfStaleInstance_NoContainerKnown_ReturnsNull()
    {
        ElementTreeViewManager.GetBehaviorOfStaleInstance(new InstanceSave { Name = "Label" }, null).ShouldBeNull();
    }
}
