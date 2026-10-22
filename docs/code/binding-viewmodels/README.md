# Binding and MVVM

## Introduction

Gum supports the MVVM pattern using binding syntax similar to other C# UI frameworks such as WPF, Avalonia, and .NET Maui. Binding is a standard way to keep your UI in sync with your game data. By using binding, you can create associations between your UI and properties on a class called a _view model_.

Binding properties provides a number of benefits including:

* Creating a centralized object to hold data
* Keeping UI in sync with the central data
* Managing dependencies between properties
* Separating UI to keep code easier to refactor and test

This document provides an overview of how binding works in Gum.

## General Concepts

A typical game uses UI to display information to the user and to allow the user to modify this data through a variety of controls. For example, a TextBox may display the user's current name and a provide a way to change this name.

The following terms are used when discussing binding:

* View - any visual object that displays information to the user. This could be simple label, or as complex as a settings screen with dozens of options.
* View Model - a class which stores information that is displayed by the view. This class typically inherits from the Gum ViewModel class, but this is not a requirement. Any class which implements the `INotifyPropertyChanged` interface can be used.
* Binding - associating a property on the view to a property on the view model. For example, a TextBox's `Text` property can be bound to a view model's `PlayerName` property. Once binding is established, the view and view model properties remain synced - changes to one results in changes to the other automatically.

{% hint style="info" %}
Gum uses a similar binding syntax as other C# UI frameworks. Binding can be created using property names, or by creating an instance of a Binding class which provides more control over binding behavior.
{% endhint %}

## How Binding Works

View properties can be bound to view model properties. Once this binding is made, the properties automatically stay in sync.

The concept of binding is very flexible - it can be performed on any type of property on a view. Some examples include:

* Properties of primitive types, such as a `TextBox`'s `Text` property
* List properties such as a `ListBox`'s `Items` property
* Properties on Visuals such as a `TextRuntime`'s `Color` property
* Custom properties, such as a `HealthPercent` property on a health bar

Binding is initially established when two properties are associated, and when a view is given reference to a view model, which is done through the `BindingContext` property. When binding is established, the view immediately updates its properties according to the values on the view model.

After that point, if the view model's property changes (such as by direct assignment in code), the view also immediately updates in response. Similarly, if a property on the view changes (such as by the user clicking on a CheckBox), the ViewModel is notified and updates immediately as well.

## Quick Example

The following example shows binding in action. A `TextBox` and a `Label` are both bound to the same `Name` property on a ViewModel. Typing in the TextBox automatically updates the Label.

First, define a ViewModel with a `Name` property:

```csharp
class ExampleViewModel : ViewModel
{
    public string Name
    {
        get => Get<string>();
        set => Set(value);
    }
}
```

Then create the controls and bind them:

