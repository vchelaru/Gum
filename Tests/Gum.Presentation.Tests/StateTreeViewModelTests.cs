using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.StatePlugin.ViewModels;
using Gum.ToolStates;
using Moq;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for StateTreeViewModel, relocated out of Gum.csproj into the
/// headless Gum.Presentation assembly (ADR-0005, #3754) once its dependency on
/// StateTreeViewRightClickService was narrowed to <see cref="IStateTreeViewRightClickService"/>.
/// That narrowing is what makes these renders testable at all: the concrete service's
/// PopulateContextMenu previously touched a real WPF ContextMenu that only got wired up in the
/// live tool.
/// </summary>
public class StateTreeViewModelTests
{
    [Fact]
    public void HandleRename_StateSave_RefreshesTitleAndPopulatesContextMenu()
    {
        Mock<IStateTreeViewRightClickService> rightClickService = new();
        Mock<ISelectedState> selectedState = new();
        StateTreeViewModel viewModel = new(rightClickService.Object, selectedState.Object);
        StateSaveCategory category = new() { Name = "Category" };
        StateSave state = new() { Name = "State" };
        category.States.Add(state);
        CategoryViewModel categoryVm = new() { Data = category };
        categoryVm.States.Add(new StateViewModel { Data = state });
        viewModel.Categories.Add(categoryVm);

        viewModel.HandleRename(state);

        rightClickService.Verify(x => x.PopulateContextMenu(), Times.Once);
    }

    [Fact]
    public void HandleRename_StateSaveCategory_PopulatesContextMenu()
    {
        Mock<IStateTreeViewRightClickService> rightClickService = new();
        Mock<ISelectedState> selectedState = new();
        StateTreeViewModel viewModel = new(rightClickService.Object, selectedState.Object);
        StateSaveCategory category = new() { Name = "Category" };
        viewModel.Categories.Add(new CategoryViewModel { Data = category });

        viewModel.HandleRename(category);

        rightClickService.Verify(x => x.PopulateContextMenu(), Times.Once);
    }
}
