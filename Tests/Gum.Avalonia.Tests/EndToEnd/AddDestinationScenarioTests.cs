using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Gum.Avalonia.Plugins.TreeView;
using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Services.Dialogs;
using Gum.Managers;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// Where a Standards chip adds: consecutive chip clicks keep adding to the same destination, and an
/// explicit click on a tree node (even the one already selected) makes that node the destination (#7003).
/// Driven through the real tree and palette; each parent is read back from the element's default state.
/// </summary>
[Trait("Category", "EndToEnd")]
public class AddDestinationScenarioTests
{
    #region The reported bug

    [AvaloniaFact]
    public void ClickingTheAlreadySelectedContainer_ThenAChip_AddsTheChipInsideTheContainer()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("Screen1");
        tree.Click(tree.NodeFor(screen));

        ClickChip(tree, "Container");
        InstanceSave container = Single(screen, "Container");
        tree.SelectedState.SelectedInstance.ShouldBeSameAs(container);

        tree.Click(tree.NodeFor(container));
        ClickChip(tree, "Sprite");

        ParentOf(screen, Single(screen, "Sprite")).ShouldBe(container.Name);
        tree.AssertOracles();
    }

    #endregion

    #region Chip sequences and explicit picks

    [AvaloniaFact]
    public void ConsecutiveChipClicks_AddSiblingsToTheSameDestination()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("Screen1");
        tree.Click(tree.NodeFor(screen));

        ClickChip(tree, "Container");
        ClickChip(tree, "Sprite");
        ClickChip(tree, "Circle");

        ParentOf(screen, Single(screen, "Container")).ShouldBeNull();
        ParentOf(screen, Single(screen, "Sprite")).ShouldBeNull();
        ParentOf(screen, Single(screen, "Circle")).ShouldBeNull();
    }

    [AvaloniaFact]
    public void ChipThenClickingAnotherInstance_ThenAChip_AddsInsideThatInstance()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("Screen1");
        InstanceSave other = tree.Project.AddInstance(screen, "Other", "Container");
        tree.Click(tree.NodeFor(screen));
        ClickChip(tree, "Sprite");

        tree.Click(tree.NodeFor(other));
        ClickChip(tree, "Circle");

        ParentOf(screen, Single(screen, "Circle")).ShouldBe("Other");
    }

    [AvaloniaFact]
    public void ChipThenClickingTheScreenRoot_ThenAChip_AddsAtTheScreenRoot()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("Screen1");
        tree.Click(tree.NodeFor(screen));
        ClickChip(tree, "Container");
        tree.Click(tree.NodeFor(Single(screen, "Container")));
        ClickChip(tree, "Sprite");

        tree.Click(tree.NodeFor(screen));
        ClickChip(tree, "Circle");

        ParentOf(screen, Single(screen, "Circle")).ShouldBeNull();
    }

    [AvaloniaFact]
    public void ClickingTheAlreadySelectedScreenRoot_AfterAChipInsideAContainer_AddsAtTheScreenRoot()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("Screen1");
        InstanceSave container = tree.Project.AddInstance(screen, "Container1", "Container");
        tree.Click(tree.NodeFor(container));
        ClickChip(tree, "Sprite");

        // The Sprite is selected; select the screen, add (anchors the screen), then click the screen again.
        tree.Click(tree.NodeFor(screen));
        ClickChip(tree, "Circle");
        tree.Click(tree.NodeFor(screen));
        ClickChip(tree, "Text");

        ParentOf(screen, Single(screen, "Text")).ShouldBeNull();
    }

    [AvaloniaFact]
    public void ClickingTheAlreadySelectedNestedContainer_ThenAChip_AddsInsideTheNestedContainer()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("Screen1");
        InstanceSave outer = tree.Project.AddInstance(screen, "Outer", "Container");
        tree.Click(tree.NodeFor(outer));
        ClickChip(tree, "Container");
        InstanceSave inner = screen.Instances.Single(instance => instance.Name != "Outer");
        ParentOf(screen, inner).ShouldBe("Outer");

        tree.Click(tree.NodeFor(inner));
        ClickChip(tree, "Sprite");

        ParentOf(screen, Single(screen, "Sprite")).ShouldBe(inner.Name);
    }

    [AvaloniaFact]
    public void ClickingTheAlreadySelectedComponent_ThenAChip_AddsAtTheComponentRoot()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave component = tree.Project.AddComponent("Button");
        tree.Click(tree.NodeFor(component));
        ClickChip(tree, "Container");
        tree.Click(tree.NodeFor(Single(component, "Container")));
        ClickChip(tree, "Sprite");

        tree.Click(tree.NodeFor(component));
        ClickChip(tree, "Circle");

        ParentOf(component, Single(component, "Circle")).ShouldBeNull();
    }

    [AvaloniaFact]
    public void ArrowKeyOntoAnotherNode_ThenAChip_AddsInsideThatNode()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("Screen1");
        InstanceSave first = tree.Project.AddInstance(screen, "First", "Container");
        InstanceSave second = tree.Project.AddInstance(screen, "Second", "Container");
        tree.Click(tree.NodeFor(screen));
        ClickChip(tree, "Sprite");
        tree.Click(tree.NodeFor(first));

        tree.Press(Key.Down, PhysicalKey.ArrowDown);
        tree.SelectedState.SelectedInstance.ShouldBeSameAs(second);
        ClickChip(tree, "Circle");

        ParentOf(screen, Single(screen, "Circle")).ShouldBe("Second");
    }

    [AvaloniaFact]
    public void RightClickingTheAlreadySelectedContainer_ThenAChip_AddsInsideTheContainer()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("Screen1");
        tree.Click(tree.NodeFor(screen));
        ClickChip(tree, "Container");
        InstanceSave container = Single(screen, "Container");

        tree.RightClick(tree.NodeFor(container));
        tree.Press(Key.Escape, PhysicalKey.Escape);
        ClickChip(tree, "Sprite");

        ParentOf(screen, Single(screen, "Sprite")).ShouldBe(container.Name);
    }

    [AvaloniaFact]
    public void SelectingAnInstanceFromSearchResults_ThenAChip_AddsInsideIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("Screen1");
        tree.Click(tree.NodeFor(screen));
        ClickChip(tree, "Container");
        InstanceSave container = Single(screen, "Container");

        tree.Search(container.Name);
        tree.ClickSearchResult(tree.SearchResultTexts().First(text => text.Contains(container.Name)));
        ClickChip(tree, "Sprite");

        ParentOf(screen, Single(screen, "Sprite")).ShouldBe(container.Name);
    }

    #endregion

    #region Other gestures

    [AvaloniaFact]
    public void PastingAfterClickingTheAlreadySelectedContainer_PastesInsideTheContainer()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("Screen1");
        tree.Click(tree.NodeFor(screen));
        ClickChip(tree, "Container");
        InstanceSave container = Single(screen, "Container");
        ClickChip(tree, "Sprite");
        InstanceSave sprite = Single(screen, "Sprite");
        tree.Click(tree.NodeFor(sprite));
        tree.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control);

        tree.Click(tree.NodeFor(container));
        tree.Click(tree.NodeFor(container));
        tree.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);

        InstanceSave pasted = screen.Instances.Single(instance => instance.BaseType == "Sprite" && instance != sprite);
        ParentOf(screen, pasted).ShouldBe(container.Name);
    }

    [AvaloniaFact]
    public void CtrlClickingAChipTwice_KeepsAddingToTheSameDestination()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("Screen1");
        InstanceSave container = tree.Project.AddInstance(screen, "Container1", "Container");
        tree.Click(tree.NodeFor(container));

        ClickChip(tree, "Sprite", RawInputModifiers.Control | RawInputModifiers.Shift);
        ClickChip(tree, "Circle", RawInputModifiers.Control | RawInputModifiers.Shift);

        ParentOf(screen, Single(screen, "Sprite")).ShouldBe("Container1");
        ParentOf(screen, Single(screen, "Circle")).ShouldBe("Container1");
    }

    [AvaloniaFact]
    public void UndoingTheLastAdd_ThenAChip_StillAddsToTheSameDestination()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("Screen1");
        InstanceSave container = tree.Project.AddInstance(screen, "Container1", "Container");
        tree.Click(tree.NodeFor(container));
        ClickChip(tree, "Sprite");

        tree.Undo();
        InstanceSave? selectedAfterUndo = tree.SelectedState.SelectedInstance;
        ClickChip(tree, "Circle");

        screen.Instances.Any(instance => instance.BaseType == "Sprite").ShouldBeFalse();
        Single(screen, "Circle").ParentContainer.ShouldBeSameAs(screen);
        // The add lands inside whatever the undo left selected, never in a removed instance.
        ParentOf(screen, Single(screen, "Circle")).ShouldBe(selectedAfterUndo?.Name);
    }

    [AvaloniaFact]
    public void DeletingTheAnchoredContainer_ThenAChip_DoesNotAddIntoTheDeletedContainer()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("Screen1");
        InstanceSave container = tree.Project.AddInstance(screen, "Container1", "Container");
        tree.Click(tree.NodeFor(container));
        ClickChip(tree, "Sprite");
        tree.Click(tree.NodeFor(container));
        tree.Dialogs.AnswerNext<DeleteOptionsDialogViewModel>(_ => true);
        tree.Press(Key.Delete, PhysicalKey.Delete);
        screen.Instances.Any(instance => instance.Name == "Container1").ShouldBeFalse();

        ClickChip(tree, "Circle");

        InstanceSave circle = Single(screen, "Circle");
        ParentOf(screen, circle).ShouldNotBe("Container1");
        tree.ThrowIfCrashed();
    }

    [AvaloniaFact]
    public void RenamingTheAnchoredContainer_ThenAChip_AddsInsideTheRenamedContainer()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("Screen1");
        InstanceSave container = tree.Project.AddInstance(screen, "Container1", "Container");
        tree.Click(tree.NodeFor(container));
        ClickChip(tree, "Sprite");

        tree.Click(tree.NodeFor(container));
        tree.Dialogs.AnswerNextUserString("Renamed");
        tree.Press(Key.F2, PhysicalKey.F2);
        ClickChip(tree, "Circle");

        ParentOf(screen, Single(screen, "Circle")).ShouldBe("Renamed");
    }

    [AvaloniaFact]
    public void SelectingAnInstanceInAnotherElement_AndBack_ThenAChip_AddsInsideTheInstanceSelectedLast()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("Screen1");
        InstanceSave container = tree.Project.AddInstance(screen, "Container1", "Container");
        ComponentSave component = tree.Project.AddComponent("Button");
        InstanceSave inComponent = tree.Project.AddInstance(component, "Inner", "Container");
        tree.Click(tree.NodeFor(container));
        ClickChip(tree, "Sprite");

        tree.Click(tree.NodeFor(inComponent));
        tree.Click(tree.NodeFor(container));
        ClickChip(tree, "Circle");

        ParentOf(screen, Single(screen, "Circle")).ShouldBe("Container1");
    }

    #endregion

    #region Helpers

    private static void ClickChip(ProjectTreeHarness tree, string typeName, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        // The chip is the innermost Border around the type name's TextBlock.
        Border chip = tree.Input.Window.GetVisualDescendants()
            .OfType<AvaloniaStandardsPalette>().Single()
            .GetVisualDescendants().OfType<Border>()
            .Where(border => border.GetVisualDescendants().OfType<TextBlock>().Any(text => text.Text == typeName))
            .OrderBy(border => border.GetVisualDescendants().Count())
            .First();
        tree.Input.Click(chip, modifiers);
        tree.ThrowIfCrashed();
    }

    private static InstanceSave Single(ElementSave element, string baseType) =>
        element.Instances.Single(instance => instance.BaseType == baseType);

    private static string? ParentOf(ElementSave element, InstanceSave instance) =>
        element.GetDefaultStateOrThrow().GetValue($"{instance.Name}.Parent") as string;

    #endregion
}
