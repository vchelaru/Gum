using Gum.DataTypes;
using Gum.Managers;
using Shouldly;

namespace Gum.Presentation.Tests.Managers;

public class ObjectFinderTests : BaseTestClass
{
    [Fact]
    public void GetQualifiedElementName_ShouldUseComponentsPrefix_ForComponent()
    {
        ComponentSave component = new() { Name = "MyComp" };

        ObjectFinder.Self.GetQualifiedElementName(component).ShouldBe("Components/MyComp");
    }

    [Fact]
    public void GetQualifiedElementName_ShouldUseScreensPrefix_ForScreen()
    {
        ScreenSave screen = new() { Name = "MyScreen" };

        ObjectFinder.Self.GetQualifiedElementName(screen).ShouldBe("Screens/MyScreen");
    }

    [Fact]
    public void GetQualifiedElementName_ShouldUseStandardsPrefix_ForStandard()
    {
        StandardElementSave standard = new() { Name = "ColoredRectangle" };

        ObjectFinder.Self.GetQualifiedElementName(standard).ShouldBe("Standards/ColoredRectangle");
    }

    [Fact]
    public void UseOnThisThread_ReturnsTheGivenFinderHere_AndTheSharedOneOnOtherThreads()
    {
        GumProjectSave shared = new();
        ObjectFinder.Self.GumProjectSave = shared;
        ObjectFinder own = new() { GumProjectSave = new GumProjectSave() };

        ObjectFinder? seenElsewhere = null;
        using (ObjectFinder.UseOnThisThread(own))
        {
            ObjectFinder.Self.ShouldBeSameAs(own);
            // A dedicated thread: Task.Run(...).Wait() may inline the task on this very thread
            // when the pool has not started it yet, which would see the override.
            Thread other = new(() => seenElsewhere = ObjectFinder.Self);
            other.Start();
            other.Join();
        }

        seenElsewhere.ShouldNotBeNull().ShouldNotBeSameAs(own);
        seenElsewhere.GumProjectSave.ShouldBeSameAs(shared);
        ObjectFinder.Self.ShouldNotBeSameAs(own);
        ObjectFinder.Self.GumProjectSave.ShouldBeSameAs(shared);
    }

    [Fact]
    public void UseOnThisThread_Nested_RestoresTheOuterFinderOnDispose()
    {
        ObjectFinder outer = new();
        ObjectFinder inner = new();

        using (ObjectFinder.UseOnThisThread(outer))
        {
            using (ObjectFinder.UseOnThisThread(inner))
            {
                ObjectFinder.Self.ShouldBeSameAs(inner);
            }

            ObjectFinder.Self.ShouldBeSameAs(outer);
        }
    }
}