```csharp
// Initialize
var panel = new StackPanel();
panel.AddToRoot();
panel.Anchor(Gum.Wireframe.Anchor.Center);

var textBox = new TextBox();
panel.AddChild(textBox);
textBox.Width = 250;

var mirrorLabel = new Label();
panel.AddChild(mirrorLabel);

var viewModel = new ExampleViewModel();
panel.BindingContext = viewModel;

textBox.SetBinding(
    nameof(TextBox.Text),
    nameof(ExampleViewModel.Name));

mirrorLabel.SetBinding(
    nameof(Label.Text),
    nameof(ExampleViewModel.Name));
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#code=H4sIAAAAAAAACqVUXY_aMBB851es7ilIp6gfqlQd5aQCPYR0VFXhaF9NshDrHDtaO4H2xH-v7YQQQnS0al7i7Ex2Ztde55rLLcyVVFOW4jRPB73ch-wyfFCU6otAOFbSkBIdyAQ3LBdmxXXOhA5X75uUeVGcZf_BCTdkVY_BOY9IabUx4U_JwgcH7RQ9X4HDKbEs4ZG108vyteARRIJpDV_2LM0Erjju5ipGAXdQr3svPbBPxdeGnMBXm9GHS9A9WzQwvIcpmk8l6T7oD2pUl-gCTVAwkWMFHXqHlhXX27dW370r6aPrCRY8wjmTbIsE27oWz8nTBZLD3fJp5q3UsWO3bd2NWrxU0IeXOhcMQeKuWzAwCdf9Acz0XOXa9krztUD7h6EcB7aSMjUpg5HBGFSBRDxGKBSPYSa54Uzw3xj0W43zfsMGwQndwvkBWSFprqQ9J7Zz9a8FI8iYtBtWGl8YFj1_c4Fm7z0j_BzHS_VdKdMByShRFJSvcIzSIF2QFhmL3NYP4WPLgWDr2sGjW3eKjxMu4sBzG7D_Dpe4t8cDbpa_MgQuwSQIa7UHi6nd3U1Lz1j2yKKl4rL8ekWz4jcIVcTOVWwSm-fdhzctjZQTKXr8h8oaf7S3qKjnqkzVHrfLrCMuY9tsd32UralTNFIfq7BDVfGDGnOPtOdbbYKqQb7J_dsuRttP6Ma736yiUdw1uZL0P2JrprE5D_Vd8eqEPWUxMxi4oV7yFGFbLbrHrWLXpNMGePVu-IqDCbHdX-ifXS7hWCCjYKyEmzxFcmNPPNLodEWePPv8bac-eOHz0PsDETdjda4GAAA)

Both controls stay in sync because they are bound to the same property. Typing in the TextBox updates the ViewModel, which notifies the Label to update. This two-way sync happens automatically.

## Built-in ViewModel Class

Any class which implements `INotifyPropertyChanged` can be used for binding, so developers familiar with binding and MVVM can use any implementation.

For convenience Gum provides a `ViewModel` class which can be used as a base for view models. Binding documentation uses this class as a base for view models, but most of the concepts apply regardless of view model implementation details.

If you store a struct in a `ViewModel` property, implement `IEquatable<T>` on it. Without it, every set compares the old and new values slowly and allocates, even when the value is unchanged.

### Get and Set

Properties in a ViewModel-implementing class typically use Get and Set methods. These methods can be used with no backing field, or can use explicit backing fields. Adding a backing field requires more code, but improves the performance of reading and writing properties.

The following code blocks show how to implement the same property with and without backing fields.

```csharp
private string _name = String.Empty;

public string Name
{
    get => _name;
    set => Set(ref _name, value);
}
```

```csharp
public string Name
{
    get => Get<string>();
    set => Set(value);
}
```

Notice that the second block of code is shorter; however, it does come at a performance cost. The following table shows speed improvements from using backing fields. A higher number is better. Of course, this can vary depending on your hardware and target platform.

Higher is faster. Same operation, same machine, by-name time divided by field time. Every case allocates 0 bytes.

| Operation                                                    | Desktop (.NET 10) int | Desktop string | Browser (WebAssembly, Release) int | Browser string |
| ------------------------------------------------------------ | --------------------- | -------------- | ---------------------------------- | -------------- |
| Read the property                                            | 54x                   | 6.7x           | 125x                               | 35x            |
| Set to the same value (nothing changes, no event)            | 8.3x                  | 2.3x           | 7.5x                               | 3.9x           |
| Set to a new value, nobody listening                         | 3.8x                  | 3.2x           | 4.6x                               | 5.0x           |
| Set to a new value, one bound control listening              | 1.7x                  | 2.0x           | 2.1x                               | 2.7x           |
| Set to a new value, one `[DependsOn]` property also notified | 1.5x                  | 1.9x           | 1.4x                               | 1.8x           |

Note that you are free to mix these two approaches in your view model - you can use the simpler syntax for infrequently-accessed properties, and switch to backing fields on properties which are accessed frequently.

### Backing Fields Details

You may choose to use backing fields for performance reasons. Using this approach, the value is kept in a field and pass the field to `Set`. Reading is a plain field read, and a set that changes the value skips the property-name dictionary that `Get` and `Set(value)` use.

```csharp
class PlayerViewModel : ViewModel
{
    private float _health = 100;

    public float Health
    {
        get => _health;
        set => Set(ref _health, value);
    }
}
```

`Set(ref field, value)` returns `true` when the value changed. `SetWithoutNotifying(ref field, value)` stores the value without raising `PropertyChanged`. `[DependsOn]`, collection change tracking (`Set` watches a collection for changes, `SetWithoutNotifying` does not), and `Clone` work the same as with `Get` and `Set(value)`.

The styles differ on the first set. `Set(value)` raises `PropertyChanged` the first time a property is set to anything other than its type's default. With a backing field, the field's current value is the starting point, so assigning the value the field already holds raises nothing. In the example above, `Health = 100` raises nothing, while `Health = 50` does.

{% hint style="info" %}
`ChangeAndNotify(ref field, value)` is the older form of `Set(ref field, value)`. It does not watch the contents of a collection, so prefer `Set` for collection properties.
{% endhint %}
