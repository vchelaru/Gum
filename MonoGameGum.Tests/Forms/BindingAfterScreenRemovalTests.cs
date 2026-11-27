using Gum.Forms.Controls;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// What a bound screen does once it leaves the root (RemoveFromRoot or Root.Children.Clear()), when it
/// is added back, and when a new screen bound to the same view model replaces it.
/// </summary>
public class BindingAfterScreenRemovalTests : BaseTestClass
{
    private class GameViewModel : ViewModel
    {
        public int Gold { get => Get<int>(); set => Set(value); }

        [DependsOn(nameof(Gold))]
        public bool CanAfford => Gold >= 10;
    }

    private static (StackPanel screen, Label goldLabel, Button buyButton) CreateScreen()
    {
        StackPanel screen = new();
        Label goldLabel = new();
        goldLabel.SetBinding(nameof(Label.Text), nameof(GameViewModel.Gold));
        screen.AddChild(goldLabel);
        Button buyButton = new();
        buyButton.SetBinding(nameof(Button.IsEnabled), nameof(GameViewModel.CanAfford));
        screen.AddChild(buyButton);
        return (screen, goldLabel, buyButton);
    }

    [Fact]
    public void RemoveFromRoot_ExplicitContext_ScreenKeepsFollowingViewModel()
    {
        GameViewModel viewModel = new() { Gold = 1 };
        (StackPanel screen, Label goldLabel, _) = CreateScreen();
        screen.AddToRoot();
        screen.BindingContext = viewModel;

        screen.RemoveFromRoot();
        viewModel.Gold = 50;

        // Pinned current behavior: like Clear(), RemoveFromRoot only unparents, so the explicit
        // context stays and the detached screen keeps receiving view model changes.
        goldLabel.Text.ShouldBe("50");
    }

    [Fact]
    public void RootChildrenClear_ExplicitContext_ScreenKeepsFollowingViewModel()
    {
        GameViewModel viewModel = new() { Gold = 1 };
        (StackPanel screen, Label goldLabel, Button buyButton) = CreateScreen();
        screen.AddToRoot();
        screen.BindingContext = viewModel;

        Gum.GumService.Default.Root.Children.Clear();
        viewModel.Gold = 50;

        // Pinned current behavior: Clear() only unparents, so an explicitly assigned context stays
        // and the detached screen keeps receiving view model changes (it is not collected while the
        // view model lives).
        screen.Visual.Parent.ShouldBeNull();
        goldLabel.Text.ShouldBe("50");
        buyButton.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void RootChildrenClear_ThenNewScreenWithSameViewModel_NewScreenFollowsViewModel()
    {
        GameViewModel viewModel = new() { Gold = 1 };
        (StackPanel oldScreen, _, _) = CreateScreen();
        oldScreen.AddToRoot();
        oldScreen.BindingContext = viewModel;

        Gum.GumService.Default.Root.Children.Clear();
        (StackPanel newScreen, Label goldLabel, Button buyButton) = CreateScreen();
        newScreen.AddToRoot();
        newScreen.BindingContext = viewModel;
        viewModel.Gold = 20;

        goldLabel.Text.ShouldBe("20");
        buyButton.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void RemoveFromRoot_ThenNewScreenWithSameViewModel_NewScreenFollowsViewModel()
    {
        GameViewModel viewModel = new() { Gold = 1 };
        (StackPanel oldScreen, _, _) = CreateScreen();
        oldScreen.AddToRoot();
        oldScreen.BindingContext = viewModel;

        oldScreen.RemoveFromRoot();
        (StackPanel newScreen, Label goldLabel, Button buyButton) = CreateScreen();
        newScreen.AddToRoot();
        newScreen.BindingContext = viewModel;
        viewModel.Gold = 20;

        goldLabel.Text.ShouldBe("20");
        buyButton.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void RemoveFromRoot_ThenAddToRootAgain_ScreenFollowsViewModelAgain()
    {
        GameViewModel viewModel = new() { Gold = 1 };
        (StackPanel screen, Label goldLabel, _) = CreateScreen();
        screen.AddToRoot();
        screen.BindingContext = viewModel;

        screen.RemoveFromRoot();
        screen.AddToRoot();
        viewModel.Gold = 30;

        goldLabel.Text.ShouldBe("30");
    }

    [Fact]
    public void RootChildrenClear_ThenAddToRootAgain_ScreenFollowsViewModel()
    {
        GameViewModel viewModel = new() { Gold = 1 };
        (StackPanel screen, Label goldLabel, _) = CreateScreen();
        screen.AddToRoot();
        screen.BindingContext = viewModel;

        Gum.GumService.Default.Root.Children.Clear();
        screen.AddToRoot();
        viewModel.Gold = 30;

        goldLabel.Text.ShouldBe("30");
    }

    [Fact]
    public void RootChildrenClear_ContextAssignedBeforeAddToRoot_ScreenKeepsFollowingViewModel()
    {
        GameViewModel viewModel = new() { Gold = 1 };
        (StackPanel screen, Label goldLabel, _) = CreateScreen();
        screen.BindingContext = viewModel;
        screen.AddToRoot();

        Gum.GumService.Default.Root.Children.Clear();
        viewModel.Gold = 7;

        goldLabel.Text.ShouldBe("7");
    }

    [Fact]
    public void DependsOn_UndottedComputedProperty_UpdatesBoundIsEnabledOnEachChange()
    {
        GameViewModel viewModel = new() { Gold = 0 };
        (StackPanel screen, _, Button buyButton) = CreateScreen();
        screen.BindingContext = viewModel;

        buyButton.IsEnabled.ShouldBeFalse();
        viewModel.Gold = 10;
        buyButton.IsEnabled.ShouldBeTrue();
        viewModel.Gold = 9;
        buyButton.IsEnabled.ShouldBeFalse();
    }
}
