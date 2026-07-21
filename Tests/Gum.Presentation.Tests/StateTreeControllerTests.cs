using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.PropertyGridHelpers;
using Gum.ToolStates;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins <see cref="StateTreeController"/>'s reactions to selection/rename/delete/variable-set
/// events after extraction from <c>MainStatePlugin</c> (issue #3926). Every method here used to be
/// an instance method on the plugin itself, reading the plugin's own private fields rather than
/// taking its dependencies as constructor parameters - none of the logic touched a WPF type, so the
/// extraction is a pure relocation (same shape as <c>AnimationTabController</c>, issue #3866).
/// </summary>
public class StateTreeControllerTests
{
    private static (StateTreeController Controller, Mock<IStateTreeViewRightClickService> RightClickService,
        Mock<ISelectedState> SelectedState, Mock<IVariableInCategoryPropagationLogic> PropagationLogic)
        CreateSut()
    {
        var rightClickService = new Mock<IStateTreeViewRightClickService>();
        var selectedState = new Mock<ISelectedState>();
        var propagationLogic = new Mock<IVariableInCategoryPropagationLogic>();

        var controller = new StateTreeController(
            rightClickService.Object,
            selectedState.Object,
            ObjectFinder.Self,
            propagationLogic.Object);

        return (controller, rightClickService, selectedState, propagationLogic);
    }

    [Fact]
    public void HandleElementSelected_PopulatesContextMenuAndRaisesTabTitleWithElementName()
    {
        var (controller, rightClickService, selectedState, _) = CreateSut();
        ComponentSave element = new() { Name = "MyComponent" };
        selectedState.SetupGet(s => s.SelectedElement).Returns(element);
        selectedState.SetupGet(s => s.SelectedStateContainer).Returns(element);
        string? raisedTitle = null;
        controller.TabTitleChanged += title => raisedTitle = title;

        controller.HandleElementSelected(element);

        rightClickService.Verify(x => x.PopulateContextMenu(), Times.Once);
        raisedTitle.ShouldBe("MyComponent States");
    }

    [Fact]
    public void HandleElementSelected_NoElement_RaisesTabTitleAsPlainStates()
    {
        var (controller, _, selectedState, _) = CreateSut();
        selectedState.SetupGet(s => s.SelectedElement).Returns((ElementSave?)null);
        selectedState.SetupGet(s => s.SelectedStateContainer).Returns((IStateContainer?)null);
        string? raisedTitle = null;
        controller.TabTitleChanged += title => raisedTitle = title;

        controller.HandleElementSelected(null);

        raisedTitle.ShouldBe("States");
    }

    [Fact]
    public void HandleStateSelected_CategorizedState_PropagatesEachVariableInThatState()
    {
        var (controller, rightClickService, selectedState, propagationLogic) = CreateSut();
        ComponentSave element = new() { Name = "MyComponent" };
        StateSaveCategory category = new() { Name = "Category" };
        StateSave state = new() { Name = "State" };
        state.Variables.Add(new VariableSave { Name = "X" });
        state.Variables.Add(new VariableSave { Name = "Y" });
        selectedState.SetupGet(s => s.SelectedElement).Returns(element);
        selectedState.SetupGet(s => s.SelectedStateCategorySave).Returns(category);
        selectedState.SetupGet(s => s.SelectedStateSave).Returns(state);

        controller.HandleStateSelected(state);

        rightClickService.Verify(x => x.PopulateContextMenu(), Times.Once);
        propagationLogic.Verify(x => x.PropagateVariablesInCategory("X", element, category), Times.Once);
        propagationLogic.Verify(x => x.PropagateVariablesInCategory("Y", element, category), Times.Once);
    }

    [Fact]
    public void HandleStateSelected_NoCategory_DoesNotPropagateVariables()
    {
        var (controller, _, selectedState, propagationLogic) = CreateSut();
        StateSave state = new() { Name = "State" };
        state.Variables.Add(new VariableSave { Name = "X" });
        selectedState.SetupGet(s => s.SelectedStateCategorySave).Returns((StateSaveCategory?)null);
        selectedState.SetupGet(s => s.SelectedStateSave).Returns(state);

        controller.HandleStateSelected(state);

        propagationLogic.Verify(
            x => x.PropagateVariablesInCategory(It.IsAny<string>(), It.IsAny<ElementSave>(), It.IsAny<StateSaveCategory>()),
            Times.Never);
    }

    [Fact]
    public void HandleTreeNodeSelected_NoBehaviorOrElementSelected_PopulatesContextMenuAndRaisesPlainStatesTitle()
    {
        var (controller, rightClickService, selectedState, _) = CreateSut();
        selectedState.SetupGet(s => s.SelectedBehavior).Returns((BehaviorSave?)null);
        selectedState.SetupGet(s => s.SelectedElement).Returns((ElementSave?)null);
        selectedState.SetupGet(s => s.SelectedStateContainer).Returns((IStateContainer?)null);
        string? raisedTitle = null;
        controller.TabTitleChanged += title => raisedTitle = title;

        controller.HandleTreeNodeSelected();

        rightClickService.Verify(x => x.PopulateContextMenu(), Times.Once);
        raisedTitle.ShouldBe("States");
    }

    [Fact]
    public void HandleTreeNodeSelected_ElementSelected_DoesNotPopulateContextMenuAgain()
    {
        var (controller, rightClickService, selectedState, _) = CreateSut();
        ComponentSave element = new() { Name = "MyComponent" };
        selectedState.SetupGet(s => s.SelectedElement).Returns(element);

        controller.HandleTreeNodeSelected();

        // RefreshTabHeaders doesn't populate the context menu on its own; that only happens when
        // neither a behavior nor an element is selected (the "root of the tree" case).
        rightClickService.Verify(x => x.PopulateContextMenu(), Times.Never);
    }

    [Fact]
    public void HandleCategoryRename_DelegatesToViewModelHandleRenameAndPopulatesContextMenu()
    {
        var (controller, rightClickService, _, _) = CreateSut();
        StateSaveCategory category = new() { Name = "Category" };
        controller.ViewModel.Categories.Add(new Gum.Plugins.InternalPlugins.StatePlugin.ViewModels.CategoryViewModel { Data = category });

        controller.HandleCategoryRename(category, "OldName");

        rightClickService.Verify(x => x.PopulateContextMenu(), Times.Once);
    }
}
