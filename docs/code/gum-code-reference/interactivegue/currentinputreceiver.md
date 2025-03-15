# CurrentInputReceiver

### Introduction

The CurrentInputReceiver property gets and sets the current object which is receiving input from the keyboard. Only one element can be the CurrentInputReceiver at one time, but this property may be null if no elements are receiving input.

This property can be set in a number of ways:

1. Forms objects such as TextBox set themselves as the CurrentInputReceiver when clicked
2. Setting IsFocused = true on some Forms objects such as TextBoxes sets this
3. Explicitly setting CurrentInputReceiver sets this value, but keep in mind that doing so may not result in objects updating their states appropriately, so this typically should be assigned internally

