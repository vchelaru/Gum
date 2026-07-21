using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.StateAnimation.SaveClasses;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Moq;
using Shouldly;
using StateAnimationPlugin;
using StateAnimationPlugin.Managers;
using StateAnimationPlugin.ViewModels;
using System.ComponentModel;
using Xunit;

namespace GumToolUnitTests.Plugins.StateAnimationPlugin;

/// <summary>
/// Covers <see cref="AnimationTabController"/> (issue #3866), the field-coupled-but-WPF-free handler
/// logic extracted from <c>MainStateAnimationPlugin</c>. The plugin itself is now a thin WPF-glue
/// wrapper (window/menu/tab wiring, DataContext push) with nothing left to unit test in isolation;
/// these tests are the replacement coverage for that logic, now that it lives in a ctor-injected,
/// constructible-without-MEF class.
/// </summary>
public class AnimationTabControllerTests : BaseTestClass
{
    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly Mock<IUndoManager> _undoManager = new();
    private readonly Mock<IGuiCommands> _guiCommands = new();
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly Mock<IProjectState> _projectState = new();
    private readonly Mock<IAnimationCollectionViewModelManager> _animationCollectionViewModelManager = new();
    private readonly Mock<IRenameManager> _renameManager = new();
    private readonly Mock<IDuplicateService> _duplicateService = new();
    private readonly Mock<IAnimationFilePathService> _animationFilePathService = new();
    private readonly IWireframeObjectManager _wireframeObjectManager = Mock.Of<IWireframeObjectManager>();

    private AnimationTabController CreateController(ElementAnimationsViewModel? factoryViewModel = null)
    {
        factoryViewModel ??= NewViewModel();

        return new AnimationTabController(
            _selectedState.Object,
            _undoManager.Object,
            _guiCommands.Object,
            _dialogService.Object,
            _projectState.Object,
            _animationCollectionViewModelManager.Object,
            _renameManager.Object,
            _duplicateService.Object,
            _animationFilePathService.Object,
            () => factoryViewModel);
    }

    private ElementAnimationsViewModel NewViewModel() => new(
        Mock.Of<INameVerifier>(),
        _dialogService.Object,
        _animationCollectionViewModelManager.Object,
        _renameManager.Object,
        _selectedState.Object,
        _wireframeObjectManager,
        Mock.Of<IOutputManager>(),
        _animationFilePathService.Object,
        Mock.Of<IUiTimer>());

    [Fact]
    public void ApplyAnimations_ThenHandleDataChange_SkipsUndoLock_WhileRestoringFromUndo()
    {
        // Mirrors the real sequence: undo/redo calls ApplyAnimations to restore the .ganx, which
        // triggers the bound view model's AnyChange -> HandleDataChange. That save must NOT itself
        // flush a brand-new undo record (it would fight the very undo/redo that triggered it).
        AnimationTabController controller = CreateController();
        ElementSave element = new ComponentSave { Name = "Foo" };
        ElementAnimationsSave save = new();

        controller.ApplyAnimations(element, save);
        controller.HandleDataChange(new ElementAnimationsViewModel(
                Mock.Of<INameVerifier>(), _dialogService.Object, _animationCollectionViewModelManager.Object,
                _renameManager.Object, _selectedState.Object, _wireframeObjectManager, Mock.Of<IOutputManager>(),
                _animationFilePathService.Object, Mock.Of<IUiTimer>()),
            new PropertyChangedEventArgs("Animations"));

        _undoManager.Verify(u => u.RequestLock(), Times.Never);
    }

    [Fact]
    public void HandleDataChange_RequestsUndoLock_ForANormalEdit()
    {
        // Same save path, but NOT preceded by ApplyAnimations (i.e. a normal user edit, not an
        // undo/redo restore) - this one must record an undo.
        AnimationTabController controller = CreateController();

        controller.HandleDataChange(new ElementAnimationsViewModel(
                Mock.Of<INameVerifier>(), _dialogService.Object, _animationCollectionViewModelManager.Object,
                _renameManager.Object, _selectedState.Object, _wireframeObjectManager, Mock.Of<IOutputManager>(),
                _animationFilePathService.Object, Mock.Of<IUiTimer>()),
            new PropertyChangedEventArgs("Animations"));

        _undoManager.Verify(u => u.RequestLock(), Times.Once);
    }

