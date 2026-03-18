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
// Initialize
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
<a href="https://xnafiddle.net/#code=H4sIAAAAAAAACp1WUW_aMBB-z6_wUB8SlUZUeyul0gAVIY2uWimbNE2VSQ5q4djIdsJYxX-f7QSThDDW5SXm7vPdfXcfB6kkbIkmnPERTmCUJl0vtSZ9DO-5SOSRIRxwpgSnDZ4hLHBK1YzIFFMZzj6WIZMsq0T_RgQshM66N05IJLjkCxV-Zzi8N64NF6sz7nAk8PqVRLocb53OKYlQRLGU6JHiLYgZgc2Ex0DRDXJn781D-inghCnTAdhaY-4yzxIU6t2hEahbDbnzg65zydz1BMrPME2hcO08-_oxhDWwWH5hPtNV8oVvwwfBz3LaOecUjWVf8BVM4ZfpGplTMHEtHN32UKf7nohSiWKasB0SudYNMNEuWhdv1nbz0Nm1ut6u1igz-WvdHvMuOrPv6RAyEsEEM7wEgZau0xaTJk8gjN8cn8e2V86210JBoEhoU_kBenOxUA8x2DQn9NUrkUFXN2nCUwmuQUiJFLr7bq8FVxApiBHPQAgSA8o4idGYEUUwJb_BD2qTtfWGJYBJ1EZV-c5ASMKZVrGerruaYYEyp6m8-JrS_Do-wYQ9YubwTwpHK2soa8qhwk9xPOVfOVfN7j5hsZ6z-Rpq2eiYrp5aXhzHdu79VCnOiuT5h5OJB6-Exn71ZglbdYRGtzpu67J1EjLQc1-hyx7yX9roJdAqcUhb5772MBe9Bl53OjUiMp0rgSP1n2warpe_y8dex-uq9XfcO8ldNZFLjOsznjt52PMZSodLZZwzhnozFTLxKxUV26OENFSDdhOoVn2xUYK6ukm-wt7LoHythCyb3RgeuOYmICf4oXUCfYZzBTuWxTY5R_14QVcaMMcSyouk-kNwajU9r2OswDfbcEoSQMvi0LynCrQDHejb7M3uMxUMBd78Q_7KVtZqByz8Aadc6D8Bgi0o34DoH37_DjXb-PVKrfGozp33B8hOO_SFCAAA" target="_blank">Try on XnaFiddle.NET</a>

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
