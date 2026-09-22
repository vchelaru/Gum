using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Moq;
using Shouldly;
using StateAnimationPlugin;
using StateAnimationPlugin.Managers;
using StateAnimationPlugin.ViewModels;

namespace Gum.Presentation.Tests.Plugins.StateAnimationPlugin;

/// <summary>
/// The controller behind the Animations tab, over the view model it swaps in per selected element.
/// </summary>
public class AnimationTabControllerTests
{
    [Fact]
    public void RefreshViewModel_ForAnotherElement_StopsTheReplacedViewModelsPlayback()
    {
        // Each element gets its own view model with its own timer; a replaced one that kept
        // playing would tick forever with nothing on screen to stop it.
        ComponentSave button = new ComponentSave { Name = "Button" };
        ComponentSave other = new ComponentSave { Name = "Other" };
        ElementSave? selected = button;
        Mock<ISelectedState> selectedState = new Mock<ISelectedState>();
        selectedState.SetupGet(state => state.SelectedElement).Returns(() => selected);
        Mock<IProjectState> projectState = new Mock<IProjectState>();
        projectState.SetupGet(state => state.GumProjectSave).Returns(new GumProjectSave { FullFileName = "/project/Project.gumx" });
        Dictionary<ElementSave, Mock<IUiTimer>> timers = new Dictionary<ElementSave, Mock<IUiTimer>>();
        Mock<IAnimationCollectionViewModelManager> collectionManager = new Mock<IAnimationCollectionViewModelManager>();
        collectionManager.Setup(manager => manager.GetAnimationCollectionViewModel(It.IsAny<ElementSave?>()))
            .Returns((ElementSave? element) =>
            {
                Mock<IUiTimer> timer = new Mock<IUiTimer>();
                timers[element!] = timer;
                ElementAnimationsViewModel viewModel = CreateViewModel(selectedState.Object, timer.Object);
                viewModel.Element = element!;
                return viewModel;
            });
        AnimationTabController controller = new AnimationTabController(
            selectedState.Object,
            Mock.Of<IUndoManager>(),
            Mock.Of<IGuiCommands>(),
            Mock.Of<IDialogService>(),
            projectState.Object,
            collectionManager.Object,
            Mock.Of<IRenameManager>(),
            Mock.Of<IDuplicateService>(),
            Mock.Of<IAnimationFilePathService>(),
            () => CreateViewModel(selectedState.Object, Mock.Of<IUiTimer>()));
        controller.CreateInitialViewModel();
        controller.RefreshViewModel();
        ElementAnimationsViewModel buttonTab = controller.ViewModel!;
        AnimationViewModel walk = new AnimationViewModel(selectedState.Object, Mock.Of<IWireframeObjectManager>()) { Name = "Walk" };
        buttonTab.Animations.Add(walk);
        buttonTab.SelectedAnimation = walk;
        buttonTab.IsPlaying = true;

        selected = other;
        controller.RefreshViewModel();

        controller.ViewModel.ShouldNotBeSameAs(buttonTab);
        buttonTab.IsPlaying.ShouldBeFalse();
        timers[button].Verify(timer => timer.Stop(), Times.Once);
    }

    private static ElementAnimationsViewModel CreateViewModel(ISelectedState selectedState, IUiTimer timer) =>
        new ElementAnimationsViewModel(
            Mock.Of<INameVerifier>(),
            Mock.Of<IDialogService>(),
            Mock.Of<IAnimationCollectionViewModelManager>(),
            Mock.Of<IRenameManager>(),
            selectedState,
            Mock.Of<IWireframeObjectManager>(),
            Mock.Of<IOutputManager>(),
            Mock.Of<IAnimationFilePathService>(),
            timer);
}
