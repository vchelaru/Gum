using Gum.Forms.Controls;
using Gum.Mvvm;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using System;
using System.ComponentModel;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Leaving the root pauses the bindings of the leaving subtree, whichever way it leaves: every element keeps
/// its BindingContext, but no binding follows or subscribes to its view model until the subtree is added to
/// the root again, which shows the view model's current values. Moving between other parents, and leaving the
/// popup or modal root, do not pause. RemoveFromManagers still clears the element's own context.
/// </summary>
public class BindingAfterScreenRemovalTests : BaseTestClass
{
    private class GameViewModel : ViewModel
    {
        public int Gold { get => Get<int>(); set => Set(value); }

        [DependsOn(nameof(Gold))]
        public bool CanAfford => Gold >= 10;

        public GameViewModel? Player { get => Get<GameViewModel?>(); set => Set(value); }
    }

    // Counts PropertyChanged subscribers, to show a removed screen does not stay subscribed.
    private class CountingViewModel : INotifyPropertyChanged
    {
        private PropertyChangedEventHandler? _propertyChanged;
        public int SubscriberCount => _propertyChanged?.GetInvocationList().Length ?? 0;

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add => _propertyChanged += value;
            remove => _propertyChanged -= value;
        }

        private int _gold;
        public int Gold
        {
            get => _gold;
            set
            {
                _gold = value;
                _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Gold)));
            }
        }
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
    public void LeavingRoot_ExplicitContext_IsKeptAndScreenStopsFollowingViewModel(string path)
    {
        GameViewModel viewModel = new() { Gold = 1 };
        (StackPanel screen, Label goldLabel, Button buyButton) = CreateScreen();
        screen.AddToRoot();
        screen.BindingContext = viewModel;

        LeaveRoot(screen, path);
        viewModel.Gold = 50;

        screen.BindingContext.ShouldBe(viewModel);
        goldLabel.BindingContext.ShouldBe(viewModel);
        goldLabel.Text.ShouldBe("1");
        buyButton.IsEnabled.ShouldBeFalse();
    }

    [Theory]
    [InlineData("FormsRemoveFromRoot")]
    [InlineData("VisualRemoveFromRoot")]
    [InlineData("ChildrenRemove")]
    [InlineData("ChildrenClear")]
    [InlineData("ParentNull")]
    [InlineData("RemoveChild")]
    public void LeavingRoot_ThenAddToRootAgain_ShowsCurrentValuesAndFollowsViewModel(string path)
    {
        GameViewModel viewModel = new() { Gold = 1 };
        (StackPanel screen, Label goldLabel, Button buyButton) = CreateScreen();
        screen.AddToRoot();
        screen.BindingContext = viewModel;

        LeaveRoot(screen, path);
        viewModel.Gold = 50;
        screen.AddToRoot();

        screen.BindingContext.ShouldBe(viewModel);
        goldLabel.Text.ShouldBe("50");
        buyButton.IsEnabled.ShouldBeTrue();
        viewModel.Gold = 3;
        goldLabel.Text.ShouldBe("3");
        buyButton.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void LeavingRoot_ViewModelLosesEverySubscriptionUntilAddedAgain()
    {
        CountingViewModel viewModel = new() { Gold = 1 };
        (StackPanel screen, Label goldLabel, _) = CreateScreen();
        screen.AddToRoot();
        screen.BindingContext = viewModel;
        int subscribedCount = viewModel.SubscriberCount;
        subscribedCount.ShouldBeGreaterThan(0);

        screen.RemoveFromRoot();
        viewModel.SubscriberCount.ShouldBe(0);

        screen.AddToRoot();
        viewModel.SubscriberCount.ShouldBe(subscribedCount);
        viewModel.Gold = 9;
        goldLabel.Text.ShouldBe("9");
    }

    [Fact]
    public void LeavingRoot_ContextAssignedBeforeAddToRoot_IsKeptAndPaused()
    {
        GameViewModel viewModel = new() { Gold = 1 };
        (StackPanel screen, Label goldLabel, _) = CreateScreen();
        screen.BindingContext = viewModel;
        screen.AddToRoot();

        Gum.GumService.Default.Root.Children.Clear();
        viewModel.Gold = 7;

        screen.BindingContext.ShouldBe(viewModel);
        goldLabel.Text.ShouldBe("1");
    }

    [Fact]
    public void LeavingRoot_DescendantWithOwnExplicitContext_KeepsItAndPausesUntilAddedAgain()
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
        goldLabel.Text.ShouldBe("2");
        buyButton.BindingContext.ShouldBe(screenViewModel);
        buyButton.IsEnabled.ShouldBeFalse();

        screen.AddToRoot();
        goldLabel.Text.ShouldBe("40");
        buyButton.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void LeavingRoot_BoundBindingContext_IsKeptAndFollowsAgainAfterAddToRoot()
    {
        GameViewModel player = new() { Gold = 1 };
        GameViewModel game = new() { Player = player };
        StackPanel screen = new();
        StackPanel playerPanel = new();
        playerPanel.SetBinding(nameof(FrameworkElement.BindingContext), nameof(GameViewModel.Player));
        screen.AddChild(playerPanel);
        Label goldLabel = new();
        goldLabel.SetBinding(nameof(Label.Text), nameof(GameViewModel.Gold));
        playerPanel.AddChild(goldLabel);
        screen.AddToRoot();
        screen.BindingContext = game;

        screen.RemoveFromRoot();
        GameViewModel nextPlayer = new() { Gold = 5 };
        game.Player = nextPlayer;

        playerPanel.BindingContext.ShouldBe(player);
        goldLabel.Text.ShouldBe("1");

        screen.AddToRoot();
        playerPanel.BindingContext.ShouldBe(nextPlayer);
        goldLabel.Text.ShouldBe("5");
    }

    [Fact]
    public void LeavingRoot_ContextAssignedWhileRemoved_IsShownAfterAddToRoot()
    {
        GameViewModel first = new() { Gold = 1 };
        GameViewModel second = new() { Gold = 2 };
        (StackPanel screen, Label goldLabel, _) = CreateScreen();
        screen.AddToRoot();
        screen.BindingContext = first;
        screen.RemoveFromRoot();

        screen.BindingContext = second;
        goldLabel.Text.ShouldBe("1");

        screen.AddToRoot();
        goldLabel.Text.ShouldBe("2");
    }

    [Fact]
    public void LeavingRoot_BindingSetWhileRemoved_AttachesAfterAddToRoot()
    {
        GameViewModel viewModel = new() { Gold = 4 };
        StackPanel screen = new();
        screen.AddToRoot();
        screen.BindingContext = viewModel;
        screen.RemoveFromRoot();
        Label goldLabel = new();
        screen.AddChild(goldLabel);
        string? unboundText = goldLabel.Text;

        goldLabel.SetBinding(nameof(Label.Text), nameof(GameViewModel.Gold));
        goldLabel.Text.ShouldBe(unboundText);

        screen.AddToRoot();
        goldLabel.Text.ShouldBe("4");
    }

    [Fact]
    public void LeavingRoot_BoundItems_KeepsRowsAndFollowsCollectionAgainAfterAddToRoot()
    {
        ItemsViewModel viewModel = new();
        viewModel.Names.Add("first");
        StackPanel screen = new();
        ListBox listBox = new();
        listBox.SetBinding(nameof(ListBox.Items), nameof(ItemsViewModel.Names));
        screen.AddChild(listBox);
        screen.AddToRoot();
        screen.BindingContext = viewModel;

        screen.RemoveFromRoot();
        listBox.ListBoxItems.Count.ShouldBe(1);

        screen.AddToRoot();
        viewModel.Names.Add("second");
        listBox.ListBoxItems.Count.ShouldBe(2);
        listBox.ListBoxItems[1].BindingContext.ShouldBe("second");
    }

    [Fact]
    public void LeavingRoot_ContextSetToNullWhileRemoved_BoundItemsLetGoOfCollectionUntilContextAssignedAgain()
    {
        ItemsViewModel viewModel = new();
        viewModel.Names.Add("first");
        StackPanel screen = new();
        ListBox listBox = new();
        listBox.SetBinding(nameof(ListBox.Items), nameof(ItemsViewModel.Names));
        screen.AddChild(listBox);
        ComboBox comboBox = new();
        comboBox.SetBinding(nameof(ComboBox.Items), nameof(ItemsViewModel.Names));
        screen.AddChild(comboBox);
        screen.AddToRoot();
        screen.BindingContext = viewModel;
        screen.RemoveFromRoot();

        // A paused screen keeps its bound collection; clearing the context is how to release it.
        screen.BindingContext = null;

        listBox.Items.ShouldNotBeSameAs(viewModel.Names);
        comboBox.Items.ShouldNotBeSameAs(viewModel.Names);
        viewModel.Names.Add("second");
        listBox.ListBoxItems.Count.ShouldBe(0);

        screen.AddToRoot();
        screen.BindingContext = viewModel;
        listBox.Items.ShouldBeSameAs(viewModel.Names);
        comboBox.Items.ShouldBeSameAs(viewModel.Names);
        listBox.ListBoxItems.Count.ShouldBe(2);
    }

    [Fact]
    public void MoveFromRootIntoDetachedPanel_ExplicitContext_IsKeptAndPaused()
    {
        GameViewModel viewModel = new() { Gold = 1 };
        (StackPanel screen, Label goldLabel, _) = CreateScreen();
        StackPanel detachedPanel = new();
        screen.AddToRoot();
        screen.BindingContext = viewModel;

        detachedPanel.AddChild(screen);
        viewModel.Gold = 2;

        screen.BindingContext.ShouldBe(viewModel);
        goldLabel.Text.ShouldBe("1");
    }

    private class ItemsViewModel : ViewModel
    {
        public System.Collections.ObjectModel.ObservableCollection<string> Names { get; } = new();
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
        oldGoldLabel.Text.ShouldBe("1");
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

        // Leaving a parent inside the root leaves the root tree, so the label pauses like a removed screen.
        goldLabel.Visual.Parent = null;
        viewModel.Gold = 13;
        goldLabel.BindingContext.ShouldBe(viewModel);
        goldLabel.Text.ShouldBe("12");
    }

    [Fact]
    public void MoveFromRootIntoPanelInRoot_KeepsFollowingViewModel()
    {
        GameViewModel viewModel = new() { Gold = 1 };
        (StackPanel screen, Label goldLabel, _) = CreateScreen();
        StackPanel panel = new();
        StackPanel panelHolder = new();
        panelHolder.AddChild(panel);
        panelHolder.AddToRoot();
        screen.AddToRoot();
        screen.BindingContext = viewModel;

        panel.AddChild(screen);
        viewModel.Gold = 2;

        goldLabel.Text.ShouldBe("2");
    }

    [Fact]
    public void PausedScreenInDetachedPanel_PanelAddedToRoot_Resumes()
    {
        GameViewModel viewModel = new() { Gold = 1 };
        (StackPanel screen, Label goldLabel, _) = CreateScreen();
        StackPanel detachedPanel = new();
        screen.AddToRoot();
        screen.BindingContext = viewModel;
        screen.RemoveFromRoot();
        detachedPanel.AddChild(screen);
        viewModel.Gold = 2;
        goldLabel.Text.ShouldBe("1");

        detachedPanel.AddToRoot();

        goldLabel.Text.ShouldBe("2");
        viewModel.Gold = 3;
        goldLabel.Text.ShouldBe("3");
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
    public void AddToManagers_RemoveFromRoot_ContainedInstancesKeepTheirOwnContextAndPause()
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

        // Forms RemoveFromRoot also calls RemoveFromManagers, which clears the screen's own context.
        screen.BindingContext.ShouldBeNull();
        goldLabel.BindingContext.ShouldBe(labelViewModel);
        goldLabel.Text.ShouldBe("2");
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
