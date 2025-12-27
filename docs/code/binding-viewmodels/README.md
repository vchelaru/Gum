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
* Binding - associating a property on the view to a property on the view model. For example, a TextBox's Text property can be bound to a view model's PlayerName property. Once binding is established, the view and view model properties remain synced - changes to one results in changes to the other automatically.

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

## Built-in ViewModel Class

Any class which implements `INotifyPropertyChanged` can be used for binding, so developers familiar with binding and MVVM can use any implementation.

For convenience Gum provides a ViewModel class which can be used as a base for view models. Binding documentation uses this class as a base for view models, but most of the concepts apply regardless of view model implementation details.
