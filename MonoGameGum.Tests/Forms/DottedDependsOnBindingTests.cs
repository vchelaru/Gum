using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.GueDeriving;
using Gum.Mvvm;
using Gum.Wireframe;
using MonoGameGum.TestsCommon;
using Shouldly;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// UI bound to a property whose <c>[DependsOn]</c> follows a dotted path into another view model
/// updates when that other view model changes, through both binding systems.
/// </summary>
public class DottedDependsOnBindingTests : BaseTestClass
{
    public class GameViewModel : ViewModel
    {
        public int Gold { get => Get<int>(); set => Set(value); }
    }

    public class ShopItemViewModel : ViewModel
    {
        public int Price { get => Get<int>(); set => Set(value); }
        public GameViewModel? Game { get => Get<GameViewModel?>(); set => Set(value); }

        [DependsOn(nameof(Price))]
        [DependsOn("Game.Gold")]
        public bool CanAfford => Game != null && Game.Gold >= Price;

        [DependsOn(nameof(CanAfford))]
        public string Status => CanAfford ? "Buy" : "Too expensive";
    }

    [Fact]
    public void FormsStringBinding_UpdatesWhenReferencedViewModelChanges()
    {
        GameViewModel game = new GameViewModel();
        ShopItemViewModel item = new ShopItemViewModel { Price = 10, Game = game };
        Label label = new Label();
        label.BindingContext = item;
        label.SetBinding(nameof(Label.Text), nameof(ShopItemViewModel.Status));
        label.Text.ShouldBe("Too expensive");

        game.Gold = 10;

        label.Text.ShouldBe("Buy");
    }

    [Fact]
    public void FormsBindingObject_UpdatesIsEnabled_WhenReferencedViewModelChanges()
    {
        GameViewModel game = new GameViewModel();
        ShopItemViewModel item = new ShopItemViewModel { Price = 10, Game = game };
        Button button = new Button();
        button.BindingContext = item;
        button.SetBinding(nameof(Button.IsEnabled), new Binding(nameof(ShopItemViewModel.CanAfford)) { Mode = BindingMode.OneWay });
        button.IsEnabled.ShouldBeFalse();

        game.Gold = 15;
        button.IsEnabled.ShouldBeTrue();

        game.Gold = 5;
        button.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void VisualBinding_UpdatesWhenReferencedViewModelChanges()
    {
        GameViewModel game = new GameViewModel();
        ShopItemViewModel item = new ShopItemViewModel { Price = 10, Game = game };
        TextRuntime text = new TextRuntime();
        text.BindingContext = item;
        text.SetBinding(nameof(TextRuntime.Text), nameof(ShopItemViewModel.Status));
        text.Text.ShouldBe("Too expensive");

        game.Gold = 20;

        text.Text.ShouldBe("Buy");
    }

    [Fact]
    public void VisualBinding_UpdatesWhenRootReplaced()
    {
        ShopItemViewModel item = new ShopItemViewModel { Price = 10, Game = new GameViewModel() };
        TextRuntime text = new TextRuntime();
        text.BindingContext = item;
        text.SetBinding(nameof(TextRuntime.Text), nameof(ShopItemViewModel.Status));

        item.Game = new GameViewModel { Gold = 30 };

        text.Text.ShouldBe("Buy");
    }

    [Fact]
    public void BindingContextSwap_FollowsNewItemOnly()
    {
        GameViewModel oldGame = new GameViewModel();
        GameViewModel newGame = new GameViewModel();
        ShopItemViewModel oldItem = new ShopItemViewModel { Price = 10, Game = oldGame };
        ShopItemViewModel newItem = new ShopItemViewModel { Price = 10, Game = newGame };
        Label label = new Label();
        label.BindingContext = oldItem;
        label.SetBinding(nameof(Label.Text), nameof(ShopItemViewModel.Status));

        label.BindingContext = newItem;
        oldGame.Gold = 50;
        label.Text.ShouldBe("Too expensive");

        newGame.Gold = 50;
        label.Text.ShouldBe("Buy");
    }

    [Fact]
    public void InheritedBindingContextSwap_OnParent_FollowsNewItem()
    {
        GameViewModel game = new GameViewModel();
        ShopItemViewModel firstItem = new ShopItemViewModel { Price = 10, Game = game };
        ShopItemViewModel secondItem = new ShopItemViewModel { Price = 100, Game = game };
        StackPanel panel = new StackPanel();
        Label label = new Label();
        panel.AddChild(label);
        label.SetBinding(nameof(Label.Text), nameof(ShopItemViewModel.Status));
        panel.BindingContext = firstItem;

        game.Gold = 50;
        label.Text.ShouldBe("Buy");

        panel.BindingContext = secondItem;
        label.Text.ShouldBe("Too expensive");
        game.Gold = 100;
        label.Text.ShouldBe("Buy");
    }

    [Fact]
    public void ListBoxTemplateRows_UpdateWhenSharedViewModelChanges()
    {
        GameViewModel game = new GameViewModel();
        ObservableCollection<ShopItemViewModel> items = new ObservableCollection<ShopItemViewModel>
        {
            new ShopItemViewModel { Price = 5, Game = game },
            new ShopItemViewModel { Price = 50, Game = game },
        };
        ListBox listBox = new ListBox();
        listBox.AddToRoot();
        listBox.FrameworkElementTemplate = new Gum.Forms.FrameworkElementTemplate(() =>
        {
            ListBoxItem row = new ListBoxItem();
            row.SetBinding(nameof(ListBoxItem.IsEnabled), nameof(ShopItemViewModel.CanAfford));
            return row;
        });
        listBox.Items = items;
        listBox.ListBoxItems.Select(row => row.IsEnabled).ShouldBe(new[] { false, false });

        game.Gold = 10;
        listBox.ListBoxItems.Select(row => row.IsEnabled).ShouldBe(new[] { true, false });

        game.Gold = 60;
        listBox.ListBoxItems.Select(row => row.IsEnabled).ShouldBe(new[] { true, true });
    }

    [Fact]
    public void ListBoxTemplateRows_RemovedItem_StopsUpdatingAndAddedItemUpdates()
    {
        GameViewModel game = new GameViewModel();
        ObservableCollection<ShopItemViewModel> items = new ObservableCollection<ShopItemViewModel>
        {
            new ShopItemViewModel { Price = 5, Game = game },
        };
        ListBox listBox = new ListBox();
        listBox.AddToRoot();
        listBox.FrameworkElementTemplate = new Gum.Forms.FrameworkElementTemplate(() =>
        {
            ListBoxItem row = new ListBoxItem();
            row.SetBinding(nameof(ListBoxItem.IsEnabled), nameof(ShopItemViewModel.CanAfford));
            return row;
        });
        listBox.Items = items;

        items.RemoveAt(0);
        items.Add(new ShopItemViewModel { Price = 20, Game = game });
        game.Gold = 25;

        listBox.ListBoxItems.Count.ShouldBe(1);
        listBox.ListBoxItems[0].IsEnabled.ShouldBeTrue();
    }
}