    [Fact]
    public void HandleDataChange_RaisesDataSaved_WhenShouldSave()
    {
        AnimationTabController controller = CreateController();
        bool raised = false;
        controller.DataSaved += () => raised = true;

        controller.HandleDataChange(new object(), new PropertyChangedEventArgs("Whatever"));

        raised.ShouldBeTrue();
    }

    [Fact]
    public void HandleDataChange_DoesNotRaiseDataSaved_WhenAnimationViewModelSelectionOnlyChanged()
    {
        // AnimationViewModel's own selection/length changes are not persisted data.
        AnimationTabController controller = CreateController();
        bool raised = false;
        controller.DataSaved += () => raised = true;
        AnimationViewModel animationViewModel = new(_selectedState.Object, _wireframeObjectManager);

        controller.HandleDataChange(animationViewModel, new PropertyChangedEventArgs("SelectedKeyframe"));

        raised.ShouldBeFalse();
        _animationCollectionViewModelManager.Verify(m => m.Save(It.IsAny<ElementAnimationsViewModel>()), Times.Never);
    }

    [Fact]
    public void HandleElementDuplicate_DelegatesToDuplicateService()
    {
        AnimationTabController controller = CreateController();
        ElementSave oldElement = new ComponentSave { Name = "Old" };
        ElementSave newElement = new ComponentSave { Name = "New" };

        controller.HandleElementDuplicate(oldElement, newElement);

        _duplicateService.Verify(d => d.HandleDuplicate(oldElement, newElement), Times.Once);
    }

    [Fact]
    public void RefreshViewModel_RaisesViewModelRefreshed()
    {
        AnimationTabController controller = CreateController();
        bool raised = false;
        controller.ViewModelRefreshed += () => raised = true;
        _projectState.SetupGet(p => p.GumProjectSave).Returns((GumProjectSave?)null);

        controller.RefreshViewModel();

        raised.ShouldBeTrue();
    }

    [Fact]
    public void GetCurrentAnimations_ReturnsNull_WhenElementHasNoPersistedAnimations()
    {
        AnimationTabController controller = CreateController();
        ElementSave element = new ComponentSave { Name = "Foo" };
        _animationCollectionViewModelManager
            .Setup(m => m.GetElementAnimationsSave(element))
            .Returns(new ElementAnimationsSave());

        ElementAnimationsSave? result = controller.GetCurrentAnimations(element);

        result.ShouldBeNull();
    }

    [Fact]
    public void GetCurrentAnimations_ReturnsSave_WhenElementHasPersistedAnimations()
    {
        AnimationTabController controller = CreateController();
        ElementSave element = new ComponentSave { Name = "Foo" };
        ElementAnimationsSave save = new();
        save.Animations.Add(new AnimationSave { Name = "Idle" });
        _animationCollectionViewModelManager
            .Setup(m => m.GetElementAnimationsSave(element))
            .Returns(save);

        ElementAnimationsSave? result = controller.GetCurrentAnimations(element);

        result.ShouldBe(save);
    }

    [Fact]
    public void HandleGetDeleteStateResponse_AsksToDelete_AndRespectsDecline()
    {
        ComponentSave element = new() { Name = "Foo" };
        StateSave state = new() { Name = "Idle" };
        element.States.Add(state);
        AnimationSave referencingAnimation = new() { Name = "Anim" };
        referencingAnimation.States.Add(new AnimatedStateSave { StateName = "Idle" });
        ElementAnimationsSave saved = new();
        saved.Animations.Add(referencingAnimation);
        _animationCollectionViewModelManager.Setup(m => m.GetElementAnimationsSave(element)).Returns(saved);
        _dialogService
            .Setup(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()))
            .Returns(MessageDialogResult.Negative);
        AnimationTabController controller = CreateController();

        Gum.Responses.DeleteResponse response = controller.HandleGetDeleteStateResponse(state, element);

        response.ShouldDelete.ShouldBeFalse();
    }

    [Fact]
    public void HandleGetDeleteStateResponse_AllowsDelete_WhenNoAnimationReferencesTheState()
    {
        ComponentSave element = new() { Name = "Foo" };
        StateSave state = new() { Name = "Idle" };
        element.States.Add(state);
        _animationCollectionViewModelManager
            .Setup(m => m.GetElementAnimationsSave(element))
            .Returns(new ElementAnimationsSave());
        AnimationTabController controller = CreateController();

        Gum.Responses.DeleteResponse response = controller.HandleGetDeleteStateResponse(state, element);

        response.ShouldDelete.ShouldBeTrue();
    }
}
