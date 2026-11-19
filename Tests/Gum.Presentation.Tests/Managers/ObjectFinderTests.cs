using Gum.DataTypes;
using Gum.DataTypes.Variables;
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

    private static (ObjectFinder finder, ScreenSave screen, StandardElementSave standard) CreateFinderWithScreenAndStandard()
    {
        ScreenSave screen = new() { Name = "MainScreen" };
        screen.States.Add(new StateSave { Name = "Default" });
        StandardElementSave standard = new() { Name = "Text" };
        standard.States.Add(new StateSave { Name = "Default" });
        GumProjectSave project = new();
        project.Screens.Add(screen);
        project.StandardElements.Add(standard);
        return (new ObjectFinder { GumProjectSave = project }, screen, standard);
    }

    [Fact]
    public void GetContainerOf_Variable_ShouldReturnOwningElement_WithCacheEnabled()
    {
        (ObjectFinder finder, ScreenSave screen, StandardElementSave standard) = CreateFinderWithScreenAndStandard();
        VariableSave standardVariable = new() { Name = "X" };
        standard.DefaultState!.Variables.Add(standardVariable);
        StateSaveCategory category = new() { Name = "Sizes" };
        StateSave categorized = new() { Name = "Big" };
        category.States.Add(categorized);
        screen.Categories.Add(category);
        VariableSave categorizedVariable = new() { Name = "Width" };
        categorized.Variables.Add(categorizedVariable);

        finder.EnableCache();
        try
        {
            finder.GetContainerOf(standardVariable).ShouldBeSameAs(standard);
            finder.GetContainerOf(categorizedVariable).ShouldBeSameAs(screen);
            finder.GetContainerOf(new VariableSave { Name = "X" }).ShouldBeNull();
        }
        finally
        {
            finder.DisableCache();
        }
    }

    [Fact]
    public void GetContainerOf_Variable_ShouldSeeVariablesAddedOrMoved_WhileCacheIsEnabled()
    {
        (ObjectFinder finder, ScreenSave screen, StandardElementSave standard) = CreateFinderWithScreenAndStandard();
        VariableSave moved = new() { Name = "X" };
        standard.DefaultState!.Variables.Add(moved);

        finder.EnableCache();
        try
        {
            finder.GetContainerOf(moved).ShouldBeSameAs(standard);

            VariableSave added = new() { Name = "Y" };
            screen.DefaultState!.Variables.Add(added);
            finder.GetContainerOf(added).ShouldBeSameAs(screen);

            standard.DefaultState!.Variables.Remove(moved);
            screen.DefaultState!.Variables.Add(moved);
            finder.GetContainerOf(moved).ShouldBeSameAs(screen);

            screen.DefaultState!.Variables.Remove(moved);
            finder.GetContainerOf(moved).ShouldBeNull();
        }
        finally
        {
            finder.DisableCache();
        }
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
