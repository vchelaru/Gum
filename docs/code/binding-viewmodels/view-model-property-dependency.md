# View Model Property Dependency

## Introduction

Gum's `ViewModel` supports using the `DependsOn` attribute to define dependencies between properties. By using this attribute, changes in one property can result in changes to other properties. The same property can be used as a dependency for multiple properties, allowing changes to one property resulting in many pieces of UI being updated.

## Code Example: Using DependsOn

The following code shows how a single variable can be used to update multiple UI properties. First we can declare a ViewModel using `DependsOn` to make `IsBrokeTextVisible` and `MoneyDisplay` depend on `Money`.

```csharp
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
```

We can use the `PlayerViewModel` to update the UI in response to `Money` changing. The following block of code shows how to do so in a code-only project.

```csharp
var viewModel = new PlayerViewModel();

var mainPanel = new StackPanel();
mainPanel.AddToRoot();
mainPanel.BindingContext = viewModel;

var addMoneyButton = new Button();
mainPanel.AddChild(addMoneyButton);
addMoneyButton.Text = "+";
addMoneyButton.Click += (_, _) =>
    viewModel.Money += 100;

var subtractMoneyButton = new Button();
mainPanel.AddChild(subtractMoneyButton);
subtractMoneyButton.Text = "-";
subtractMoneyButton.Click += (_, _) =>
    viewModel.Money -= 100;

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
```

If this code were in a Screen defined in Gum with code generation, the code might look like the following block:

```csharp
void CustomInitialize()
{
    var viewModel = new PlayerViewModel();
    // Can be added to the entire screen:
    this.BindingContext = viewModel;
    
    AddMoneyButton.Click += (_, _) =>
        viewModel.Money += 100;
        
    SubtractMoneyButton.Click += (_, _) =>
        viewModel.Money -= 100;
        
    MoneyLabel.SetBinding(
        nameof(MoneyLabel.Text),
        nameof(viewModel.MoneyDisplay));
        
    IsBrokeLabel.SetBinding(
        nameof(IsBrokeLabel.IsVisible),
        nameof(viewModel.IsBrokeTextVisible));
}
```

<figure><img src="../../.gitbook/assets/13_08 51 15.gif" alt=""><figcaption><p>Two buttons updating money</p></figcaption></figure>
