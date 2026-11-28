using Gum.Forms.Controls;
using Gum.Mvvm;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using System;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Leaving the root clears the leaving element's own explicitly set BindingContext, whichever way it
/// leaves; descendants given their own keep theirs. Moving between other parents, and leaving the
/// popup or modal root, keep it.
/// </summary>
public class BindingAfterScreenRemovalTests : BaseTestClass
{
    private class GameViewModel : ViewModel
    {
        public int Gold { get => Get<int>(); set => Set(value); }

        [DependsOn(nameof(Gold))]
        public bool CanAfford => Gold >= 10;
    }

    private const string ResetBecause = "because a target whose BindingContext becomes null resets (#8543)";

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

    private static void LeaveRoot(StackPanel screen, string path)
    {
        GraphicalUiElement root = Gum.GumService.Default.Root;
        switch (path)
        {
            case "FormsRemoveFromRoot":
                screen.RemoveFromRoot();
                break;
            case "VisualRemoveFromRoot":
                screen.Visual.RemoveFromRoot();
                break;
            case "ChildrenRemove":
                root.Children.Remove(screen.Visual);
                break;
            case "ChildrenClear":
                root.Children.Clear();
                break;
            case "ParentNull":
                screen.Visual.Parent = null;
                break;
            case "RemoveChild":
                root.RemoveChild(screen.Visual);
                break;
            case "MoveToOtherParent":
                new StackPanel().AddChild(screen);
                break;
            default:
                throw new ArgumentException(path);
        }
    }

    [Theory]
    [InlineData("FormsRemoveFromRoot")]
    [InlineData("VisualRemoveFromRoot")]
    [InlineData("ChildrenRemove")]
    [InlineData("ChildrenClear")]
    [InlineData("ParentNull")]
    [InlineData("RemoveChild")]
    [InlineData("MoveToOtherParent")]
    public void LeavingRoot_ExplicitContext_IsClearedAndScreenStopsFollowingViewModel(string path)
    {
        GameViewModel viewModel = new() { Gold = 1 };
        (StackPanel screen, Label goldLabel, Button buyButton) = CreateScreen();
        screen.AddToRoot();
        screen.BindingContext = viewModel;

        LeaveRoot(screen, path);
        viewModel.Gold = 50;

        screen.BindingContext.ShouldBeNull();
        goldLabel.BindingContext.ShouldBeNull();
        goldLabel.Text.ShouldBe(ContextResetExpectation.Of(new Label().Text), ResetBecause);
        buyButton.IsEnabled.ShouldBe(ContextResetExpectation.Of(new Button().IsEnabled), ResetBecause);
    }

    [Fact]
    public void LeavingRoot_ContextAssignedBeforeAddToRoot_IsCleared()
    {
        GameViewModel viewModel = new() { Gold = 1 };
        (StackPanel screen, Label goldLabel, _) = CreateScreen();
        screen.BindingContext = viewModel;
        screen.AddToRoot();

        Gum.GumService.Default.Root.Children.Clear();
        viewModel.Gold = 7;

        screen.BindingContext.ShouldBeNull();
        goldLabel.Text.ShouldBe(ContextResetExpectation.Of(new Label().Text), ResetBecause);
    }

    [Fact]
    public void LeavingRoot_DescendantWithOwnExplicitContext_KeepsIt()
    {
        GameViewModel screenViewModel = new() { Gold = 1 };
        GameViewModel childViewModel = new() { Gold = 2 };
        (StackPanel screen, Label goldLabel, Button buyButton) = CreateScreen();
        screen.AddToRoot();
        screen.BindingContext = screenViewModel;
        goldLabel.BindingContext = childViewModel;

        screen.RemoveFromRoot();
        childViewModel.Gold = 40;
        screenViewModel.Gold = 50;

        goldLabel.BindingContext.ShouldBe(childViewModel);
        goldLabel.Text.ShouldBe("40");
        buyButton.BindingContext.ShouldBeNull();
        buyButton.IsEnabled.ShouldBe(ContextResetExpectation.Of(new Button().IsEnabled), ResetBecause);
    }

