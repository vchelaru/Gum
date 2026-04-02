# Advanced Binding Options

## Introduction

Gum provides multiple options to control how binding behaves using the `Binding` type.

## Using the Binding Class

The fastest way to set up binding is to use the name of the properties being bound, as shown in the following code example:

```csharp
// Initialize
label.SetBinding(
    // UI property:
    nameof(label.Text),

    // ViewModel property:
    nameof(MyViewModel.LabelText));
```

The SetBinding call shown above is a shortcut for creating a `Binding` object. The following code is functionally identical:

```csharp
// Initialize
// Binding is created using the ViewModel property
Binding binding = new(nameof(MyViewModel.LabelText));
label.SetBinding(
    // UI property:
    nameof(label.Text),
    binding);
```

The Binding instance can be modified by setting its properties on initializers. For example the following code shows how to update the binding to be one-way from the source (ViewModel) to the target (Label):

```csharp
// Initialize
Binding binding = new(nameof(MyViewModel.LabelText))
{
    Mode = BindingMode.OneWay
};

label.SetBinding(
    nameof(label.Text),
    binding);
```

## Lambda Binding

Binding can also be created using lambda expressions, which provide compile-time checking and refactoring support. Lambda expressions are extension methods available on `FrameworkElement`.

A typed lambda specifies the ViewModel type explicitly:

```csharp
// Initialize
textBox.SetBinding<MyViewModel>(
    nameof(TextBox.Text),
    vm => vm.PlayerName);
```

A parameterless lambda captures the ViewModel from a local variable:

```csharp
// Initialize
MyViewModel vm = new();
textBox.BindingContext = vm;
textBox.SetBinding(
    nameof(TextBox.Text),
    () => vm.PlayerName);
```

Both approaches extract the property path from the expression at call time. The resulting binding behaves identically to a string-based binding.

## Nested Property Paths

The `Path` on a `Binding` supports dotted paths to bind through multiple levels of properties. For example, if a ViewModel has a `CurrentPlayer` property which itself has a `Name` property, the binding can reach through both:

```csharp
// Initialize
Binding binding = new("CurrentPlayer.Name");
label.SetBinding(nameof(Label.Text), binding);
```

If any intermediate property in the path changes, the binding automatically re-evaluates. For example, if `CurrentPlayer` is replaced with a different object, the label updates to show the new player's name.

Lambda expressions also support nested paths:

```csharp
// Initialize
label.SetBinding<MyViewModel>(
    nameof(Label.Text),
    vm => vm.CurrentPlayer.Name);
```

## Index-Based Binding

Binding paths support integer indexer access using bracket notation. This is useful for binding to a specific item in a list or array.

```csharp
// Initialize
// Bind to the Text property of the first item in the Items collection
textBox.SetBinding(
    nameof(TextBox.Text),
    new Binding("Items[0].Text"));
```

Indexes can appear at any position in a path:

```csharp
// Initialize
// Bind through a nested property, then into a collection
textBox.SetBinding(
    nameof(TextBox.Text),
    new Binding("CurrentTeam.Members[0].Name"));
```

Lambda expressions also support indexing:

```csharp
// Initialize
textBox.SetBinding<MyViewModel>(
    nameof(TextBox.Text),
    vm => vm.Items[0].Text);
```

If the collection implements `INotifyCollectionChanged` (such as `ObservableCollection`), the binding automatically re-evaluates when items are added, removed, replaced, or cleared. If the bound index is out of range, the binding uses the `FallbackValue` if one is set.

The following fiddle demonstrates index-based binding with a team of players. The TextBox is two-way bound to `Players[0].Name`, and three Labels display each player by index. Typing in the TextBox updates the first player's label, and the Replace button demonstrates collection change notification.

