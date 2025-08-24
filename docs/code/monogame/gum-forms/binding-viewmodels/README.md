# Binding (ViewModels)

## Introduction

Gum Forms supports binding of `FrameworkElement` properties to properties on another object such as a view model. If the view model implements `INotifyPropertyChanged` then the `FrameworkElement` subscribes to property changes and automatically updates itself in response to changes.

Binding can be performed on the following types of properties:

* Properties of primitive types, such as a `TextBox`'s `Text` property
* List properties such as a `ListBox`'s `Items` property
* Properties on Visuals such as a `TextRuntime`'s `Color` property

## Built-in ViewModel Class

Any class which implements `INotifyPropertyChanged` can be used for binding, so if you have a preferred implementation that you'd like to use, you can use it with Gum. If you have no preference, or if you are new to binding, you can use Gum's built-in `ViewModel` class. For simplicity the rest of this document uses the built-in ViewModel class.

## Binding Concepts

The approach that Gum uses for binding is common in other C# front end frameworks such as WPF, Maui, and Avalonia. This pattern is addresses a few common considerations when developing UI.

* Creating a centralized object to hold data
* Keeping UI in sync with the central data
* Managing dependencies between properties
* Separating UI to keep code easier to refactor and test

Typically the class that contains the data is called a _view model_. View models can also contain logic to respond to actions such as adding health to a player, or subtracting money when an item is purchased. The term _view model_ appears in the pattern Model-View-ViewModel (MVVM), although this document focuses primarily on the binding capabilities in Gum and not the entire MVVM pattern.

## ViewModel-Inheritance

The view model class is responsible for storing information that is displayed by the UI, and similarly which can be set by the UI through user interaction. The view model often is specific to a particular page or component. For example, if your game includes an OptionsScreen, then you might also have an OptionsScreenViewModel.

This class inherits from the ViewModel class and contains properties which can be viewed or edited by the OptionsScreen or its contained UI.

Binding to FrameworkElement properties is usually two-way. This means that changes to the UI also result in changes to the view model's property. Similarly changes to the view model in code update the UI. For example, a view model may contain a property named `PlayerName` which is bound to a `TextBox`'s `Text` property. If the user changes the text in the `TextBox`, then the value is also changed on the view model. Similarly, if the code changes the view model's `PlayerName` property, this change is pushed to the `TextBox`.

FrameworkElements like TextBox automatically push their changes to their bound view model, so the only code needed to support UI->view model changes is the initial binding.

The view model must broadcast its changes to push changes to the `FrameworkElement`. The easiest way to do this is to inherit from the Gum ViewModel class and use the Get and Set methods in the property getters and setters.

The following class is an example of an OptionsScreenViewModel which has values for common options in a game.

```csharp
public class OptionsScreenViewModel : ViewModel
{
    public bool IsFullscreen
    {
        get => Get<bool>();
        set => Set(value);
    }
    
    public float MusicVolume
    {
        get => Get<float>();
        set => Set(value);
    }
    
    public float SfxVolume
    {
        get => Get<float>();
        set => Set(value);
    }
    
}
```

## BindingContext

The `BindingContext` property exists on `FrameworkElement` types as well as all visuals which inherit from `BindableGue` such as TextRuntime and ContainerRuntime.

This property is used to assign the view model that a control should use. The assignment of `BindingContext` cascades from parent to all children recursively, so typically the BindingContext is only assigned at the top level, such as on a screen or `StackPanel`.&#x20;

Once `BindingContext` is assigned, individual UI properties can be bound to properties on the view model. To bind to a property, a reference to a UI control is needed. This reference can be obtained by using properties in generated code, by casting the Visual to its specific visual class (such as `ButtonVisual`), or by getting an instance of a FrameworkElement by calling `GetFrameworkElementByName`.

The following code shows how to assign an instance of an `OptionsScreenViewModel` to a code generated OptionsScreen:

```csharp
var optionsScreen = new OptionsScreen();
optionsScreen.AddToRoot();

var viewModel = new OptionsScreenViewModel();
optionsScreen.BindingContext = viewModel;
// All items within OptionsScreenViewModel inherit the binding context
```

BindingContext can also be assigned on Forms controls created in code, as shown in the following block:

```csharp
var stackPanel = new StackPanel();
stackPanel.AddToRoot();

var viewModel = new OptionsScreenViewModel();
stackPanel.BindingContext = viewModel;
// all items added to stackPanel inherit the binding context. 
// For example, the following button would inherit binding context:
var button = new Button();
stackPanel.AddChild(button);
```

