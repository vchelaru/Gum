using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Input;
using Gum.Avalonia.Plugins.States;
using Gum.Commands;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.StatePlugin.ViewModels;
using Gum.PropertyGridHelpers;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Moq;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// Alt+Up/Down reordering a state, driven through the real Avalonia <see cref="TreeView"/> the
/// States tab renders (issue #4755: reordering a state deselects it in the Avalonia head, which
/// never happened in WPF).
/// </summary>
public class StateTreeViewTests
{
    [AvaloniaFact]
    public void MovingASelectedStateUp_KeepsItSelected()
    {
        (Window window, StateViewModel[] states, ComponentSave component) = CreateTreeWithStates(count: 2, selected: 1);
        StateViewModel stateB = states[1];

        TreeViewItem container = FindContainer(window, stateB);
        container.Focus();
        Dispatcher.UIThread.RunJobs();

        window.KeyPress(Key.Up, RawInputModifiers.Alt, PhysicalKey.ArrowUp, null);
        Dispatcher.UIThread.RunJobs();

        component.Categories[0].States[0].ShouldBe(stateB.Data);
        stateB.IsSelected.ShouldBeTrue();
        window.Close();
    }

    [AvaloniaFact]
    public void MovingASelectedStateDown_KeepsItSelected()
    {
        (Window window, StateViewModel[] states, ComponentSave component) = CreateTreeWithStates(count: 2, selected: 0);
        StateViewModel stateA = states[0];

        TreeViewItem container = FindContainer(window, stateA);
        container.Focus();
        Dispatcher.UIThread.RunJobs();

        window.KeyPress(Key.Down, RawInputModifiers.Alt, PhysicalKey.ArrowDown, null);
        Dispatcher.UIThread.RunJobs();

        component.Categories[0].States[1].ShouldBe(stateA.Data);
        stateA.IsSelected.ShouldBeTrue();
        window.Close();
    }

    [AvaloniaFact]
    public void MovingASelectedStateUpTwice_KeepsKeyboardFocusOnTheTree()
    {
        // The exact repro from #4755: with states 1..6, select 3 (index 2), Alt+Up moves it up and
        // it stays highlighted, but the second Alt+Up doesn't move it further - focus has silently
        // left the tree for the window's own chrome, so the second press never reaches the tree at all.
        (Window window, StateViewModel[] states, ComponentSave component) = CreateTreeWithStates(count: 6, selected: 2);
        StateViewModel moved = states[2];

        TreeViewItem container = FindContainer(window, moved);
        container.Focus();
        Dispatcher.UIThread.RunJobs();

        window.KeyPress(Key.Up, RawInputModifiers.Alt, PhysicalKey.ArrowUp, null);
        Dispatcher.UIThread.RunJobs();
        window.KeyPress(Key.Up, RawInputModifiers.Alt, PhysicalKey.ArrowUp, null);
        Dispatcher.UIThread.RunJobs();

        component.Categories[0].States[0].ShouldBe(moved.Data);
        moved.IsSelected.ShouldBeTrue();
        window.Close();
    }