    [Fact]
    public void RemoveFromRoot_ThenAddToRootAgain_NeedsContextAssignedAgain()
    {
        GameViewModel viewModel = new() { Gold = 1 };
        (StackPanel screen, Label goldLabel, _) = CreateScreen();
        screen.AddToRoot();
        screen.BindingContext = viewModel;

        screen.RemoveFromRoot();
        screen.AddToRoot();
        viewModel.Gold = 30;

        screen.BindingContext.ShouldBeNull();
        goldLabel.Text.ShouldBe(ContextResetExpectation.Of(new Label().Text), ResetBecause);

        screen.BindingContext = viewModel;
        goldLabel.Text.ShouldBe("30");
        viewModel.Gold = 31;
        goldLabel.Text.ShouldBe("31");
    }

    [Fact]
    public void RemoveFromRoot_ThenNewScreenWithSameViewModel_OnlyNewScreenFollowsViewModel()
    {
        GameViewModel viewModel = new() { Gold = 1 };
        (StackPanel oldScreen, Label oldGoldLabel, _) = CreateScreen();
        oldScreen.AddToRoot();
        oldScreen.BindingContext = viewModel;

        oldScreen.RemoveFromRoot();
        (StackPanel newScreen, Label goldLabel, Button buyButton) = CreateScreen();
        newScreen.AddToRoot();
        newScreen.BindingContext = viewModel;
        viewModel.Gold = 20;

        goldLabel.Text.ShouldBe("20");
        buyButton.IsEnabled.ShouldBeTrue();
        oldGoldLabel.Text.ShouldBe(ContextResetExpectation.Of(new Label().Text), ResetBecause);
    }

    [Fact]
    public void ReparentBetweenNonRootParents_ExplicitContext_IsKept()
    {
        GameViewModel viewModel = new() { Gold = 1 };
        StackPanel screen = new();
        screen.AddToRoot();
        StackPanel firstPanel = new();
        StackPanel secondPanel = new();
        screen.AddChild(firstPanel);
        screen.AddChild(secondPanel);
        Label goldLabel = new();
        goldLabel.SetBinding(nameof(Label.Text), nameof(GameViewModel.Gold));
        firstPanel.AddChild(goldLabel);
        goldLabel.BindingContext = viewModel;

        secondPanel.AddChild(goldLabel);
        viewModel.Gold = 12;

        goldLabel.BindingContext.ShouldBe(viewModel);
        goldLabel.Text.ShouldBe("12");

        goldLabel.Visual.Parent = null;
        viewModel.Gold = 13;
        goldLabel.BindingContext.ShouldBe(viewModel);
        goldLabel.Text.ShouldBe("13");
    }

    [Fact]
    public void ReparentBetweenNonRootParents_InheritedContext_FollowsNewParent()
    {
        GameViewModel firstViewModel = new() { Gold = 1 };
        GameViewModel secondViewModel = new() { Gold = 2 };
        StackPanel firstPanel = new();
        firstPanel.BindingContext = firstViewModel;
        StackPanel secondPanel = new();
        secondPanel.BindingContext = secondViewModel;
        Label goldLabel = new();
        goldLabel.SetBinding(nameof(Label.Text), nameof(GameViewModel.Gold));
        firstPanel.AddChild(goldLabel);
        goldLabel.Text.ShouldBe("1");

        secondPanel.AddChild(goldLabel);

        goldLabel.BindingContext.ShouldBe(secondViewModel);
        goldLabel.Text.ShouldBe("2");
        firstPanel.BindingContext.ShouldBe(firstViewModel);
    }

    [Fact]
    public void PopupAndModalRootRemoval_ExplicitContext_IsKept()
    {
        GameViewModel viewModel = new() { Gold = 1 };
        (StackPanel popup, Label goldLabel, _) = CreateScreen();
        popup.BindingContext = viewModel;

        FrameworkElement.PopupRoot.AddChild(popup.Visual);
        popup.Visual.Parent = null;
        FrameworkElement.ModalRoot.AddChild(popup.Visual);
        FrameworkElement.ModalRoot.Children.Clear();
        viewModel.Gold = 8;

        popup.BindingContext.ShouldBe(viewModel);
        goldLabel.Text.ShouldBe("8");
    }

