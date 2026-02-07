# Advanced Binding Options

## Introduction

Gum provides multiple options to control how binding behaves using the `Binding` type.

## Using the Binding Class

The fastest way to set up binding is to use the name of the properties being bound, as shown in the following code example:

```csharp
label.SetBinding(
    // UI property:
    nameof(label.Text),

    // ViewModel property:
    nameof(MyViewModel.LabelText));
```

The SetBinding call shown above is a shortcut for creating a `Binding` object. The following code is functionally identical:

```csharp
// Binding is created using the ViewModel property
Binding binding = new(nameof(MyViewModel.LabelText));
label.SetBinding(
    // UI property:
    nameof(label.Text),
    binding);
```

The Binding instance can be modified by setting its properties on initializers. For example the following code shows how to update the binding to be one-way from the source (ViewModel) to the target (Label):

```csharp
Binding binding = new(nameof(MyViewModel.LabelText))
{
    Mode = BindingMode.OneWay
};

label.SetBinding(
    nameof(label.Text),
    binding);
```

## Available Binding Properties

The following properties are available on the Binding class. Note that the term "source" typically refers to the ViewModel:

* `Path` - the property name on the source, such as `LabelText` &#x20;
* `Mode` - the direction of data flow in binding. This can be used to create one-way binding.
* `UpdateSourceTrigger` - controls when binding is applied
* `FallbackValue` - the value to apply when when binding cannot retrieve a value from the source
* `TargetNullValue` - Gets or sets the value to use when the source property value is null
* `Converter` - Gets or sets a converter to transform values between the source and target
* `ConverterParameter` - Gets or sets an optional parameter to pass to the Converter
* `StringFormat` - Gets or sets a format string to apply to the value
