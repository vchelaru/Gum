using Gum.Input;
using Gum.Managers;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Shouldly;
using StateAnimationPlugin;
using StateAnimationPlugin.Managers;
using StateAnimationPlugin.ViewModels;

namespace Gum.Presentation.Tests;

/// <summary>
/// The Animations tab's list hotkeys, shared by both heads' views (extracted from the WPF view's
/// code-behind). These pin the guards: nothing happens without a selection or a bound key.
/// </summary>
public class AnimationTabKeyHandlerTests
{
    private readonly Mock<IHotkeyManager> _hotkeyManager;
    private readonly Mock<IDialogService> _dialogService;
    private readonly AnimationTabKeyHandler _handler;
    private readonly ElementAnimationsViewModel _viewModel;

    public AnimationTabKeyHandlerTests()
    {
        _hotkeyManager = new Mock<IHotkeyManager>();
        _hotkeyManager.SetupGet(x => x.Delete).Returns(KeyCombination.Pressed(GumKey.Delete));
        _hotkeyManager.SetupGet(x => x.Copy).Returns(KeyCombination.Ctrl(GumKey.C));
        _hotkeyManager.SetupGet(x => x.Paste).Returns(KeyCombination.Ctrl(GumKey.V));
        _hotkeyManager.SetupGet(x => x.ReorderUp).Returns(KeyCombination.Alt(GumKey.Up));
        _hotkeyManager.SetupGet(x => x.ReorderDown).Returns(KeyCombination.Alt(GumKey.Down));
        _dialogService = new Mock<IDialogService>();
        _handler = new AnimationTabKeyHandler(_hotkeyManager.Object, _dialogService.Object);
        _viewModel = new ElementAnimationsViewModel(
            Mock.Of<INameVerifier>(), _dialogService.Object, Mock.Of<IAnimationCollectionViewModelManager>(),
            Mock.Of<IRenameManager>(), Mock.Of<ISelectedState>(), Mock.Of<IWireframeObjectManager>(),
            Mock.Of<IOutputManager>(), Mock.Of<IAnimationFilePathService>(), Mock.Of<IUiTimer>());
    }

    [Fact]
    public void HandleAnimationListKey_DoesNotConsumeDelete_WhenNoAnimationIsSelected()
    {
        bool handled = _handler.HandleAnimationListKey(new GumKeyEventArgs { Key = GumKey.Delete }, _viewModel);

        handled.ShouldBeFalse();
        _dialogService.Invocations.ShouldBeEmpty();
    }

    [Fact]
    public void HandleAnimationListKey_DoesNotConsumeAnUnboundKey()
    {
        bool handled = _handler.HandleAnimationListKey(new GumKeyEventArgs { Key = GumKey.F12 }, _viewModel);

        handled.ShouldBeFalse();
    }

    [Fact]
    public void HandleKeyframeListKey_DoesNothing_WhenNoKeyframeIsSelected()
    {
        AnimatedKeyframeViewModel? pasted = _handler.HandleKeyframeListKey(new GumKeyEventArgs { Key = GumKey.V, IsCtrlDown = true }, _viewModel);

        pasted.ShouldBeNull();
    }
}
