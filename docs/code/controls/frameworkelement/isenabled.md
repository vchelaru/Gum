# IsEnabled

### Introduction

IsEnabled controls whether the framework element responds to input from the user. Many controls reflect whether they are enabled visually, so a disabled element (such as a Button) appears differently than an enabled button.

### Code Example - Disabling a Control

Set `IsEnabled` to `false` to prevent a control from responding to user input. The control immediately transitions to its **Disabled** visual state.

```csharp
// Initialize
var button = new Button();
button.AddToRoot();
button.Text = "Purchase";
button.Width = 150;
button.Height = 40;

// Disable the button so it cannot be clicked
button.IsEnabled = false;
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACk3OMQvCMBAF4L3Q_3BkUhCroIvFQVG0m0jBpUuanPawJtBcVRT_u2lF63jf4z3uGQYAInGb-iJmwFWNg1bIEJMs6YGexVVWkNfM1sAcDN5g2R69fpyZjw8XWqd2by3_Y4p39o1M7OpKFdJhJrrwQJoLn46now63SKei6UwazEwUwYqczEsELvD7hLNADEoaYxlyBFWSOqP-rSRubZqO9kNHWTqMRRi8wuAN8rSfrO4AAAA" target="_blank">Try on XnaFiddle.NET</a>

### Cascading to Child Controls

Disabling a parent control also prevents all of its children from responding to input. Gum checks `IsEnabledRecursively` when processing cursor events, so a child whose own `IsEnabled` is `true` still ignores input when any ancestor is disabled. You do not need to disable each child individually.

```csharp
// Initialize
var panel = new StackPanel();
panel.AddToRoot();
panel.Width = 200;
panel.Height = 120;

var button1 = new Button();
button1.Text = "Button 1";
button1.Width = 180;
button1.Height = 40;
panel.AddChild(button1);

var button2 = new Button();
button2.Text = "Button 2";
button2.Width = 180;
button2.Height = 40;
panel.AddChild(button2);

// Disabling the parent panel disables all child controls
panel.IsEnabled = false;
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACo2QTUsDMRCG7wv7H4acKki7GzyIxYNfaG-iBS97STdjNxgnsplVUfzvJum2DajgMc-bmfdhPssCQCz89fAsToD7AQ8TMWTYKGs-MGDxqnp4UYQWToHwDe5ZtU-3EUwO5g2laHqm9dLdOccZezCauzAkq2rHbtCsOw6wlhE2FLevBmZH9bj_PL3SnjGYLvE9zjRik0HdiCzd9tTHVUZ3TUf79mB50RmrJ-On2JEryL8U5A8FmSnIXxXkfxTkRmE2g0vj1coaWgN3GA7eI_F4d50i9KCshTZOQ-uIe2f9du3CX1H8o0Pbo7Ie56IsvsriG-K32EvjAQAA" target="_blank">Try on XnaFiddle.NET</a>

### Visual State Change

When `IsEnabled` is set to `false`, the control transitions to its **Disabled** state category, which is defined in the Gum component's state machine. Most default controls (such as `Button`, `CheckBox`, and `ComboBox`) include a **Disabled** state that visually distinguishes them from their enabled counterparts — typically by reducing opacity or changing color. Setting `IsEnabled` back to `true` returns the control to its normal enabled state and triggers a full state update.
