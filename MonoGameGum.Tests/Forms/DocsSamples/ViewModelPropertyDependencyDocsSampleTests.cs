using Gum.Forms.Controls;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms.DocsSamples;

/// <summary>
/// Runs the samples on docs/code/binding-viewmodels/view-model-property-dependency.md.
/// </summary>
public class ViewModelPropertyDependencyDocsSampleTests : BaseTestClass
{
    class PlayerViewModel : ViewModel
    {
        public int Money
        {
            get => Get<int>();
            set => Set(value);
        }

        [DependsOn(nameof(Money))]
        public bool IsBrokeTextVisible => Money <= 0;

        [DependsOn(nameof(Money))]
        public string MoneyDisplay => $"${Money:N0}";
    }

    class GameViewModel : ViewModel
    {
        public int Gold
        {
            get => Get<int>();
            set => Set(value);
        }
    }

    class ShopItemViewModel : ViewModel
    {
        public int Price
        {
            get => Get<int>();
            set => Set(value);
        }

        public GameViewModel Game
        {
            get => Get<GameViewModel>();
            set => Set(value);
        }

        [DependsOn(nameof(Price))]
        [DependsOn("Game.Gold")]
        public bool CanAfford => Game != null && Game.Gold >= Price;
    }

    [Fact]
    public void DependsOn_MoneyButtons_UpdateBothLabels()
    {
        var viewModel = new PlayerViewModel();

        var mainPanel = new StackPanel();
        mainPanel.AddToRoot();
        mainPanel.BindingContext = viewModel;

        var addMoneyButton = new Button();
        mainPanel.AddChild(addMoneyButton);
        addMoneyButton.Text = "+";
        addMoneyButton.Click += (_, _) =>
            viewModel.Money += 100;

        var moneyLabel = new Label();
        mainPanel.AddChild(moneyLabel);
        moneyLabel.SetBinding(
            nameof(moneyLabel.Text),
            nameof(viewModel.MoneyDisplay));

        var isBrokeLabel = new Label();
        mainPanel.AddChild(isBrokeLabel);
        isBrokeLabel.Text = "No more money!";
        isBrokeLabel.SetBinding(
            nameof(isBrokeLabel.IsVisible),
            nameof(viewModel.IsBrokeTextVisible));

        isBrokeLabel.IsVisible.ShouldBeTrue();

        addMoneyButton.PerformClick();

        moneyLabel.Text.ShouldBe($"${100:N0}");
        isBrokeLabel.IsVisible.ShouldBeFalse();
    }

    [Fact]
    public void DottedDependsOn_GoldChange_TogglesButton()
    {
        GameViewModel game = new() { Gold = 50 };
        ShopItemViewModel shopItem = new() { Price = 100, Game = game };
        Button buyButton = new();

        buyButton.BindingContext = shopItem;
        buyButton.SetBinding(nameof(buyButton.IsEnabled), nameof(ShopItemViewModel.CanAfford));
        buyButton.IsEnabled.ShouldBeFalse();

        game.Gold = 150;
        buyButton.IsEnabled.ShouldBeTrue();

        shopItem.Game = new GameViewModel { Gold = 0 };
        buyButton.IsEnabled.ShouldBeFalse();
    }
}
