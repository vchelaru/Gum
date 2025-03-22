# RaiseChildrenEventsOutsideOfBounds

## Introduction

RaiseChildrenEventsOutsideOfBounds determines whether InteractiveGue instances check all children for events even if the children fall outside of the bounds of the instance. This is false by default for performance reasons, but is needed if a parent has children which are intentionally placed outside of its bounds.

## Code Example - Setting RaiseChildrenEventsOutsideOfBounds

By default RaiseChildrenEventsOutsideOfBounds is set to false. This means that all parents only raise events for their children if the cursor is within the bounds of the parent. For example, the following code adds a wide Button to a narrow StackLayout. The Button only receives clicks if the Cursor is within the bounds of the StackLayout.

```csharp
var mainPanel = new StackPanel();
var visual = mainPanel.Visual;
mainPanel.Visual.AddToRoot();

var label = new Label();
mainPanel.AddChild(label);

mainPanel.Visual.Width = 100;
mainPanel.Visual.Height = 100;
mainPanel.Visual.WidthUnits = DimensionUnitType.Absolute;
mainPanel.Visual.HeightUnits = DimensionUnitType.Absolute;
// If this is false, then no events are raised when the cursor is 
// outside of the bounds of mainPanel
mainPanel.Visual.RaiseChildrenEventsOutsideOfBounds = false;

var button = new Button();
button.Visual.Width = 300;
button.Click += (_, _) => label.Text = $"Clicked at {DateTime.Now}";
mainPanel.AddChild(button);
```

<figure><img src="../../../.gitbook/assets/22_07 33 21.gif" alt=""><figcaption><p>Clicks only register on the left side of the button</p></figcaption></figure>

Notice that all events are disabled when outside of the bounds of mainPanel including hover, so the button does not respond to events.

The code can be modified to enable clicks outside of the mainPanel:

```diff
var mainPanel = new StackPanel();
var visual = mainPanel.Visual;
mainPanel.Visual.AddToRoot();

var label = new Label();
mainPanel.AddChild(label);

mainPanel.Visual.Width = 100;
mainPanel.Visual.Height = 100;
mainPanel.Visual.WidthUnits = DimensionUnitType.Absolute;
mainPanel.Visual.HeightUnits = DimensionUnitType.Absolute;
// If this is false, then no events are raised when the cursor is 
// outside of the bounds of mainPanel
-mainPanel.Visual.RaiseChildrenEventsOutsideOfBounds = false;
+mainPanel.Visual.RaiseChildrenEventsOutsideOfBounds = true;

var button = new Button();
button.Visual.Width = 300;
button.Click += (_, _) => label.Text = $"Clicked at {DateTime.Now}";
mainPanel.AddChild(button);
```

<figure><img src="../../../.gitbook/assets/22_07 35 42.gif" alt=""><figcaption><p>Clicks are now registered outside of the bounds of the mainPanel</p></figcaption></figure>

