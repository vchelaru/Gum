# For WPF Users

## Introduction

Much of Gum's syntax is modeled after WPF syntax. Developers familiar with WPF will find Gum controls similar. This document provides an overview of the similarities and differences to WPF.

## Control Naming

Gum controls share names with WPF. The following lists the main controls in Gum:

* Button
* CheckBox
* ComboBox
* ItemsControl
* Label
* ListBox
* ListBoxItem
* MenuItem
* PasswordBox
* RadioButton
* ScrollBar
* ScrollViewer
* StackPanel
* Slider
* Splitter
* TextBox
* Window

## No Direct Visual Properties

Gum separates its visuals from the main controls, so many of the visual properties that exist on WPF are not present in Gum. This separation exists for the practical reason that Gum control visuals can be made of quite literally anything, so Gum cannot make assumptions through these properties.

For example, the following code is valid in WPF:

```csharp
// Assuming MyButton is a valid button:
MyButton.Background = Brushes.LightBlue;
```

This code is not valid in Gum because there is no guarantee that the button has a background, or if it does there is no guarantee on the background's type. It could be a NineSliceRuntime, SpriteRuntime, or even a 3D model or Tiled map.

To style a button, your code must access the visual object, which requires making some assumptions about how the Button is built.

If you are using a code-only setup, then you can cast the Visual property to the appropriate type to access its values. For example, the following code can be used to access and modify a Button's background:

```csharp
var buttonVisual = (ButtonVisual)MyButton.Visual;
buttonVisual.Background.Color = Color.Red;
```

## Limited Layout Properties

Gum provides limited properties for its controls. Gum code allows changing position, size, dock, and anchor values. For example, the following would result in a button filling its parent and setting a margin of 4 pixels:

```csharp
MyButton.Dock(Dock.Fill);
// Mulitply by 2 since we need 4 pixel margin on left and right:
MyButton.Width = -4 * 2; 
// Also multiply by 2 since we need 4 pixel margin on top and bottom:
MyButton.Height = -4 * 2;
```

More advanced layout control must be performed through the `Visual` property:

```csharp
MyButton.Visual.XOrigin = HorizontalAlignment.Center;
```

## Binding

Binding in Gum works similarly to WPF, but the syntax is slightly different. Gum relies on binding to classes which implement INotifyPropertyChanged and INotifyCollectionChanged. If a class implements these properties, then binding to their properties automatically results in Gum controls updating whenever the bound property changes.

Gum uses the property `BindingContext` rather than `DataContext`, similar to .NET MAUI. Also, binding is performed using the name of a property rather than a static property with the `Property` suffix used in WPF.

The following code blocks compare the two approaches:

```csharp
// Gum:
MyButton.BindingContext = MyViewModel;
MyButton.SetBinding(nameof(Button.Text), nameof(MyViewModel.ButtonText));
```

```csharp
// WPF:
MyButton.DataContext = MyViewModel;
MyButton.SetBinding(Button.TextProperty, nameof(MyViewModel.ButtonText));
```

Since Gum does not use the `DependencyProperty` type (or `BindableProperty` if you are familiar with .NET MAUI), the creation of bindable properties requires much less code as shown in the following blocks:

```csharp
// Gum
string _text;
public string Text
{
    get => _text;
    set
    {
        _text = value;
        PushValueToViewModel();    
    }
}
```

```csharp
// WPF
public static readonly DependencyProperty TextProperty =
    DependencyProperty.Register(
        "Text",                           // Property name
        typeof(string),                   // Property type
        typeof(MyCustomControl),          // Owner type
        new PropertyMetadata(string.Empty)); // Default value and metadata

// CLR wrapper property
public string Text
{
    get { return (string)GetValue(TextProperty); }
    set { SetValue(TextProperty, value); }
}
```
