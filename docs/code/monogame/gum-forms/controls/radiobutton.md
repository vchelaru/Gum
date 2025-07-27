# RadioButton

## Introduction

RadioButtons provide a way to display multiple mutually-exclusive options to the user. Clicking on one RadioButton unchecks all other RadioButtons in a group.

## Code Example: Creating RadioButtons

The following code creates RadioButtons for selecting difficulty:

```csharp
var stackPanel = new StackPanel();
stackPanel.AddToRoot();

var easyRadioButton = new RadioButton();
stackPanel.AddChild(easyRadioButton);
easyRadioButton.Text = "Easy";

var mediumRadioButton = new RadioButton();
stackPanel.AddChild(mediumRadioButton);
mediumRadioButton.Text = "Medium";

var hardRadioButton = new RadioButton();
stackPanel.AddChild(hardRadioButton);
hardRadioButton.Text = "Hard";
```

<figure><img src="../../../../.gitbook/assets/13_09 42 01.gif" alt=""><figcaption></figcaption></figure>

## Grouping by Container

RadioButtons automatically group themselves based on their container. If RadioButtons are added directly to a Screen or Component, then they all use the same group and will all be mutually exclusive.

Alternatively, RadioButton instances can be grouped into separate containers (such as StackPanels) to control their grouping. the following example shows two StackPanels, each with three RadioButton instances.

<figure><img src="../../../../.gitbook/assets/31_06 17 29.png" alt=""><figcaption><p>StackPanels grouping RadioButtons</p></figcaption></figure>

At runtime, the RadioButtons in each StackPanel are mutually exclusive.

<figure><img src="../../../../.gitbook/assets/31_06 18 57.gif" alt=""><figcaption><p>Each RadioButton is mutually exclusive with the other RadioButtons in the same column</p></figcaption></figure>