    [Fact]
    public void ComboBoxInScreen_DropDownOpenedClosedAndOpened_ItemsKeepTheirContext()
    {
        StackPanel screen = new();
        screen.AddToRoot();
        ComboBox comboBox = new();
        screen.AddChild(comboBox);
        comboBox.Items!.Add("first");
        comboBox.Items.Add("second");

        comboBox.IsDropDownOpen = true;
        comboBox.IsDropDownOpen = false;
        comboBox.IsDropDownOpen = true;

        comboBox.ListBox.ListBoxItems[1].BindingContext.ShouldBe("second");
    }

    [Fact]
    public void ListBoxInScreen_RemovedAndAddedAgain_ItemsKeepTheirContext()
    {
        StackPanel screen = new();
        screen.AddToRoot();
        ListBox listBox = new();
        screen.AddChild(listBox);
        listBox.Items!.Add("first");
        listBox.Items.Add("second");

        screen.RemoveFromRoot();
        screen.AddToRoot();

        listBox.ListBoxItems[1].BindingContext.ShouldBe("second");
    }

    [Fact]
    public void AddToManagers_RemoveFromRoot_ContainedInstancesKeepTheirOwnContext()
    {
        GameViewModel screenViewModel = new() { Gold = 1 };
        GameViewModel labelViewModel = new() { Gold = 2 };
        StackPanel screen = new();
        Label goldLabel = new();
        goldLabel.SetBinding(nameof(Label.Text), nameof(GameViewModel.Gold));
        screen.AddChild(goldLabel);
        ListBox listBox = new();
        screen.AddChild(listBox);
        listBox.Items!.Add("first");
        listBox.Items.Add("second");
        // A screen built from a project lists its instances as contained elements, which
        // RemoveFromManagers walks.
        goldLabel.Visual.ElementGueContainingThis = screen.Visual;
        listBox.Visual.ElementGueContainingThis = screen.Visual;
        screen.Visual.AddToManagers(SystemManagers.Default, null);
        screen.AddToRoot();
        screen.BindingContext = screenViewModel;
        goldLabel.BindingContext = labelViewModel;

        screen.RemoveFromRoot();
        labelViewModel.Gold = 40;

        screen.BindingContext.ShouldBeNull();
        goldLabel.BindingContext.ShouldBe(labelViewModel);
        goldLabel.Text.ShouldBe("40");
        listBox.ListBoxItems[1].BindingContext.ShouldBe("second");
    }

    private class RemovesOtherOnRemoval : Gum.GueDeriving.ContainerRuntime
    {
        public GraphicalUiElement? Other;

        public override void RemoveFromManagers()
        {
            Other?.RemoveFromManagers();
            base.RemoveFromManagers();
        }
    }

    [Fact]
    public void RemoveFromManagers_UnrelatedElementRemovedDuringChildWalk_ClearsItsOwnContext()
    {
        GameViewModel otherViewModel = new() { Gold = 1 };
        Gum.GueDeriving.ContainerRuntime other = new();
        other.AddToManagers(SystemManagers.Default, null);
        other.BindingContext = otherViewModel;
        Gum.GueDeriving.ContainerRuntime screen = new();
        RemovesOtherOnRemoval child = new() { Other = other };
        screen.Children.Add(child);
        child.ElementGueContainingThis = screen;
        screen.AddToManagers(SystemManagers.Default, null);

        screen.RemoveFromManagers();

        other.BindingContext.ShouldBeNull();
    }

    [Fact]
    public void AddToManagers_ExplicitContext_FollowsViewModelUntilRemovedFromManagers()
    {
        GameViewModel viewModel = new() { Gold = 1 };
        (StackPanel screen, Label goldLabel, _) = CreateScreen();
        screen.Visual.AddToManagers(SystemManagers.Default, null);
        screen.BindingContext = viewModel;

        viewModel.Gold = 5;
        goldLabel.Text.ShouldBe("5");

        screen.Visual.RemoveFromManagers();
        viewModel.Gold = 6;

        screen.BindingContext.ShouldBeNull();
        goldLabel.Text.ShouldBe(ContextResetExpectation.Of(new Label().Text), ResetBecause);
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
