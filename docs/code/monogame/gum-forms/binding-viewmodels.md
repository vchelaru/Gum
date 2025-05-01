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

Once `BindingContext` is assigned, individual UI properties can be bound to properties on the view model. To bind to a property, a reference to a UI control is needed. This reference can be obtained by using properties in generated code or by getting an instance of a FrameworkElement by calling `GetFrameworkElementByName`.



UNDER CONSTRUCTION....
