# StackPanel

## Introduction

StackPanel is a container used to for controls which should stack vertically or horizontally, and which supports wrapping. StackPanels do not include any visual so they are always invisible.

## Code Example: Adding Buttons to a StackPanel

The following code shows how to add Button instances to a StackPanel. Notice that each button is automatically stacked vertically.

```csharp
var stackPanel = new StackPanel();
stackPanel.AddToRoot();
stackPanel.X = 50;
stackPanel.Y = 50;
stackPanel.Width = 200;

var random = new System.Random();
for (int i = 0; i < 10; i++)
{
    var button = new Button();
    stackPanel.AddChild(button);
    button.Text = "Button " + i;
    button.Height = 36;
    button.Click += (_, _) => 
        button.Text = DateTime.Now.ToString();
}
```

## Stack Panel Sizing

By defaut StackPanels contain a Visual with the following properties:

* WidthUnits = Absolute
* HeightUnits = RelativeToChildren

This means that as more children are added to a StackPanel, the StackPanel grows vertically.

The following code creates a main StackPanel with two internal StackPanels. Each internal StackPanel contains a Button which can be used to add labels to the respective internal StackPanel.

```csharp
var mainStackPanel = new StackPanel();
mainStackPanel.AddToRoot();
mainStackPanel.X = 50;
mainStackPanel.Y = 50;
mainStackPanel.Width = 200;

var firstInternalStackPanel = new StackPanel();
mainStackPanel.AddChild(firstInternalStackPanel);
var firstButton = new Button();
firstButton.Text = "Add to first stack panel";
firstButton.Width = 250;
firstInternalStackPanel.AddChild(firstButton);
firstButton.Click += (_, _) =>
{
    var label = new Label();
    label.Text = $"Added at {DateTime.Now}";
    firstInternalStackPanel.AddChild(label);
};

var secondInternalStackPanel = new StackPanel();
mainStackPanel.AddChild(secondInternalStackPanel);
var secondButton = new Button();
secondButton.Text = "Add to second stack panel";
secondButton.Width = 250;
secondInternalStackPanel.AddChild(secondButton);
secondButton.Click += (_, _) =>
{
    var label = new Label();
    label.Text = $"Added at {DateTime.Now}";
    secondInternalStackPanel.AddChild(label);
};
```

<figure><img src="../../.gitbook/assets/13_09 51 57.gif" alt=""><figcaption></figcaption></figure>

## Orientation

StackPanel Orientation controls whether items in a StackPanel are positioned top-to-bottom or left-to-right. Changing the StackPanel Orientation property changes the internal visual's ChildrenLayout property. For more information on ChildrenLayout see the [ChildrenLayout page](../../gum-tool/gum-elements/container/children-layout.md).

WidthUnits and HeightUnits are not changed when changing Orientation. If you want the StackPanel instance to grow horizontally when changing Orientation to Horizontal, you need to modify the WidthUnits and HeightUnits.