    [AvaloniaFact]
    public void DeletingTheSelectedState_KeepsKeyboardFocusInTheTree()
    {
        // Deleting a state removes its row entirely, so unlike the reorder tests above there is no
        // longer a specific object to re-select - the tree moves selection on to a sibling itself
        // (mirroring EditCommands.AskToDeleteState/DeleteLogic.Remove), and focus needs to follow
        // whatever that turns out to be. ElementTreeViewManager already does this for the element
        // tree ("On a delete, the popup appears, which steals focus from the treeview. If we had
        // focus before, let's get it now."); the Avalonia states tree had no equivalent (#4810 follow-up).
        ComponentSave component = new ComponentSave { Name = "Button" };
        StateSaveCategory category = new StateSaveCategory { Name = "ColorCategory" };
        StateSave stateSave = new StateSave { Name = "State1", ParentContainer = component };
        StateSave sibling = new StateSave { Name = "State2", ParentContainer = component };
        category.States.Add(stateSave);
        category.States.Add(sibling);
        component.Categories.Add(category);

        Mock<ISelectedState> selectedState = new Mock<ISelectedState>();
        selectedState.SetupGet(x => x.SelectedStateContainer).Returns(component);
        selectedState.SetupProperty(x => x.SelectedStateCategorySave, category);
        selectedState.SetupProperty(x => x.SelectedStateSave, stateSave);

        Mock<IHotkeyManager> hotkeyManager = new Mock<IHotkeyManager>();
        hotkeyManager.Setup(x => x.ReorderUp).Returns(KeyCombination.Alt(GumKey.Up));
        hotkeyManager.Setup(x => x.ReorderDown).Returns(KeyCombination.Alt(GumKey.Down));
        hotkeyManager.Setup(x => x.Rename).Returns(KeyCombination.Pressed(GumKey.F2));
        hotkeyManager.Setup(x => x.Delete).Returns(KeyCombination.Pressed(GumKey.Delete));
        hotkeyManager.Setup(x => x.Copy).Returns(KeyCombination.Ctrl(GumKey.C));
        hotkeyManager.Setup(x => x.Paste).Returns(KeyCombination.Ctrl(GumKey.V));

        Mock<IStateTreeViewRightClickService> rightClickService = new Mock<IStateTreeViewRightClickService>();

        StateTreeController controller = new StateTreeController(
            rightClickService.Object, selectedState.Object, ObjectFinder.Self,
            Mock.Of<IVariableInCategoryPropagationLogic>(), Mock.Of<IDialogService>());
        controller.HandleRefreshStateTreeView();
        controller.ViewModel.SetSelectedState(stateSave);

        // Stands in for the real EditCommands.AskToDeleteState -> DeleteLogic.Remove: shows a
        // (mocked-away) confirmation, then removes the state and selects the sibling left in its place.
        rightClickService.Setup(x => x.DeleteStateClick()).Callback(() =>
        {
            category.States.Remove(stateSave);
            selectedState.Object.SelectedStateSave = sibling;
            controller.HandleRefreshStateTreeView();
            controller.ViewModel.SetSelectedState(sibling);
        });

        StateTreeKeyboardHandler keyboardHandler = new StateTreeKeyboardHandler(
            rightClickService.Object, hotkeyManager.Object, selectedState.Object, Mock.Of<ICopyPasteLogic>());

        AvaloniaStateTreeView view = new AvaloniaStateTreeView(controller.ViewModel, rightClickService.Object, keyboardHandler);
        Window window = new Window { Width = 300, Height = 400, Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        TreeView tree = window.GetVisualDescendants().OfType<TreeView>().Single();
        TreeViewItem container = window.GetVisualDescendants().OfType<TreeViewItem>()
            .Single(item => item.DataContext is StateViewModel state && state.Data == stateSave);
        container.Focus();
        Dispatcher.UIThread.RunJobs();
        container.IsFocused.ShouldBeTrue("container should be focused before Delete is pressed");

        window.KeyPress(Key.Delete, RawInputModifiers.None, PhysicalKey.Delete, null);
        Dispatcher.UIThread.RunJobs();

        category.States.Single().ShouldBe(sibling, "the delete callback should have run");
        TreeViewItem siblingRow = window.GetVisualDescendants().OfType<TreeViewItem>()
            .Single(item => item.DataContext is StateViewModel state && state.Data == sibling);
        siblingRow.IsFocused.ShouldBeTrue();
        window.Close();
    }

    private static TreeViewItem FindContainer(Window window, StateViewModel state) =>
        window.GetVisualDescendants().OfType<TreeViewItem>().Single(item => item.DataContext == state);

    private static (Window Window, StateViewModel[] States, ComponentSave Component) CreateTreeWithStates(int count, int selected)
    {
        ComponentSave component = new ComponentSave { Name = "Button" };
        StateSaveCategory category = new StateSaveCategory { Name = "ColorCategory" };
        StateSave[] stateSaves = Enumerable.Range(1, count)
            .Select(i => new StateSave { Name = $"State{i}", ParentContainer = component })
            .ToArray();
        foreach (StateSave state in stateSaves)
        {
            category.States.Add(state);
        }
        component.Categories.Add(category);
        StateSave selectedData = stateSaves[selected];

        Mock<ISelectedState> selectedState = new Mock<ISelectedState>();
        selectedState.SetupGet(x => x.SelectedStateContainer).Returns(component);
        selectedState.SetupProperty(x => x.SelectedStateCategorySave, category);
        selectedState.SetupProperty(x => x.SelectedStateSave, selectedData);

        Mock<IHotkeyManager> hotkeyManager = new Mock<IHotkeyManager>();
        hotkeyManager.Setup(x => x.ReorderUp).Returns(KeyCombination.Alt(GumKey.Up));
        hotkeyManager.Setup(x => x.ReorderDown).Returns(KeyCombination.Alt(GumKey.Down));

        Mock<ICopyPasteLogic> copyPasteLogic = new Mock<ICopyPasteLogic>();
        copyPasteLogic.Setup(x => x.CopiedData).Returns(new CopiedData());

        StateTreeController? controller = null;
        Mock<IGuiCommands> guiCommands = new Mock<IGuiCommands>();
        // The real production wiring: a state move refreshes the tree view through this event -
        // reproduced directly here rather than through the full plugin/event-bus indirection.
        guiCommands.Setup(x => x.RefreshStateTreeView()).Callback(() => controller!.HandleRefreshStateTreeView());

        StateTreeRightClickService rightClickService = new StateTreeRightClickService(
            selectedState.Object,
            Mock.Of<IElementCommands>(),
            Mock.Of<IEditCommands>(),
            Mock.Of<IDialogService>(),
            guiCommands.Object,
            Mock.Of<IFileCommands>(),
            copyPasteLogic.Object);
        StateTreeKeyboardHandler keyboardHandler = new StateTreeKeyboardHandler(
            rightClickService, hotkeyManager.Object, selectedState.Object, copyPasteLogic.Object);
        controller = new StateTreeController(
            rightClickService, selectedState.Object, ObjectFinder.Self,
            Mock.Of<IVariableInCategoryPropagationLogic>(), Mock.Of<IDialogService>());

        controller.HandleRefreshStateTreeView();
        controller.ViewModel.SetSelectedState(selectedData);

        AvaloniaStateTreeView view = new AvaloniaStateTreeView(controller.ViewModel, rightClickService, keyboardHandler);
        Window window = new Window { Width = 300, Height = 400, Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        StateViewModel[] stateVms = stateSaves
            .Select(state => controller.ViewModel.Categories[0].States.Single(item => item.Data == state))
            .ToArray();
        return (window, stateVms, component);
    }
}