[Try on XnaFiddle.NET](https://xnafiddle.net/#code=H4sIAAAAAAAAA7VW32_TMBB-719h5YVUTGHAC1rZpLXTpkkUEC0DCSHkJNfWzI0r22k3pv7vnB3XcfpDDSDy0Lh33_k-3_kuVypWTMlQFOKGzuGmnPc6pRXhMrkWcq52BMlAFFoKvkdzBRNacn3HVEm5Su5eh5DhctnY_QuTMJHodSMcskwKJSY6-VrQ5NqoVkLeH1EnN5IuZizzdEaPSsMcWXIOmWaiUMmH9CcuhyIH3ut0FmXKWUYyTpUiHzl9BHnHYGXV5Iz4deepQ_BxcKWl2f09urXiSmmeKWhyfkFuQL-tQBdxt-e1qtKOQMdLyktwqnVnvcVkDHTegseHVIFc0pRDfcK3W6e4cMdSh5m22qbdQUJ2jUPE3S3_jhU5JwWs2p3E29a7mMfYb6fuySYHN48ukQpEZH3yBzZ9kf6hxWBGJWfGjzdaH0quqa6XmFTzdvnc3NsrWCLbIS3oFCSZ-ttsMeV8hDFCvVl-vrXZ87JNvfUaKbCu4i4S3ezl4r3XYaxnTHV75FYNRakAS5dhQtBCyxJ6PrtSaEwQ5EQsQUqWA1kKlpPbgmlGOfsFO7m2fJMAYBydkGaLuMPbgFnHTtF1hzBPsxKWfmWPEYfIkabZ_UdaoHJhfz1kg7Di5DLPx-KTEHqPqshmQsaNjuSEyQAKDXLHZLSgmekF5-RNwOXFC_KOpsAVSUVZ5EQLohaQsQnmhBU5PIDCN9Ez8HWQ-Xvvd7FbkAmTSlfLwycazBjP4xoaYGph0kffSNY0bXjACq7juReO5e0s4mYpYFjEJK5AY9ypu1sqwYeAarrxHEfuuN9OvyemdqJuw7JZ2FVWTRc1O1FDOLo29M7I0-k6amDX4VWooqUgE0XeLnIBttHmvLRV7EL8_wzey78M3sjyaxc9LFHZMng1NMDUwlahC-D_M3Kv_jJyY0PvWOCw6i-JIdYXD6bkJRjKtsptVZGFZbEVaMhZy_L2yADiZTYihuqnymvo8SxqdNSKoEGZ9zGvDhcAnAR7ZK5naP_q9HRX2SbpG-yRjDvG_9xqdnKVllqLokoVRirbzRXBro9fAv7oDfuVjTNw_47FsIEOYA15kEEzXyGrZ-_90PEsOmQ1wA_9PXl-TuIfJ-RHF8eCA3OST0BSR8aNA4cHm5pBtJlo7L0PQplSBeHHfWsYPDAufF7kVENsJpQxQ19Tt9g_Ozi0B_Wa3verjzC4knTVwn9jUsJoA5UxDqlmJBCymHCxAtmvZ-Cas91_m6kV7vBcd34DDJXIxX0NAAA)

## Available Binding Properties

The following properties are available on the Binding class. Note that the term "source" typically refers to the ViewModel:

* `Path` - the property path on the source. This can be a simple name like `LabelText`, a dotted path like `Player.Name`, or an indexed path like `Items[0].Text`.
* `Mode` - the direction of data flow in binding. This can be used to create one-way binding.
* `UpdateSourceTrigger` - controls when binding is applied
* `FallbackValue` - the value to apply when binding cannot retrieve a value from the source
* `TargetNullValue` - the value to use when the source property value is null
* `Converter` - a converter to transform values between the source and target
* `ConverterParameter` - an optional parameter to pass to the Converter
* `StringFormat` - a format string to apply to the value

### Converter Example

The `Converter` property accepts an `IValueConverter` implementation that transforms values as they flow between the source and target. The following example converts a boolean to a display string:

```csharp
// Class scope
class BoolToYesNoConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter)
    {
        return value is true ? "Yes" : "No";
    }

    public object? ConvertBack(object? value, Type sourceType, object? parameter)
    {
        return value is string s && s.Equals("Yes",
            System.StringComparison.OrdinalIgnoreCase);
    }
}
```

The converter is assigned through the Binding object:

```csharp
// Initialize
Binding binding = new(nameof(MyViewModel.IsActive))
{
    Converter = new BoolToYesNoConverter()
};
label.SetBinding(nameof(Label.Text), binding);
```

When `IsActive` is `true`, the label displays "Yes". If the binding is two-way and the label text is set to "No", the converter's `ConvertBack` method sets `IsActive` to `false`.

### StringFormat Example

The `StringFormat` property applies a format string when displaying source values. This is useful for formatting numbers, dates, or adding prefix text:

```csharp
// Initialize
Binding binding = new(nameof(MyViewModel.Health))
{
    StringFormat = "HP: {0:N0}"
};
label.SetBinding(nameof(Label.Text), binding);
```

{% hint style="info" %}
When `StringFormat` is set, the binding effectively becomes one-way for display purposes. Setting the target value does not attempt to parse the formatted string back to the source.
{% endhint %}

### FallbackValue Example

The `FallbackValue` is used when the binding path cannot be resolved, such as when an intermediate property is null or a collection index is out of range:

```csharp
// Initialize
Binding binding = new("CurrentPlayer.Name")
{
    FallbackValue = "No player selected"
};
label.SetBinding(nameof(Label.Text), binding);
```
