# Common Control Events

## Introduction

Controls provide events for actions. This page introduces and provides code samples for the most common events. Many controls provide additional events for more advanced scenarios but this page covers the more common events.

## Button

Click is raised when a button is clicked with the cursor, touch screen, keyboard, or gamepad. A click is defined as the input device was down last frame, but is not down this frame:

```csharp
button.Click += (_,_) =>
    button.Text = "Clicked at " + DateTime.Now.ToString();
```

Push is raised when a button is pushed with the cursor, touch screen, keyboard, or gamepad. A push is defined as the input device was not down last frame, but is down this frame:

```csharp
button.Push += (_,_) =>
    button.Text = "Pushed at " + DateTime.Now.ToString();
```

## CheckBox

Checked and Unchecked are raised when the CheckBox is checked or unchecked, either programmatically or by the user.

```csharp
checkbox.Checked += (_,_) => 
    checkbox.Text = $"Checked at {DateTime.Now}";
checkbox.Unchecked += (_,_) => 
    checkbox.Text = $"Unchecked at {DateTime.Now}";
```

Click is raised when a checkbox is clicked with the cursor, touch screen, keyboard, or gamepad. This requires a click from an input device, so changing the IsChecked property does not raise the Click event.

```csharp
checkbox.Click += (_, _) =>
    checkbox.Text = $"Clicked at {DateTime.Now}";
```

## ComboBox

SelectionChanged is raised whenever the selection is changed through a click or selection using keyboard/gamepad.

```csharp
comboBox.SelectionChanged += (_, _) =>
    System.Diagnostics.Debug.WriteLine($"Selection is: {comboBox.SelectedObject}");
```

## ListBox

SelectionChanged is raised whenever the selection is changed through a click or selection using keyboard/gamepad.

```csharp
listBox.SelectionChanged += (_, _) =>
    System.Diagnostics.Debug.WriteLine($"Selection is: {listBox.SelectedObject}");
```

## MenuItem

Click is raised whenever the MenuItem is clicked with the cursor, touch screen, keyboard, or gamepad.

```csharp
menuItem.Click += (_, _) =>
    menuItem.Text = $"Clicked at {DateTime.Now}";
```

## Slider

ValueChanged is raised whenever the value is changed either through input hardware or programmatically.

```csharp
slider.ValueChanged += (_,_) =>
    System.Diagnostics.Debug.WriteLine($"Value is now {slider.Value}");
```

ValueChangedByUi is only raised when the value changes from input hardware. Changing the Value property programmatically does not raise this event.

```csharp
slider.ValueChangedByUi += (_,_) =>
    System.Diagnostics.Debug.WriteLine($"Value is now {slider.Value}");
```

## TextBox

TextChanged is raised whenever the Text property changes, either through input hardware (such as typing in a TextBox) or programmatically.

```csharp
textBox.TextChanged += (_,_) =>
    System.Diagnostics.Debug.WriteLine($"Text is now {textBox.Text}");
```
