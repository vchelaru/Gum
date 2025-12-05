# IsFocused

### Introduction

The IsFocused property gets or sets whether a component has focus. For example a TextBox will have its IsFocused set to true if the caret is visible and if it is taking input from the keyboard.

### Code Example - Setting IsFocused

IsFocused can be directly assigned to give focus to an object. For example the following code gives a TextBoxInstance focus:

```csharp
TextBoxInstance.IsFocused = true;
```

If you are setting focus on a TextBox in response to a click event, you may need to also clear the Cursor's input. For example, the following would set focus on TextBoxInstance when ButtonInstance is clicked:

```csharp
ButtonInstance.Click += (_,_) =>
{
    TextBoxInstance.IsFocused = true;
    // Remove the clear so that the TextBoxInstance doesn't immediately lose focus
    // on the same frame it got focus due to the cursor having been clicked
    FormsUtilities.Cursor.ClearInputValues();
};
```