BindingContext can be assigned on FrameworkElements obtained from non-code-generated screens as well. For example, the following code shows how to obtain a StackPanel that was added to a Gum screen and assign its BindingContext.

```csharp
var stackPanel = myScreen.GetFrameworkElementByName<StackPanel>("StackPanelInstance");
stackPanel.BindingContext = viewModel;
// all children of stackPanel inherit the binding context
```

## SetBinding Method

Once a FrameworkElement is assigned a BindingContext, either directly or indirectly through its parent, it can bind any of its properties to a property on the view model. Usually binding is performed using the `nameof` keyword for compile time safety.

For example, the following code shows how to bind a `Button` instance's `Text` property to a view model's `ButtonText` property.&#x20;

```csharp
buttonInstance.SetBinding(
    nameof(buttonInstance.Text), 
    nameof(viewModel.ButtonText));
```

Binding can be performed on properties which might change during normal interaction, such as a `ListBox` instance's `SelectedObject`. In this case if the `SelectedObject` changes, the view model's property is automatically updated.

```csharp
listBoxInstance.SetBinding(
    nameof(listBoxInstance.SelectedObject),
    nameof(viewModel.SelectedObject));
```

## DependsOn

Gum's `ViewModel` supports using the `DependsOn` attribute to define dependencies between properties. By using this attribute, changes in one property can result in changes to other properties. The same property can be used as a dependency for multiple properties, allowing changes to one property resulting in many pieces of UI being updated.

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

<figure><img src="../../../../.gitbook/assets/13_08 51 15.gif" alt=""><figcaption><p>Two buttons updating money</p></figcaption></figure>

## Binding Visual Properties

Visual properties can be directly bound. These can be directly bound to properties on a ViewModel, or they can be bound through custom properties on the Forms control. This section discusses the multiple ways that binding can be performed on visuals.

{% hint style="warning" %}
Typical ViewModels do not include view-specific properties such as colors, fonts, or margins. This allows ViewModels to be reusable across multiple views and even across different platforms (such as Gum Forms vs WPF). However, there are times when binding view properties is preferred. This section discusses how to do so, but it is up to the developer to decide when such binding should be performed.
{% endhint %}

### Binding Visual Properties Directly

Visual properties can be bound directly. If the property is not specific to a type of Visual, then the Visual can be directly bound without being casted. For example, the following code shows how to bind the width of a Button to a ButtonWidth property on a ViewModel.

```csharp
// assume MyButton is a valid button
var buttonVisual = MyButton.Visual;
buttonVisual.SetBinding(
    nameof(buttonVisual.Width),
    nameof(ViewModel.ButtonWidth));
```

If the property requires a specific type, then the Visual can be casted to access type-specific properties, as shown in the following code block:

```csharp
// This assumes the project is code-only
var buttonVisual = (ButtonVisual)MyButton.Visual;
var text = buttonVisual.TextInstance;
text.SetBinding(
    nameof(Text.FontScale),
    nameof(ViewModel.ButtonFontScale));
```

### Binding Converter Properties (Code Generation)

If your project uses the Gum tool and code generation, then each component and screen has a _custom code_ file which can contain converter properties that can be bound. This allows ViewModels to avoid adding view-specific properties.

For example, consider a screen named `GameScreenHud` which contains a single label named `HealthLabel`.&#x20;

<figure><img src="../../../../.gitbook/assets/23_10 58 42.png" alt=""><figcaption></figcaption></figure>

The ViewModel may contain general properties such as `CurrentHealth` and `IsLowHealth`, but the view will process these to display the health with a prefix, and to modify the Label property.

The following code could be added to `GameScreenHud.cs`, which is the custom code file created when the `GameScreenHud` is generated:

```csharp
partial class GameScreenHud
{
    public int CurrentHealth
    {
        set => this.HealthLabel.Text = 
                $"Current Health: {value}";
    }

    public bool IsLowHealth
    {
        set
        {
            var textInstance = HealthLabel.GetVisual<TextRuntime>();
            textInstance.Color = value 
                ? Microsoft.Xna.Framework.Color.Red 
                : Microsoft.Xna.Framework.Color.White;
        }
    }

    GameScreenViewModel ViewModel => (GameScreenViewModel)BindingContext;

    partial void CustomInitialize()
    {
        this.SetBinding(
            nameof(this.CurrentHealth),
            nameof(ViewModel.CurrentHealth));

        this.SetBinding(
            nameof(this.IsLowHealth),
            nameof(ViewModel.IsLowHealth));


    }
}

```
